using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// The sneak is a slow move that keeps the character facing forward.
    /// 
    /// This motion will force the camera into the third-person-fixed mode.
    /// </summary>
    [MotionTooltip("A forward motion that looks like the avatar is sneaking. The motion is slower than a walk and has the " +
                   "avatar always facing forward.")]
    public class Sneak : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 600;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Sneak()
            : base()
        {
            _Priority = 1;
            mIsStartable = true;
            mIsGroundedExpected = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Sneak(MotionController rController)
            : base(rController)
        {
            _Priority = 1;
            mIsStartable = true;
            mIsGroundedExpected = true;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            mController.AddAnimatorName("AnyState");

            mController.AddAnimatorName("AnyState -> Sneak-SM.SneakIdle");
            mController.AddAnimatorName("AnyState -> Sneak-SM.SneakForward");

            mController.AddAnimatorName("Sneak-SM.SneakIdle");
            mController.AddAnimatorName("Sneak-SM.SneakForward");
            mController.AddAnimatorName("Sneak-SM.SneakLeft");
            mController.AddAnimatorName("Sneak-SM.SneakRight");
            mController.AddAnimatorName("Sneak-SM.SneakBackward");
            mController.AddAnimatorName("Sneak-SM.SneakBlend -> Idle-SM.Idle_Casual");
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }
            if (!mController.IsGrounded) { return false; }

            // Only move in if the stance is set or it's time to move in
            if (mController.State.Stance == EnumControllerStance.SNEAK ||
                mController.State.Stance == EnumControllerStance.TRAVERSAL && InputManager.IsJustPressed("ChangeStance"))
            {
                return true;
            }
            
            // If we get here, we should not be in the stance
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }

            if (!IsInSneakState) { return false; }
            if (!mController.IsGrounded) { return false; }
            if (mController.State.Stance != EnumControllerStance.SNEAK) { return false; }
            if (InputManager.IsJustPressed("ChangeStance")) { return false; }

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Force the character's stance to change
            mController.Stance = EnumControllerStance.SNEAK;

            // Trigger the change in the animator
            mController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, Sneak.PHASE_START, true);

            // Allow the base to finish
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // If we're still flagged as in the sneak stance, move out
            if (mController.Stance == EnumControllerStance.SNEAK)
            {
                mController.Stance = EnumControllerStance.TRAVERSAL;
            }

            // Deactivate
            base.Deactivate();
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            if (!TestUpdate())
            {
                Deactivate();
                return;
            }

            DetermineAngularVelocity();
            DetermineVelocity();

            // Trend data allows us to wait for the speed to peak or bottom-out before we send it to
            // the animator. This is important for pivots that need to be very precise.
            mUseTrendData = true;
        }

        /// <summary>
        /// Returns the current angular velocity of the motion
        /// </summary>
        protected override Vector3 DetermineAngularVelocity()
        {
            mAngularVelocity = Vector3.zero;

            // Rotate the character towards the direction of the camera. We always want
            // him facing forward when moving
            if (mController.State.Velocity.HorizontalMagnitude() > 0.01f)
            {
                float lAngle = mController.transform.forward.HorizontalAngleTo(mController.CameraTransform.forward);
                mAngularVelocity.y = lAngle;
            }

            return mAngularVelocity;
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <returns></returns>
        public override void CleanRootMotion(ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
            rRotationDelta = Quaternion.identity;
        }

        /// <summary>
        /// Raised when the animator's state has changed
        /// </summary>
        /// <param name="rLastStateID">State the animator is leaving</param>
        /// <param name="rNewStateID">State the animator is now at</param>
        public override void OnAnimatorStateChange(int rLastStateID, int rNewStateID)
        {
        }

        /// <summary>
        /// Test to see if we're currently in the state
        /// </summary>
        public bool IsInSneakState
        {
            get
            {
                string lState = mController.GetAnimatorStateName(mAnimatorLayerIndex);
                string lTransition = mController.GetAnimatorStateTransitionName(mAnimatorLayerIndex);

                if (lTransition == "AnyState -> Sneak-SM.SneakIdle" ||
                    lTransition == "AnyState -> Sneak-SM.SneakForward" ||
                    lState == "Sneak-SM.SneakIdle" ||
                    lState == "Sneak-SM.SneakForward" ||
                    lState == "Sneak-SM.SneakLeft" ||
                    lState == "Sneak-SM.SneakRight" ||
                    lState == "Sneak-SM.SneakBackward"
                    )
                {
                    return true;
                }

                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// Simple blend that allows the avatar to walk or run forward.
    /// There is no rotation, pivoting, etc.
    /// </summary>
    [MotionTooltip("A forward walk/run blend that always keeps the avatar facing forward.")]
    public class Walk360 : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 1000;

        /// <summary>
        /// Number of degrees we'll accelerate and decelerate by
        /// in order to reach the rotation target
        /// </summary>
        [SerializeField]
        protected float mRotationAcceleration = 12.0f;

        [MotionTooltip("Determines how quickly the avatar will start rotating or stop rotating.")]
        public float RotationAcceleration
        {
            get { return mRotationAcceleration; }
            set { mRotationAcceleration = value; }
        }

        /// <summary>
        /// Current yaw we're rotating towards
        /// </summary>
        private float mYaw = 0f;

        /// <summary>
        /// Keeps track the the camera mode to revert to
        /// </summary>
        private int mSavedCameraMode = EnumCameraMode.THIRD_PERSON_FOLLOW;

        /// <summary>
        /// Keeps track of the previous stance
        /// </summary>
        private int mSavedStance = EnumControllerStance.TRAVERSAL;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Walk360()
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
        public Walk360(MotionController rController)
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
            mController.AddAnimatorName("AnyState -> Walk360-SM.WalkBlend");
            mController.AddAnimatorName("Walk360-SM.WalkBlend");
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
            

            // Only move in if the stance if we're aiming
            if (InputManager.IsPressed("Aiming")) { return true; }

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

            if (!mController.IsGrounded) { return false; }

            if (!InputManager.IsPressed("Aiming")) { return false; }

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Store the last camera mode and force it to a fixed view.
            // We do this to always keep the camera behind the player
            if (mController.UseInput && mController.CameraRig != null)
            {
                mSavedCameraMode = mController.CameraRig.Mode;
                mController.CameraRig.TransitionToMode(EnumCameraMode.FIRST_PERSON);
            }

            // Force the character's stance to change
            mSavedStance = mController.Stance;
            mController.Stance = EnumControllerStance.COMBAT_RANGED;

            // Trigger the change in the animator
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, Walk360.PHASE_START, true);

            // Continue with the activation
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Return the camera to what it was
            if (mController.CameraRig.Mode == EnumCameraMode.FIRST_PERSON)
            {
                mController.CameraRig.TransitionToMode(mSavedCameraMode);
            }

            // If we're still flagged as in the sneak stance, move out
            if (mController.Stance == EnumControllerStance.COMBAT_RANGED)
            {
                mController.Stance = mSavedStance;
            }

            // Continue with the deactivation
            base.Deactivate();
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            // Test if we should continue in the state
            if (!TestUpdate())
            {
                Deactivate();
                return;
            }

            // Determine movement and rotation
            DetermineAngularVelocity();
            DetermineVelocity();
        }

        /// <summary>
        /// Returns the current angular velocity of the motion
        /// </summary>
        protected override Vector3 DetermineAngularVelocity()
        {
            float lView = InputManager.ViewX;

            // Get the desired rotation amount
            float lYawTarget = lView * mController.RotationSpeed;

            // We want to work our way to the goal smoothly
            if (mYaw < lYawTarget)
            {
                mYaw += mRotationAcceleration;
                if (mYaw > lYawTarget) { mYaw = lYawTarget; }
            }
            else if (mYaw > lYawTarget)
            {
                mYaw -= mRotationAcceleration;
                if (mYaw < lYawTarget) { mYaw = lYawTarget; }
            }

            // Assign the current rotation
            mAngularVelocity.y = mYaw;

            // Return the results
            return mAngularVelocity;
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <returns></returns>
        public override void CleanRootMotion(ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
            // No automatic rotation in this motion
            rRotationDelta = Quaternion.identity;
        }

        /// <summary>
        /// Test to see if we're currently in the locomotion state
        /// </summary>
        public bool IsInWalkState
        {
            get
            {
                string lState = mController.GetAnimatorStateName(mAnimatorLayerIndex);
                string lTransition = mController.GetAnimatorStateTransitionName(mAnimatorLayerIndex);

                // We may be transitioning. If we are, consider us in a run state
                if (lState == "Walk360-SM.WalkBlend" || lTransition == "AnyState -> Walk360-SM.WalkBlend")
                {
                    return true;
                }

                return false;
            }
        }
    }
}

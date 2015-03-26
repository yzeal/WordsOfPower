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
    /// Simple idle that has the avatar waiting for another motion
    /// </summary>
    [MotionTooltip("Simple idle that has the avatar standing and waiting.")]
    public class CasualIdle : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 100;

        /// <summary>
        /// Determines if the actor will rotate to match the view. This is useful if you're using the Follow Camera.
        /// </summary>
        [SerializeField]
        protected bool mRotateWithView = true;

        [MotionTooltip("Determines if the actor will rotate to match the view. This is useful if you're using the Follow Camera.")]
        public bool RotateWithView
        {
            get { return mRotateWithView; }
            set { mRotateWithView = value; }
        }

        /// <summary>
        /// Turning radius we're trying to reach
        /// </summary>
        private float mYaw = 0f;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CasualIdle()
            : base()
        {
            _Priority = 0;
            mIsStartable = true;
            mIsGroundedExpected = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public CasualIdle(MotionController rController)
            : base(rController)
        {
            _Priority = 0;
            mIsStartable = true;
            mIsGroundedExpected = true;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            mController.AddAnimatorName("Idle-SM.Idle_Casual");
            mController.AddAnimatorName("AnyState -> Idle-SM.Idle_Casual");
            mController.AddAnimatorName("Idle-SM.IdleRotateLeft90");
            mController.AddAnimatorName("Idle-SM.IdleRotateRight90");
            mController.AddAnimatorName("Idle-SM.IdleRotateLeft135");
            mController.AddAnimatorName("Idle-SM.IdleRotateRight135");
            mController.AddAnimatorName("Idle-SM.IdleRotate180");
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            // This is a catch all. If there are no motions found to match
            // the controller's state, we default to this motion.
            if (mMotionLayer.ActiveMotion == null)
            {
                // We used different timing based on the grounded flag
                if (mController.IsGrounded)
                {
                    if (mMotionLayer.ActiveMotionDuration > 0.1f) 
                    {
                        return true; 
                    }
                }
                else
                {
                    if (mMotionLayer.ActiveMotionDuration > 1.0f) 
                    {
                        return true; 
                    }
                }
            }

            // Handle the disqualifiers
            if (!mIsStartable) { return false; }
            if (!mController.IsGrounded) { return false; }
            if (mController.State.InputMagnitudeTrend.Average != 0f) { return false; }

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mYaw = 0f;
            mController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, CasualIdle.PHASE_START, true);

            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            // If we're not in the casual state, we need to get out
            if (mController.GetActiveMotion(mAnimatorLayerIndex) != this)
            {
                Deactivate();
                return;
            }

            // Allow for rotating the view
            if (mController._UseInput && mRotateWithView)
            {
                float lYawTarget = 0f;
                if (InputManager.ViewX != 0f)
                {
                    lYawTarget = InputManager.ViewX * mController.RotationSpeed;
                }

                // We want to work our way to the goal smoothly
                if (mYaw < lYawTarget)
                {
                    mYaw += (mController.RotationSpeed * 0.1f);
                    if (mYaw > lYawTarget) { mYaw = lYawTarget; }
                }
                else if (mYaw > lYawTarget)
                {
                    mYaw -= (mController.RotationSpeed * 0.1f);
                    if (mYaw < lYawTarget) { mYaw = lYawTarget; }
                }

                // Assign the current rotation
                mAngularVelocity.y = mYaw;
            }

            // Ensure we're using trend data so we can react appropriately
            mUseTrendData = true;
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <returns></returns>
        public override void CleanRootMotion(ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
            // We may get some movement from transitions to idle. If we
            // do, we want to remove it
            rVelocityDelta = Vector3.zero;

            if (!IsInPivotState)
            {
                rRotationDelta = Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Test to see if we're currently pivoting
        /// </summary>
        public bool IsInPivotState
        {
            get
            {
                string lState = mController.GetAnimatorStateName(mAnimatorLayerIndex);

                if (lState == "Idle-SM.IdleRotateLeft90" ||
                    lState == "Idle-SM.IdleRotateRight90" ||
                    lState == "Idle-SM.IdleRotateLeft135" ||
                    lState == "Idle-SM.IdleRotateRight135" ||
                    lState == "Idle-SM.IdleRotate180")
                {
                    return true;
                }

                return false;
            }
        }
    }
}

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
    [MotionTooltip("A forward walk/run blend that allows the avatar to move. In this case, the avatar would " +
                   "not typically rotate towards the camera. Use this for basic movement with the included camera.")]
    public class SimpleForward : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 800;

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
        /// Default constructor
        /// </summary>
        public SimpleForward()
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
        public SimpleForward(MotionController rController)
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
            mController.AddAnimatorName("AnyState -> SimpleForward-SM.Forward");
            mController.AddAnimatorName("SimpleForward-SM.Forward");
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            // We let the ExplorationRun take over if we're in 
            // the traversal stance and groundes. There must be an 
            // attempt to move the avatar with some input/AI.
            if (!mIsStartable) { return false; }
            if (!mController.IsGrounded) { return false; }

            ControllerState lState = mController.State;
            //Debug.Log("SimpleForward.TestActivate() x:" + lState.InputX + " y:" + lState.InputY);
            if (lState.InputMagnitudeTrend.Value < 0.1f) { return false; }
            if (lState.InputX == 0 && lState.InputY == 0) { return false; }

            if (lState.Stance != EnumControllerStance.TRAVERSAL) { return false; }

            return true;
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

            ControllerState lState = mController.State;
            if (lState.InputMagnitudeTrend.Average == 0f) { return false; }
            if (lState.InputX == 0 && lState.InputY == 0) { return false; }
            if (lState.Stance != EnumControllerStance.TRAVERSAL) { return false; }

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

            // Store the last camera mode and force it to a fixed view.
            // We do this to always keep the camera behind the player
            if (mController.UseInput && mController.CameraRig != null)
            {
                mController.CameraRig.TransitionToMode(EnumCameraMode.THIRD_PERSON_FIXED);
            }

            // Trigger the change in the animator
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, SimpleForward.PHASE_START, true);

            // Continue with the activation
            return base.Activate(rPrevMotion);
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

            // If we're blocked, we're going to modify the speed
            // in order to blend into and out of a stop
            if (mController.State.IsForwardPathBlocked)
            {
                float lAngle = Vector3.Angle(mController.State.ForwardPathBlockNormal, mController.transform.forward);

                float lDiff = 180f - lAngle;
                float lSpeed = mController.State.InputMagnitudeTrend.Value * (lDiff / mController.ForwardBumperBlendAngle);

                mController.State.InputMagnitudeTrend.Replace(lSpeed);
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
            float lMovement = InputManager.MovementX;

            float lViewTarget = 0f;

            // If there is no view or no view AND no movement
            if (lView != 0f) // || lMovement == 0f)
            {
                lViewTarget = lView * mController.RotationSpeed;
            }
            // if we have movement, let it control the view
            else if (lMovement != 0f)
            {
                lViewTarget = lMovement * mController.RotationSpeed;
            }

            // We want to work our way to the goal smoothly
            if (mYaw < lViewTarget)
            {
                mYaw += mRotationAcceleration;
                if (mYaw > lViewTarget) { mYaw = lViewTarget; }
            }
            else if (mYaw > lViewTarget)
            {
                mYaw -= mRotationAcceleration;
                if (mYaw < lViewTarget) { mYaw = lViewTarget; }
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
            // Remove any x movement. This will prevent swaying
            rVelocityDelta.x = 0f;

            // In this motion, there is mo moving backwards
            if (mController.State.InputY > -0.2f && rVelocityDelta.z < 0f)
            {
                rVelocityDelta.z = 0f;
            }

            // No automatic rotation in this motion
            rRotationDelta = Quaternion.identity;
        }

        /// <summary>
        /// Test to see if we're currently in the locomotion state
        /// </summary>
        public bool IsInRunState
        {
            get
            {
                string lState = mController.GetAnimatorStateName(mAnimatorLayerIndex);
                string lTransition = mController.GetAnimatorStateTransitionName(mAnimatorLayerIndex);

                // We may be transitioning. If we are, consider us in a run state
                if (lState == "SimpleForward-SM.Forward" || lTransition == "AnyState -> SimpleForward-SM.Forward")
                {
                    return true;
                }

                return false;
            }
        }
    }
}

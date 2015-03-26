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
    /// Fall that occurs when the player is no longer grounded
    /// and didn't come off of a jump
    /// </summary>
    [MotionTooltip("Motion the avatar moves into when they are no longer grounded and are falling. Once they land, " +
                   "the avatar can move into the idle pose or a run.")]
    public class Fall : Jump
    {
        // Enum values for the motion
        public new const int PHASE_UNKNOWN = 0;
        public new const int PHASE_START_FALL = 250;

        /// <summary>
        /// The minimum distance the avatar can fall from
        /// </summary>
        [SerializeField]
        protected float mMinFallHeight = 0.3f;

        [MotionTooltip("Minimum height before the avatar moves into the motion.")]
        public float MinFallHeight
        {
            get { return mMinFallHeight; }
            set { mMinFallHeight = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Fall()
            : base()
        {
            _Priority = 5;
            mImpulse = 0f;
            mIsStartable = true;
            mIsNavMeshChangeExpected = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Fall(MotionController rController)
            : base(rController)
        {
            _Priority = 5;
            mImpulse = 0f;
            mIsStartable = true;
            mIsNavMeshChangeExpected = true;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }            
            if (IsInMidJumpState) { return false; }

            if (mController.IsGrounded) { return false; }
            if (mController.GroundDistance < mMinFallHeight) { return false; }

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // When falling, we don't want to change the camera mode. However,
            // the deactivate of the 'jump' will set it back. So we need to save the
            // current mode.
            if (mController.UseInput && mController.CameraRig != null)
            {
                mSavedCameraMode = mController.CameraRig.Mode;
            }

            // Flag the motion as active
            mIsActive = true;
            mIsActivatedFrame = true;
            mIsStartable = false;
            mLaunchForward = mController.transform.forward;

            mPhase = Fall.PHASE_START_FALL;
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, Fall.PHASE_START_FALL, true);

            // Remove any accumulated velocity so that gravity can start over
            mController.AccumulatedVelocity = new Vector3(mController.AccumulatedVelocity.x, 0f, mController.AccumulatedVelocity.z);

            // Set the ground velocity so that we can keep momentum going
            ControllerState lState = mController.State;
            lState.GroundLaunchVelocity = (mController.transform.rotation * mController.RootMotionVelocityAvg.Average);

            // Set the controller state with the modified values
            mController.State = lState;

            // Report that we're good to enter the jump
            return true;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            // If the player as initiated a jump while falling (maybe at the land part), cancel the fall
            if (!TestUpdate() || MotionLayer.ActiveMotion != this)
            {
                mIsActive = false;
                mIsStartable = true;

                mVelocity = Vector3.zero;
            }

            // continue
            base.UpdateMotion();
        }
    }
}

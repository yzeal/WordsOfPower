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
    /// This is a simple motion that shows a sliding animation when we're 
    /// sliding down a slope that exceeds our mininum slide condition.
    /// </summary>
    [MotionTooltip("This motion is a simple pose used when the avatar starts to slide down a ramp.")]
    public class Slide : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 700;

        /// <summary>
        /// Speed of the rotation that has the avatar rotating towards
        /// the slide. Set the value to 0 for no rotation.
        /// </summary>
        [SerializeField]
        protected float mRotationSpeed = 180f;

        [MotionTooltip("Determines how quickly the avatar rotates to face the downward slope. Set the value to 0 to not rotate.")]
        public float RotationSpeed
        {
            get { return mRotationSpeed; }
            set { mRotationSpeed = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Slide()
            : base()
        {
            _Priority = 5;
            mIsStartable = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Slide(MotionController rController)
            : base(rController)
        {
            _Priority = 5;
            mIsStartable = true;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            mController.AddAnimatorName("AnyState -> Slide-SM.Slide");
            mController.AddAnimatorName("Slide-SM.Slide");
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
            if (mController.State.InputMagnitudeTrend.Value > 0f) { return false; }
            if (mController.State.GroundAngle < mController.MinSlideAngle) { return false; }

            // Get out
            return true;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }

            if (!mController.IsGrounded) { return false; }
            if (mController.State.InputMagnitudeTrend.Value > 0f) { return false; }
            if (mController.State.GroundAngle < mController.MinSlideAngle) { return false; }

            // Defalut to true
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, Slide.PHASE_START, true);
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            // Test if we should move out of the slide
            if (!TestUpdate())
            {
                Deactivate();
                return;
            }

            // If we're meant to, rotate towards the direction of the slide
            if (mRotationSpeed > 0f)
            {
                // Grab the direction of force along the ground plane
                Vector3 lDirection = Vector3.down;
                Vector3 lGroundNormal = mController.State.GroundNormal;
                Vector3.OrthoNormalize(ref lGroundNormal, ref lDirection);

                // Convert the direction to a horizontal forward
                lDirection.y = 0f;
                lDirection.Normalize();

                float lAngle = NumberHelper.GetHorizontalAngle(mController.transform.forward, lDirection);
                if (Mathf.Abs(lAngle) > 0.01f)
                {
                    float lAngularSpeed = lAngle * mRotationSpeed * Time.deltaTime;
                    mAngularVelocity.y = lAngularSpeed;
                }
            }
        }
    }
}

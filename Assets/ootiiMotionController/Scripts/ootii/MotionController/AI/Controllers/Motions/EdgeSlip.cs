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
    /// If the avatar is standing on an edge with a foot hanging off,
    /// we want to apply a small velocity that will have them falling off
    /// the edge. Other wise it looks wierd with one foot in the air
    /// and one on the ground.
    /// </summary>
    [MotionTooltip("When the avatar is idle, a test is made to ensure each foot is actually on the ground. " + 
                   "If either foot is floating in the air, the avatar will slowly slip off the edge.")]
    public class EdgeSlip : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;

        /// <summary>
        /// Max distance we can have a gap between the foot and
        /// the ground before the edge slip occurs.
        /// </summary>
        protected float mMaxFootGap = 0.3f;

        [MotionTooltip("Maximum distance between the foot and ground before the edge slip starts.")]
        public float MaxFootGap
        {
            get { return mMaxFootGap; }
            set { mMaxFootGap = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public EdgeSlip()
            : base()
        {
            _Priority = 1;
            mIsStartable = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public EdgeSlip(MotionController rController)
            : base(rController)
        {
            _Priority = 1;
            mIsStartable = true;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }
            if (!IsInIdleState) { return false; }
            if (!mController.State.IsGrounded) { return false; }
            
            // If we get here, we need to do a more indepth test. This time we're looking
            // to see if there is ground under the avatar
            Vector3 lLeftFoot = mController.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            Vector3 lRightFoot = mController.Animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

            // Shoot a ray out of each toe to determine if we're grounded
            //Ray lLeftFootRay = new Ray(lLeftFoot, Vector3.down);
            //TT bool lLeftFootHit = UnityEngine.Physics.Raycast(lLeftFootRay, mMaxFootGap, mController.PlayerMask);
            bool lLeftFootHit = mController.SafeRaycast(lLeftFoot, Vector3.down, mMaxFootGap);

            //Ray lRightFootRay = new Ray(lRightFoot, Vector3.down);
            //TT bool lRightFootHit = UnityEngine.Physics.Raycast(lRightFootRay, mMaxFootGap, mController.PlayerMask);
            bool lRightFootHit = mController.SafeRaycast(lRightFoot, Vector3.down, mMaxFootGap);

            // If both feet are on the ground, we can get out. If neither feet
            // are on the ground, we'll get out an let another motion (ie Fall)
            // handle this.
            if (lLeftFootHit && lRightFootHit) { return false; }
            if (!lLeftFootHit && !lRightFootHit) { return false; }

            // Time to slip
            return true;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            // Get out if we're not idling. Don't 'Stop' since we don't want
            // to change any 
            if (!mController.IsGrounded || !IsInIdleState)
            {
                Deactivate();
                return;
            }

            // Clear the velocity and reset it
            mVelocity = Vector3.zero;

            // Re-shoot the rays so we can get an ever increasing slope
            Vector3 lLeftFoot = mController.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            Vector3 lRightFoot = mController.Animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

            // Shoot a ray out of each toe to determine if we're grounded
            //Ray lLeftFootRay = new Ray(lLeftFoot, Vector3.down);
            //TT bool lLeftFootHit = UnityEngine.Physics.Raycast(lLeftFootRay, mMaxFootGap, mController.PlayerMask);
            bool lLeftFootHit = mController.SafeRaycast(lLeftFoot, Vector3.down, mMaxFootGap);

            //Ray lRightFootRay = new Ray(lRightFoot, Vector3.down);
            //TT bool lRightFootHit = UnityEngine.Physics.Raycast(lRightFootRay, mMaxFootGap, mController.PlayerMask);
            bool lRightFootHit = mController.SafeRaycast(lRightFoot, Vector3.down, mMaxFootGap);

            // If both feet are on the ground, we can get out. If neither feet
            // are on the ground, we'll get out an let another motion (ie Fall)
            // handle this.
            if (lLeftFootHit && lRightFootHit) { return; }
            if (!lLeftFootHit && !lRightFootHit) { return; }

            // Since only one hit, we want to start moving the avatar towards the edge.
            // First, create a fake ground position under the floating foot
            if (!lLeftFootHit) { lLeftFoot.y += 1f; }
            if (!lRightFootHit) { lRightFoot.y += 1f; }

            // Next, create a slope between the "ground positions"
            Vector3 lSlope = (lLeftFootHit != true ? lLeftFoot - lRightFoot : lRightFoot - lLeftFoot).normalized;

            // Set this as our velocity change and send gravity downwards
            mVelocity = lSlope * mController.Gravity.magnitude;
            mVelocity.y = -mVelocity.y;
        }

        /// <summary>
        /// Sets a value indicating whether this instance is in idle state.
        /// </summary>
        /// <value><c>true</c> if this instance is in idle state; otherwise, <c>false</c>.</value>
        private bool IsInIdleState
        {
            get
            {
                if (mController.CompareAnimatorStateName(mAnimatorLayerIndex, "Idle-SM.Idle_Casual"))
                {
                    return true;
                }

                return false;
            }
        }
    }
}

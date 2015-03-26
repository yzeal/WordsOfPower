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
    /// Handles the basic motion for getting the onto a 
    /// mid (0.75m) height object
    /// </summary>
    [MotionTooltip("When the avatar is idle and facing the object, this motion allows the avatar " + 
                   "to climb on top of a 'table high' object.")]
    public class ClimbMid : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 900;
        public const int PHASE_TO_TOP = 910;

        /// <summary>
        /// Keeps us from having to reallocate over and over
        /// </summary>
        private static RaycastHit sCollisionInfo = new RaycastHit();

        /// <summary>
        /// Max horizontal distance the avatar can be from the object
        /// he is trying to climb onto.
        /// </summary>
        [SerializeField]
        protected float mMaxDistance = 0.4f;

        [MotionTooltip("Maximum distance the avatar can be from the object before trying to climb onto it.")]
        public float MaxDistance
        {
            get { return mMaxDistance; }
            set { mMaxDistance = value; }
        }

        /// <summary>
        /// Min height of the object that can be climbed.
        /// </summary>
        [SerializeField]
        protected float mMinHeight = 0.5f;

        [MotionTooltip("Minimum height of the object to climb onto.")]
        public float MinHeight
        {
            get { return mMinHeight; }
            set { mMinHeight = value; }
        }

        /// <summary>
        /// Max height of the object that can be climbed.
        /// </summary>
        [SerializeField]
        protected float mMaxHeight = 1.0f;

        [MotionTooltip("Maximum height of the object to climb onto.")]
        public float MaxHeight
        {
            get { return mMaxHeight; }
            set { mMaxHeight = value; }
        }

        /// <summary>
        /// Offset to the final position of the animation used to help the 
        /// character line up with the idle (or other) animation that will follow
        /// after it.
        /// </summary>
        [SerializeField]
        protected Vector3 mExitPositionOffset = new Vector3(0f, 0.025f, 0f);

        [MotionTooltip("When the avatar moves to the top of the ledge, an offset used to ensure the avatar lines up with the idle pose that follows.")]
        public Vector3 ExitPositionOffset
        {
            get { return mExitPositionOffset; }
            set { mExitPositionOffset = value; }
        }

        /// <summary>
        /// The X distance from the grab position that the hands will
        /// be positions. If a value is set, we'll check to make sure there
        /// is something for them to grab or fail.
        /// </summary>
        [SerializeField]
        protected float mHandGrabOffset = 0.13f;

        [MotionTooltip("Position offset from the avatar's middle grab where the left and right hands will be positioned.")]
        public float HandGrabOffset
        {
            get { return mHandGrabOffset; }
            set { mHandGrabOffset = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        [SerializeField]
        protected int mClimableLayer = 9;

        [MotionTooltip("Any object that is to be climbed needs to be set to this user layer.")]
        public int ClimbableLayer
        {
            get { return mClimableLayer; }
            set { mClimableLayer = value; }
        }

        /// <summary>
        /// Tracks the object that is being climbed
        /// </summary>
        protected GameObject mClimbable = null;

        /// <summary>
        /// Tracks the last grab position
        /// </summary>
        protected Vector3 mGrabPosition = Vector3.zero;

        /// <summary>
        /// Tracks the last grab position relative to the climbable
        /// </summary>
        protected Vector3 mLocalGrabPosition = Vector3.zero;

        /// <summary>
        /// Tracks the contact position from the avatar's perspective
        /// </summary>
        protected Vector3 mAvatarContactPosition = Vector3.zero;

        /// <summary>
        /// Normal extruding out of the climbable in the direction of the avatar
        /// </summary>
        protected Vector3 mGrabPositionNormal = Vector3.zero;

        /// <summary>
        /// Tracks where we want the avatar to go
        /// </summary>
        protected Vector3 mTargetPosition = Vector3.zero;

        /// <summary>
        /// Tracks where we want the right hand to go
        /// </summary>
        protected Vector3 mRightHandTargetPosition = Vector3.zero;

        /// <summary>
        /// Tracks where we want the left hand to go
        /// </summary>
        protected Vector3 mLeftHandTargetPosition = Vector3.zero;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClimbMid()
            : base()
        {
            _Priority = 15;
            mIsStartable = true;
            mIsGravityEnabled = false;
            mIsGroundedExpected = false;
            mIsNavMeshChangeExpected = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public ClimbMid(MotionController rController)
            : base(rController)
        {
            _Priority = 15;
            mIsStartable = true;
            mIsGravityEnabled = false;
            mIsGroundedExpected = false;
            mIsNavMeshChangeExpected = true;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            mController.AddAnimatorName("AnyState -> ClimbMid-SM.IdleClimbMid");
            mController.AddAnimatorName("ClimbMid-SM.IdleClimbMid");
            mController.AddAnimatorName("ClimbMid-SM.IdleClimbMid -> ClimbMid-SM.ClimbRecoverIdle");
            mController.AddAnimatorName("ClimbMid-SM.ClimbRecoverIdle");
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
            if (!InputManager.IsJustPressed("Jump")) { return false; }

            // Edge info
            bool lEdgeGrabbed = false;

            // If we're on the ground, we can't grab. However, we
            // want to clear our last grab position
            lEdgeGrabbed = TestGrab(mMinHeight, mMaxHeight);

            // Return the final result
            return lEdgeGrabbed;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Ensure we have good collision info
            if (sCollisionInfo.collider == null) { return false; }

            // Flag the motion as active
            mIsActive = true;
            mIsActivatedFrame = true;
            mIsStartable = false;
            //mHasArrived = false;
            //mArrivalLerp = 0.25f;

            mController.AccumulatedVelocity = Vector3.zero;

            // Set the control state
            ControllerState lState = mController.State;

            mPhase = ClimbMid.PHASE_START;
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbMid.PHASE_START, true);

            mController.State = lState;

            // Track the object we're trying to climb and store it
            mClimbable = sCollisionInfo.collider.gameObject;
            UnityEngine.Physics.IgnoreCollision(mController.CharController.GetComponent<Collider>(), mClimbable.GetComponent<Collider>(), true);

            // Set the grab position we're anchoring on
            mGrabPosition = sCollisionInfo.point;
            mLocalGrabPosition = Quaternion.Inverse(mClimbable.transform.rotation) * (sCollisionInfo.point - mClimbable.transform.position);

            // Set the grab normal coming out of the wall we're anchoring on
            mGrabPositionNormal = sCollisionInfo.normal;

            // Determine the target position given the grab info
            mTargetPosition = DetermineTargetPositions(ref mGrabPosition, ref mGrabPositionNormal);

            // Clear the avatar contact position so it can be reset
            mAvatarContactPosition = Vector3.zero;

            // Return
            return true;
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            mIsActive = false;
            mIsStartable = true;
            mDeactivationTime = Time.time;
            mVelocity = Vector3.zero;
            mAngularVelocity = Vector3.zero;

            // Reenable collisions
            UnityEngine.Physics.IgnoreCollision(mController.CharController.GetComponent<Collider>(), mClimbable.GetComponent<Collider>(), false);
            mClimbable = null;

            // Get out of the climb motion
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbMid.PHASE_UNKNOWN);
        }

        /// <summary>
        /// Allows the motion to modify the ground and support information
        /// </summary>
        /// <param name="rState">Current state whose support info can be modified</param>
        /// <returns>Boolean that determines if the avatar is grounded</returns>
        public override bool DetermineGrounding(ref ControllerState rState)
        {
            // These values need to be updated in LateUpdateMotion to have the
            // most accurate values. However, they will work here to help
            // modify things like root motion.
            rState.Support = mClimbable;
            rState.SupportPosition = mClimbable.transform.position;
            rState.SupportRotation = mClimbable.transform.rotation;
            if (rState.Support != mController.PrevState.Support || rState.SupportContactPosition.sqrMagnitude == 0f)
            {
                // This is set for the avatar
                rState.SupportContactPosition = mAvatarContactPosition;
            }

            return rState.IsGrounded;
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <returns></returns>
        public override void CleanRootMotion(ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
             //rVelocityDelta.x = 0f;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            mVelocity = Vector3.zero;
            mAngularVelocity = Vector3.zero;

            string lStateName = mController.GetAnimatorStateName(mAnimatorLayerIndex);
            //if (lStateName == "ClimbMid-SM.IdleClimbMid" && mController.GetAnimatorMotionPhase(mAnimatorLayerIndex) == ClimbMid.PHASE_START) { mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbMid.PHASE_UNKNOWN); }

            float lStateTime = mController.State.AnimatorStates[mAnimatorLayerIndex].StateInfo.normalizedTime;

            // Orient ourselves towards the anchor point
            // Orient ourselves towards the anchor point
            if (lStateName == "ClimbMid-SM.IdleClimbMid")
            {
                // At this point, we're moving onto the table
                if (lStateTime > 0.575f)
                {
                    // As we exit the final animation, move towards the exact position that the
                    // following animation (usually idle) will match to.
                    Vector3 lTargetPosition = mController.transform.position + mExitPositionOffset;
                    lTargetPosition.y = mGrabPosition.y + mExitPositionOffset.y;

                    // Check if we're at the destination
                    if (Vector3.Distance(lTargetPosition, mController.transform.position) > 0.01f)
                    {
                        mTargetPosition = lTargetPosition;

                        // Convert the positions into velocity so they can be processed later
                        Vector3 lNewPosition = Vector3.Lerp(mController.transform.position, mTargetPosition, 0.25f);
                        mVelocity = (lNewPosition - mController.transform.position) / Time.fixedDeltaTime;
                    }

                    // Create a new contact position
                    if (mAvatarContactPosition.sqrMagnitude == 0f)
                    {
                        mAvatarContactPosition = Quaternion.Inverse(mClimbable.transform.rotation) * (mTargetPosition - mClimbable.transform.position);
                    }
                }
                
                // If we're done, get out of the climb
                if (lStateTime >= 1.0f)
                {
                    Deactivate();
                }
            }
            // Once we're at the top, we want to make sure there is no popping. So we'll force the
            // avatar to the right height
            else if (lStateName == "ClimbMid-SM.ClimbRecoverIdle")
            {
                // As we exit the final animation, move towards the exact position that the
                // following animation (usually idle) will match to.
                Vector3 lTargetPosition = mController.transform.position + mExitPositionOffset;
                lTargetPosition.y = mGrabPosition.y + mExitPositionOffset.y;

                // Check if we're at the destination
                if (Vector3.Distance(lTargetPosition, mController.transform.position) > 0.01f)
                {
                    mTargetPosition = lTargetPosition;

                    // Convert the positions into velocity so they can be processed later
                    Vector3 lNewPosition = Vector3.Lerp(mController.transform.position, mTargetPosition, 0.25f);
                    mVelocity = (lNewPosition - mController.transform.position) / Time.fixedDeltaTime;
                }

                // Create a new contact position
                if (mAvatarContactPosition.sqrMagnitude == 0f)
                {
                    mAvatarContactPosition = Quaternion.Inverse(mClimbable.transform.rotation) * (mTargetPosition - mClimbable.transform.position);
                }

                // If we're done, get out of the climb
                if (lStateTime >= 1.0f) 
                { 
                    Deactivate(); 
                }
            }
        }

        /// <summary>
        /// Called by the controller during the late update
        /// </summary>
        public override void LateUpdateMotion()
        {
            // Set the support as the object we're attached to
            ControllerState lState = mController.State;
            ControllerState lPrevState = mController.PrevState;

            // Grab the latest positioning data
            lState.Support = mClimbable;
            lState.SupportPosition = mClimbable.transform.position;
            lState.SupportRotation = mClimbable.transform.rotation;
            if (lState.Support != lPrevState.Support || lState.SupportContactPosition.sqrMagnitude == 0f)
            {
                // This is set for the avatar
                lState.SupportContactPosition = mAvatarContactPosition;
            }

            // Set the controller so we can use the data later
            mController.State = lState;

            // Update the target positions if the support moves
            if (lState.Support != null && (lState.Support == lPrevState.Support))
            {
                Vector3 lSupportMove = Vector3.zero;

                // Determine the linear move based on position
                if (lState.SupportPosition != lPrevState.SupportPosition)
                {
                    lSupportMove = lState.SupportPosition - lPrevState.SupportPosition;
                }

                // Determine the linear move based on orbit
                if (Quaternion.Angle(lState.SupportRotation, lPrevState.SupportRotation) != 0f)
                {
                    lSupportMove += (lState.SupportRotation * mLocalGrabPosition) - (lPrevState.SupportRotation * mLocalGrabPosition);
                }

                // If there's a move, update the grab info
                if (lSupportMove.sqrMagnitude != 0f)
                {
                    // Adjust the grab info
                    mGrabPosition += lSupportMove;
                    mTargetPosition = DetermineTargetPositions(ref mGrabPosition, ref mGrabPositionNormal);
                }
            }

            // Debug
            //Draw.DrawSphere(mGrabPosition, 0.1f, Color.green);
            //Draw.DrawSphere(mLeftHandTargetPosition, 0.1f, Color.yellow);
            //Draw.DrawSphere(mRightHandTargetPosition, 0.1f, Color.cyan);
            //Draw.DrawSphere(mTargetPosition, 0.1f, Color.yellow);
            //Draw.DrawSphere(mController.transform.position, 0.1f, Color.red);
        }

        /// <summary>
        /// Shoot rays to determine if a horizontal edge exists that
        /// we may be able to grab onto. It needs to be within the range
        /// of the avatar's feelers.
        /// </summary>
        /// <returns>Boolean that says if we've found an acceptable edge</returns>
        public virtual bool TestGrab(float rBottom, float rTop)
        {
            Vector3 lRayStart = Vector3.zero;

            float lTargetDistance = mMaxDistance;

            // Only climb those things that are on the climbable layer
            int lIsClimbableMask = 1 << mClimableLayer;

            // Root position for the test
            Transform lRoot = mController.transform;

            // Determine the ray positions
            float lEdgeTop = rTop;
            float lEdgeBottom = rBottom;

            // Debug
            //Debug.DrawLine(lRoot.position + new Vector3(0f, lEdgeTop, 0f), lRoot.position + new Vector3(0f, lEdgeTop, 0f) + mController.transform.forward * lTargetDistance, Color.red);
            //Debug.DrawLine(lRoot.position + new Vector3(0f, lEdgeBottom, 0f), lRoot.position + new Vector3(0f, lEdgeBottom, 0f) + mController.transform.forward * lTargetDistance, Color.red);

            // Shoot forward and ensure below the edge is blocked
            lRayStart = lRoot.position + new Vector3(0f, lEdgeBottom, 0f);

            //TT if (!UnityEngine.Physics.Raycast(lRayStart, mController.transform.forward, out sCollisionInfo, lTargetDistance, lIsClimbableMask))
            if (!mController.SafeRaycast(lRayStart, mController.transform.forward, ref sCollisionInfo, lTargetDistance, lIsClimbableMask))
            {
                return false;
            }

            // Shoot forward and ensure above the edge is open
            lRayStart = lRoot.position + new Vector3(0f, lEdgeTop, 0f);

            //TT if (UnityEngine.Physics.Raycast(lRayStart, mController.transform.forward, out sCollisionInfo, lTargetDistance, lIsClimbableMask))
            if (mController.SafeRaycast(lRayStart, mController.transform.forward, ref sCollisionInfo, lTargetDistance, lIsClimbableMask))
            {
                return false;
            }

            // Now that we know there is an edge, determine it's exact position.
            // First, we sink into the collision point a tad. Then, we use our 
            // collision point and start above it (where the top ray failed). Finally,
            // we shoot a ray down
            lRayStart = sCollisionInfo.point + (mController.transform.forward * 0.01f);
            lRayStart.y = lRoot.position.y + lEdgeTop;
            
            //TT if (!UnityEngine.Physics.Raycast(lRayStart, -mController.transform.up, out sCollisionInfo, lEdgeTop - lEdgeBottom + 0.01f, lIsClimbableMask))
            if (!mController.SafeRaycast(lRayStart, -mController.transform.up, ref sCollisionInfo, lEdgeTop - lEdgeBottom + 0.01f, lIsClimbableMask))
            {
                return false;
            }

            // Finally we shoot one last ray forward. We do this because we want the collision
            // data to be about the wall facing the avatar, not the wall facing the
            // last ray (which was shot down).
            lRayStart = lRoot.position;
            lRayStart.y = sCollisionInfo.point.y - 0.01f;
            //TT if (!UnityEngine.Physics.Raycast(lRayStart, mController.transform.forward, out sCollisionInfo, lTargetDistance, lIsClimbableMask))
            if (!mController.SafeRaycast(lRayStart, mController.transform.forward, ref sCollisionInfo, lTargetDistance, lIsClimbableMask))
            {
                return false;
            }

            // If we have hand positions, ensure that they collide with something as well. Otherwise,
            // the hand will look like it's floating in the air.
            if (mHandGrabOffset > 0)
            {
                // Check the right hand
                Vector3 lRightHandPosition = lRayStart + (Controller.transform.rotation * new Vector3(mHandGrabOffset, 0f, 0f));
                
                //TT if (!UnityEngine.Physics.Raycast(lRightHandPosition, Controller.transform.forward, lTargetDistance, lIsClimbableMask))
                if (!mController.SafeRaycast(lRightHandPosition, Controller.transform.forward, lTargetDistance, lIsClimbableMask))
                {
                    return false;
                }

                // Check the left hand
                Vector3 lLeftHandPosition = lRayStart + (Controller.transform.rotation * new Vector3(-mHandGrabOffset, 0f, 0f));
                
                //TT if (!UnityEngine.Physics.Raycast(lLeftHandPosition, Controller.transform.forward, lTargetDistance, lIsClimbableMask))
                if (!mController.SafeRaycast(lLeftHandPosition, Controller.transform.forward, lTargetDistance, lIsClimbableMask))
                {
                    return false;
                }
            }

            // If we got here, we found an edge
            return true;
        }

        /// <summary>
        /// Raised when the animator's state has changed
        /// </summary>
        public override void OnAnimatorStateChange(int rLastStateID, int rNewStateID)
        {
            // Ensure we don't re-enter the 'any state'
            if (rNewStateID == mController.AnimatorStateIDs["ClimbMid-SM.ClimbRecoverIdle"])
            {
                mPhase = ClimbMid.PHASE_TO_TOP;
            }

            // If we've moved out the recovery state, reset the motion and state
            if (rLastStateID == mController.AnimatorStateIDs["ClimbMid-SM.ClimbRecoverIdle"])
            {
                // Clear out the climb info
                Deactivate();

                // Clear out the old position
                mGrabPosition = Vector3.zero;

                // Reset the motion state so we can move to fall
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbMid.PHASE_UNKNOWN);
            }
        }

        /// <summary>
        /// Callback for animating IK. All IK functionality should go here
        /// </summary>
        public override void UpdateIK()
        {
            if (IsInClimbUpState || IsInClimbIdleState)
            {
                SetIKTargets(1.0f);
            }
            else
            {
                ClearIKTargets();
            }
        }	

        /// <summary>
        /// Given the current grab position, calculates the avatar's target
        /// position
        /// </summary>
        /// <returns></returns>
        private Vector3 DetermineTargetPositions(ref Vector3 rGrabPosition, ref Vector3 rGrabNormal)
        {
            // If we're grounded (on the ground, climbing, etc), we may need to apply the velocity of the ground
            if (mController.State.Support != null && (mController.State.Support == mController.PrevState.Support))
            {
                // Test if the support has rotated. Note that we may be a frame behind. Technically this is
                // best done in LateUpdate() after the support has updated, but we don't want to get ahead of the camera.
                if (Quaternion.Angle(mController.State.SupportRotation, mController.PrevState.SupportRotation) != 0f)
                {
                    // Rotate the avatar
                    Quaternion lDeltaRotation = mController.PrevState.SupportRotation.RotationTo(mController.State.SupportRotation);
                    rGrabNormal = lDeltaRotation * rGrabNormal;
                }
            }

            // Determine the hand position
            if (mHandGrabOffset > 0f)
            {
                mLeftHandTargetPosition = rGrabPosition - ((Quaternion.AngleAxis(-90, Vector3.up) * rGrabNormal) * mHandGrabOffset);
                mRightHandTargetPosition = rGrabPosition - ((Quaternion.AngleAxis(90, Vector3.up) * rGrabNormal) * mHandGrabOffset);
            }

            // Find the desired angle we're going for
            float lWallAngle = NumberHelper.GetHorizontalAngle(Vector3.forward, rGrabNormal);

            // Determine the new position
            Vector3 lTargetPosition = rGrabPosition;
            lTargetPosition.x += mMaxDistance * Mathf.Sin(lWallAngle * Mathf.Deg2Rad);
            lTargetPosition.z += mMaxDistance * Mathf.Cos(lWallAngle * Mathf.Deg2Rad);

            return lTargetPosition;
        }

        /// <summary>
        /// Forces the hands to the grab positions
        /// </summary>
        /// <param name="rWeight"></param>
        private void SetIKTargets(float rWeight)
        {
            //mController.Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rWeight);
            //mController.Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.0f);
            //mController.Animator.SetIKPosition(AvatarIKGoal.RightHand, mRightHandTargetPosition);

            //mController.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, rWeight);
            //mController.Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.0f);
            //mController.Animator.SetIKPosition(AvatarIKGoal.LeftHand, mLeftHandTargetPosition);
        }

        /// <summary>
        /// Removes the hand grab
        /// </summary>
        private void ClearIKTargets()
        {
            //mController.Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
            //mController.Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.0f);

            //mController.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
            //mController.Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.0f);
        }

        /// <summary>
        /// Test to see if we're currently in the process
        /// of trying to grab onto something
        /// </summary>
        protected bool IsInClimbUpState
        {
            get
            {
                string lState = mController.GetAnimatorStateName(mAnimatorLayerIndex);
                string lTransition = mController.GetAnimatorStateTransitionName(mAnimatorLayerIndex);

                if (lTransition == "AnyState -> ClimbMid-SM.IdleClimbMid" ||
                    lState == "ClimbMid-SM.IdleClimbMid"
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently in the idle state for the crouch
        /// </summary>
        protected bool IsInClimbIdleState
        {
            get
            {
                string lState = mController.GetAnimatorStateName(mAnimatorLayerIndex);
                string lTransition = mController.GetAnimatorStateTransitionName(mAnimatorLayerIndex);

                if (lTransition == "ClimbMid-SM.IdleClimbMid -> ClimbMid-SM.ClimbRecoverIdle" ||
                    lState == "ClimbMid-SM.ClimbRecoverIdle"
                   )
                {
                    return true;
                }

                return false;
            }
        }
    }
}

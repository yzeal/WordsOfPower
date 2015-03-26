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
    /// Handles the basic motion for getting the avatar into the
    /// crouch position and moving them from the crouch to idle.
    /// </summary>
    [MotionTooltip("Allows the avatar to move into a 'cat grab' parkour style position. When jumping or falling " +
                   "the avatar will attempt to grab a ledge. From there, this motions will allow them to move " +
                   "left or right or climb to the top of the ledge.")]
    public class ClimbCrouch : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_FROM_IDLE = 300;
        public const int PHASE_FROM_JUMP_RISE = 301;
        public const int PHASE_FROM_JUMP_TOP = 302;
        public const int PHASE_FROM_JUMP_FALL = 303;
        public const int PHASE_FROM_JUMP_IMPACT = 304;
        public const int PHASE_IDLE = 320;
        public const int PHASE_TO_TOP = 350;
        public const int PHASE_TO_FALL = 370;
        public const int PHASE_SHIMMY_LEFT = 380;
        public const int PHASE_SHIMMY_RIGHT = 385;

        /// <summary>
        /// Keeps us from having to reallocate over and over
        /// </summary>
        private static RaycastHit sCollisionInfo = new RaycastHit();
        private static RaycastHit sCollisionUpdateInfo = new RaycastHit();

        /// <summary>
        /// Ensure when we're trying to climb with the crouch that
        /// the avatar is at a certain ground distance.
        /// Otherwise, it's odd to latch onto something when the avatar
        /// jumps 0.1m.
        /// </summary>
        [SerializeField]
        protected float mMinGroundDistance = 0.3f;

        [MotionTooltip("Minimum distance the grab is valid. Otherwise, the avatar will not grab or will release.")]
        public float MinGroundDistance
        {
            get { return mMinGroundDistance; }
            set { mMinGroundDistance = value; }
        }

        /// <summary>
        /// Minimum distance the new grab point must be from the last
        /// grab point in order for the grab to work.
        /// </summary>
        [SerializeField]
        protected float mMinRegrabDistance = 1.0f;

        [MotionTooltip("Minimum distance from the previous grab position before another grab is attempted.")]
        public float MinRegrabDistance
        {
            get { return mMinRegrabDistance; }
            set { mMinRegrabDistance = value; }
        }

        /// <summary>
        /// The X distance from the grab position that the hands will
        /// be positions. If a value is set, we'll check to make sure there
        /// is something for them to grab or fail.
        /// </summary>
        [SerializeField]
        protected float mHandGrabOffset = 0.13f;

        [MotionTooltip("Position offset from the avatar's middle ledge grab where the left and right hands will be positioned.")]
        public float HandGrabOffset
        {
            get { return mHandGrabOffset; }
            set { mHandGrabOffset = value; }
        }

        /// <summary>
        /// Target of the character's body from the grab position relative
        /// to the grab position in the direction of the body.
        /// </summary>
        [SerializeField]
        protected Vector3 mBodyTargetOffset = new Vector3(0f, -1.25f, -0.65f);

        [MotionTooltip("When a ledge is grabbed, this defines the avatar position from the grab point. Change these values to ensure the avatar's hands fit the ledge.")]
        public Vector3 BodyTargetOffset
        {
            get { return mBodyTargetOffset; }
            set { mBodyTargetOffset = value; }
        }

        /// <summary>
        /// Offset to the final position of the animation used to help the 
        /// character line up with the idle (or other) animation that will follow
        /// after it.
        /// </summary>
        [SerializeField]
        protected Vector3 mExitPositionOffset = new Vector3(0f, 0.015f, 0f);

        [MotionTooltip("When the avatar moves to the top of the ledge, an offset used to ensure the avatar lines up with the idle pose that follows.")]
        public Vector3 ExitPositionOffset
        {
            get { return mExitPositionOffset; }
            set { mExitPositionOffset = value; }
        }

        /// <summary>
        /// Offset to add to the root motion velocity when the character starts
        /// to climb up to the top.
        /// </summary>
        [SerializeField]
        protected Vector3 mToTopVelocity = Vector3.zero;

        [MotionTooltip("Additional velocity that can be added to help move the avatar to the top.")]
        public Vector3 ToTopVelocity
        {
            get { return mToTopVelocity; }
            set { mToTopVelocity = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        [SerializeField]
        protected int mClimbableLayer = 9;

        [MotionTooltip("Any object that is to be climbed needs to be set to this user layer.")]
        public int ClimbableLayer
        {
            get { return mClimbableLayer; }
            set { mClimbableLayer = value; }
        }

        /// <summary>
        /// Deteremines the amount of room needed in order for
        /// the climbing character to move left or right.
        /// </summary>
        protected float mMinSideSpaceForMove = 0.6f;

        [MotionTooltip("Minimum space required for the avatar to shimmy to the left or right.")]
        public float MinSideSpaceForMove
        {
            get { return mMinSideSpaceForMove; }
            set { mMinSideSpaceForMove = value; }
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
        /// Normal extruding out of the climbable
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
        /// Determines if the avatar arrived to the anchor spot
        /// </summary>
        protected bool mHasArrived = false;

        /// <summary>
        /// The speed at which we try to arrive at the anchor spot
        /// </summary>
        protected float mArrivalLerp = 0.25f;

        /// <summary>
        /// Motion we were in when we grabbed
        /// 0 = idle
        /// 1 = rise
        /// 2 = top
        /// 3 = fall
        /// </summary>
        protected int mGrabMotion = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClimbCrouch()
            : base()
        {
            _Priority = 15;
            mIsStartable = true;
            mIsGravityEnabled = false;
            mIsNavMeshChangeExpected = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public ClimbCrouch(MotionController rController)
            : base(rController)
        {
            _Priority = 15;
            mIsStartable = true;
            mIsGravityEnabled = false;
            mIsNavMeshChangeExpected = true;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            mController.AddAnimatorName("AnyState -> ClimbCrouch-SM.IdleToClimbCrouch");
            mController.AddAnimatorName("AnyState -> ClimbCrouch-SM.JumpRiseToClimbCrouch");
            mController.AddAnimatorName("AnyState -> ClimbCrouch-SM.JumpTopToClimbCrouch");
            mController.AddAnimatorName("AnyState -> ClimbCrouch-SM.JumpFallToClimbCrouch");
            mController.AddAnimatorName("AnyState -> ClimbCrouch-SM.JumpImpactToClimbCrouch");

            mController.AddAnimatorName("Jump-SM.JumpRise");
            mController.AddAnimatorName("Jump-SM.JumpRisePose");
            mController.AddAnimatorName("Jump-SM.JumpRiseToTop");
            mController.AddAnimatorName("Jump-SM.JumpTopPose");
            mController.AddAnimatorName("Jump-SM.JumpTopToFall");
            mController.AddAnimatorName("Jump-SM.JumpFallPose");

            mController.AddAnimatorName("Jump-SM.JumpRise -> ClimbCrouch-SM.JumpRiseToClimbCrouch");

            mController.AddAnimatorName("ClimbCrouch-SM.IdleToClimbCrouch");
            mController.AddAnimatorName("ClimbCrouch-SM.JumpRiseToClimbCrouch");
            mController.AddAnimatorName("ClimbCrouch-SM.JumpFallToClimbCrouch");
            mController.AddAnimatorName("ClimbCrouch-SM.JumpTopToClimbCrouch");
            mController.AddAnimatorName("ClimbCrouch-SM.JumpImpactToClimbCrouch");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchPose");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchToTop");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchToJumpFall");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchToJumpFall");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchToTop");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchToTop -> ClimbCrouch-SM.ClimbCrouchRecoverIdle");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchRecoverIdle");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchRecoverIdle -> Idle-SM.Idle_Casual");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchRecoverIdle -> Idle-SM.Idle_Alert");

            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchShimmyLeft");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchShimmyRight");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchShimmyLeft");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchShimmyRight");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchShimmyLeft -> ClimbCrouch-SM.ClimbCrouchPose");
            mController.AddAnimatorName("ClimbCrouch-SM.ClimbCrouchShimmyRight -> ClimbCrouch-SM.ClimbCrouchPose");
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }

            // Edge info
            bool lEdgeGrabbed = false;

            // If we're on the ground, we can't grab. However, we
            // want to clear our last grab position
            if (mController.IsGrounded)
            {
                if (mGrabPosition.sqrMagnitude > 0f)
                {
                    mGrabPosition = Vector3.zero;
                }
            }
            // If we're in the air, we can test for a grab
            else
            {
                // If we're moving up, we can test for a grab
                if (mController.State.Velocity.y > 2f)
                {
                    mGrabMotion = 1;
                    lEdgeGrabbed = TestGrab(mController.transform.position, mController.transform.rotation, mController.transform.forward, mController.ColliderRadius, 1.20f * mController.CharacterScale, 1.35f * mController.CharacterScale);
                }
                // If we're at the peak
                else if (mController.State.Velocity.y > -2f)
                {
                    mGrabMotion = 2;
                    lEdgeGrabbed = TestGrab(mController.transform.position, mController.transform.rotation, mController.transform.forward, mController.ColliderRadius, 1.00f * mController.CharacterScale, 1.20f * mController.CharacterScale);
                }
                // When falling, we just test for a grab
                else
                {
                    mGrabMotion = 3;
                    lEdgeGrabbed = TestGrab(mController.transform.position, mController.transform.rotation, mController.transform.forward, mController.ColliderRadius, 1.00f * mController.CharacterScale, 1.42f * mController.CharacterScale);
                }

                // While going down, ensure that the height of the edge
                // passes our minimum grab standard
                if (lEdgeGrabbed)
                {
                    if (mController.State.GroundDistance < mMinGroundDistance)
                    {
                        lEdgeGrabbed = false;
                    }
                }
            }

            // Ensure we meet the minimum distance from the last grab point
            if (lEdgeGrabbed)
            {
                if (Vector3.Distance(sCollisionInfo.point, mGrabPosition) < mMinRegrabDistance)
                {
                    lEdgeGrabbed = false;
                }
            }

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
            mHasArrived = false;
            mArrivalLerp = 0.25f;

            mController.AccumulatedVelocity = Vector3.zero;

            // Set the control state
            ControllerState lState = mController.State;

            // Test if we're coming from a jump rise
            if (mGrabMotion == 1)
            {
                mPhase = ClimbCrouch.PHASE_FROM_JUMP_RISE;
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_FROM_JUMP_RISE);
            }
            // Test if we're coming from the top of the jump
            else if (mGrabMotion == 2)
            {
                mPhase = ClimbCrouch.PHASE_FROM_JUMP_TOP;
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_FROM_JUMP_TOP);
            }
            // Test if we're coming from a fall
            else if (mGrabMotion == 3)
            {
                mPhase = ClimbCrouch.PHASE_FROM_JUMP_FALL;
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_FROM_JUMP_FALL);
            }

            mController.State = lState;

            // Track the object we're trying to climb and store it
            mClimbable = sCollisionInfo.collider.gameObject;

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
            mGrabMotion = 0;

            // Reenable collisions
            UnityEngine.Physics.IgnoreCollision(mController.CharController.GetComponent<Collider>(), mClimbable.GetComponent<Collider>(), false);
            mClimbable = null;

            // Get out of the climb motion
            if (mPhase == ClimbCrouch.PHASE_IDLE)
            {
                mPhase = ClimbCrouch.PHASE_UNKNOWN;
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_UNKNOWN);
            }
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
            string lStateName = mController.GetAnimatorStateTransitionName(mAnimatorLayerIndex);

            // No y movement as we're finishing our climb
            if (lStateName == "ClimbCrouch-SM.ClimbCrouchRecoverIdle")
            {
                //rVelocityDelta.y = 0f;
            }

            // Some checks not needed since we baked the root motion
            if (lStateName == "AnyState -> ClimbCrouch-SM.JumpRiseToClimbCrouch" ||
                lStateName == "ClimbCrouch-SM.JumpFallToClimbCrouch" ||
                lStateName == "ClimbCrouch-SM.JumpImpactToClimbCrouch" ||
                lStateName == "ClimbCrouch-SM.JumpTopToClimbCrouch" ||
                lStateName == "ClimbCrouch-SM.ClimbCrouchRecoverIdle"
                )
            {
                rVelocityDelta.x = 0f;
                rVelocityDelta.z = 0f;
            }
            // Allow movement from the shimmy
            else if (IsInClimbShimmy)
            {
                rVelocityDelta.z = 0f;
                rVelocityDelta.y = 0f;
            }
            // However, when climbing up there is some forward motion
            else
            {
                rVelocityDelta.x = 0f;
            }
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public override void UpdateMotion()
        {
            if (Time.deltaTime == 0f) { return; }

            mVelocity = Vector3.zero;
            mAngularVelocity = Vector3.zero;

            string lStateName = mController.GetAnimatorStateName(mAnimatorLayerIndex);
            float lStateTime = mController.State.AnimatorStates[mAnimatorLayerIndex].StateInfo.normalizedTime;

            // Orient ourselves towards the anchor point
            if (!mHasArrived)
            {
                // Get the angular distance. We want there to be an initial 180-degree difference
                float lAngle = NumberHelper.GetHorizontalAngle(Vector3.forward, mController.transform.forward);

                // Find the desired angle we're going for
                float lWallAngle = NumberHelper.GetHorizontalAngle(Vector3.forward, mGrabPositionNormal);
                float lTargetWallAngle = (lAngle <= 0f ? lWallAngle - 180f : lWallAngle + 180f);

                // Convert the positions into velocity so they can be processed later
                Vector3 lNewPosition = Vector3.Lerp(mController.transform.position, mTargetPosition, mArrivalLerp);
                mVelocity = (lNewPosition - mController.transform.position) / Time.fixedDeltaTime;

                // We need to ensure our velocity doesn't exceed that of a normal jump
                mVelocity.y = Mathf.Min(mVelocity.y, 5.5f);

                // Convert the angle into a velocity so it can be processed later
                float lNewAngle = Mathf.LerpAngle(lAngle, lTargetWallAngle, mArrivalLerp);
                mAngularVelocity = new Vector3(0f, (lNewAngle - lAngle) / Time.fixedDeltaTime, 0f);
            }
            // If we're at the idle, allow the player to climb to the top
            else if (lStateName == "ClimbCrouch-SM.ClimbCrouchPose")
            {
                // Test if we should shimmy left
                if (mController.UseInput && InputManager.IsPressed("MoveLeft"))
                {
                    if (TestShimmy(-mMinSideSpaceForMove))
                    {
                        mPhase = ClimbCrouch.PHASE_SHIMMY_LEFT;
                        mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_SHIMMY_LEFT, true);
                    }
                }
                // Test if we should shimmy right
                else if (mController.UseInput && InputManager.IsPressed("MoveRight"))
                {
                    if (TestShimmy(mMinSideSpaceForMove))
                    {
                        mPhase = ClimbCrouch.PHASE_SHIMMY_RIGHT;
                        mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_SHIMMY_RIGHT, true);
                    }
                }
                // If the player is in control, test if we go to the top
                else if (mController.UseInput && (InputManager.IsJustPressed("Jump") || InputManager.IsPressed("MoveUp")))
                {
                    // As we start the climb to the top, disable the collision
                    UnityEngine.Physics.IgnoreCollision(mController.CharController.GetComponent<Collider>(), mClimbable.GetComponent<Collider>(), true);

                    // Start the movement to the top
                    mPhase = ClimbCrouch.PHASE_TO_TOP;
                    mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_TO_TOP);
                }
                // If the player is NOT in control, test if we should go to the top
                else if (!mController.UseInput && mController.State.InputY > 0f)
                {
                    // As we start the climb to the top, disable the collision
                    UnityEngine.Physics.IgnoreCollision(mController.CharController.GetComponent<Collider>(), mClimbable.GetComponent<Collider>(), true);

                    // Start the movement to the top
                    mPhase = ClimbCrouch.PHASE_TO_TOP;
                    mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_TO_TOP);
                }
                // Test if we should drop
                else if (mController.State.GroundDistance < mMinGroundDistance || (mController.UseInput && InputManager.IsJustPressed("Release")))
                {
                    // Start the drop
                    mPhase = ClimbCrouch.PHASE_TO_FALL;
                    mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_TO_FALL);
                }
            }
            // If we're in the middle of a shimmy, ensure we're sticking to the wall
            else if (IsInClimbShimmy)
            {
                // Get the angular distance. We want there to be an initial 180-degree difference
                float lAngle = NumberHelper.GetHorizontalAngle(Vector3.forward, mController.transform.forward);

                // Find the desired angle we're going for
                float lWallAngle = NumberHelper.GetHorizontalAngle(Vector3.forward, mGrabPositionNormal);
                float lTargetWallAngle = (lAngle <= 0f ? lWallAngle - 180f : lWallAngle + 180f);

                // Convert the angle into a velocity so it can be processed later
                float lNewAngle = Mathf.LerpAngle(lAngle, lTargetWallAngle, mArrivalLerp);
                mAngularVelocity = new Vector3(0f, (lNewAngle - lAngle) / Time.fixedDeltaTime, 0f);

                // We need to keep a fixed distance from the wall. So we'll do the
                // raycast and adjust our forward velocity to move us into the right position
                int lIsClimbableMask = 1 << mClimbableLayer;
                float lDistance = Mathf.Abs(mBodyTargetOffset.z);

                mVelocity = Vector3.zero;
                

                //TT if (UnityEngine.Physics.Raycast(mController.transform.position, mController.transform.forward, out sCollisionUpdateInfo, lDistance * mController.CharacterScale * 1.5f, lIsClimbableMask))
                if (mController.SafeRaycast(mController.transform.position, mController.transform.forward, ref sCollisionUpdateInfo, lDistance * mController.CharacterScale * 1.5f, lIsClimbableMask))
                {
                    mVelocity = (mController.transform.forward * (sCollisionUpdateInfo.distance - lDistance)) / Time.deltaTime;
                }

                // If we're rotating, we need to adjust the contact position. This way
                // we rotate correctly with the platform
                mAvatarContactPosition = Quaternion.Inverse(mClimbable.transform.rotation) * (mController.transform.position - mClimbable.transform.position);
            }
            // As we're climbing up to the top, we may want to add some extra velocity
            else if (lStateName == "ClimbCrouch-SM.ClimbCrouchToTop")
            {
                mVelocity = mToTopVelocity;

                // As we climb, fake the contact position
                mAvatarContactPosition = Quaternion.Inverse(mClimbable.transform.rotation) * (mController.transform.position - mClimbable.transform.position);
            }
            // Once we're at the top, we want to make sure there is no popping. So we'll force the
            // avatar to the right height
            else if (lStateName == "ClimbCrouch-SM.ClimbCrouchRecoverIdle")
            {
                //mController.ResetColliderHeight();

                // As we exit the final animation, move towards the exact position that the
                // following animation (usually idle) will match to.
                Vector3 lTargetPosition = mController.transform.position + (mController.transform.rotation * mExitPositionOffset);
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
                    //mAvatarContactPosition = Vector3.zero;
                }

                if (lStateTime >= 1.0f) { Deactivate(); }
            }
            // Drops us out of a climb so we can fall
            else if (lStateName == "ClimbCrouch-SM.ClimbCrouchToJumpFall")
            {
                // If we're in the second half of the release, we can stop and let the fall take over.
                if (lStateTime > 0.5f && mPhase != ClimbCrouch.PHASE_UNKNOWN)
                {
                    // Stop, but do not clear the GrabPosition. This way we don't
                    // grab the same spot or a spot too close to it.
                    Deactivate();

                    // Reset the motion state so we can move to fall
                    mPhase = ClimbCrouch.PHASE_UNKNOWN;
                    mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_UNKNOWN);
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

            // Determine if we've reached the goal
            float lDistance = Vector3.Distance(mController.transform.position, mTargetPosition);
            if (!mHasArrived && lDistance < 0.01f)
            {
                mHasArrived = true;

                // Get the angle we need to rotate to be lined up
                Vector3 lForward = Quaternion.AngleAxis(180, Vector3.up) * mGrabPositionNormal;
                float lYRotation = mController.transform.forward.HorizontalAngleTo(lForward);

                // Set the controller in the perfect spot and rotation
                mController.transform.Rotate(0f, lYRotation, 0f);
                mController.transform.position = mTargetPosition;

                // Track the contact position for orbiting
                mAvatarContactPosition = Quaternion.Inverse(lState.SupportRotation) * (mTargetPosition - lState.SupportPosition);

                // We need to set the previous values so that we don't attempt
                // to move the avatar this frame
                lPrevState.SupportPosition = mClimbable.transform.position;
                lPrevState.SupportRotation = mClimbable.transform.rotation;
                mController.PrevState = lPrevState;

                // Set the contact position based on the target posiion
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
                    // We need to increase how quickly we get to the anchor
                    // or we'll always be behind
                    mArrivalLerp = 1.0f;

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
        }

        /// <summary>
        /// Shoot rays to determine if a horizontal edge exists that
        /// we may be able to grab onto. It needs to be within the range
        /// of the avatar's feelers.
        /// </summary>
        /// <returns>Boolean that says if we've found an acceptable edge</returns>
        public virtual bool TestGrab(Vector3 rPosition, Quaternion rRotation, Vector3 rForward, float rRadius, float rBottom, float rTop)
        {
            Vector3 lRayStart = Vector3.zero;

            float lTargetDistance = -mBodyTargetOffset.z * 1.5f;

            // Only climb those things that are on the climbable layer
            int lIsClimbableMask = 1 << mClimbableLayer;
            
            // Root position for the test
            //Transform lRoot = mController.transform;

            // Determine the ray positions
            //float lEdgeTop = rTop;
            //float lEdgeBottom = rBottom;

            // Debug
            //Debug.DrawLine(lRoot.position + new Vector3(0f, lEdgeTop, 0f), lRoot.position + new Vector3(0f, lEdgeTop, 0f) + mController.transform.forward * lTargetDistance, Color.red);
            //Debug.DrawLine(lRoot.position + new Vector3(0f, lEdgeBottom, 0f), lRoot.position + new Vector3(0f, lEdgeBottom, 0f) + mController.transform.forward * lTargetDistance, Color.red);

            // Shoot forward and ensure below the edge is blocked
            lRayStart = rPosition + new Vector3(0f, rBottom, 0f);
            //TT if (!UnityEngine.Physics.Raycast(lRayStart, rForward, out sCollisionInfo, lTargetDistance * mController.CharacterScale, lIsClimbableMask))
            if (!mController.SafeRaycast(lRayStart, rForward, ref sCollisionInfo, lTargetDistance * mController.CharacterScale, lIsClimbableMask))
            {
                return false;
            }

            // Shoot forward and ensure above the edge is open
            lRayStart = rPosition + new Vector3(0f, rTop, 0f);
            //TT if (UnityEngine.Physics.Raycast(lRayStart, rForward, out sCollisionInfo, lTargetDistance * mController.CharacterScale, lIsClimbableMask))
            if (mController.SafeRaycast(lRayStart, rForward, ref sCollisionInfo, lTargetDistance * mController.CharacterScale, lIsClimbableMask))
            {
                return false;
            }

            // Now that we know there is an edge, determine it's exact position.
            // First, we sink into the collision point a tad. Then, we use our 
            // collision point and start above it (where the top ray failed). Finally,
            // we shoot a ray down
            lRayStart = sCollisionInfo.point + (rForward * 0.01f);
            lRayStart.y = rPosition.y + rTop;
            //TT if (!UnityEngine.Physics.Raycast(lRayStart, Vector3.down, out sCollisionInfo, rTop - rBottom + 0.01f, lIsClimbableMask))
            if (!mController.SafeRaycast(lRayStart, Vector3.down, ref sCollisionInfo, rTop - rBottom + 0.01f, lIsClimbableMask))
            {
                return false;
            }

            // Finally we shoot one last ray forward. We do this because we want the collision
            // data to be about the wall facing the avatar, not the wall facing the
            // last ray (which was shot down).
            lRayStart = rPosition;
            lRayStart.y = sCollisionInfo.point.y - 0.01f;
            //TT if (!UnityEngine.Physics.Raycast(lRayStart, rForward, out sCollisionInfo, lTargetDistance, lIsClimbableMask))
            if (!mController.SafeRaycast(lRayStart, rForward, ref sCollisionInfo, lTargetDistance, lIsClimbableMask))
            {
                return false;
            }

            // Test to make sure there's enough room between the grab point (at waist-ish level) and an object behind the player
            //TT if (UnityEngine.Physics.Raycast(sCollisionInfo.point + Vector3.down, sCollisionInfo.normal, rRadius * 3f, lIsClimbableMask))
            if (mController.SafeRaycast(sCollisionInfo.point + Vector3.down, sCollisionInfo.normal, rRadius * 3f, lIsClimbableMask))
            {
                return false;
            }

            // If we have hand positions, ensure that they collide with something as well. Otherwise,
            // the hand will look like it's floating in the air.
            if (mHandGrabOffset > 0)
            {
                // Check the right hand
                Vector3 lRightHandPosition = lRayStart + (rRotation * new Vector3(mHandGrabOffset, 0f, 0f));
                //TT if (!UnityEngine.Physics.Raycast(lRightHandPosition, rForward, lTargetDistance, lIsClimbableMask))
                if (!mController.SafeRaycast(lRightHandPosition, rForward, lTargetDistance, lIsClimbableMask))
                {
                    return false;
                }

                // Check the left hand
                Vector3 lLeftHandPosition = lRayStart + (rRotation * new Vector3(-mHandGrabOffset, 0f, 0f));
                //TT if (!UnityEngine.Physics.Raycast(lLeftHandPosition, rForward, lTargetDistance, lIsClimbableMask))
                if (!mController.SafeRaycast(lLeftHandPosition, rForward, lTargetDistance, lIsClimbableMask))
                {
                    return false;
                }
            }

            // If we got here, we found an edge
            return true;
        }

        /// <summary>
        /// Performs a test to determine if the character is able to move to the left or right
        /// </summary>
        /// <param name="rOffset">Side distance to check. Left is negative and right is posative.</param>
        /// <returns>Boolean that determerines is the avatar can move</returns>
        private bool TestShimmy(float rOffset)
        {
            float lSideDistance = Mathf.Abs(rOffset);
            Transform lRoot = mController.transform;

            // Shoot a ray to the left to see if there is room to move
            Vector3 lRayStart = lRoot.position;
            Vector3 lRayDirection = lRoot.rotation * new Vector3((rOffset < 0 ? -1 : 1), 0, 0);
            //TT if (UnityEngine.Physics.Raycast(lRayStart, lRayDirection, lSideDistance))
            if (mController.SafeRaycast(lRayStart, lRayDirection, lSideDistance))
            {
                return false;
            }

            // Shoot a ray forward from the future position to see if we can move there
            Vector3 lFuturePosition = lRoot.position + (lRoot.rotation * new Vector3(rOffset, 0, 0));
            if (!TestGrab(lFuturePosition, mController.transform.rotation, mController.transform.forward, mController.ColliderRadius, 1.0f * mController.CharacterScale, 1.5f * mController.CharacterScale))
            {
                return false;
            }

            // Track the object we're trying to climb and store it
            mClimbable = sCollisionInfo.collider.gameObject;

            // Set the grab position we're anchoring on
            mGrabPosition = sCollisionInfo.point;
            mLocalGrabPosition = Quaternion.Inverse(mClimbable.transform.rotation) * (sCollisionInfo.point - mClimbable.transform.position);

            // Set the grab normal coming out of the wall we're anchoring on
            mGrabPositionNormal = sCollisionInfo.normal;

            // Determine the target position given the grab info
            mTargetPosition = DetermineTargetPositions(ref mGrabPosition, ref mGrabPositionNormal);

            // If we got here, we can climb
            return true;
        }

        /// <summary>
        /// Raised when the animator's state has changed
        /// </summary>
        public override void OnAnimatorStateChange(int rLastStateID, int rNewStateID)
        {
            // Ensure we don't re-enter the 'any state'
            if (rNewStateID == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpRiseToClimbCrouch"] ||
                rNewStateID == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpFallToClimbCrouch"] ||
                rNewStateID == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpImpactToClimbCrouch"] ||
                rNewStateID == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpTopToClimbCrouch"])
            {
                mPhase = ClimbCrouch.PHASE_IDLE;
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_IDLE);
            }

            // If we've moved out the recovery state, reset the motion and state
            if (rLastStateID == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchRecoverIdle"])
            {
                // Clear out the climb info
                Deactivate();

                // Clear out the old position
                mGrabPosition = Vector3.zero;

                // Reset the motion state so we can move to fall
                mPhase = ClimbCrouch.PHASE_UNKNOWN;
                mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, ClimbCrouch.PHASE_UNKNOWN);
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
            lTargetPosition.x += -mBodyTargetOffset.z * Mathf.Sin(lWallAngle * Mathf.Deg2Rad);
            lTargetPosition.y += mBodyTargetOffset.y;
            lTargetPosition.z += -mBodyTargetOffset.z * Mathf.Cos(lWallAngle * Mathf.Deg2Rad);

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
                AnimatorStateInfo lAnimatorState = mController.State.AnimatorStates[mAnimatorLayerIndex].StateInfo;
                AnimatorTransitionInfo lAnimatorTransition = mController.State.AnimatorStates[mAnimatorLayerIndex].TransitionInfo;

                if (lAnimatorTransition.nameHash == mController.AnimatorStateIDs["AnyState -> ClimbCrouch-SM.JumpRiseToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.IdleToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpRiseToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpTopToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpFallToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpImpactToClimbCrouch"]
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
                if (mController.CompareAnimatorStateName(mAnimatorLayerIndex, "ClimbCrouch-SM.ClimbCrouchPose"))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently in the climb state
        /// </summary>
        protected bool IsInClimbState
        {
            get
            {
                AnimatorStateInfo lAnimatorState = mController.State.AnimatorStates[mAnimatorLayerIndex].StateInfo;
                AnimatorTransitionInfo lAnimatorTransition = mController.State.AnimatorStates[mAnimatorLayerIndex].TransitionInfo;

                if (lAnimatorTransition.nameHash == mController.AnimatorStateIDs["AnyState -> ClimbCrouch-SM.JumpRiseToClimbCrouch"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["AnyState -> ClimbCrouch-SM.IdleToClimbCrouch"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["AnyState -> ClimbCrouch-SM.JumpTopToClimbCrouch"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["AnyState -> ClimbCrouch-SM.JumpFallToClimbCrouch"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["AnyState -> ClimbCrouch-SM.JumpImpactToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.IdleToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpRiseToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpTopToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpFallToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.JumpImpactToClimbCrouch"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchPose"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchToTop"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchRecoverIdle"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchToJumpFall"] ||

                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyLeft"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyRight"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchShimmyLeft"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchShimmyRight"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyLeft -> ClimbCrouch-SM.ClimbCrouchPose"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyRight -> ClimbCrouch-SM.ClimbCrouchPose"]
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Determine if we're shimmy-ing left or right
        /// </summary>
        protected bool IsInClimbShimmy
        {
            get
            {
                AnimatorStateInfo lAnimatorState = mController.State.AnimatorStates[mAnimatorLayerIndex].StateInfo;
                AnimatorTransitionInfo lAnimatorTransition = mController.State.AnimatorStates[mAnimatorLayerIndex].TransitionInfo;

                if (lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyLeft"] ||
                    lAnimatorState.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyRight"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchShimmyLeft"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchPose -> ClimbCrouch-SM.ClimbCrouchShimmyRight"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyLeft -> ClimbCrouch-SM.ClimbCrouchPose"] ||
                    lAnimatorTransition.nameHash == mController.AnimatorStateIDs["ClimbCrouch-SM.ClimbCrouchShimmyRight -> ClimbCrouch-SM.ClimbCrouchPose"]                    
                    )
                {
                    return true;
                }

                return false;
            }
        }
    }
}

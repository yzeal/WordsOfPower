//#define MC_ENABLE_PROFILING 

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Physics;
using com.ootii.Utilities;
using com.ootii.Utilities.Debug;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// The Motion Controller is built to manage character actions
    /// like run, jump, climb, fight, etc. We use layers which hold motions
    /// in order to manage the controller's state.
    /// 
    /// Mechanim's animator is still critical to the process.
    /// </summary>
    public class MotionController : Controller
    {
        /// <summary>
        /// Distance the avatar's feed are from the ground before
        /// it is considered on the ground.
        /// </summary>
        public const float GROUND_DISTANCE_TEST = 0.075f;

        /// <summary>
        /// Keeps us from reallocating each frame
        /// </summary>
        private static RaycastHit sGroundCollisionInfo = new RaycastHit();

        /// <summary>
        /// Keeps us from reallocating each frame
        /// </summary>
        private static RaycastHit sRaycastHitInfo = new RaycastHit();

        /// <summary>
        /// Layer the owner is attached to so we can ignore it when it
        /// </summary>
        public int _PlayerLayer = 8;
        public int PlayerLayer
        {
            get { return _PlayerLayer; }
            set { _PlayerLayer = value; }
        }

        /// <summary>
        /// Determines if the controller used input from the keyboard, game
        /// controller, etc. to control it.
        /// </summary>
        public bool _UseInput = false;
        public bool UseInput
        {
            get { return _UseInput; }
            set { _UseInput = value; }
        }

        /// <summary>
        /// Gravity to be used with this avatar. 
        /// </summary>
        public Vector3 _Gravity = new Vector3(0f, -11.81f, 0f);
        public Vector3 Gravity
        {
            get { return _Gravity; }
            set { _Gravity = value; }
        }

        /// <summary>
        /// Mass that we apply in physics calculations
        /// </summary>
        public float _Mass = 5f;
        public float Mass
        {
            get { return _Mass; }
            set { _Mass = value; }
        }

        /// <summary>
        /// The minimum angle of the ground that causes the avatar
        /// to start sliding
        /// </summary>
        public float _MinSlideAngle = 25f;
        public float MinSlideAngle
        {
            get { return _MinSlideAngle; }
            set { _MinSlideAngle = value; }
        }

        /// <summary>
        /// Determines if the controller can go into the stance
        /// </summary>
        public bool _MeleeStanceEnabled = true;
        public bool MeleeStanceEnabled
        {
            get { return _MeleeStanceEnabled; }
            set { _MeleeStanceEnabled = value; }
        }

        /// <summary>
        /// Determines if the controller can go into the stance
        /// </summary>
        public bool _TargetingStanceEnabled = true;
        public bool TargetingStanceEnabled
        {
            get { return _TargetingStanceEnabled; }
            set { _TargetingStanceEnabled = value; }
        }

        /// <summary>
        /// Degrees per second to rotate the player when in the
        /// targeting stance
        /// </summary>
        public float _TargetingStanceRotationSpeed = 90f;
        public float TargetingStanceRotationSpeed
        {
            get { return _TargetingStanceRotationSpeed; }
            set { _TargetingStanceRotationSpeed = value; }
        }

        /// <summary>
        /// Offset from the controller's root position that the camera
        /// is attempting to follow. Typically this is the head or eye height.
        /// </summary>
        public Vector3 _CameraRigOffset = new Vector3(0f, 1.7f, 0f);
        public Vector3 CameraRigOffset
        {
            get { return _CameraRigOffset; }
            set { _CameraRigOffset = value; }
        }

        /// <summary>
        /// Lerp factor for determining how quickly the camera follows
        /// the avatar as it goes up (typically in a jump)
        /// </summary>
        public float _CameraRigUpLerp = 0.03f;
        public float CameraRigUpLerp
        {
            get { return _CameraRigUpLerp; }
            set { _CameraRigUpLerp = value; }
        }

        /// <summary>
        /// Lerp factor for determining how quickly the camera follows
        /// the avatar as it goes down (typically in a fall)
        /// </summary>
        public float _CameraRigDownLerp = 0.9f;
        public float CameraRigDownLerp
        {
            get { return _CameraRigDownLerp; }
            set { _CameraRigDownLerp = value; }
        }

        /// <summary>
        /// This is the position that the camera is attempting to move
        /// towards. It's the default position of the camera.
        /// </summary>
        protected Vector3 mCameraRigAnchor = Vector3.zero;
        public override Vector3 CameraRigAnchor
        {
            get
            {
                // Get the default anchor position
                Vector3 lAnchor = transform.position + (transform.rotation * _CameraRigOffset);

                // Process any motions that are active. 
                for (int i = 0; i < MotionLayers.Count; i++)
                {
                    lAnchor += MotionLayers[i].CameraOffset;
                }

                // The lerp factor provides a smooth jump up with a harder fall
                float lLerpFactor = (mCameraRigAnchor.y < lAnchor.y ? _CameraRigUpLerp : _CameraRigDownLerp);

                mCameraRigAnchor.x = lAnchor.x;
                mCameraRigAnchor.y = Mathf.Lerp(mCameraRigAnchor.y, lAnchor.y, lLerpFactor);
                mCameraRigAnchor.z = lAnchor.z;

                // Return the final anchor position
                return mCameraRigAnchor;
            }
        }

        /// <summary>
        /// Radius of the collider surrounding the controller
        /// </summary>
        public override float ColliderRadius
        {
            get { return mCharController.radius; }
        }

        /// <summary>
        /// Defines the height and distance of the forward bumper
        /// </summary>
        public Vector3 _ForwardBumper = new Vector3(0, 0.4f, 0.4f);
        public Vector3 ForwardBumper
        {
            get { return _ForwardBumper; }
            set { _ForwardBumper = value; }
        }

        /// <summary>
        /// When the forward bumper hits something, this is the minimum
        /// angle the character's forward needs to be from the wall's normal
        /// before the player is stopped.
        /// </summary>
        public float _ForwardBumperBlendAngle = 40f;
        public float ForwardBumperBlendAngle
        {
            get { return _ForwardBumperBlendAngle; }
            set { _ForwardBumperBlendAngle = value; }
        }

        /// <summary>
        /// Sets the height of the collider that is part of the
        /// controller. The key is to keep it at the same base.
        /// </summary>
        public float ColliderHeight
        {
            get { return mCharController.height; }
            set { mCharController.height = value; }
        }

        /// <summary>
        /// Sets the center of the collider that is part of the controller.
        /// </summary>
        public Vector3 ColliderCenter
        {
            get { return mCharController.center; }
            set { mCharController.center = value; }
        }

        /// <summary>
        /// The controller is built to the scale of a normal human. For
        /// things that require ray casts and correct sizing, having a
        /// different world scale could cause problems.
        /// </summary>
        public float CharacterScale
        {
            get { return gameObject.transform.localScale.y * (mBaseColliderHeight / 1.8f); }
        }

        /// <summary>
        /// Defines the max speed of the actor when in a full run
        /// </summary>
        public float _MaxSpeed = 7f;
        public float MaxSpeed
        {
            get { return _MaxSpeed; }
            set { _MaxSpeed = value; }
        }

        /// <summary>
        /// Determines how quickly the character is able to rotate
        /// </summary>
        public float _RotationSpeed = 360f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// When simulating input, this gives us a velocity 
        /// the controller uses to move with. The controller converts this information
        /// into psuedo input values to calculate movement.
        /// </summary>
        protected Vector3 mTargetVelocity = Vector3.zero;
        public Vector3 TargetVelocity
        {
            get { return mTargetVelocity; }
        }

        /// <summary>
        /// When simulating input, this gives us a target to move
        /// the controller to. The controller converts this information
        /// into psuedo input values to calculate movement.
        /// </summary>
        protected Vector3 mTargetPosition = Vector3.zero;
        public Vector3 TargetPosition
        {
            get { return mTargetPosition; }
        }

        /// <summary>
        /// When simulating input, this gives us the speed at which we
        /// should move towards the PositionTarget. Think of this as how
        /// hard we push the gamepad stick forward.
        /// </summary>
        protected float mTargetNormalizedSpeed = 1f;
        public float TargetNormalizedSpeed
        {
            get { return mTargetNormalizedSpeed; }
        }

        /// <summary>
        /// When simulating input, this gives us a target to rotate
        /// the controller to. The controller converts this information
        /// into psuedo input values to calculate rotation.
        /// </summary>
        protected Quaternion mTargetRotation = Quaternion.identity;
        public Quaternion TargetRotation
        {
            get { return mTargetRotation; }
        }

        /// <summary>
        /// Reports back the grounded state of the avatar
        /// </summary>
        public bool IsGrounded
        {
            get { return mState.IsGrounded; }
        }

        /// <summary>
        /// Determines if we're expected to be grounded or not
        /// </summary>
        public bool IsGroundedExpected
        {
            get
            {
                bool lResult = true;

                // Check each layer. Really we should have this determined
                // in the first layer, but any of them could be set
                for (int i = 0; i < MotionLayers.Count; i++)
                {
                    if (MotionLayers[i].ActiveMotion != null && !MotionLayers[i].ActiveMotion.IsGroundedExpected)
                    {
                        lResult = false;
                        break;
                    }
                }

                // Return the results
                return lResult;
            }
        }

        /// <summary>
        /// Reports back the current ground distance
        /// </summary>
        public float GroundDistance
        {
            get { return mState.GroundDistance; }
        }

        /// <summary>
        /// Set the stance the character is currently in
        /// </summary>
        public int Stance
        {
            get { return mState.Stance; }
            set { mState.Stance = value; }
        }

        /// <summary>
        /// Returns the inverted layer mask the player is
        /// set to (this is layer 8). We use this for ignoring ray tests
        /// </summary>
        public int PlayerMask
        {
            get
            {
                // First, determine the height from the ground. To do this, ignore
                // the player layer (layer index 8). Invert the bitmask so we get
                // everything EXCEPT the player
                int lPlayerLayerMask = 1 << _PlayerLayer;
                lPlayerLayerMask = ~lPlayerLayerMask;

                return lPlayerLayerMask;
            }
        }

        /// <summary>
        /// The character controller tied to the avatar
        /// </summary>
        protected CharacterController mCharController = null;
        public CharacterController CharController
        {
            get { return mCharController; }
        }

        /// <summary>
        /// Animator that the controller works with
        /// </summary>
        protected Animator mAnimator = null;
        public Animator Animator
        {
            get { return mAnimator; }
        }

        /// <summary>
        /// The current state of the controller including speed, direction, etc.
        /// </summary>
        protected ControllerState mState = new ControllerState();
        public ControllerState State
        {
            get { return mState; }
            set { mState = value; }
        }

        /// <summary>
        /// The previous state of the controller including speed, direction, etc.
        /// </summary>
        protected ControllerState mPrevState = new ControllerState();
        public ControllerState PrevState
        {
            get { return mPrevState; }
            set { mPrevState = value; }
        }

        /// <summary>
        /// Angles at which we limit forward rotation
        /// </summary>
        protected float mForwardHeadingLimit = 80f;
        public float ForwardHeadingLimit
        {
            get { return mForwardHeadingLimit; }
            set { mForwardHeadingLimit = value; }
        }

        /// <summary>
        /// Angles at which we limit backward rotation
        /// </summary>
        protected float mBackwardsHeadingLimit = 50f;
        public float BackwardsHeadingLimit
        {
            get { return mBackwardsHeadingLimit; }
            set { mBackwardsHeadingLimit = value; }
        }

        /// <summary>
        /// Contains a list of forces currently being applied to
        /// the controller.
        /// </summary>
        protected List<Force> mAppliedForces = new List<Force>();
        public List<Force> AppliedForces
        {
            get { return mAppliedForces; }
            set { mAppliedForces = value; }
        }

        /// <summary>
        /// List of motions the avatar is able to perform.
        /// </summary>
        public List<MotionControllerLayer> MotionLayers = new List<MotionControllerLayer>();

        /// <summary>
        /// Initial collider height used to base the collisions on
        /// </summary>
        protected float mBaseColliderHeight = 1.8f;
        public float BaseColliderHeight
        {
            get { return mBaseColliderHeight; }
            set { mBaseColliderHeight = value; }
        }

        /// <summary>
        /// Starting collider center used to base the collider at
        /// </summary>
        protected Vector3 mBaseColliderCenter = new Vector3(0, 0.925f, 0);

        /// <summary>
        /// The current speed trend decreasing, static, increasing (-1, 0, or 1)
        /// </summary>
        private int mSpeedTrendDirection = EnumSpeedTrend.CONSTANT;

        /// <summary>
        /// Add a delay before we update the mecanim parameters. This way we can
        /// give a chance for things like speed to settle.
        /// </summary>
        private float mMecanimUpdateDelay = 0f;

        /// <summary>
        /// Time (in seconds) for the acceleration to drop to 0 when grounded
        /// </summary>
        private float mGroundDragDuration = 0.1f;

        /// <summary>
        /// Acceleration that is being processed per frame. It takes into account the
        /// forces applied and drag.
        /// </summary>
        private Vector3 mAccumulatedAcceleration = Vector3.zero;
        public Vector3 AccumulatedAcceleration
        {
            get { return mAccumulatedAcceleration; }
        }

        /// <summary>
        /// Use this to store up velocity over time
        /// </summary>
        private Vector3 mAccumulatedVelocity = Vector3.zero;
        public Vector3 AccumulatedVelocity
        {
            get { return mAccumulatedVelocity; }
            set { mAccumulatedVelocity = value; }
        }

        /// <summary>
        /// Tracks the root motion so we can apply it later
        /// </summary>
        private Vector3 mRootMotionVelocity = Vector3.zero;
        public Vector3 RootMotionVelocity
        {
            get { return mRootMotionVelocity; }
            set { mRootMotionVelocity = value; }
        }

        private Vector3Value mRootMotionVelocityAvg = new Vector3Value();
        public Vector3Value RootMotionVelocityAvg
        {
            get { return mRootMotionVelocityAvg; }
            set { mRootMotionVelocityAvg = value; }
        }

        /// <summary>
        /// Tracks the root motion rotation so we can apply it later
        /// </summary>
        private Quaternion mRootMotionAngularVelocity = Quaternion.identity;
        public Quaternion RootMotionAngularVelocity
        {
            get { return mRootMotionAngularVelocity; }
            set { mRootMotionAngularVelocity = value; }
        }

        /// <summary>
        /// Tracks how long the update process is taking
        /// </summary>
        private static Utilities.Profiler mUpdateProfiler = new Utilities.Profiler("MotionController");
        public static Utilities.Profiler UpdateProfiler
        {
            get { return mUpdateProfiler; }
        }

        /// <summary>
        /// Stores the animator state names by hash-id
        /// </summary>	
        [HideInInspector]
        public Dictionary<int, string> AnimatorStateNames = new Dictionary<int, string>();

        /// <summary>
        /// Stores the animator hash-ids by state name
        /// </summary>
        [HideInInspector]
        public Dictionary<string, int> AnimatorStateIDs = new Dictionary<string, int>();

        /// <summary>
        /// Flag to let us know if the rotation and movement
        /// values have been applied to the avatar.
        /// </summary>
        private bool mMovementApplied = false;

        /// <summary>
        /// Called right before the first frame update
        /// </summary>
        public void Start()
        {
            // Initialize the camera if possible
            if (_CameraRig == null && _CameraTransform != null)
            {
                GameObject lCameraRigObject = null;

                // Attempt to get the game object holding our rig script
                if (_CameraTransform.parent != null)
                {
                    lCameraRigObject = _CameraTransform.parent.gameObject;
                }
                else if (_CameraTransform.gameObject != null)
                {
                    lCameraRigObject = _CameraTransform.gameObject;
                }

                // Grab the rig script
                if (lCameraRigObject != null)
                {
                    _CameraRig = lCameraRigObject.GetComponent<CameraRig>();
                }
            }

            // Ensure we're using the right transform
            if (_CameraRig != null)
            {
                _CameraTransform = _CameraRig.transform;
            }

            // Initialize the camera anchor position
            mCameraRigAnchor = transform.position + _CameraRigOffset;

            // Grab the character controller assigned 
            mCharController = GetComponent<CharacterController>();
            mBaseColliderHeight = mCharController.height;
            mBaseColliderCenter = mCharController.center;

            // If a bumper is requested, and missing defaults, create them
            if (_ForwardBumper.sqrMagnitude > 0f)
            {
                if (mCharController != null && _ForwardBumper.y == 0f) { _ForwardBumper.y = mCharController.stepOffset + 0.1f; }
                if (mCharController != null && _ForwardBumper.z == 0f) { _ForwardBumper.z = mCharController.radius + 0.1f; }
            }

            // Load the animator and grab all the state info
            mAnimator = GetComponent<Animator>();

            // Build the list of available layers
            if (mAnimator != null)
            {
                mState.AnimatorStates = new AnimatorLayerState[mAnimator.layerCount];
                mPrevState.AnimatorStates = new AnimatorLayerState[mAnimator.layerCount];

                // Initialize our objects with each of the animator layers
                for (int i = 0; i < mState.AnimatorStates.Length; i++)
                {
                    mState.AnimatorStates[i] = new AnimatorLayerState();
                    mPrevState.AnimatorStates[i] = new AnimatorLayerState();
                }
            }

            // Load the animator state and transition hash IDs
            LoadAnimatorData();
        }

        /// <summary>
        /// Called once per frame to update objects. This happens after FixedUpdate().
        /// Reactions to calculations should be handled here.
        /// </summary>
        public void Update()
        {
            // Start the timer for tracking performance
            mUpdateProfiler.Start();

            // Determines if we wait for a trend to stop before
            // passing information to the animator
            bool lUseTrendData = false;

            // Reset our movement flag
            mMovementApplied = false;

            // 1. Shift the current state to previous and initialize the current
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("01");
#endif
            ControllerState.Shift(ref mState, ref mPrevState);
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("01");
#endif

            // 2. Update the animator state and transition information so it can by
            // used by the motions
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("02");
#endif
            int lCount = 0;

            if (mAnimator != null)
            {
                lCount = mState.AnimatorStates.Length;
                for (int i = 0; i < lCount; i++)
                {
                    mState.AnimatorStates[i].StateInfo = mAnimator.GetCurrentAnimatorStateInfo(i);
                    mState.AnimatorStates[i].TransitionInfo = mAnimator.GetAnimatorTransitionInfo(i);

                    if (mState.AnimatorStates[i].StateInfo.nameHash != mPrevState.AnimatorStates[i].StateInfo.nameHash)
                    {
                        OnAnimatorStateChange(i);
                    }
                }
            }

#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("02");
#endif

            // 3. Test if we're grounded or not
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("03");
#endif
            DetermineGrounding();
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("03");
#endif

            // 4. Grab the direction and speed of the input from the keyboard, game controller, etc.
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("04");
#endif
            if (_UseInput)
            {
                ProcessInput();
            }
            // Otherwise, use the target movement and rotation from AI to simulate movement
            else
            {
                SimulateInput();
            }
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("04");
#endif

            // 5. Clean the existing root motion so we don't have motion we don't want
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("05");
#endif
            CleanRootMotion();
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("05");
#endif

            // 6. Update each layer to determine the final velocity and rotation
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("06");
#endif
            lCount = MotionLayers.Count;
            for (int i = 0; i < lCount; i++)
            {
                MotionLayers[i].UpdateMotions();
                if (MotionLayers[i].UseTrendData) { lUseTrendData = true; }
            }
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("06");
#endif

            // 7. Determine the trend so we can figure out acceleration
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("07");
#endif
            DetermineTrendData();
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("07");
#endif

            // When we update the controller here, things are smooth and in synch
            // with the camera. If we put this code in the FixedUpdate() or OnAnimateMove()
            // the camera is out of synch with the camera (in LateUpdate()) and avatar stutters.
            //
            // We need the camera in LateUpdate() since this is where it's smoothest and 
            // preceeds for each draw call.
            
            // 8. Apply rotation
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("08");
#endif
            ApplyRotation(Time.deltaTime);
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("08");
#endif

            // 9. Apply translation
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("09");
#endif
            ApplyMovement(Time.deltaTime);
            mMovementApplied = true;
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("09");
#endif

            // 10. There may be cases where the avatar can get stuck in the environment.
            // We'll use this logic to undo that
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("10");
#endif
            FreeColliderFromEdge();
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("10");
#endif

            // 11. Send the current state data to the animator
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Start("11");
#endif
           SetAnimatorProperties(mState, lUseTrendData);
#if MC_ENABLE_PROFILING
            Utilities.Profiler.Stop("11");
#endif

            // Stop the timer
            mUpdateProfiler.Stop();

            // Write out debug info
            if (_UseInput)
            {
                Log.ScreenWrite(String.Format("AC.Update() Time:{0:f4}ms {1} Mx:{2:f4} My:{3:f4} Vx:{4:f4} Vy:{5:f4} MCTime:{6:f4}ms", Time.deltaTime, CurrentMotionName, InputManager.MovementX, InputManager.MovementY, InputManager.ViewX, InputManager.ViewY, mUpdateProfiler.Time), 2);
                //Log.ScreenWrite(String.Format("AC.Update() IsGrd:{0} GrdDist:{1:f4} IsFwdBlocked:{2} Pos:{3} Vel:{4}", mState.IsGrounded, mState.GroundDistance, mState.IsForwardPathBlocked, StringHelper.ToString(transform.position), StringHelper.ToString(mState.Velocity)), 3);
                //Log.ScreenWrite(String.Format("AC.Update() Motion:{0} MotionDur:{1:f4} StPhase:{2:f4} Anim:{3} AT:{4}", CurrentMotionName, CurrentMotionDuration, mState.AnimatorStates[0].MotionPhase, AnimatorHashToString(mState.AnimatorStates[0].StateInfo.nameHash, mState.AnimatorStates[0].TransitionInfo.nameHash), mState.AnimatorStates[0].StateInfo.normalizedTime), 4);

                //Log.FileWrite("AC.Update() DT:" + Time.deltaTime.ToString("0.000000") + " IsG:" + mState.IsGrounded + "|" + mCharController.isGrounded + " GD:" + mState.GroundDistance.ToString("0.000") + " IMag:" + mState.InputMagnitudeTrend.Value.ToString("0.000") + " IMagAvg:" + mState.InputMagnitudeTrend.Average.ToString("0.000") +  " CamMode:" + _CameraRig.Mode + " CAngle:" + mState.InputFromCameraAngle.ToString("0.000") + " AAngle:" + mState.InputFromAvatarAngle.ToString("0.000") + " Motion:" + CurrentMotionName + " Phase:" + GetAnimatorMotionPhase(0) + " Pos:" + StringHelper.ToString(transform.position) + " Vel:" + StringHelper.ToString(mState.Velocity) + " AccVel:" + StringHelper.ToString(mAccumulatedVelocity) + " Rot:" + StringHelper.ToString(transform.rotation) + " Anim:" + AnimatorHashToString(mState.AnimatorStates[0].StateInfo.nameHash, mState.AnimatorStates[0].TransitionInfo.nameHash) + " AT:" + mState.AnimatorStates[0].StateInfo.normalizedTime.ToString("0.000") + " TT:" + mState.AnimatorStates[0].TransitionInfo.normalizedTime.ToString("0.000") + " RM-Vel:" + StringHelper.ToString(mRootMotionVelocity));

#if MC_ENABLE_PROFILING
                if (InputManager.IsJustPressed(KeyCode.P))
                {
                    Log.FileWrite(Utilities.Profiler.ToString(""));
                }
#endif
            }
        }

        /// <summary>
        /// LateUpdate is called once per frame after all Update() functions have
        /// finished.. Things (like a follow camera) that rely on objects updating 
        /// themselves first before they update should be placed here.
        /// </summary>
        public void LateUpdate()
        {
            // Update each layer to determine the final support updates
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                MotionLayers[i].LateUpdateMotions();
            }

            // If we're grounded (on the ground, climbing, etc), we may need to apply the velocity of the ground
            if (mState.Support != null && (mState.Support == mPrevState.Support))
            {
                Vector3 lSupportMove = Vector3.zero;

                if (mState.Support.GetComponent<Rigidbody>() != null)
                {
                    mState.SupportPosition = mState.Support.GetComponent<Rigidbody>().position;
                    mState.SupportRotation = mState.Support.GetComponent<Rigidbody>().rotation;
                }
                else
                {
                    mState.SupportPosition = mState.Support.transform.position;
                    mState.SupportRotation = mState.Support.transform.rotation;
                }

                // Test if the support has moved. Note that we may be a frame behind. Technically this is
                // best done in LateUpdate() after the support has updated, but we don't want to get ahead of the camera.
                if (mState.SupportPosition != mPrevState.SupportPosition)
                {
                    lSupportMove = mState.SupportPosition - mPrevState.SupportPosition;
                }

                // Test if the support has rotated. Note that we may be a frame behind. Technically this is
                // best done in LateUpdate() after the support has updated, but we don't want to get ahead of the camera.
                if (Quaternion.Angle(mState.SupportRotation, mPrevState.SupportRotation) != 0f)
                {
                    // Rotate the avatar
                    Quaternion lDeltaRotation = mPrevState.SupportRotation.RotationTo(mState.SupportRotation);
                    transform.Rotate(0f, lDeltaRotation.eulerAngles.y, 0f);

                    // Orbit the support
                    lSupportMove += (mState.SupportRotation * mState.SupportContactPosition) - (mPrevState.SupportRotation * mState.SupportContactPosition);
                }

                // Combine the values to create the support velocity
                if (lSupportMove.sqrMagnitude != 0f) 
                { 
                    mCharController.Move(lSupportMove);
                }
            }

            // If there was movement, clear out any possible contact point so it can be re calculated
            if (mState.Support != null && mState.Velocity.HorizontalMagnitude() > 0f)
            {
                mState.SupportContactPosition = Vector3.zero;
            }

            // If this is the character being controlled, update the camera here 
            // after the controller has totally finished moving
            if (_UseInput && _CameraRig != null)
            {
                _CameraRig.PostControllerLateUpdate();
            }
        }

        /// <summary>
        /// Performs a raycast down from the player to see if there
        /// is any ground directly below the player.
        /// </summary>
        /// <returns>boolean that says if there is a ground location</returns>
        /// <param name="rCollisionInfo">Collision Info about the potential collision</param>
        public float GroundCast(ref RaycastHit rCollisionInfo)
        {
            float lDistance = float.MaxValue;

            // Create an adjusted player position so we can hit the ground
            Vector3 lRayStart = mCharController.transform.position + (mCharController.transform.rotation * mCharController.center);

            // Test the collision and set the ground distance
            if (SafeRaycast(lRayStart, Vector3.down, ref rCollisionInfo, 10f))
            {
                lDistance = rCollisionInfo.distance - mCharController.center.y;
            }

            return lDistance;
        }

        /// <summary>
        /// Resets the collider height to the base height
        /// </summary>
        public void ResetColliderHeight()
        {
            SetColliderHeightAtBase(mBaseColliderHeight);
        }

        /// <summary>
        /// Allows us to change the collider height, but also to
        /// center it on the base avatar position as well.
        /// </summary>
        /// <param name="rHeight"></param>
        public void SetColliderHeightAtCenter(float rHeight)
        {
            if (rHeight == mCharController.height) { return; }

            mCharController.height = rHeight;

            Vector3 lCenter = mCharController.center;
            lCenter.y = (mBaseColliderHeight / 2f);

            mCharController.center = lCenter;
        }

        /// <summary>
        /// Allows us to change the collider height, but based on it's bottom
        /// being at the avatar's feed 
        /// </summary>
        /// <param name="rHeight"></param>
        public void SetColliderHeightAtBase(float rHeight)
        {
            if (rHeight == mCharController.height) { return; }

            mCharController.height = rHeight;

            Vector3 lCenter = mCharController.center;
            lCenter.y = (mCharController.height / 2f);

            mCharController.center = lCenter;
        }

        /// <summary>
        /// Applies an instant force. As an impulse, the force of a full second
        /// is automatically applied. The resulting impulse is Force / delta-time.
        /// The impulse is immediately removed after being applied.
        /// </summary>
        /// <param name="rForce">Force including direction and magnitude</param>
        public void AddImpulse(Vector3 rForce)
        {
            if (mAppliedForces == null) { mAppliedForces = new List<Force>(); }

            // Convert the force into an impulse (using a reliable delta time)
            rForce = rForce / Time.fixedDeltaTime;

            // Test if we can apply the impulse this frame directly
            // to the velocity
            if (!mMovementApplied)
            {
                Vector3 lAcceleration = rForce / _Mass;

                // Again, use a reliable delta time to add the instant velocity change.
                // If we don't, the drops and spikes in time could start us off bad.
                // This is the problem with not using FixedUpdate()
                mAccumulatedVelocity += lAcceleration * Time.fixedDeltaTime;
            }
            // If not, we'll apply it next frame
            else
            {
                Force lForce = Force.Allocate();
                lForce.Type = ForceMode.Impulse;
                lForce.Value = rForce;
                lForce.StartTime = Time.time;
                lForce.Duration = 0f;

                mAppliedForces.Add(lForce);
            }
        }

        /// <summary>
        /// Applies a continual force to the avatar.
        /// </summary>
        /// <param name="rForce">Force including direction and magnitude</param>
        public void AddForce(Vector3 rForce)
        {
            if (mAppliedForces == null) { mAppliedForces = new List<Force>(); }

            Force lForce = Force.Allocate();
            lForce.Type = ForceMode.Force;
            lForce.Value = rForce;
            lForce.StartTime = Time.time;
            lForce.Duration = 0f;

            mAppliedForces.Add(lForce);
        }

        /// <summary>
        /// Applies a force to the avatar over time. 
        /// </summary>
        /// <param name="rForce">Force including direction and magnitude</param>
        /// <param name="rDuration">Number of seconds to apply the force for (0f is infinite)</param>
        public void AddForce(Vector3 rForce, float rDuration)
        {
            if (mAppliedForces == null) { mAppliedForces = new List<Force>(); }

            Force lForce = Force.Allocate();
            lForce.Type = ForceMode.Force;
            lForce.Value = rForce;
            lForce.StartTime = Time.time;
            lForce.Duration = rDuration;

            mAppliedForces.Add(lForce);
        }

        /// <summary>
        /// Remove the force(s) we find matching the value
        /// </summary>
        /// <param name="rForce">Force value to remove</param>
        /// <param name="rRemoveAll">Determines if we remove all matching forces or the first one</param> 
        public void RemoveForce(Vector3 rForce, bool rRemoveAll)
        {
            for (int i = mAppliedForces.Count - 1; i >= 0; i--)
            {
                Force lForce = mAppliedForces[i];
                if (lForce.Value == rForce)
                {
                    mAppliedForces.RemoveAt(i);
                    if (!rRemoveAll) { return; }
                }
            }
        }

        /// <summary>
        /// Return the first motion in a layer that matches the specific motion
        /// type.
        /// </summary>
        /// <param name="rLayerIndex">Layer to look through</param>
        /// <param name="rType">Type of controller motion to look for</param>
        /// <returns>Returns reference to the first motion matching the type or null if not found</returns>
        public MotionControllerMotion GetMotion(int rLayerIndex, Type rType)
        {
            if (rLayerIndex >= MotionLayers.Count) { return null; }

            for (int i = 0; i < MotionLayers[rLayerIndex].Motions.Count; i++)
            {
                if (MotionLayers[rLayerIndex].Motions[i].GetType() == rType)
                {
                    return MotionLayers[rLayerIndex].Motions[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Return the first motion in a layer that matches the specific motion
        /// type.
        /// </summary>
        /// <param name="rType">Type of controller motion to look for</param>
        /// <returns>Returns reference to the first motion matching the type or null if not found</returns>
        public MotionControllerMotion GetMotion(Type rType)
        {
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                for (int j = 0; j < MotionLayers[i].Motions.Count; j++)
                {
                    if (MotionLayers[i].Motions[j].GetType() == rType)
                    {
                        return MotionLayers[i].Motions[j];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Return the first motion in a layer that matches the specific motion
        /// type.
        /// </summary>
        /// <param name="rName">Name of controller motion to look for</param>
        /// <returns>Returns reference to the first motion matching the type or null if not found</returns>
        public MotionControllerMotion GetMotion(int rLayerIndex, String rName)
        {
            for (int i = 0; i < MotionLayers[rLayerIndex].Motions.Count; i++)
            {
                if (MotionLayers[rLayerIndex].Motions[i].Name == rName)
                {
                    return MotionLayers[rLayerIndex].Motions[i];
                }
            }

            return null;
        }
        
        /// <summary>
        /// Return the first motion in a layer that matches the specific motion
        /// type.
        /// </summary>
        /// <param name="rName">Name of controller motion to look for</param>
        /// <returns>Returns reference to the first motion matching the type or null if not found</returns>
        public MotionControllerMotion GetMotion(String rName)
        {
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                for (int j = 0; j < MotionLayers[i].Motions.Count; j++)
                {
                    if (MotionLayers[i].Motions[j].Name == rName)
                    {
                        return MotionLayers[i].Motions[j];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Return the first active motion in a layer.
        /// </summary>
        /// <param name="rLayerIndex">Layer to look through</param>
        /// <returns>Returns reference to the motion or null if not found</returns>
        public MotionControllerMotion GetActiveMotion(int rLayerIndex)
        {
            if (rLayerIndex >= MotionLayers.Count) { return null; }
            return MotionLayers[rLayerIndex].ActiveMotion;
        }

        /// <summary>
        /// Forces the motion to become active for the layer
        /// </summary>
        /// <param name="rLayerIndex">Layer to work with</param>
        /// <param name="rMotion">Motion to activate</param>
        /// <returns>Boolean used to determine if the motion was activated</returns>
        public bool QueueMotion(int rLayerIndex, MotionControllerMotion rMotion)
        {
            if (rLayerIndex >= MotionLayers.Count) { return false; }
            return MotionLayers[rLayerIndex].QueueMotion(rMotion);
        }

        /// <summary>
        /// Activate the specified motion (on the next frame).
        /// </summary>
        /// <param name="rMotion">Motion to activate</param>
        public void ActivateMotion(MotionControllerMotion rMotion)
        {
            if (rMotion != null)
            {
                rMotion.MotionLayer.QueueMotion(rMotion);
            }
        }

        /// <summary>
        /// Finds the first motion matching the motion type and then attempts
        /// to activate it (on the next frame).
        /// </summary>
        /// <param name="rMotionType">Type of motion to activate</param>
        /// <returns>Returns the motion to be activated or null if a matching motion isn't found</returns>
        public MotionControllerMotion ActivateMotion(Type rMotion)
        {
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                for (int j = 0; j < MotionLayers[i].Motions.Count; j++)
                {
                    MotionControllerMotion lMotion = MotionLayers[i].Motions[j];
                    if (lMotion.GetType() == rMotion)
                    {
                        MotionLayers[i].QueueMotion(lMotion);
                        return lMotion;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Moves the actor based on the velocity passed. This will
        /// determine rotation as well as position as the actor typically
        /// attempts to face forward the velocity.
        /// </summary>
        /// <param name="rVelocity">Velocity to move the actor</param>
        public void SetTargetVelocity(Vector3 rVelocity)
        {
            mTargetVelocity = rVelocity;

            if (mTargetVelocity.sqrMagnitude > 0f)
            {
                mTargetPosition = Vector3.zero;
                mTargetNormalizedSpeed = 0f;
            }
        }

        /// <summary>
        /// Moves the actor towards the target position using the normalized
        /// speed. This normalized speed will be used to temper the standard
        /// root-motion velocity
        /// </summary>
        /// <param name="rPosition"></param>
        /// <param name="rNormalizedSpeed"></param>
        public void SetTargetPosition(Vector3 rPosition, float rNormalizedSpeed)
        {
            mTargetPosition = rPosition;
            mTargetNormalizedSpeed = rNormalizedSpeed;

            if (mTargetPosition.sqrMagnitude > 0f) 
            { 
                mTargetVelocity = Vector3.zero; 
            }
        }

        /// <summary>
        /// Rotates the actor towards the target rotation.
        /// </summary>
        /// <param name="rPosition"></param>
        /// <param name="rNormalizedSpeed"></param>
        public void SetTargetRotation(Quaternion rRotation)
        {
            mTargetRotation = rRotation;
        }

        /// <summary>
        /// Clears out the target movement values
        /// </summary>
        public void ClearTarget()
        {
            mTargetVelocity = Vector3.zero;
            mTargetPosition = Vector3.zero;
            mTargetRotation = Quaternion.identity;
            mTargetNormalizedSpeed = 0f;
        }

        /// <summary>
        /// Moves the avatar towards this position over time using the controller's
        /// locomotion functions. 
        /// </summary>
        /// <param name="rPosition"></param>
        [Obsolete("Use SetTargetPosition instead.")]
        public void MoveTowards(Vector3 rPosition)
        {
            SetTargetPosition(rPosition, 1f);
        }

        /// <summary>
        /// DEPRICATED
        /// Moves the avatar towards this position over time using the controller's
        /// locomotion functions. 
        /// </summary>
        /// <param name="rPosition"></param>
        [Obsolete("Use SetTargetPosition instead.")]
        public void MoveTowards(Vector3 rPosition, float rNormalizedSpeed)
        {
            SetTargetPosition(rPosition, rNormalizedSpeed);
        }

        /// <summary>
        /// DEPRICATED
        /// Rotates the avatar to
        /// </summary>
        /// <param name="rRotation"></param>
        [Obsolete("Use SetTargetRotation instead.")]
        public void RotateTowards(Quaternion rRotation)
        {
            mTargetRotation = rRotation;
        }

        /// <summary>
        /// This function is used to convert the game control stick value to
        /// speed and direction values for the player.
        /// </summary>
        private void ProcessInput()
        {
            if (_CameraTransform == null) { return; }

            // Grab the movement, but create a bit of a dead zone
            float lHInput = InputManager.MovementX;
            float lVInput = InputManager.MovementY;
            float lMagnitude = Mathf.Sqrt((lHInput * lHInput) + (lVInput * lVInput));

            // Add the value to our averages so we track trends. 
            mState.InputMagnitudeTrend.Value = lMagnitude;

            // Get out early if we can simply this
            if (lVInput == 0f && lHInput == 0f)
            {
                mState.InputX = 0f;
                mState.InputY = 0f;
                mState.InputForward = Vector3.zero;
                mState.InputFromAvatarAngle = 0f;
                mState.InputFromCameraAngle = 0f;

                InputManager.InputFromCameraAngle = float.NaN;
                InputManager.InputFromAvatarAngle = float.NaN;

                return;
            }

            // Set the forward direction of the input
            mState.InputForward = new Vector3(lHInput, 0f, lVInput);

            // Direction of the avatar
            Vector3 lControllerForward = transform.forward;
            lControllerForward.y = 0f;
            lControllerForward.Normalize();

            // Direction of the camera
            if (_CameraTransform != null)
            {
                Vector3 lCameraForward = _CameraTransform.forward;
                lCameraForward.y = 0f;
                lCameraForward.Normalize();

                // Create a quaternion that gets us from our world-forward to our camera direction.
                // FromToRotation creates a quaternion using the shortest method which can sometimes
                // flip the angle. LookRotation will attempt to keep the "up" direction "up".
                //Quaternion rToCamera = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(lCameraForward));
                Quaternion rToCamera = Quaternion.LookRotation(lCameraForward);

                // Transform joystick from world space to camera space. Now the input is relative
                // to how the camera is facing.
                Vector3 lMoveDirection = rToCamera * mState.InputForward;
                mState.InputFromCameraAngle = NumberHelper.GetHorizontalAngle(lCameraForward, lMoveDirection);
                mState.InputFromAvatarAngle = NumberHelper.GetHorizontalAngle(lControllerForward, lMoveDirection);
            }
            else
            {
                mState.InputFromCameraAngle = 0f;
                mState.InputFromAvatarAngle = 0f;
            }

            // Set the direction of the movement in ranges of -1 to 1
            mState.InputX = lHInput;
            mState.InputY = lVInput;

            // Keep this info in the camera as well. Note that this info isn't
            // reliable as objects looking for it's set it will have old data
            InputManager.InputFromCameraAngle = (mState.InputMagnitudeTrend.Value == 0f ? float.NaN : mState.InputFromCameraAngle);
            InputManager.InputFromAvatarAngle = (mState.InputMagnitudeTrend.Value == 0f ? float.NaN : mState.InputFromAvatarAngle);
        }

        /// <summary>
        /// Gen a target position and rotation, this function converts the data into
        /// input values that will drive the controller.
        /// </summary>
        private void SimulateInput()
        {
            // Get out early if there's nothing to do
            if (mTargetVelocity.sqrMagnitude == 0f && mTargetPosition.sqrMagnitude == 0f && mTargetRotation == Quaternion.identity) 
            {
                mState.InputX = 0;
                mState.InputY = 0;
                mState.InputForward = Vector3.zero;
                mState.InputFromCameraAngle = 0f;
                mState.InputFromAvatarAngle = 0f;
                mState.InputMagnitudeTrend.Value = 0f;

                return; 
            }

            float lRotation = 0f;
            Vector3 lMovement = Vector3.zero;

            if (mTargetVelocity.sqrMagnitude > 0f)
            {
                lMovement = mTargetVelocity;
                mTargetNormalizedSpeed = Mathf.Clamp01(lMovement.magnitude / _MaxSpeed);

                lRotation = NumberHelper.GetHorizontalAngle(transform.forward, lMovement.normalized);
            }
            else
            {
                NumberHelper.GetHorizontalDifference(transform.position, mTargetPosition, ref lMovement);
                mTargetNormalizedSpeed = Mathf.Clamp01(lMovement.magnitude / _MaxSpeed);

                lRotation = NumberHelper.GetHorizontalAngle(transform.forward, lMovement.normalized);
            }

            // Determine the simulated input
            float lHInput = 0f;
            float lVInput = 0f;

            // Simulate the input
            if (lMovement.magnitude < 0.001f)
            {
                lHInput = 0f;
                lVInput = 0f;
            }
            else
            {
                lHInput = 0f;
                lVInput = mTargetNormalizedSpeed;
            }

            // Set the forward direction of the input, making it relative to the forward direction of the actor
            mState.InputForward = new Vector3(lHInput, 0f, lVInput);
            mState.InputForward = Quaternion.FromToRotation(transform.forward, lMovement.normalized) * mState.InputForward;

            mState.InputX = mState.InputForward.x;
            mState.InputY = mState.InputForward.z;

            // Determine the relative speed
            mState.InputMagnitudeTrend.Value = Mathf.Sqrt((lHInput * lHInput) + (lVInput * lVInput));

            // Direction of the avatar
            Vector3 lControllerForward = transform.forward;
            lControllerForward.y = 0f;
            lControllerForward.Normalize();

            // Direction of the camera
            if (_CameraTransform != null)
            {
                Vector3 lCameraForward = _CameraTransform.forward;
                lCameraForward.y = 0f;
                lCameraForward.Normalize();

                // Create a quaternion that gets us from our world-forward to our camera direction.
                // FromToRotation creates a quaternion using the shortest method which can sometimes
                // flip the angle. LookRotation will attempt to keep the "up" direction "up".
                //Quaternion rToCamera = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(lCameraForward));
                Quaternion rToCamera = Quaternion.LookRotation(lCameraForward);

                // Transform joystick from world space to camera space. Now the input is relative
                // to how the camera is facing.
                Vector3 lMoveDirection = rToCamera * mState.InputForward;
                mState.InputFromCameraAngle = NumberHelper.GetHorizontalAngle(lCameraForward, lMoveDirection);
            }
            else
            {
                mState.InputFromCameraAngle = 0f;
            }

            mState.InputFromAvatarAngle = lRotation;
        }

        /// <summary>
        /// Initializes the new state and sets the most basic values. We'll
        /// use this state to hold changing data until it becomes the current state.
        /// </summary>
        private void DetermineGrounding()
        {
            if (SafeRaycast(transform.position + (mCharController.transform.rotation * mCharController.center), Vector3.down, ref sGroundCollisionInfo, mCharController.center.y + 1f))            
            {
                mState.GroundNormal = sGroundCollisionInfo.normal;
                mState.GroundDistance = sGroundCollisionInfo.distance - mCharController.center.y;
                mState.GroundAngle = Vector3.Angle(mState.GroundNormal, Vector3.up);

                // If we're grounded, setup the support
                if (mState.GroundDistance <= GROUND_DISTANCE_TEST)
                {
                    mState.IsGrounded = true;
                    mState.Support = sGroundCollisionInfo.collider.gameObject;

                    Rigidbody lRigidBody = mState.Support.GetComponent<Rigidbody>();
                    if (lRigidBody != null)
                    {
                        mState.SupportPosition = lRigidBody.position;
                        mState.SupportRotation = lRigidBody.rotation;
                    }
                    else
                    {
                        Transform lTransform = mState.Support.transform;
                        mState.SupportPosition = lTransform.position;
                        mState.SupportRotation = lTransform.rotation;
                    }

                    // We use the contact point to determine the amount of orbit
                    // to apply. That means we want the initial contact point
                    if (mState.Support != mPrevState.Support || mState.SupportContactPosition.sqrMagnitude == 0f)
                    {
                        // We can't do a simple transform because that's effected by scale. So we'll
                        // figure out the position relative to the support's center ourselves.
                        mState.SupportContactPosition = Quaternion.Inverse(mState.SupportRotation) * (sGroundCollisionInfo.point - mState.SupportPosition);
                    }
                }
            }

            // Simple test for sliding. If we're on an extreme ramp, it may not seem like we're
            // grounded due to the gap between the collision capsule's hit point and our center-based ray, but we are. 
            // Use this test to force us to the ground
            if (!mState.IsGrounded && mState.GroundDistance < mCharController.stepOffset)
            {
                if (mState.GroundAngle > (mCharController.slopeLimit / 2f))
                {
                    mState.IsGrounded = true;
                }
            }

            // We didn't hit with a ray cast. We may need to do a final check
            // using a sphere. The goal is to ensure we're not on some crack that
            // makes us think we're not grounded when we are
            if (!mState.IsGrounded && IsGroundedExpected)
            {
                RaycastHit[] lSphereHits = null;
                Ray lRay = new Ray(transform.position + (mCharController.transform.rotation * mCharController.center), Vector3.down);

                // The sphere is smaller than the character controller and we only shoot it to the edge of the controller. This
                // way we don't taint our results too much.
                lSphereHits = UnityEngine.Physics.SphereCastAll(lRay, CharController.radius * 1.5f, mCharController.center.y + 0.01f, PlayerMask);
                if (lSphereHits != null && lSphereHits.Length > 0)
                {
                    // If we found a single collision point, we're probably on a steep slope.
                    // The problem is that we could be running off an edge too
                    if (lSphereHits.Length == 1)
                    {
                    }
                    // If we found multiple collision points, we're probably between two walls
                    else if (lSphereHits.Length > 1)
                    {
                        if (mPrevState.Support != null)
                        {
                            mState.Support = mPrevState.Support;

                            if (mState.Support.GetComponent<Rigidbody>() != null)
                            {
                                mState.SupportPosition = mState.Support.GetComponent<Rigidbody>().position;
                                mState.SupportRotation = mState.Support.GetComponent<Rigidbody>().rotation;
                            }
                            else
                            {
                                mState.SupportPosition = mState.Support.transform.position;
                                mState.SupportRotation = mState.Support.transform.rotation;
                            }

                            mState.SupportContactPosition = mPrevState.SupportContactPosition;
                        }

                        mState.GroundNormal = mPrevState.GroundNormal;
                        mState.GroundDistance = mPrevState.GroundDistance;
                        mState.GroundAngle = mPrevState.GroundAngle;
                        mState.IsGrounded = true;
                    }
                }
            }

            // Test if the space in front of the character is safe for movement.
            if (_ForwardBumper.sqrMagnitude > 0f && (mState.Velocity.x != 0f && mState.Velocity.z != 0))
            {
                mState.IsForwardPathBlocked = false;
                mState.ForwardPathBlockNormal = Vector3.zero;

                Vector3 lStart = transform.position;
                lStart.y = transform.position.y + _ForwardBumper.y;

                if (SafeRaycast(lStart, transform.forward, ref sRaycastHitInfo, _ForwardBumper.z))
                {

                    mState.IsForwardPathBlocked = true;
                    mState.ForwardPathBlockNormal = sRaycastHitInfo.normal;
                }
            }

            // Allows the motions to override the grounded values. If even one
            // layer disables the ground value, we shut it down. The layer 
            // is responsible for setting ground values when it contridicts
            // the current setting.
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                if (!MotionLayers[i].DetermineGrounding(ref mState)) { mState.IsGrounded = false; }
            }
        }

        /// <summary>
        /// Grab the acceleration to use in our movement
        /// </summary>
        /// <returns>The sum of our forces</returns>
        private Vector3 CalculateAcceleration()
        {
            Vector3 lAcceleration = Vector3.zero;

            // Apply each force
            if (mAppliedForces != null)
            {
                for (int i = mAppliedForces.Count - 1; i >= 0; i--)
                {
                    Force lForce = mAppliedForces[i];
                    if (lForce.StartTime == 0f) { lForce.StartTime = Time.time; }

                    // If the force is no longer valid, remove it
                    if (lForce.Value.sqrMagnitude == 0f)
                    {
                        mAppliedForces.RemoveAt(i);
                        Force.Release(lForce);
                    }
                    // If the force has started, look to apply it
                    else if (lForce.StartTime <= Time.time)
                    {
                        // For an impulse, apply it and remove it
                        if (lForce.Type == ForceMode.Impulse)
                        {
                            lAcceleration += (lForce.Value / _Mass);

                            mAppliedForces.RemoveAt(i);
                            Force.Release(lForce);
                        }
                        // Determine if the force has expired
                        else if (lForce.Duration > 0f && lForce.StartTime + lForce.Duration < Time.time)
                        {
                            mAppliedForces.RemoveAt(i);
                            Force.Release(lForce);
                        }
                        // Since it hasn't expired, apply it
                        else
                        {
                            lAcceleration += (lForce.Value / _Mass);
                        }
                    }
                }
            }

            return lAcceleration;
        }

        /// <summary>
        /// Apply the final rotations to our avatar
        /// </summary>
        /// <param name="rDeltaTime">Time (in seconds) since the last call</param>
        private void ApplyRotation(float rDeltaTime)
        {
            // First apply the root motion rotation. Before we apply it, we need
            // to move it from a velocity to a rotation delta
            Quaternion lRMRotation = mRootMotionAngularVelocity;
            lRMRotation.x *= rDeltaTime;
            lRMRotation.y *= rDeltaTime;
            lRMRotation.z *= rDeltaTime;
            lRMRotation.w *= rDeltaTime;
            transform.rotation *= lRMRotation;

            Vector3 lRotation = Vector3.zero;
            Vector3 lMotionVelocity = Vector3.zero;

            // Apply the angular velocity from each of the active motions
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                lMotionVelocity += MotionLayers[i].AngularVelocity;
            }

            // Increase the rotation based on the motion velocities
            lRotation += lMotionVelocity;

            // Rotate the avatar
            transform.Rotate(lRotation * rDeltaTime);
        }

        /// <summary>
        /// Apply the final movement to our avatar
        /// </summary>
        /// <param name="rDeltaTime">Time (in seconds) since the last call to use</param>
        private void ApplyMovement(float rDeltaTime)
        {
            if (Time.deltaTime == 0f) { return; }

            bool lIsGravityEnabled = true;
            Vector3 lGapVelocity = Vector3.zero;
            Vector3 lMotionVelocity = Vector3.zero;

            int lMotionLayerCount = MotionLayers.Count;

            // Apply the velocity from each of the active motions
            for (int i = 0; i < lMotionLayerCount; i++)
            {
                lMotionVelocity += MotionLayers[i].Velocity;
                if (!MotionLayers[i].IsGravityEnabled) { lIsGravityEnabled = false; }
            }

            // If the support moves up, it can move into the character. This logic ensures
            // the character moves up with the support/platform
            if (mState.IsGrounded && mState.GroundDistance < -0.01f && lIsGravityEnabled)
            {
                lGapVelocity.y = (-mState.GroundDistance + 0.01f) / Time.deltaTime;
            }

            // Clear out our acceleration and reload based on the applied forces.
            mAccumulatedAcceleration = CalculateAcceleration();

            // If we're on the ground, we can clear out some of the calculations.
            if (mState.IsGrounded)
            {
                // Reduce the acceleration and velocity to 0 over the ground drag duration
                if (mAccumulatedAcceleration.sqrMagnitude < 0.005f) { mAccumulatedAcceleration = Vector3.zero; } else { mAccumulatedAcceleration -= (mAccumulatedAcceleration * (rDeltaTime / mGroundDragDuration)); }
                if (mAccumulatedVelocity.sqrMagnitude < 0.005f) { mAccumulatedVelocity = Vector3.zero; } else { mAccumulatedVelocity -= (mAccumulatedVelocity * (rDeltaTime / mGroundDragDuration)); }
            }

            // If we're applying gravity, apply it
            if (lIsGravityEnabled)
            {
                // We may force some acceleration based on the ground slop. This will
                // allow us to slide if needed. It also forces the controller to register
                // if it's being pushed into the ground
                Vector3 lGravityAcceleration = _Gravity;

                // If there's a large slope or if we don't think we're grounded, but the controller does it's probably
                // because there is a steep slant or edge under us. Adjust gravity to help us slide past it.
                if (mState.GroundAngle > _MinSlideAngle)
                {
                    if (mState.IsGrounded || (!mState.IsGrounded && mCharController.isGrounded))
                    {
                        if (mState.GroundNormal != Vector3.up)
                        {
                            // Grab the direction of gravitational force along the ground plane
                            Vector3 lGravityDirection = _Gravity.normalized;
                            Vector3.OrthoNormalize(ref mState.GroundNormal, ref lGravityDirection);

                            // Use this direction to determine the actual components of gravity. Note
                            // that we're not accumulating it the way we normally should. This is so the
                            // slide down the slope isn't too much.
                            lGravityAcceleration = lGravityDirection * _Gravity.magnitude;
                        }
                    }
                }

                // Add the slope acceleration acceleration
                mAccumulatedAcceleration += lGravityAcceleration;
            }

            // Accumulate velocity from acceleration. We can't accumulate it
            // in the state since root motion velocity doesn't accumulate.
            mAccumulatedVelocity += (mAccumulatedAcceleration * rDeltaTime);

            // Calculate the final velocity given all of our data
            mState.Velocity = mAccumulatedVelocity;
            mState.Velocity += (transform.rotation * mRootMotionVelocity);
            mState.Velocity += lMotionVelocity;

            // Add velocities that are not due to the controller
            Vector3 lVelocity = mState.Velocity;

            if (lGapVelocity.y > 0)
            {
                lVelocity.y = Mathf.Max(lVelocity.y, lGapVelocity.y);
            }

            // Use the new velocity to move the avatar. The biggest hit we take in performance
            // is from the mCharController.Move() function. So we want to bypass it if we can.
            bool lUpdateMove = false;
            if (!lUpdateMove && !mState.IsGrounded) { lUpdateMove = true; }
            if (!lUpdateMove && mState.IsGrounded && mState.GroundDistance > 0.03f) { lUpdateMove = true; }
            if (!lUpdateMove && (lVelocity.x != 0f || lVelocity.y > 0f || lVelocity.z != 0f)) { lUpdateMove = true; }

            if (lUpdateMove)
            {
                mCharController.Move(lVelocity * rDeltaTime);

                // If we're grounded, we want to force the avatar down onto the
                // surface. We do this because running downhill can cause a
                // large gap between the avatar and the surface
                if (mState.IsGrounded && mState.GroundDistance > 0.01f && lIsGravityEnabled)
                {
                    float lGroundDistance = GroundCast(ref sGroundCollisionInfo);
                    Vector3 lGroundDistanceAdjust = new Vector3(0, -lGroundDistance, 0);

                    mCharController.Move(lGroundDistanceAdjust);
                    mState.GroundDistance = 0f;
                }
            }

            // Store the new position for future reference
            mState.Position = transform.position;
        }

        /// <summary>
        /// Check if the velocity has us trending so that we can
        /// determine if we'll update the animator immediately
        /// </summary>
        private void DetermineTrendData()
        {
            if (mState.InputMagnitudeTrend.Value == mPrevState.InputMagnitudeTrend.Value)
            {
                if (mSpeedTrendDirection != EnumSpeedTrend.CONSTANT)
                {
                    mSpeedTrendDirection = EnumSpeedTrend.CONSTANT;
                }
            }
            else if (mState.InputMagnitudeTrend.Value < mPrevState.InputMagnitudeTrend.Value)
            {
                if (mSpeedTrendDirection != EnumSpeedTrend.DECELERATE)
                {
                    mSpeedTrendDirection = EnumSpeedTrend.DECELERATE;
                    if (mMecanimUpdateDelay <= 0f) { mMecanimUpdateDelay = 0.2f; }
                }

                // Acceleration needs to stay consistant for mecanim
                //mNewState.Acceleration = mNewState.InputMagnitude - mSpeedTrendStart;
            }
            else if (mState.InputMagnitudeTrend.Value > mPrevState.InputMagnitudeTrend.Value)
            {
                if (mSpeedTrendDirection != EnumSpeedTrend.ACCELERATE)
                {
                    mSpeedTrendDirection = EnumSpeedTrend.ACCELERATE;
                    if (mMecanimUpdateDelay <= 0f) { mMecanimUpdateDelay = 0.2f; }
                }

                // Acceleration needs to stay consistant for mecanim
                //mNewState.Acceleration = mNewState.InputMagnitude - mSpeedTrendStart;
            }
        }

        /// <summary>
        /// Returns the friendly name of the state or transition that
        /// is currently being run by the first animator layer that is active. 
        /// </summary>
        /// <returns></returns>
        public string GetAnimatorStateName()
        {
            string lResult = "";

            for (int i = 0; i < Animator.layerCount; i++)
            {
                lResult = GetAnimatorStateName(i);
                if (lResult.Length > 0) { break; }
            }

            return lResult;
        }

        /// <summary>
        /// Returns the friendly name of the state that
        /// is currently being run.
        /// </summary>
        /// <param name="rLayerIndex">Layer whose index we want the state for</param>
        /// <returns>Name of the state that the character is in</returns>
        public string GetAnimatorStateName(int rLayerIndex)
        {
            string lResult = "";

            int lStateID = mState.AnimatorStates[rLayerIndex].StateInfo.nameHash;
            if (AnimatorStateNames.ContainsKey(lStateID))
            {
                lResult = AnimatorStateNames[lStateID];
            }

            return lResult;
        }

        /// <summary>
        /// Returns the friendly name of the state or transition that
        /// is currently being run.
        /// </summary>
        /// <param name="rLayerIndex">Layer whose index we want the state for</param>
        /// <returns>Name of the state or transition that the character is in</returns>
        public string GetAnimatorStateTransitionName(int rLayerIndex)
        {
            string lResult = "";

            int lStateID = mState.AnimatorStates[rLayerIndex].StateInfo.nameHash;
            int lTransitionID = mState.AnimatorStates[rLayerIndex].TransitionInfo.nameHash;

            if (lTransitionID != 0 && AnimatorStateNames.ContainsKey(lTransitionID))
            {
                lResult = AnimatorStateNames[lTransitionID];
            }
            else if (AnimatorStateNames.ContainsKey(lStateID))
            {
                lResult = AnimatorStateNames[lStateID];
            }

            return lResult;
        }

        /// <summary>
        /// Tests if the current animator state matches the name passed in. If not
        /// found, it tests for a match with the transition
        /// </summary>
        /// <param name="rLayerIndex">Layer to test</param>
        /// <param name="rStateName">State name to test for</param>
        /// <returns></returns>
        public bool CompareAnimatorStateName(int rLayerIndex, string rStateName)
        {
            if (mState.AnimatorStates[rLayerIndex].StateInfo.nameHash == AnimatorStateIDs[rStateName]) { return true; }
            if (mState.AnimatorStates[rLayerIndex].TransitionInfo.nameHash == AnimatorStateIDs[rStateName]) { return true; }
            return false;
        }

        /// <summary>
        /// Test if the current transition state matches the name passed in
        /// </summary>
        /// <param name="rLayerIndex">Layer to test</param>
        /// <param name="rTransitionName">Transition name to test for</param>
        /// <returns></returns>
        public bool CompareAnimatorTransitionName(int rLayerIndex, string rTransitionName)
        {
            return (mState.AnimatorStates[rLayerIndex].TransitionInfo.nameHash == AnimatorStateIDs[rTransitionName]);
        }

        /// <summary>
        /// Returns the motion phase the animator is currently in. We can
        /// use this to test where we're have from a motion perspective
        /// </summary>
        /// <param name="rLayerIndex"></param>
        /// <returns></returns>
        public int GetAnimatorMotionPhase(int rLayerIndex)
        {
            if (rLayerIndex >= mState.AnimatorStates.Length) { return 0; }
            return mState.AnimatorStates[rLayerIndex].MotionPhase;
        }

        /// <summary>
        /// Sets the motion phase that will be sent to the animator
        /// </summary>
        /// <param name="rLayer">Layer to apply the phase to</param>
        /// <param name="rPhase">Phase value to set</param>
        public void SetAnimatorMotionPhase(int rLayerIndex, int rPhase)
        {
            if (rLayerIndex >= mState.AnimatorStates.Length) { return; }
            mState.AnimatorStates[rLayerIndex].MotionPhase = rPhase;
            mState.AnimatorStates[rLayerIndex].AutoClearMotionPhase = false;
        }

        /// <summary>
        /// Sets the motion phase that will be sent to the animator
        /// </summary>
        /// <param name="rLayer">Layer to apply the phase to</param>
        /// <param name="rPhase">Phase value to set</param>
        public void SetAnimatorMotionPhase(int rLayerIndex, int rPhase, bool rAutoClear)
        {
            if (rLayerIndex >= mState.AnimatorStates.Length) { return; }
            mState.AnimatorStates[rLayerIndex].MotionPhase = rPhase;
            mState.AnimatorStates[rLayerIndex].AutoClearMotionPhase = rAutoClear;
        }

        /// <summary>
        /// Update the animator with data from the current state
        /// </summary>
        /// <param name="rState">ControllerState containing the current data</param>
        private void SetAnimatorProperties(ControllerState rState, bool rUseTrendData)
        {
            if (mAnimator == null) { return; }

            // The primary 'mode' the character is in
            mAnimator.SetInteger("Stance", rState.Stance);

            // The relative speed magnitude of the character (0 to 1)
            // Delay a bit before we update the speed if we're accelerating
            // or decelerating.
            mMecanimUpdateDelay -= Time.deltaTime;
            if (!rUseTrendData || mMecanimUpdateDelay <= 0f)
            {
                mAnimator.SetFloat("Input Magnitude", rState.InputMagnitudeTrend.Value); //, 0.05f, Time.deltaTime);
            }

            // The magnituded averaged out over the last 10 frames
            mAnimator.SetFloat("Input Magnitude Avg", rState.InputMagnitudeTrend.Average);

            // Direction of the input relative to the avatar's forward (-180 to 180)
            mAnimator.SetFloat("Input Angle From Avatar", rState.InputFromAvatarAngle); //, 0.15f, Time.deltaTime);

            // Direction of the input relative to the camera's forward (-180 to 180)
            mAnimator.SetFloat("Input Angle From Camera", rState.InputFromCameraAngle); //, 0.15f, Time.deltaTime); //, 0.05f, Time.deltaTime);

            // The raw input from the UI
            mAnimator.SetFloat("Input X", rState.InputX, 0.15f, Time.deltaTime);
            mAnimator.SetFloat("Input Y", rState.InputY, 0.15f, Time.deltaTime);

            // Motion phase per layer. Layer index is identified as "L0", "L1", etc.
            for (int i = 0; i < rState.AnimatorStates.Length; i++)
            {
                mAnimator.SetInteger("L" + i.ToString() + " Motion Phase", rState.AnimatorStates[i].MotionPhase);

                // By default, transitions are atomic. That means we can't interrupt one. 
                // Therefore, we will wait for any transitions to complete before we set the motion phase
                // on the Animator. Otherwise, the new value could get ignored by the Animator
                if (rState.AnimatorStates[i].AutoClearMotionPhase && rState.AnimatorStates[i].TransitionInfo.nameHash == 0)
                {
                    rState.AnimatorStates[i].MotionPhase = 0;
                }
            }
        }

        /// <summary>
        /// Given the current root motion value, we need to ensure
        /// it is still valid and clean it up if it's not. We can't
        /// do this inside of OnAnimatorMove since that is called on a
        /// FixedUpdate() and the animator state could change during an Update().
        /// </summary>
        private void CleanRootMotion()
        {
            // Check the motions to determine if we should remove the root motion.
            // If even one active motion wants it removed, remove it.
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                MotionLayers[i].CleanRootMotion(ref mRootMotionVelocity, ref mRootMotionAngularVelocity);
            }

            // Average the root motion after we clean it
            mRootMotionVelocityAvg.Add(mRootMotionVelocity);
        }

        /// <summary>
        /// There may be some cases where the avatar can get stuck in the
        /// enviornment. This should be rare, but if it happens, we'll attempt
        /// to pull it to a safe location
        /// </summary>
        private void FreeColliderFromEdge()
        {
            // If we get here, more than likely we are falling and the character controller has
            // skimmed a edge of a mesh and is considering it a stop. We'll force the avatar down 
            // the edged.
            if (!mState.IsGrounded && mCharController.isGrounded)
            {
                // Shoot a sphere from the center down so we can see where the 
                // capsule collider hit. This mimicks the character controller collider
                Vector3 lRayStart = transform.position + (mCharController.transform.rotation * mCharController.center);

                RaycastHit lSphereCollisionInfo = new RaycastHit();
                bool lSphereHit = UnityEngine.Physics.SphereCast(lRayStart, mCharController.radius, Vector3.down, out lSphereCollisionInfo, 10f, PlayerMask);
                if (lSphereHit)
                {
                    // We only want to handle the case when we're falling off 
                    // an edge. We don't want to do this with stairs or ramps
                    if (mState.GroundDistance > mCharController.stepOffset)
                    {
                        // Create a vector that directs the avatar off of the edge
                        Vector3 lContactPushVelocity = (transform.position - lSphereCollisionInfo.point).normalized;

                        // Move the avatar along the vector and away from the edge
                        float lEdgeForce = 0.5f;
                        mCharController.Move(lContactPushVelocity * (lEdgeForce * Time.deltaTime));

                        // Debug
                        //UnityEngine.Debug.DrawLine(transform.position, lSphereCollisionInfo.point, Color.blue);
                    }
                }
            }
        }

        /// <summary>
        /// Called to apply root motion manually. The existance of this
        /// stops the application of any existing root motion since we're
        /// essencially overriding the function. 
        /// 
        /// This function is called right after FixedUpdate() whenever
        /// FixedUpdate() is called. It is not called if FixedUpdate() is not
        /// called.
        /// </summary>
        private void OnAnimatorMove()
        {
            if (Time.deltaTime == 0f) { return; }

            // Store the root motion as a velocity per second. We also
            // want to keep it relative to the avatar's forward vector (for now).
            // Use Time.deltaTime to create an accurate velocity (as opposed to Time.fixedDeltaTime).
            mRootMotionVelocity = Quaternion.Inverse(transform.rotation) * (mAnimator.deltaPosition / Time.deltaTime);

            // Store the rotation as a velocity per second.
            mRootMotionAngularVelocity = mAnimator.deltaRotation;
            mRootMotionAngularVelocity.x /= Time.deltaTime;
            mRootMotionAngularVelocity.y /= Time.deltaTime;
            mRootMotionAngularVelocity.z /= Time.deltaTime;
            mRootMotionAngularVelocity.w /= Time.deltaTime;
        }

        /// <summary>
        /// PRO ONLY
        /// Callback for animating IK. All IK functionality should go here
        /// </summary>
        public void OnAnimatorIK()
        {
            // Send the event to all active motions
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                MotionLayers[i].UpdateIK();
            }
        }

        /// <summary>
        /// Raised when the animator's state has changed
        /// </summary>
        private void OnAnimatorStateChange(int rAnimatorLayer)
        {
            // Find the Motion Layers tied to the Animator Layer
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                if (MotionLayers[i].AnimatorLayerIndex == rAnimatorLayer)
                {
                    MotionLayers[i].OnAnimatorStateChange(rAnimatorLayer, mPrevState.AnimatorStates[rAnimatorLayer].StateInfo.nameHash, mState.AnimatorStates[rAnimatorLayer].StateInfo.nameHash);
                }
            }
        }

        /// <summary>
        /// Allow the controller to render debug info
        /// </summary>
        public void OnDrawGizmos()
        {
            // Find the Motion Layers tied to the Animator Layer
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                MotionLayers[i].OnDrawGizmos();
            }
        }

        /// <summary>
        /// Load the animator state and transition IDs
        /// </summary>
        private void LoadAnimatorData()
        {
            // Set the actual state names
            AddAnimatorName("Start");
            AddAnimatorName("Any State");

            // Allow the motion layers to set the names
            for (int i = 0; i < MotionLayers.Count; i++)
            {
                MotionLayers[i].Controller = this;
                MotionLayers[i].LoadAnimatorData();
            }
        }

        /// <summary>
        /// Initialize the id with the right has based on the name. Then store
        /// the data for easy recall.
        /// </summary>
        /// <param name="rName"></param>
        /// <param name="rID"></param>
        public void AddAnimatorName(string rName, ref int rID)
        {
            rID = Animator.StringToHash(rName);
            if (!AnimatorStateNames.ContainsKey(rID)) { AnimatorStateNames.Add(rID, rName); }
            if (!AnimatorStateIDs.ContainsKey(rName)) { AnimatorStateIDs.Add(rName, rID); }
        }

        /// <summary>
        /// Initialize the id with the right has based on the name. Then store
        /// the data for easy recall.
        /// </summary>
        /// <param name="rName"></param>
        /// <param name="rID"></param>
        public void AddAnimatorName(string rName)
        {
            int lID = Animator.StringToHash(rName);
            if (!AnimatorStateNames.ContainsKey(lID)) { AnimatorStateNames.Add(lID, rName); }
            if (!AnimatorStateIDs.ContainsKey(rName)) { AnimatorStateIDs.Add(rName, lID); }
        }

        /// <summary>
        /// Convert the animator hash ID to a readable string
        /// </summary>
        /// <param name="rStateID"></param>
        /// <param name="rTransitionID"></param>
        /// <returns></returns>
        private string AnimatorHashToString(int rStateID, int rTransitionID)
        {
            string lState = (AnimatorStateNames.ContainsKey(rStateID) ? AnimatorStateNames[rStateID] : rStateID.ToString());
            string lTransition = (AnimatorStateNames.ContainsKey(rTransitionID) ? AnimatorStateNames[rTransitionID] : rTransitionID.ToString());

            return String.Format("state:{0} trans:{1}", lState, lTransition);
        }

        /// <summary>
        /// Simple way to get the current animator / transition state name
        /// </summary>
        public string CurrentAnimatorStateName
        {
            get
            {
                return AnimatorHashToString(mState.AnimatorStates[0].StateInfo.nameHash, mState.AnimatorStates[0].TransitionInfo.nameHash);
            }
        }

        /// <summary>
        /// Simple way to get the currentmotion name
        /// </summary>
        public string CurrentMotionName
        {
            get
            {
                string lResult = "null";
                if (MotionLayers.Count > 0 && MotionLayers[0].ActiveMotion != null) { lResult = MotionLayers[0].ActiveMotion.GetType().Name; }

                return lResult;
            }
        }

        /// <summary>
        /// Simple way to get the duration of the current motion
        /// </summary>
        public float CurrentMotionDuration
        {
            get
            {
                float lResult = 0f;
                if (MotionLayers.Count > 0) { lResult = MotionLayers[0].ActiveMotionDuration; }

                return lResult;
            }
        }
    }
}


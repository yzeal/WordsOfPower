using System;
using UnityEngine;

using com.ootii.AI.Controllers;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.Cameras
{
    /// <summary>
    /// The adventure camera rig creates a 3rd person
    /// camera similiar to what you'd find in Uncharted, Tomb Raider,
    /// and Assassin's Creed. It allows the player full motion in
    /// the view, but follows the player if they try to move out.
    /// </summary>
    public class AdventureRig : CameraRig
    {
		public int pubCamMode = EnumCameraMode.THIRD_PERSON_FIXED;

        /// <summary>
        /// Keeps us from reallocating each frame
        /// </summary>
        private static RaycastHit sCollisionInfo = new RaycastHit();

        /// <summary>
        /// The character controller that the rig is tied to
        /// </summary>
        public Controller _Controller;
        public Controller Controller
        {
            get { return _Controller; }
        }

        /// <summary>
        /// Determines if we smooth out the positioning
        /// </summary>
        public bool _UseSmoothMovement = true;
        public bool UseSmoothMovement
        {
            get { return _UseSmoothMovement; }
            set { _UseSmoothMovement = value; }
        }

        /// <summary>
        /// Determines if the camera will test and adjust for 
        /// collision.
        /// </summary>
        public bool _UseCollisionDetection = true;
        public bool UseCollisionDetection
        {
            get { return _UseCollisionDetection; }
            set { _UseCollisionDetection = value; }
        }

        /// <summary>
        /// The distance the camera is to stay way from the target
        /// </summary>
        public float _Distance = 3f;
        public float Distance
        {
            get { return _Distance; }
            set { _Distance = value; }
        }

        /// <summary>
        /// Determines if we invert the Y axis
        /// </summary>
        public bool _InvertYAxis = false;
        public bool InvertYAxis
        {
            get { return _InvertYAxis; }
            set { _InvertYAxis = value; }
        }

        /// <summary>
        /// The distance the camera is to stay way from the target
        /// </summary>
        public float _FPCDistance = 1f;
        public float FPCDistance
        {
            get { return _FPCDistance; }
            set { _FPCDistance = value; }
        }

        /// <summary>
        /// The distance the camera is to the rigth of the avatar when
        /// in first person mode
        /// </summary>
        public float _FPCSideDistance = 0.65f;
        public float FPCSideDistance
        {
            get { return _FPCSideDistance; }
            set { _FPCSideDistance = value; }
        }

        /// <summary>
        /// Time it takes to transition to the first-person camera
        /// </summary>
        public float _TransitionTimeToFPC = 0.15f;
        public float TransitionTimeToFPC
        {
            get { return _TransitionTimeToFPC; }
            set { _TransitionTimeToFPC = value; }
        }

        /// <summary>
        /// Time it takes to transition to the third-person camera
        /// </summary>
        public float _TransitionTimeToTPC = 0.4f;
        public float TransitionTimeToTPC
        {
            get { return _TransitionTimeToTPC; }
            set { _TransitionTimeToTPC = value; }
        }

        /// <summary>
        /// The pitch of the camera. The vector contains the value (x),
        /// the min (y), and the max (z)
        /// </summary>
        public Vector3 _Pitch = new Vector3(4f, -45f, 45f);
        public Vector3 Pitch
        {
            get { return _Pitch; }
            set { _Pitch = value; }
        }

        /// <summary>
        /// The yaw of the camera. The vector contains the value (x),
        /// the min (y), and the max (z)
        /// </summary>
        public Vector3 _Yaw = new Vector3(0f, 0f, 0f);
        public Vector3 Yaw
        {
            get { return _Yaw; }
            set { _Yaw = value; }
        }

        /// <summary>
        /// Multiplier applied to the raw mouse movement for rotation
        /// </summary>
        public Vector3 _OrbitSpeed = new Vector3(180f, 360f, 180f);
        public Vector3 OrbitSpeed
        {
            get { return _OrbitSpeed; }
            set { _OrbitSpeed = value; }
        }

        /// <summary>
        /// Causes orbiting to smooth down, but creates a much smoother experience
        /// </summary>
        public float _OrbitSmoothing = 3f;
        public float OrbitSmoothing
        {
            get { return _OrbitSmoothing; }
            set { _OrbitSmoothing = value; }
        }

        /// <summary>
        /// Controlls how tight the camera sticks to the
        /// position it's trying to get to
        /// </summary>
        public float _Stiffness = 800.0f;
        public float Stiffness
        {
            get { return _Stiffness; }
            set { _Stiffness = value; }
        }

        /// <summary>
        /// Drag use during acceleration and deceleration
        /// </summary>
        public float _Damping = 80.0f;
        public float Damping
        {
            get { return _Damping; }
            set { _Damping = value; }
        }

        /// <summary>
        /// Mass of the camera for acceleration purposes
        /// </summary>
        public float _Mass = 3.0f;
        public float Mass
        {
            get { return _Mass; }
            set { _Mass = value; }
        }

        /// <summary>
        /// Position of the controller we're looking towards
        /// </summary>
        private Vector3 mAnchorPosition = Vector3.zero;

        /// <summary>
        /// New rig position after processing
        /// </summary>
        private Vector3 mNewRigPosition = Vector3.zero;

        /// <summary>
        /// The velocity the camera is moving at to reach the destination
        /// </summary>
        private Vector3 mRigVelocity = Vector3.zero;

        /// <summary>
        /// Rig position offset created when the player moves the view up, down,
        /// and arround the avatar
        /// </summary>
        protected Vector3 mRigViewOffset = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// Determines the camera's looking direction
        /// </summary>
        private Quaternion mViewRotation = Quaternion.identity;

        /// <summary>
        /// Current distance the camera is from the avatar as we transition
        /// from one camera to another
        /// </summary>
		[HideInInspector]
        public float mTransitionDistance = 0f;

        /// <summary>
        /// The side distance the camera is from the avatar as we transition
        /// from one camera to another
        /// </summary>
		[HideInInspector]
		public float mSideTransitionDistance = 0f;

        /// <summary>
        /// Time remaining for the transition to finish
        /// </summary>
        private float mTransitionTimer = 0f;

        /// <summary>
        /// Percentage remaining for the transition to finish
        /// </summary>
        private float mTransitionPercent = 0f;

        /// <summary>
        /// Smooth out the orbiting (pitch) of the camera
        /// </summary>
		[HideInInspector]
		public float mViewVelocityX = 0f;

        /// <summary>
        /// Smooth out the orbiting (yaw) of the camera
        /// </summary>
        private float mViewVelocityY = 0f;

        /// <summary>
        /// Modify the distance as we move over and under the avatar. This
        /// creates more of an oval path vs a pure circle.
        /// </summary>
        private float mDistanceHeightModifier = 1f;

        /// <summary>
        /// Amount to lerp the rotation by to balance the spring smoothing
        /// </summary>
        private float mSmoothPitchLerp = 1f;

        /// <summary>
        /// Smoothed out pitch value being used
        /// </summary>
        private float mSmoothPitch = 0f;

        /// <summary>
        /// Use this for initialization
        /// </summary>
        public void Start()
        {
            // Initialize the camera distances
            mTransitionDistance = _Distance;
            mSideTransitionDistance = 0f;

            // Force the orbit function as it will get everything in synch
            // from the beginning.
            mViewVelocityX = 0.0001f;
        }

        /// <summary>
        /// Called once per frame to update objects. This happens after FixedUpdate().
        /// Reactions to calculations should be handled here.
        /// </summary>
        public void Update()
        {
			if(UnityEngine.Input.GetButtonDown("ResetCamera")){
				_Pitch = new Vector3(4f, -60f, 70f);
				_Yaw = Vector3.zero;
				Debug.Log("camera reset");
			}

            if (Time.deltaTime == 0f) { return; }

            float lMouseX = 0f;
            float lMouseY = 0f;

            float lViewX = 0f;
            float lViewY = 0f;

            // Update the age of the camera
            mAge += Time.deltaTime;

            // Determine if we're simply aiming and follow the target
            if (mMode == EnumCameraMode.FIRST_PERSON)
            {
                lMouseX = InputManager.ViewX;
                lMouseY = InputManager.ViewY * 0.25f;

                // Determine if there's drag to keep the borders from creating a hard stop
                float lDragStrength = 1.0f;
                if (_Yaw.y != 0 && _Yaw.z != 0)
                {
                    lDragStrength = _Yaw.x / (lMouseX < 0 ? _Yaw.y : _Yaw.z);
                    lDragStrength = 1.08368f - (1.02989f * lDragStrength * lDragStrength);
                    if (_Yaw.x > 0 && lMouseX < 0) { lDragStrength = 1; }
                    if (_Yaw.x < 0 && lMouseX > 0) { lDragStrength = 1; }
                }

                // Determine the new yaw (making clamping it)
                lViewX = lMouseX * _OrbitSpeed.y * Time.deltaTime * lDragStrength;
                _Yaw.x = _Yaw.x + (_OrbitSmoothing <= 0f ? lViewX : Mathf.SmoothDampAngle(0, lViewX, ref mViewVelocityY, Time.deltaTime * _OrbitSmoothing));
                _Yaw.x = NumberHelper.NormalizeAngle(_Yaw.x);

                if (_Yaw.y != 0 && _Yaw.z != 0) { _Yaw.x = Mathf.Clamp(_Yaw.x, _Yaw.y, _Yaw.z); }

                // Process the pitch logic, respecing the borders
                if (_InvertYAxis) { lMouseY = -lMouseY; }

                lDragStrength = 1.0f;
                if (_Pitch.y != 0 && _Pitch.z != 0)
                {
                    lDragStrength = _Pitch.x / (lMouseY < 0 ? _Pitch.y : _Pitch.z);
                    lDragStrength = 1.08368f - (1.02989f * lDragStrength * lDragStrength);
                    if (_Pitch.x > 0 && lMouseY < 0) { lDragStrength = 1; }
                    if (_Pitch.x < 0 && lMouseY > 0) { lDragStrength = 1; }
                }

                // Determine the new pitch (making clamping it)
                lViewY = lMouseY * _OrbitSpeed.x * Time.deltaTime * lDragStrength;
                _Pitch.x = _Pitch.x + (_OrbitSmoothing <= 0f ? lViewY : Mathf.SmoothDampAngle(0, lViewY, ref mViewVelocityX, Time.deltaTime * _OrbitSmoothing));
                _Pitch.x = NumberHelper.NormalizeAngle(_Pitch.x);

                if (_Pitch.y != 0 && _Pitch.z != 0) { _Pitch.x = Mathf.Clamp(_Pitch.x, _Pitch.y, _Pitch.z); }

                // As we go over the top or bottom, bring the camera closer to the avatar
                float lModifierStart = 20f;
                if (_Pitch.z != 0 && _Pitch.x >= lModifierStart)
                {
                    mDistanceHeightModifier = 0.8f + (0.2f * (_Pitch.z - _Pitch.x) / (_Pitch.z - lModifierStart));
                }
                else if (_Pitch.y != 0 && _Pitch.x <= -lModifierStart)
                {
                    mDistanceHeightModifier = 0.8f + (0.2f * (_Pitch.y - _Pitch.x) / (_Pitch.y - -lModifierStart));
                }
                else
                {
                    mDistanceHeightModifier = 1f;
                }
            }
            // Otherwise, rotate around the target position
            else
            {
                lMouseX = InputManager.ViewX;
                lMouseY = InputManager.ViewY; // (_InvertYAxis ? -InputManager.ViewY : InputManager.ViewY);
                mIsOrbiting = (lMouseX != 0 || lMouseY != 0);

                // Process the orbiting logic
                if (mIsOrbiting || mMode == EnumCameraMode.THIRD_PERSON_FIXED || mViewVelocityY != 0f || mViewVelocityX != 0f)
                {
                    // Determine if there's drag to keep the borders from creating a hard stop
                    float lDragStrength = 1.0f;
                    if (_Yaw.y != 0 && _Yaw.z != 0)
                    {
                        lDragStrength = _Yaw.x / (lMouseX < 0 ? _Yaw.y : _Yaw.z);
                        lDragStrength = 1.08368f - (1.02989f * lDragStrength * lDragStrength);
                        if (_Yaw.x > 0 && lMouseX < 0) { lDragStrength = 1; }
                        if (_Yaw.x < 0 && lMouseX > 0) { lDragStrength = 1; }
                    }

                    // Determine the new yaw (making clamping it)
                    //lViewX = lMouseX * _RotationSpeed.y * Time.deltaTime * lDragStrength;
                    lViewX = lMouseX * (_OrbitSpeed.y) * Time.deltaTime * lDragStrength;
                    _Yaw.x = _Yaw.x + (_OrbitSmoothing <= 0f ? lViewX : Mathf.SmoothDampAngle(0, lViewX, ref mViewVelocityY, Time.deltaTime * _OrbitSmoothing));
                    _Yaw.x = NumberHelper.NormalizeAngle(_Yaw.x);

                    if (_Yaw.y != 0 && _Yaw.z != 0) { _Yaw.x = Mathf.Clamp(_Yaw.x, _Yaw.y, _Yaw.z); }

                    // Determine if there's drag to keep the borders from creating a hard stop
                    if (_InvertYAxis) { lMouseY = -lMouseY; }

                    lDragStrength = 1.0f;
                    if (_Pitch.y != 0 && _Pitch.z != 0)
                    {
                        lDragStrength = _Pitch.x / (lMouseY < 0 ? _Pitch.y : _Pitch.z);
                        lDragStrength = 1.08368f - (1.02989f * lDragStrength * lDragStrength);
                        if (_Pitch.x > 0 && lMouseY < 0) { lDragStrength = 1; }
                        if (_Pitch.x < 0 && lMouseY > 0) { lDragStrength = 1; }
                    }

                    //lViewY = lMouseY * _RotationSpeed.x * Time.deltaTime * lDragStrength;
                    lViewY = lMouseY * _OrbitSpeed.x * Time.deltaTime * lDragStrength;
                    _Pitch.x = _Pitch.x + (_OrbitSmoothing <= 0f ? lViewY : Mathf.SmoothDampAngle(0, lViewY, ref mViewVelocityX, Time.deltaTime * _OrbitSmoothing));
                    _Pitch.x = NumberHelper.NormalizeAngle(_Pitch.x);

                    if (_Pitch.y != 0 && _Pitch.z != 0) { _Pitch.x = Mathf.Clamp(_Pitch.x, _Pitch.y, _Pitch.z); }

                    // As we go over the top or bottom, bring the camera closer to the avatar
                    float lModifierStart = 20f;
                    if (_Pitch.z != 0 && _Pitch.x >= lModifierStart)
                    {
                        mDistanceHeightModifier = 0.8f + (0.2f * (_Pitch.z - _Pitch.x) / (_Pitch.z - lModifierStart));
                    }
                    else if (_Pitch.y != 0 && _Pitch.x <= -lModifierStart)
                    {
                        mDistanceHeightModifier = 0.8f + (0.2f * (_Pitch.y - _Pitch.x) / (_Pitch.y - -lModifierStart));
                    }
                    else
                    {
                        mDistanceHeightModifier = 1f;
                    }
                }
            }
        }

        /// <summary>
        /// LateUpdate is called once per frame after all Update() functions have
        /// finished.. Things (like a follow camera) that rely on objects updating 
        /// themselves first before they update should be placed here.
        /// </summary>
        public void LateUpdate()
        {
            if (_IsLateUpdateEnabled)
            {
                PostControllerLateUpdate();
            }
        }

        /// <summary>
        /// We really want the update function to happen after the controller updates.
        /// However, if the controller is on a platform, it has to update in the LateUpdate()
        /// function. Therefore, we let the controller call this function directly.
        /// </summary>
        public override void PostControllerLateUpdate()
        {
            // This is the point we're going use as our base for positioning the rig and looking at.
            mAnchorPosition = _Controller.CameraRigAnchor;

            // Move the camera and then rotate it
            ApplyMovement();
            ApplyRotation();

            // Since we're moving the view, we need to update the avatar rotation to match the
            // view. If we don't do it here, the avatar will lag behind and we'll get wobbling
            if ((mMode == EnumCameraMode.FIRST_PERSON && mTransitionTimer <= 0f))
            {
                _Controller.FaceCameraForward();
            }
        }

        /// <summary>
        /// Determines the final movement deltas and sets the camera position
        /// </summary>
        public void ApplyMovement()
        {
            if (Time.deltaTime == 0f) { return; }

            bool lUseSmoothMovement = true;
            Vector3 lAvatarRight = Vector3.zero;

            // If we're aiming, we shift into first person mode
            if (mMode == EnumCameraMode.FIRST_PERSON)
            {
                // Don't use smooth movement since we need the camera to
                // be incredibly precise. If we do, it gets pretty wobbly as the
                // avatar transform is a frame or two behind the camera.
                lUseSmoothMovement = false;

                // In the FPC, the view controls the controller's rotation
                Quaternion lViewRotationY = Quaternion.AngleAxis(_Yaw.x, Vector3.up);
                Quaternion lViewRotationX = Quaternion.AngleAxis(_Pitch.x, Vector3.right);
                mRigViewOffset = lViewRotationY * lViewRotationX * Vector3.forward;

                lAvatarRight = lViewRotationY * Vector3.right;

                // Determine how far along the aim transition process we are
                mTransitionPercent = 1f;
                float lTimePercent = 1f;
                if (mTransitionTimer > 0f)
                {
                    // Determine how far along the transition we are
                    mTransitionTimer = Mathf.Max(0f, mTransitionTimer - Time.deltaTime);
                    lTimePercent = 1.0f - (mTransitionTimer / _TransitionTimeToFPC);

                    // Create an curve for smoothing
                    mTransitionPercent = (1.74864f * Mathf.Sin(lTimePercent)) + (1.02553f * Mathf.Cos(lTimePercent)) - 1.02553f;

                    mTransitionDistance = _Distance - ((_Distance - _FPCDistance) * mTransitionPercent);
                    mSideTransitionDistance = _FPCSideDistance * mTransitionPercent;
                }

                // Determine the new position of the camera
                mNewRigPosition = mAnchorPosition - (mRigViewOffset * (mTransitionDistance * mDistanceHeightModifier));

                // Shift the camera to the right by the specified abount (as we transition into aim)
                mNewRigPosition += (lAvatarRight * mSideTransitionDistance);
            }
            // If the camera is orbiting, let it control the rig position
            else if (mIsOrbiting || mMode == EnumCameraMode.THIRD_PERSON_FIXED || mTransitionTimer > 0f)
            {
                // We need the camera rotation information to determine it's position
                Quaternion lViewRotationY = Quaternion.AngleAxis(_Yaw.x, Vector3.up);
                Quaternion lViewRotationX = Quaternion.AngleAxis(_Pitch.x, Vector3.right);
                mRigViewOffset = lViewRotationY * lViewRotationX * Vector3.forward;

                lAvatarRight = _Controller.transform.right;

                // Determine how far along in the transition process we are
                mTransitionPercent = 1f;
                float lTimePercent = 1f;
                if (mTransitionTimer > 0f)
                {
                    lUseSmoothMovement = false;

                    // Determine how far along the transition we are
                    mTransitionTimer = Mathf.Max(0f, mTransitionTimer - Time.deltaTime);
                    lTimePercent = 1.0f - (mTransitionTimer / _TransitionTimeToTPC);

                    // Create an curve for smoothing
                    mTransitionPercent = (1.34035f * Mathf.Sin(lTimePercent)) + (0.278153f * Mathf.Cos(lTimePercent)) - 0.278153f;

                    mTransitionDistance = _FPCDistance + ((_Distance - _FPCDistance) * mTransitionPercent);
                    mSideTransitionDistance = _FPCSideDistance * (1.0f - mTransitionPercent);
                }

                // When orbiting the camera is behind the target
                mNewRigPosition = mAnchorPosition - (mRigViewOffset * (mTransitionDistance * mDistanceHeightModifier));

                // Shift to the right by the speified amount (as we transition out of aim)
                mNewRigPosition += (lAvatarRight * mSideTransitionDistance);
            }
            // Otherwise, the rig position is based on the controller
            else
            {
                // We want the yaw to be our forward direction as we move. 
                // Should the player start moving the view, the orbit condition (above)
                // will take over. 
                //
                // Convert the angles to be posative when looking to the right and negative when 
                // looking to the left.
                _Yaw.x = transform.eulerAngles.y;
                if (_Yaw.x > 180f) { _Yaw.x -= 360f; }

                // Determine the camera position based on the angles
                Quaternion lViewRotationY = Quaternion.AngleAxis(_Yaw.x, Vector3.up);
                Quaternion lViewRotationX = Quaternion.AngleAxis(_Pitch.x, Vector3.right);
                mRigViewOffset = lViewRotationY * lViewRotationX * Vector3.forward;

                // If we were directly following behind the character, this would be the camera
                // position. We don't want this, but we do want the expected horizontal distance of the 
                // camera (meaning no y value used) from the target so we can use it to get the correct position
                mNewRigPosition = mAnchorPosition - (mRigViewOffset * (mTransitionDistance * mDistanceHeightModifier));
                float lLength = NumberHelper.GetHorizontalDistance(mNewRigPosition, mAnchorPosition);

                // Calculate the direction from the camera towards the follow target. Since this is the
                // camera's "last frame" position, it will slowly pull the camera towards
                // a flat position. To fix this, we remove the height element
                Vector3 lTargetDirection = mAnchorPosition - transform.position;
                lTargetDirection.y = 0;

                // Use the normal and our desired distance to determine the next position of the camera.
                // This could pull the camera forward or push the camera back as it tries to keep a hard
                // distance.
                lTargetDirection.Normalize();
                lTargetDirection = lTargetDirection * lLength;

                // Grab the new target and keep our desired distance from it (along the view direction).
                // This has the player rotating around the camera when they run 90, but
                // the camera will follow them if they are running away or towards the camera
                mNewRigPosition.x = mAnchorPosition.x - lTargetDirection.x;
                mNewRigPosition.z = mAnchorPosition.z - lTargetDirection.z;
            }

            // Determine the position based on our spring camera

            mSmoothPitchLerp = 1f;
            if (_UseSmoothMovement && lUseSmoothMovement && mAge > 1.0f)
            {
                Vector3 lPositionDelta = mNewRigPosition - transform.position;

                // Determine the acceleration for X & Z
                Vector3 lForce = (_Stiffness * lPositionDelta) - (_Damping * mRigVelocity);
                Vector3 lAcceleration = lForce / _Mass;

                // Determine the rig velocity and position
                mRigVelocity.x += lAcceleration.x * Time.deltaTime;
                mRigVelocity.y += lAcceleration.y * Time.deltaTime;
                mRigVelocity.z += lAcceleration.z * Time.deltaTime;
                mNewRigPosition = transform.position + (mRigVelocity * Time.deltaTime);

                // Since we're smoothing, we need to determine how much to slow the pitch down.
                // We want to synch the rotation with the positioning. Otherwise we'll get
                // some odd looking movement.
                if (lPositionDelta.y != 0f)
                {
                    mSmoothPitchLerp = (mRigVelocity.y * Time.deltaTime) / lPositionDelta.y;
                }
            }

            // Adjust for collision if we need to 
            if (_UseCollisionDetection)
            {
                HandleCollision(mAnchorPosition, ref mNewRigPosition);
            }

            // Set the adjusted position
            transform.position = mNewRigPosition;
        }

        /// <summary>
        /// Determines the final rotation deltas and sets the camera rotation
        /// </summary>
        private void ApplyRotation()
        {
            if (Time.deltaTime == 0f) { return; }

            Vector3 lAvatarRight = Vector3.zero;
            Quaternion lNewRigRotation = Quaternion.identity;

            // If we're aiming, we shift into first person mode
            if (mMode == EnumCameraMode.FIRST_PERSON)
            {
                // In the FPC, the view controls the controller's rotation
                Quaternion lViewRotationY = Quaternion.AngleAxis(_Yaw.x, Vector3.up);
                lAvatarRight = lViewRotationY * Vector3.right;

                // Set the position we're looking towards
                Vector3 lLookAtPosition = mAnchorPosition + (mRigViewOffset * 8f * mTransitionPercent) + (lAvatarRight * mSideTransitionDistance);
                mViewRotation = Quaternion.LookRotation(lLookAtPosition - mNewRigPosition);
            }
            // If the camera is transitioning, we need to use the distant target
            else if (mTransitionTimer > 0f)
            {
                // Represents the right side of the avatar
                lAvatarRight = _Controller.transform.right;

                // Set the position we're looking towards. If we're transitioning
                // We want to use the distant target
                Vector3 lLookAtPosition = mAnchorPosition + (mRigViewOffset * 8f * (1.0f - mTransitionPercent)) + (lAvatarRight * mSideTransitionDistance);
                mViewRotation = Quaternion.LookRotation(lLookAtPosition - mNewRigPosition);
            }
            // Otherwise, use the basic rotation
            else
            {
                mSmoothPitch = Mathf.Lerp(mSmoothPitch, _Pitch.x, mSmoothPitchLerp);

                // Set the position we're looking towards
                NumberHelper.GetHorizontalQuaternion(mNewRigPosition, mAnchorPosition, ref lNewRigRotation);
                mViewRotation = lNewRigRotation * Quaternion.AngleAxis(mSmoothPitch, Vector3.right);
            }

            // Set the adjusted rotation
            transform.rotation = mViewRotation;
        }

        /// <summary>
        /// Transitions the camera from one type to another. This helps us
        /// to move from first-person to third-person smoothly.
        /// </summary>
        /// <param name="rMode">Camera mode we're moving to</param>
        public override void TransitionToMode(int rMode)
        {
            if (rMode == EnumCameraMode.FIRST_PERSON)
            {
                // Finally, create the offset
                Quaternion lViewRotation = Quaternion.Euler(_Pitch.x, _Yaw.x, 0);
                mRigViewOffset = lViewRotation * Vector3.forward;

                float lRange = _Distance - _FPCDistance;
                float lPercentToFinish = (mTransitionDistance - _FPCDistance) / lRange;

                mTransitionTimer = _TransitionTimeToFPC * lPercentToFinish;
            }
            else if (rMode == EnumCameraMode.THIRD_PERSON_FIXED || rMode == EnumCameraMode.THIRD_PERSON_FOLLOW)
            {
                // Transition from the first person camera
                if (mMode == EnumCameraMode.FIRST_PERSON)
                {
                    Quaternion lViewRotation = Quaternion.Euler(_Pitch.x, _Yaw.x, 0);
                    mRigViewOffset = lViewRotation * _Controller.transform.forward;

                    float lRange = _Distance - _FPCDistance;
                    float lPercentToFinish = 1.0f - ((mTransitionDistance - _FPCDistance) / lRange);

                    mTransitionTimer = _TransitionTimeToTPC * lPercentToFinish;
                }
            }

            // Set the mode
            mMode = rMode;
			pubCamMode = rMode;
        }

        /// <summary>
        /// Test if there is a collision and set the new position so we don't collide
        /// </summary>
        /// <param name="rPosition">Current position of the camera</param>
        /// <param name="rTargetPosition">Camera position we are moving to</param>
        /// <returns>Boolean that lets us know if the camera should be repositioned at all</returns>
        private bool HandleCollision(Vector3 rAnchorPosition, ref Vector3 rCameraPosition)
        {
            bool lReposition = true;

            // Test the collision and return the collision point
            Vector3 lDirection = rCameraPosition - rAnchorPosition;
            if (_Controller.SafeRaycastAll(rAnchorPosition, lDirection.normalized, ref sCollisionInfo, lDirection.magnitude))
            {
                // Now, test if the collision point is too close to our avatar. If
                // it gets closer than the camera near plane our avatar will start culling itself.
                // to fix this, we'll use the collider radius as a min distance.
                float lDistance = NumberHelper.GetHorizontalDistance(sCollisionInfo.point, rAnchorPosition);
                if (lDistance < _Controller.ColliderRadius + _Camera.nearClipPlane)
                {
                    // prevent the camera from moving
                    rCameraPosition = transform.position;
                    lReposition = false;
                }
                // Reposition the camera
                else
                {
                    rCameraPosition = sCollisionInfo.point;
                }
            }

            // Allow the camera to be repositioned
            return lReposition;
        }
    }
}


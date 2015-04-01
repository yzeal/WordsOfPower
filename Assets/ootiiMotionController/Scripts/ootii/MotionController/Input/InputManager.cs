using System;
using System.Collections;
using UnityEngine;
using com.ootii.Utilities.Debug;

namespace com.ootii.Input
{
    /// <summary>
    /// Simple class to consolidate inut
    /// </summary>
    public class InputManager
    {
        /// <summary>
        /// Create the stub at startup and tie it into the Unity update path
        /// </summary>
#pragma warning disable 0414
        private static InputManagerStub sStub = (new GameObject("InputManagerStub")).AddComponent<InputManagerStub>();
#pragma warning restore 0414

        /// <summary>
        /// Set by an external object, it tracks the angle of the
        /// user input compared to the camera's forward direction
        /// Note that this info isn't reliable as objects using it 
        /// before it's set it will get float.NaN.
        /// </summary>
        private static float mInputFromCameraAngle = 0f;
        public static float InputFromCameraAngle
        {
            get { return mInputFromCameraAngle; }
            set { mInputFromCameraAngle = value; }
        }

        /// <summary>
        /// Set by an external object, it tracks the angle of the
        /// user input compared to the avatars's forward direction
        /// Note that this info isn't reliable as objects using it 
        /// before it's set it will get float.NaN.
        /// </summary>
        private static float mInputFromAvatarAngle = 0f;
        public static float InputFromAvatarAngle
        {
            get { return mInputFromAvatarAngle; }
            set { mInputFromAvatarAngle = value; }
        }

        /// <summary>
        /// Retrieves horizontal movement from the the input
        /// </summary>
        public static float MovementX
        {
            get
            {
				if (UnityEngine.Input.GetButton("FirstPerson") || WordsOfPower.Instance.typing) { return 0f;}
                float lMovement = UnityEngine.Input.GetAxis("Horizontal");
                return lMovement;    
            }
        }

        /// <summary>
        /// Retrieves vertical movement from the the input
        /// </summary>
        public static float MovementY
        {
            get
            {
				if (UnityEngine.Input.GetButton("FirstPerson") || WordsOfPower.Instance.typing) { return 0f;}
                float lMovement = UnityEngine.Input.GetAxis("Vertical");
                return lMovement;
            }
        }

        /// <summary>
        /// Retrieves horizontal view movement from the the input
        /// </summary>
        public static float ViewX
        {
            get
            {
                float lView = 0f;
                if (mIsXboxControllerEnabled)
                {
                    lView = UnityEngine.Input.GetAxisRaw("WXRightStickX");
                    if (lView != 0f) { return lView; }
                }

                // Mouse
                if (UnityEngine.Input.GetMouseButton(1))
                {
                    lView = mViewX;
                }

				//TESTI First Person mit ctrl
				if (UnityEngine.Input.GetButton("FirstPerson")) {
					lView = UnityEngine.Input.GetAxisRaw("Horizontal") + UnityEngine.Input.GetAxisRaw("CameraHorizontal") + UnityEngine.Input.GetAxisRaw("CameraHorizontalKeys");
					lView /= 2f;
					if(WordsOfPower.Instance.typing){
						lView = UnityEngine.Input.GetAxisRaw("CameraHorizontal");
						lView /= 8f;
					}
					if (lView != 0f) { return lView; }
				}else{
					lView = UnityEngine.Input.GetAxisRaw("CameraHorizontal") + UnityEngine.Input.GetAxisRaw("CameraHorizontalKeys");
					if(WordsOfPower.Instance.typing){
						lView = UnityEngine.Input.GetAxisRaw("CameraHorizontal");
						lView /= 8f;
					}
					if (lView != 0f) { return lView; }
				}

                return lView;
            }
        }

        /// <summary>
        /// Retrieves vertical view movement from the the input
        /// </summary>
        public static float ViewY
        {
            get
            {
                float lView = 0f;

                if (mIsXboxControllerEnabled)
                {
                    lView = UnityEngine.Input.GetAxisRaw("WXRightStickY");
                    if (lView != 0f) { return lView; }
                }

                // Mouse
                if (UnityEngine.Input.GetMouseButton(1))
                {
                    lView = mViewY;
                }

				//TESTI First Person mit ctrl
				if (UnityEngine.Input.GetButton("FirstPerson")) {
					lView = UnityEngine.Input.GetAxisRaw("Vertical") + UnityEngine.Input.GetAxisRaw("CameraVertical") + UnityEngine.Input.GetAxisRaw("CameraVerticalKeys");
					if(WordsOfPower.Instance.typing){
						lView = UnityEngine.Input.GetAxisRaw("CameraVertical");
						lView /= 3f;
					}
					if (lView != 0f) { return lView; }
				}else{
					lView = UnityEngine.Input.GetAxisRaw("CameraVertical") + UnityEngine.Input.GetAxisRaw("CameraVerticalKeys");
					if(WordsOfPower.Instance.typing){
						lView = UnityEngine.Input.GetAxisRaw("CameraVertical");
						lView /= 3f;
					}
					if (lView != 0f) { return lView; }
				}

                return lView;
            }
        }

        /// <summary>
        /// Determines if the player can freely look around
        /// </summary>
        public static bool IsFreeViewing
        {
            get { return true; }
        }

        private static bool mIsXboxControllerEnabled = false;

        private static float mMouseSensativity = 2f;

        private static float mViewX = 0f;
        private static float mViewY = 0f;

        private static bool mOldLTrigger = false;
        private static bool mOldRTrigger = false;

        private static float mVSyncTimer = 0f;

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Grab and process information from the input in one place. This
        /// allows us to calculated changes over time too.
        /// </summary>
        public static void Update()
        {
            mInputFromCameraAngle = float.NaN;
            mInputFromAvatarAngle = float.NaN;

            float lMouseSensativity = mMouseSensativity;
            if (UnityEngine.QualitySettings.vSyncCount == 0) { lMouseSensativity = lMouseSensativity * 2f; }

            float lViewX = UnityEngine.Input.GetAxis("Mouse X") * lMouseSensativity;
            if (lViewX != 0) 
            { 
                mViewX = lViewX;
                mVSyncTimer = 0f;
            }

            float lViewY = UnityEngine.Input.GetAxis("Mouse Y") * lMouseSensativity;
            if (lViewY != 0) 
            { 
                mViewY = lViewY;
                mVSyncTimer = 0f;
            }

            if (lViewX == 0f && lViewY == 0f)
            {
                mVSyncTimer += Time.deltaTime;
                if (mVSyncTimer > Time.fixedDeltaTime)
                {
                    mViewX = 0f;
                    mViewY = 0f;
                    mVSyncTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Test if a specific key is pressed
        /// </summary>
        /// <param name="rKey"></param>
        /// <returns></returns>
        public static bool IsPressed(KeyCode rKey)
        {
            return UnityEngine.Input.GetKey(rKey);
        }

        /// <summary>
        /// Test if a specific key is pressed
        /// </summary>
        /// <param name="rKey"></param>
        /// <returns></returns>
        public static bool IsJustPressed(KeyCode rKey)
        {
            return UnityEngine.Input.GetKeyUp(rKey);
        }

        /// <summary>
        /// Tests if a specific action is pressed. This is used for continuous checking.
        /// </summary>
        /// <param name="rAction">Action to test for</param>
        /// <returns>Boolean that determines if the action is taking place</returns>
        public static bool IsPressed(string rAction)
        {
			//TESTI
			if(WordsOfPower.Instance.typing){
				if (rAction == "Aiming") return true;
				return false;
			}

            // Determines if the character should go into a first-person
            // perspective for targeting
            if (rAction == "Aiming")
            {
                if (UnityEngine.Input.GetMouseButton(2)) { return true; }
                if (mIsXboxControllerEnabled && UnityEngine.Input.GetAxis("WXLeftTrigger") > 0.5f) { return true; }
				if (UnityEngine.Input.GetButton("FirstPerson")) { return true; }
			}
//			else if(WordsOfPower.Instance.typing){
//				return false;
//			}
            // Determines if the character should sprint forward
			else if (rAction == "Sprint")
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift)) { return true; }
                if (mIsXboxControllerEnabled && UnityEngine.Input.GetButton("WXButton3")) { return true; }
            }
            // Determines if the player is taking action to move the character to the left. 
            // Use this to trigger special case movement, like climbing (after the motion controller sets it!)
            else if (rAction == "MoveLeft")
            {
                if (float.IsNaN(mInputFromAvatarAngle)) { return false; }
                if (mInputFromAvatarAngle <= -45 && mInputFromAvatarAngle >= -135) { return true; }
            }
            // Determines if the player is taking action to move the character to the right. 
            // Use this to trigger special case movement, like climbing (after the motion controller sets it!)
            else if (rAction == "MoveRight")
            {
                if (float.IsNaN(mInputFromAvatarAngle)) { return false; }
                if (mInputFromAvatarAngle >= 45 && mInputFromAvatarAngle <= 135) { return true; }
            }
            // Determines if the player is taking action to move the character up or forward. 
            // Use this to trigger special case movement, like climbing (after the motion controller sets it!)
            else if (rAction == "MoveUp")
            {
                if (float.IsNaN(mInputFromAvatarAngle)) { return false; }
                if (mInputFromAvatarAngle > -45 && mInputFromAvatarAngle < 45) { return true; }
            }
            // Determines if the player is taking action to move the character down or backwards. 
            // Use this to trigger special case movement, like climbing (after the motion controller sets it!)
            else if (rAction == "MoveDown")
            {
                if (float.IsNaN(mInputFromAvatarAngle)) { return false; }
                if (mInputFromAvatarAngle < -135 || mInputFromAvatarAngle > 135) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Tests if a specific action just occured this frame.
        /// </summary>
        /// <param name="rAction">Action to test for</param>
        /// <returns>Boolean that determines if the action just took place</returns>
        public static bool IsJustPressed(string rAction)
        {
			//TESTI
			if(WordsOfPower.Instance.typing) return false;

			if (rAction == "Jump" && !WordsOfPower.Instance.typing)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) { return true; }
                if (mIsXboxControllerEnabled && UnityEngine.Input.GetButtonDown("WXButton0")) { return true; }
            }
            else if (rAction == "Aiming")
            {
                if (UnityEngine.Input.GetMouseButton(2)) 
                {
                    if (!mOldLTrigger)
                    {
                        mOldLTrigger = true;
                        return true;
                    }
                }
                else if (mIsXboxControllerEnabled && UnityEngine.Input.GetAxis("WXLeftTrigger") > 0.5f)
                {
                    if (!mOldLTrigger)
                    {
                        mOldLTrigger = true;
                        return true;
                    }
                }
				else if (UnityEngine.Input.GetButton("FirstPerson"))
				{
					if (!mOldLTrigger)
					{
						mOldLTrigger = true;
						return true;
					}
				}
                else
                {
                    mOldLTrigger = false;
                }
            }
            else if (rAction == "Release")
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift)) { return true; }
                if (mIsXboxControllerEnabled && UnityEngine.Input.GetButtonDown("WXButton3")) { return true; }
            }
            else if (rAction == "ChangeStance")
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.T)) { return true; }

                if (mIsXboxControllerEnabled && UnityEngine.Input.GetAxis("WXRightTrigger") > 0.5f)
                {
                    if (!mOldRTrigger)
                    {
                        mOldRTrigger = true;
                        return true;
                    }
                }
                else
                {
                    mOldRTrigger = false;
                }
            }
            else if (rAction == "PrimaryAttack")
            {
                if (UnityEngine.Input.GetMouseButtonDown(0)) { return true; }
				if (UnityEngine.Input.GetButtonDown("Punch")) { return true; }
                if (mIsXboxControllerEnabled && UnityEngine.Input.GetButtonDown("WXButton1")) { return true; }
            }
            else if (rAction == "Sprint")
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift)) { return true; }
                if (mIsXboxControllerEnabled && UnityEngine.Input.GetButtonDown("WXButton3")) { return true; }
            }

            return false;
        }
    }

    /// <summary>
    /// Used by the InputManager to hook into the unity update process. This allows us
    /// to update the input and track old values
    /// </summary>
    public sealed class InputManagerStub : MonoBehaviour
    {
        /// <summary>
        /// Raised first when the object comes into existance. Called
        /// even if script is not enabled.
        /// </summary>
        void Awake()
        {
            // Don't destroyed automatically when loading a new scene
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called after the Awake() and before any update is called.
        /// </summary>
        public IEnumerator Start()
        {
            // Initialize the manager
            InputManager.Initialize();

            // Create the coroutine here so we don't re-create over and over
            WaitForEndOfFrame lWaitForEndOfFrame = new WaitForEndOfFrame();

            // Loop endlessly so we can process the input
            // at the end of each frame, preparing for the next
            while (true)
            {
                yield return lWaitForEndOfFrame;
                InputManager.Update();
            }
        }

        /// <summary>
        /// Called when the InputManager is disabled. We use this to
        /// clean up objects that were created.
        /// </summary>
        public void OnDisable()
        {
        }
    }
}

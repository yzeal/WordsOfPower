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
                   "not typically rotate towards the camera. Here, the avatar walks until the input " +
                   "action occurs. At that point, the avatar would run.")]
    public class SimpleForwardActivated : SimpleForward
    {
        /// <summary>
        /// Modification to the SimpleForward motion that forces a walk,
        /// but allows for a sprint.
        /// </summary>
        [SerializeField]
        protected string mInputAction = "Sprint";

        [MotionTooltip("The value of the input action to look for that triggers the run.")]
        public string InputAction
        {
            get { return mInputAction; }
            set { mInputAction = value; }
        }

        /// <summary>
        /// Determines if the input action needs to constantly be
        /// pressed for the action to take place or if it acts like
        /// a toggle.
        /// </summary>
        [SerializeField]
        protected bool mIsInputActionContinuous = true;

        [MotionTooltip("Determines if the input action needs to be constantly pressed or if it acts like a toggle.")]
        public bool IsInputActionContinuous
        {
            get { return mIsInputActionContinuous; }
            set { mIsInputActionContinuous = value; }
        }

        /// <summary>
        /// Overrides the states input in order to control the speed
        /// of the forward motion.
        /// </summary>
        [SerializeField]
        protected float mMaxInputMagnitude = 0.5f;

        [MotionTooltip("Overrides the normal input to control the avatar's speed when not running.")]
        public float MaxInputMagnitude
        {
            get { return mMaxInputMagnitude; }
            set { mMaxInputMagnitude = value; }
        }

        /// <summary>
        /// Overrides the states input in order to control the speed
        /// of the forward motion when the flag is activated.
        /// </summary>
        [SerializeField]
        protected float mMaxActivatedInputMagnitude = 1.0f;

        [MotionTooltip("Overrides the normal input to control the avatar's speed when running.")]
        public float MaxActivatedInputMagnitude
        {
            get { return mMaxActivatedInputMagnitude; }
            set { mMaxActivatedInputMagnitude = value; }
        }

        /// <summary>
        /// Determines if the alternate speed is activated
        /// </summary>
        protected bool mIsInputActivated = false;

        /// <summary>
        /// Used internally to calculate the velocity of the motion. Root motion
        /// is handled by the controller directly and won't come through this function.
        /// </summary>
        /// <returns>Vector3 representing the current velocity</returns>
        protected override Vector3 DetermineVelocity()
        {
            // Test the input to determine if we're triggered
            if (mInputAction.Length > 0)
            {
                if (mIsInputActionContinuous)
                {
                    mIsInputActivated = InputManager.IsPressed(mInputAction);
                }
                else if (InputManager.IsJustPressed(mInputAction))
                {
                    mIsInputActivated = !mIsInputActivated;
                }
            }

            // Grab the current state so we can test the input
            ControllerState lState = mController.State;

            // Determine the actual speed
            float lInputMagnitude = (mIsInputActivated ? mMaxActivatedInputMagnitude : mMaxInputMagnitude);
            lInputMagnitude = Mathf.Min(lState.InputMagnitudeTrend.Value, lInputMagnitude);

            // If it doesn't match our last value, change it
            if (lState.InputMagnitudeTrend.Value != lInputMagnitude)
            {
                lState.InputMagnitudeTrend.Replace(lInputMagnitude);
                mController.State = lState;
            }

            // This motion won't add velocity itself
            return Vector3.zero;
        }
    }
}

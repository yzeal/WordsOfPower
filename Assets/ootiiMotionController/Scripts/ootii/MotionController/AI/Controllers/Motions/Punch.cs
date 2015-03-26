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
    /// This is a simple punch used to test using different
    /// motions at the same time with MotionLayers.
    /// </summary>
    [MotionTooltip("A simple motion used to test motions on different layers. When put on a seperate layer, " +
                   "this motion will cause the avatar to punch with his left hand.")]
    public class Punch : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 500;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Punch()
            : base()
        {
            _Priority = 10;
            mIsStartable = true;
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Punch(MotionController rController)
            : base(rController)
        {
            _Priority = 10;
            mIsStartable = true;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (mController.UseInput && InputManager.IsJustPressed("PrimaryAttack"))
            {
                // Grab the state name from the first active state we find
                string lStateName = mController.GetAnimatorStateName();

                // Ensure we're not currently climbing
                if (!lStateName.Contains("ClimbCrouch-SM"))
                {
                    return true;
                }
            }            

            // Get out
            return false;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mController.SetAnimatorMotionPhase(mAnimatorLayerIndex, Punch.PHASE_START, true);
            return base.Activate(rPrevMotion);
        }
    }
}

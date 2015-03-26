using System;
using UnityEngine;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// Holds the mecanim animation state information
    /// for a layer. This keeps us from having to ask for
    /// it over and over. We can also use it to track changes
    /// </summary>
    public struct AnimatorLayerState
    {
        /// <summary>
        /// Contains the current state information for the layer
        /// </summary>
        public AnimatorStateInfo StateInfo;

        /// <summary>
        /// Contains the current transition information for the layer
        /// </summary>
        public AnimatorTransitionInfo TransitionInfo;

        /// <summary>
        /// Tracks the last state name hash that was running
        /// </summary>
        //public int LastAnimatorStateHash;

        /// <summary>
        /// The phase of the curren motion to pass to the animator. While
        /// many motions and motion layers can exist, eventually the information has
        /// to be placed here so it can be sent to the animator.
        /// </summary>
        public int MotionPhase;

        /// <summary>
        /// Clear out the phase if we need to. This way we don't re-enter. Especially
        /// usefull for not re-entering from the 'AnyState'.
        /// </summary>
        public bool AutoClearMotionPhase;
    }
}

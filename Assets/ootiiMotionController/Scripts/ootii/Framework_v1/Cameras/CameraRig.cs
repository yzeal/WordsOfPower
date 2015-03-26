using System;
using UnityEngine;
using com.ootii.Base;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Camera rigs are used to position and rotate
    /// a unity camera. Think of a unity camera as a lense that
    /// can have filters, a field of view, etc. While they also
    /// can have position and rotation, we use the camera rig
    /// do manage these so we can manage it better
    /// </summary>
    public class CameraRig : BaseMonoObject
    {
        /// <summary>
        /// Determines if the camera updates itself during LateUpdate
        /// or if the camers will have someone else call it. This is
        /// important for some controllers that may do processing in thier
        /// LateUpdate and want to ensure the camera's function is called
        /// explicitly. See PostControllerLateUpdate()
        /// </summary>
        public bool _IsLateUpdateEnabled = true;
        public virtual bool IsLateUpdateEnabled
        {
            get { return _IsLateUpdateEnabled; }
        }

        /// <summary>
        /// The camera that is mounted on the rig. This is the 'lens'
        /// that we'll actually see through.
        /// </summary>
        public Camera _Camera;
        public Camera Camera
        {
            get { return _Camera; }
        }

        /// <summary>
        /// The type of the camera in order to help determine
        /// how the camera moves and rotates.
        /// </summary>
        protected int mMode = EnumCameraMode.THIRD_PERSON_FOLLOW;
        public virtual int Mode
        {
            get { return mMode; }
            set { mMode = value; }
        }

        /// <summary>
        /// Determines if the camera is currently orbiting
        /// around a position.
        /// </summary>
        protected bool mIsOrbiting = false;
        public virtual bool IsOrbiting
        {
            get { return mIsOrbiting; }
        }

        /// <summary>
        /// Allows us to track how long the camera has been around.
        /// </summary>
        protected float mAge = 0f;

        /// <summary>
        /// We really want the update function to happen after the controller updates.
        /// However, if the controller is on a platform, it has to update in the LateUpdate()
        /// function. Therefore, we let the controller call this function directly.
        /// </summary>
        public virtual void PostControllerLateUpdate()
        {
        }

        /// <summary>
        /// Transitions the camera from one type to another. This helps us
        /// to move from first-person to third-person and back smoothly (as needed).
        /// </summary>
        /// <param name="rMode">Camera mode we're moving to</param>
        public virtual void TransitionToMode(int rMode)
        {
        }
    }
}


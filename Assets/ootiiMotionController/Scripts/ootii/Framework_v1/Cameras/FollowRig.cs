using System;
using UnityEngine;

using com.ootii.AI.Controllers;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Traditional third-person camera that follows the transform
    /// at a specific offset. Unlike the Adventure Camera, this camera
    /// always rotates around the transform + offset and does not have
    /// an orbiting view
    /// 
    /// The Adventure Camera is a much more advanced camera with several
    /// modes, a physics based spring, and a modern third-person approach
    /// where the player rotates around the camera.
    /// https://www.assetstore.unity3d.com/#/content/13768
    /// </summary>
    public class FollowRig : CameraRig
    {
        /// <summary>
        /// Keeps us from reallocating each frame
        /// </summary>
        private static RaycastHit sCollisionInfo = new RaycastHit();

        /// <summary>
        /// Transform that represents the anchor we want to follow
        /// </summary>
        public Transform _Anchor;
        public Transform Anchor
        {
            get { return _Anchor; }
            set { _Anchor = value; }
        }

        /// <summary>
        /// Offset from the anchor's position we are looking towards (typically head)
        /// </summary>
        public Vector3 _AnchorOffset = new Vector3(0.0f, 1.7f, 0.0f);
        public Vector3 AnchorOffset
        {
            get { return _AnchorOffset; }
            set { _AnchorOffset = value; }
        }

        /// <summary>
        /// Radius of the controller collider used to represent the character
        /// </summary>
        public float _AnchorRadius = 0.5f;
        public float AnchorRadius
        {
            get { return _AnchorRadius; }
            set { _AnchorRadius = value; }
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
        /// The distance the camera is to stay way from the target
        /// </summary>
        public float _Distance = 3f;
        public float Distance
        {
            get { return _Distance; }
            set { _Distance = value; }
        }

        /// <summary>
        /// The pitch of the camera in degrees
        /// </summary>
        public float _Pitch = 4f;
        public float Pitch
        {
            get { return _Pitch; }
            set { _Pitch = value; }
        }

        /// <summary>
        /// Position of the controller we're looking towards
        /// </summary>
        private Vector3 mAnchorPosition = Vector3.zero;

        /// <summary>
        /// Use this for initialization
        /// </summary>
        public void Start()
        {
        }

        /// <summary>
        /// LateUpdate is called once per frame after all Update() functions have
        /// finished.. Things (like a follow camera) that rely on objects updating 
        /// themselves first before they update should be placed here.
        /// </summary>
        public void LateUpdate()
        {
            // If the update is enabled, we can update the
            // camera here. Otherwise, another caller will do it.
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
            mAnchorPosition = _Anchor.position + _AnchorOffset;

            // Move the camera and then rotate it
            ApplyMovement();
            ApplyRotation();
        }

        /// <summary>
        /// Determines the final movement deltas and sets the camera position
        /// </summary>
        public void ApplyMovement()
        {
            bool lUseSmoothMovement = true;
            Vector3 lNewRigPosition = Vector3.zero;

            // We need the camera rotation information to determine it's position
            Quaternion lViewRotationX = Quaternion.AngleAxis(_Pitch, Vector3.right);
            Vector3 lRigViewOffset = _Anchor.rotation * (lViewRotationX * Vector3.forward);

            // When orbiting the camera is behind the target
            lNewRigPosition = mAnchorPosition - (lRigViewOffset * _Distance);

            // Determine the position based on our spring camera
            if (_UseSmoothMovement && lUseSmoothMovement && Time.realtimeSinceStartup > 2.0f)
            {
                lNewRigPosition = Vector3.Lerp(transform.position, lNewRigPosition, 0.95f);
            }

            // Adjust for collision if we need to 
            HandleCollision(mAnchorPosition, ref lNewRigPosition);

            // Set the adjusted position
            transform.position = lNewRigPosition;
        }

        /// <summary>
        /// Determines the final rotation deltas and sets the camera rotation
        /// </summary>
        private void ApplyRotation()
        {
            Vector3 lNewRigPosition = transform.position;

             // Set the position we're looking towards
            Vector3 lLookAtPosition = mAnchorPosition;
            Quaternion lViewRotation = Quaternion.LookRotation(lLookAtPosition - lNewRigPosition);

            // Smooth out the rotation
            if (_UseSmoothMovement)
            {
                lViewRotation = Quaternion.Slerp(transform.rotation, lViewRotation, 0.95f);
            }

            // Set the adjusted rotation
            transform.rotation = lViewRotation;
        }

        /// <summary>
        /// Transitions the camera from one type to another. This helps us
        /// to move from first-person to third-person smoothly.
        /// </summary>
        /// <param name="rMode">Camera mode we're moving to</param>
        public override void TransitionToMode(int rMode)
        {
            mMode = rMode;
        }

        /// <summary>
        /// Test if there is a collision and set the new position so we don't collide
        /// </summary>
        /// <param name="rPosition">Current position of the camera</param>
        /// <param name="rTargetPosition">Camera position we are moving to</param>
        /// <returns>Boolean that lets us know if the camera should be repositioned at all</returns>
        private bool HandleCollision(Vector3 rPosition, ref Vector3 rTargetPosition)
        {
            bool lReposition = true;

            // First, determine the collision point. To do this, ignore
            // the player layer (layer index 8). Invert the bitmask so we get
            // everything EXCEPT the player
            int lPlayerLayerMask = 1 << 8;
            lPlayerLayerMask = ~lPlayerLayerMask;

            // Test the collision and return the collision point
            bool lCollisionHit = UnityEngine.Physics.Linecast(rPosition, rTargetPosition, out sCollisionInfo, lPlayerLayerMask);
            if (lCollisionHit)
            {
                // Safety check in case the builder forgot to set the 'player' layer on
                // the avatar. This way we don't collide with ourselves.
                if (!sCollisionInfo.collider.isTrigger && sCollisionInfo.collider.gameObject.transform != _Anchor)
                {
                    // Now, test if the collision point is too close to our avatar. If
                    // it gets closer than the camera near plane our avatar will start culling itself.
                    // to fix this, we'll use the collider radius as a min distance.
                    float lDistance = NumberHelper.GetHorizontalDistance(sCollisionInfo.point, _Anchor.transform.position);
                    if (lDistance < _AnchorRadius + _Camera.nearClipPlane)
                    {
                        // prevent the camera from moving
                        rTargetPosition = transform.position;
                        lReposition = false;
                    }
                    // Reposition the camera
                    else
                    {
                        rTargetPosition = sCollisionInfo.point;
                    }
                }
            }

            // Allow the camera to be repositioned
            return lReposition;
        }
    }
}


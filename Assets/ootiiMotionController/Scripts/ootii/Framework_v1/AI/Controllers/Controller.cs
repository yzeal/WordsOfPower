using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// A controller is used to manage the movement and behavior of
    /// a character. Typically this is the character being controlled by
    /// the player, but that's not always the case.
    /// </summary>
    public class Controller : BaseMonoObject
    {
        /// <summary>
        /// Keep us from having to reallocate over and over
        /// </summary>
        private RaycastHit mRaycastHitInfo = new RaycastHit();

        /// <summary>
        /// Radius of the collider surrounding the controller
        /// </summary>
        public virtual float ColliderRadius
        {
            get { return 0f; }
        }

        /// <summary>
        /// This is the position that the camera is attempting to move
        /// towards. It's the default position of the camera and typically
        /// represents the avatar's head.
        /// </summary>
        public virtual Vector3 CameraRigAnchor
        {
            get { return Vector3.zero; }
        }

        /// <summary>
        /// Transform we used to understand the camera's position and orientation
        /// </summary>
        public Transform _CameraTransform;
        public virtual Transform CameraTransform
        {
            get { return _CameraTransform; }
        }

        /// <summary>
        /// Camera rig holding the camera that represents the 
        /// view from this controller
        /// </summary>
        public CameraRig _CameraRig;
        public virtual CameraRig CameraRig
        {
            get { return _CameraRig; }
        }

        /// <summary>
        /// When casting a ray from the motion controller, we don't want it to collide with
        /// ourselves or triggers. The problem is that we may want to collide with another avatar. So, we
        /// can't put every avatar on their own layer. This ray cast will take a little longer,
        /// but will ignore triggers and ourselves.
        /// 
        /// Note: This function isn't virutal to eek out ever ounce of performance we can.
        /// </summary>
        /// <param name="rRayStart"></param>
        /// <param name="rRayDirection"></param>
        /// <param name="rDistance"></param>
        /// <returns></returns>
        public bool SafeRaycast(Vector3 rRayStart, Vector3 rRayDirection, float rDistance)
        {
            bool lHit = UnityEngine.Physics.Raycast(rRayStart, rRayDirection, out mRaycastHitInfo, rDistance);
            if (!lHit) { return false; }

            if (mRaycastHitInfo.collider.isTrigger) { return false; }
            if (mRaycastHitInfo.collider.gameObject == gameObject) { return false; }

            return true;
        }

        /// <summary>
        /// When casting a ray from the motion controller, we don't want it to collide with
        /// ourselves. The problem is that we may want to collide with another avatar. So, we
        /// can't put every avatar on their own layer. This ray cast will take a little longer,
        /// but will ignore this avatar.
        /// 
        /// Note: This function isn't virutal to eek out ever ounce of performance we can.
        /// </summary>
        /// <param name="rRayStart"></param>
        /// <param name="rRayDirection"></param>
        /// <param name="rDistance"></param>
        /// <param name="rMask"></param>
        /// <returns></returns>
        public bool SafeRaycast(Vector3 rRayStart, Vector3 rRayDirection, float rDistance, int rMask)
        {
            bool lHit = UnityEngine.Physics.Raycast(rRayStart, rRayDirection, out mRaycastHitInfo, rDistance, rMask);
            if (!lHit) { return false; }

            if (mRaycastHitInfo.collider.isTrigger) { return false; }
            if (mRaycastHitInfo.collider.gameObject == gameObject) { return false; }

            return true;
        }

        /// <summary>
        /// When casting a ray from the motion controller, we don't want it to collide with
        /// ourselves. The problem is that we may want to collide with another avatar. So, we
        /// can't put every avatar on their own layer. This ray cast will take a little longer,
        /// but will ignore this avatar.
        /// 
        /// Note: This function isn't virutal to eek out ever ounce of performance we can.
        /// </summary>
        /// <param name="rRayStart"></param>
        /// <param name="rRayDirection"></param>
        /// <param name="rHitInfo"></param>
        /// <param name="rDistance"></param>
        /// <returns></returns>
        public bool SafeRaycast(Vector3 rRayStart, Vector3 rRayDirection, ref RaycastHit rHitInfo, float rDistance)
        {
            bool lHit = UnityEngine.Physics.Raycast(rRayStart, rRayDirection, out rHitInfo, rDistance);
            if (!lHit) { return false; }

            if (rHitInfo.collider.isTrigger) { return false; }
            if (rHitInfo.collider.gameObject == gameObject) { return false; }

            return true;
        }

        /// <summary>
        /// When casting a ray from the motion controller, we don't want it to collide with
        /// ourselves. The problem is that we may want to collide with another avatar. So, we
        /// can't put every avatar on their own layer. This ray cast will take a little longer,
        /// but will ignore this avatar.
        /// 
        /// Note: This function isn't virutal to eek out ever ounce of performance we can.
        /// </summary>
        /// <param name="rRayStart"></param>
        /// <param name="rRayDirection"></param>
        /// <param name="rHitInfo"></param>
        /// <param name="rDistance"></param>
        /// <param name="rMask"></param>
        /// <returns></returns>
        public bool SafeRaycast(Vector3 rRayStart, Vector3 rRayDirection, ref RaycastHit rHitInfo, float rDistance, int rMask)
        {
            bool lHit = UnityEngine.Physics.Raycast(rRayStart, rRayDirection, out rHitInfo, rDistance, rMask);
            if (!lHit) { return false; }

            if (rHitInfo.collider.isTrigger) { return false; }
            if (rHitInfo.collider.gameObject == gameObject) { return false; }

            return true;
        }

        /// <summary>
        /// When casting a ray from the motion controller, we don't want it to collide with
        /// ourselves. The problem is that we may want to collide with another avatar. So, we
        /// can't put every avatar on their own layer. This ray cast will take a little longer,
        /// but will ignore this avatar.
        /// 
        /// Note: This function isn't virutal to eek out ever ounce of performance we can.
        /// </summary>
        /// <param name="rRayStart"></param>
        /// <param name="rRayDirection"></param>
        /// <param name="rHitInfo"></param>
        /// <param name="rDistance"></param>
        /// <returns></returns>
        public bool SafeRaycastAll(Vector3 rRayStart, Vector3 rRayDirection, ref RaycastHit rHitInfo, float rDistance)
        {
            RaycastHit[] lHitInfo = UnityEngine.Physics.RaycastAll(rRayStart, rRayDirection, rDistance);
            int lCount = lHitInfo.Length;

            // With one hit, this is easy too
            if (lCount == 1)
            {
                if (lHitInfo[0].collider.isTrigger) { return false; }
                if (lHitInfo[0].collider.gameObject == gameObject) { return false; }
                rHitInfo = lHitInfo[0];

                return true;
            }
            // With no hits, this is easy
            else if (lCount == 0)
            {
                return false;
            }
            // Find the closest hit
            else
            {
                int lLowIndex = 0;
                float lLowDistance = float.MaxValue;

                // We don't expect many collisions, so do this without reallocating
                // another array: lHitInfo = lHitInfo.OrderBy(h => h.distance).ToArray();
                for (int i = 0; i < lCount; i++)
                {
                    if (lHitInfo[i].collider.isTrigger) { continue; }
                    if (lHitInfo[i].collider.gameObject == gameObject) { continue; }

                    if (lHitInfo[i].distance < lLowDistance)
                    {
                        lLowIndex = i;
                        lLowDistance = lHitInfo[i].distance;
                    }
                }

                if (lLowDistance == float.MaxValue) { return false; }
                rHitInfo = lHitInfo[lLowIndex];

                return true;
            }
        }

        /// <summary>
        /// When casting a ray from the motion controller, we don't want it to collide with
        /// ourselves. The problem is that we may want to collide with another avatar. So, we
        /// can't put every avatar on their own layer. This ray cast will take a little longer,
        /// but will ignore this avatar.
        /// 
        /// Note: This function isn't virutal to eek out ever ounce of performance we can.
        /// </summary>
        /// <param name="rRayStart"></param>
        /// <param name="rRayDirection"></param>
        /// <param name="rHitInfo"></param>
        /// <param name="rDistance"></param>
        /// <param name="rMask"></param>
        /// <returns></returns>
        public bool SafeRaycastAll(Vector3 rRayStart, Vector3 rRayDirection, ref RaycastHit rHitInfo, float rDistance, int rMask)
        {
            RaycastHit[] lHitInfo = UnityEngine.Physics.RaycastAll(rRayStart, rRayDirection, rDistance, rMask);
            int lCount = lHitInfo.Length;

            // With one hit, this is easy too
            if (lCount == 1)
            {
                if (lHitInfo[0].collider.isTrigger) { return false; }
                if (lHitInfo[0].collider.gameObject == gameObject) { return false; }
                rHitInfo = lHitInfo[0];

                return true;
            }
            // With no hits, this is easy
            else if (lCount == 0)
            {
                return false;
            }
            // Find the closest hit
            else
            {
                int lLowIndex = 0;
                float lLowDistance = float.MaxValue;

                // We don't expect many collisions, so do this without reallocating
                // another array: lHitInfo = lHitInfo.OrderBy(h => h.distance).ToArray();
                for (int i = 0; i < lCount; i++)
                {
                    if (lHitInfo[i].collider.isTrigger) { continue; }
                    if (lHitInfo[i].collider.gameObject == gameObject) { continue; }

                    if (lHitInfo[i].distance < lLowDistance)
                    {
                        lLowIndex = i;
                        lLowDistance = lHitInfo[i].distance;
                    }
                }

                if (lLowDistance == float.MaxValue) { return false; }
                rHitInfo = lHitInfo[lLowIndex];

                return true;
            }
        }

        /// <summary>
        /// Used to rotate the avatar in the direction of the camera. When in 
        /// first-person mode, the camera needs to do this at the end of the
        /// LateUpdate() or the avatar rotation will be behind the camera and
        /// we get wobbling effects
        /// </summary>
        public virtual void FaceCameraForward()
        {
            if (_CameraTransform == null) { return; }

            // Don't lerp or smooth. Otherwise we get wobbling
            float lAngle = NumberHelper.GetHorizontalAngle(transform.forward, _CameraTransform.forward);
            transform.Rotate(transform.up, lAngle);
        }
    }
}

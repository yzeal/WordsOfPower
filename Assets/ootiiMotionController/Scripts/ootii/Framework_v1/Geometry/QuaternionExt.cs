using System;
using UnityEngine;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Extension for the standard quaternion that allows us to add functions
    /// </summary>
    public static class QuaternionExt
    {
        /// <summary>
        /// Creates a quaterion that represents the rotation required to turn the
        /// original quaterion to the specified quaterion
        /// </summary>
        /// <param name="rFrom"></param>
        /// <param name="rTo"></param>
        /// <returns></returns>
        public static Quaternion RotationTo(this Quaternion rFrom, Quaternion rTo)
        {
            Quaternion lInvFrom = Quaternion.Inverse(rFrom);
            Quaternion lResult = rTo * lInvFrom;
            return lResult;
        }
    }
}

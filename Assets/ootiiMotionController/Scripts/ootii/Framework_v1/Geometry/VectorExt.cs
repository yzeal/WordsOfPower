using System;
using UnityEngine;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Extension for the standard Vector3 that allows us to add functions
    /// </summary>
    public static class VectorExt
    {
        /// <summary>
        /// Search the dictionary based on value and return the key
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <param name="rDictionary">Object the extension is tied to</param>
        /// <param name="rValue">Value that we are searching for</param>
        /// <returns>Returns the first key associated with the value</returns>
        public static float HorizontalMagnitude(this Vector3 rVector)
        {
            return Mathf.Sqrt((rVector.x * rVector.x) + (rVector.z * rVector.z));
        }

        /// <summary>
        /// Search the dictionary based on value and return the key
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <param name="rDictionary">Object the extension is tied to</param>
        /// <param name="rValue">Value that we are searching for</param>
        /// <returns>Returns the first key associated with the value</returns>
        public static float HorizontalSqrMagnitude(this Vector3 rVector)
        {
            return (rVector.x * rVector.x) + (rVector.z * rVector.z);
        }

        /// <summary>
        /// Gets the angle required to reach the target vector
        /// </summary>
        /// <returns>The signed horizontal angle (in degrees).</returns>
        /// <param name="rFrom">Starting vector</param>
        /// <param name="rTo">Resulting vector</param>
        public static float HorizontalAngleTo(this Vector3 rFrom, Vector3 rTo)
        {
            float lAngle = Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(rFrom, rTo)), Vector3.Dot(rFrom, rTo));
            lAngle *= Mathf.Rad2Deg;

            if (Mathf.Abs(lAngle) < 0.0001) { lAngle = 0f; }

            return lAngle;
        }

        /// <summary>
        /// Gets the angle required to reach this vector
        /// </summary>
        /// <returns>The signed horizontal angle (in degrees).</returns>
        /// <param name="rTo">Resulting vector</param>
        /// <param name="rFrom">Starting vector</param>
        public static float HorizontalAngleFrom(this Vector3 rTo, Vector3 rFrom)
        {
            float lAngle = Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(rFrom, rTo)), Vector3.Dot(rFrom, rTo));
            lAngle *= Mathf.Rad2Deg;

            if (Mathf.Abs(lAngle) < 0.0001) { lAngle = 0f; }

            return lAngle;
        }

        /// <summary>
        /// Parses out the vector values given a string
        /// </summary>
        /// <param name="rThis">Vector we are filling</param>
        /// <param name="rString">String containing the vector values. In the form of "(0,0)"</param>
        public static Vector2 FromString(this Vector2 rThis, string rString)
        {
            string[] lTemp = rString.Substring(1, rString.Length - 2).Split(',');
            if (lTemp.Length != 2) { return rThis; }

            rThis.x = float.Parse(lTemp[0]);
            rThis.y = float.Parse(lTemp[1]);
            return rThis;
        }

        /// <summary>
        /// Parses out the vector values given a string
        /// </summary>
        /// <param name="rThis">Vector we are filling</param>
        /// <param name="rString">String containing the vector values. In the form of "(0,0,0)"</param>
        public static Vector3 FromString(this Vector3 rThis, string rString)
        {
            string[] lTemp = rString.Substring(1, rString.Length - 2).Split(',');
            if (lTemp.Length != 3) { return rThis; }

            rThis.x = float.Parse(lTemp[0]);
            rThis.y = float.Parse(lTemp[1]);
            rThis.z = float.Parse(lTemp[2]);
            return rThis;
        }

        /// <summary>
        /// Parses out the vector values given a string
        /// </summary>
        /// <param name="rThis">Vector we are filling</param>
        /// <param name="rString">String containing the vector values. In the form of "(0,0,0)"</param>
        public static Vector4 FromString(this Vector4 rThis, string rString)
        {
            string[] lTemp = rString.Substring(1, rString.Length - 2).Split(',');
            if (lTemp.Length != 4) { return rThis; }

            rThis.x = float.Parse(lTemp[0]);
            rThis.y = float.Parse(lTemp[1]);
            rThis.z = float.Parse(lTemp[2]);
            rThis.w = float.Parse(lTemp[3]);
            return rThis;
        }
    }
}

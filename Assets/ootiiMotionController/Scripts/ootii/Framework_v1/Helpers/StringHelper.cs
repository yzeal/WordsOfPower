/// Tim Tryzbiak, ootii, LLC
using System;
using UnityEngine;

namespace com.ootii.Helpers
{
    /// <summary>
    /// Static functions to help us
    /// </summary>
    public class StringHelper
    {
        /// <summary>
        /// Converts the data to a string value
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="rInput">Value to convert</param>
        public static string ToString(Vector3 rInput)
        {
            return String.Format("[{0:f4},{1:f4},{2:f4}]", rInput.x, rInput.y, rInput.z);
        }

        /// <summary>
        /// Converts the data to a string value
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="rInput">Value to convert</param>
        public static string ToString(Quaternion rInput)
        {
            Vector3 lEuler = rInput.eulerAngles;
            return String.Format("[p:{0:f4} y:{1:f4} r:{2:f4}]", lEuler.x, lEuler.y, lEuler.z);
        }
    }
}


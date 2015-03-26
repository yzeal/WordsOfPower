using System;
using UnityEngine;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Simple class to help find the running average of values.
    /// Meant to be fast by using fixed arrays.
    /// </summary>
    public class Vector3Value
    {
        /// <summary>
        /// Determines the number of samples to keep track of over time
        /// </summary>
        private int mSampleCount = 20;
        public int SampleCount
        {
            get { return mSampleCount; }
            
            set 
            {
                if (value > 0)
                {
                    mSampleCount = value;
                    Resize(mSampleCount, mDefault);
                }
            }
        }

        /// <summary>
        /// Current sum of the sample set
        /// </summary>
        private Vector3 mSum;
        public Vector3 Sum
        {
            get { return mSum; }
        }

        /// <summary>
        /// Current average of the sample set
        /// </summary>
        private Vector3 mAverage;
        public Vector3 Average
        {
            get { return mAverage; }
        }

        /// <summary>
        /// Value of the last added sample
        /// </summary>
        private Vector3 mValue;
        public Vector3 Value
        {
            get { return mValue; }

            set
            {
                mValue = value;
                mSamples[mIndex % mSampleCount] = mValue;

                mSum = Vector3.zero;
                for (int i = 0; i < mSampleCount; i++)
                {
                    mSum += mSamples[i];
                }

                mAverage = mSum / mSampleCount; 
            }
        }

        /// <summary>
        /// Samples we're storing
        /// </summary>
        private Vector3[] mSamples = null;

        /// <summary>
        /// Default value for the samples
        /// </summary>
        private Vector3 mDefault = Vector3.zero;

        /// <summary>
        /// Index to place the next sample value
        /// </summary>
        private int mIndex = -1;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Vector3Value()
        {
            Resize(mSampleCount, mDefault);
        }

        /// <summary>
        /// Size constructor constructor
        /// </summary>
        public Vector3Value(int rSampleCount, Vector3 rDefault)
        {
            mDefault = rDefault;
            mSampleCount = rSampleCount;
            Resize(mSampleCount, mDefault);
        }

        /// <summary>
        /// Adds a value to the sample set and returns the
        /// average of the sample
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public Vector3 Add(Vector3 rValue)
        {
            mValue = rValue;

            mIndex++;
            mSamples[mIndex % mSampleCount] = mValue;

            mSum = Vector3.zero;
            for (int i = 0; i < mSampleCount; i++)
            {
                mSum += mSamples[i];
            }

            mAverage = mSum / mSampleCount;            
            return mAverage;
        }

        /// <summary>
        /// Resize the array
        /// </summary>
        /// <param name="rSize">New size of the array</param>
        private void Resize(int rSize, Vector3 rDefault)
        {
            lock (this)
            {
                int lCount = 0;

                // Build the new array and copy the contents
                Vector3[] lNewSamples = new Vector3[rSize];

                if (mSamples != null)
                {
                    lCount = mSamples.Length;
                    Array.Copy(mSamples, lNewSamples, Math.Min(lCount, rSize));

                    // Allocate items in the new array
                    for (int i = lCount; i < rSize; i++)
                    {
                        lNewSamples[i] = rDefault;
                    }
                }

                // Replace the old array
                mSamples = lNewSamples;
            }
        }
    }
}

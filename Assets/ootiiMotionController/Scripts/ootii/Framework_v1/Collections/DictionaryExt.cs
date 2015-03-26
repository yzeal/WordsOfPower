using System;
using System.Collections.Generic;

namespace com.ootii.Collections
{
    /// <summary>
    /// Extension for the standard dictionary that allows us to add functions
    /// </summary>
    public static class DictionaryExt
    {
        /// <summary>
        /// Search the dictionary based on value and return the key
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <param name="rDictionary">Object the extension is tied to</param>
        /// <param name="rValue">Value that we are searching for</param>
        /// <returns>Returns the first key associated with the value</returns>
        public static TKey FindKeyByValue<TKey, TValue>(this Dictionary<TKey, TValue> rDictionary, TValue rValue)
        {
            // Ensure we have a valid option
            if (rDictionary == null) { throw new ArgumentNullException("rDictionary"); }

            // Search for the value and return the associated key
            foreach (KeyValuePair<TKey, TValue> lPair in rDictionary)
            {
                if (rValue.Equals(lPair.Value)) { return lPair.Key; }
            }

            // If not found, report it
            throw new KeyNotFoundException("Value not found");
        }
    }
}

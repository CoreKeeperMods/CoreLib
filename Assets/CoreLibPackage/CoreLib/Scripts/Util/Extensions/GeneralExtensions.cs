using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using PugMod;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Provides a collection of extension methods for general utility and functionality enhancements.
    /// </summary>
    public static class GeneralExtensions
    {
        /// <summary>
        /// A private readonly array of characters that specifies character separators.
        /// </summary>
        /// <remarks>
        /// This array is used to identify directory or file path separators in strings,
        /// such as '/' for UNIX-style paths or '\\' for Windows-style paths.
        /// </remarks>
        private static readonly char[] separators =
        {
            '/',
            '\\'
        };

        /// Generates a globally unique identifier (GUID) in string format based on the provided input string.
        /// <param name="objectId">
        /// A string input used to compute the GUID. This input is hashed using the MD5 algorithm to generate the GUID.
        /// </param>
        /// <returns>
        /// A string representing the generated GUID in a 32-character, lowercase, hexadecimal format (without dashes).
        /// </returns>
        public static string GetGUID(this string objectId)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(objectId));
            Guid result = new Guid(hash);
            return result.ToString("N");
        }

        /// <summary>
        /// Retrieves all child transforms of a given parent transform and adds them to a list.
        /// </summary>
        /// <param name="parent">The parent Transform whose children will be retrieved.</param>
        /// <param name="transformList">An optional list to populate with the child Transforms. If null, a new list will be created.</param>
        /// <returns>A list of all child Transforms of the given parent Transform, including descendants.</returns>
        public static List<Transform> GetAllChildren(this Transform parent, List<Transform> transformList = null)
        {
            if (transformList == null) transformList = new List<Transform>();

            foreach (var o in parent)
            {
                var child = (Transform)o;
                transformList.Add(child);
                child.GetAllChildren(transformList);
            }

            return transformList;
        }

        /// <summary>
        /// Copies data from a byte array into the specified FixedArray64 structure.
        /// </summary>
        /// <param name="fixedArray">The FixedArray64 structure to copy the data into.</param>
        /// <param name="bytes">The byte array containing the source data.</param>
        /// <param name="startIndex">The zero-based index in the byte array at which to begin copying data.</param>
        public static void CopyFrom(this ref FixedArray64 fixedArray, byte[] bytes, int startIndex)
        {
            var size = math.min(fixedArray.Size, bytes.Length - startIndex);
            byte[] dataBytes = new byte[size];
            Array.Copy(bytes, startIndex, dataBytes, 0, size);
            fixedArray.Set(dataBytes);
        }

        /// <summary>
        /// Copies data from the given FixedArray64 instance to a byte array, starting at the specified index in the target array.
        /// </summary>
        /// <param name="fixedArray">The FixedArray64 instance to copy data from.</param>
        /// <param name="bytes">The target byte array where the data will be copied.</param>
        /// <param name="startIndex">The starting index in the target byte array where the data will be copied.</param>
        public static void CopyTo(this ref FixedArray64 fixedArray, byte[] bytes, int startIndex)
        {
            var size = math.min(fixedArray.Size, bytes.Length - startIndex);
            var dataBytes = fixedArray.ToArray<byte>(size);
            Array.Copy(dataBytes, 0, bytes, startIndex, size);
        }

        /// Converts the given string into an ObjectID by attempting an enum conversion.
        /// If the conversion fails, retrieves the ObjectID via an external API.
        /// <param name="value">
        /// The string representation of the object that needs to be converted into an ObjectID.
        /// </param>
        /// <returns>
        /// The corresponding ObjectID if the conversion is successful, or a fallback ObjectID
        /// retrieved from an external API. If all attempts fail, it returns a default ObjectID value.
        /// </returns>
        public static ObjectID GetObjectID(this string value)
        {
            if (Enum.TryParse(value, true, out ObjectID objectID))
            {
                return objectID;
            }

            return API.Authoring.GetObjectID(value);
        }

        /// Extracts the file name from a given file path, including the file extension, if present.
        /// <param name="path">
        /// The full file path from which the file name will be extracted. The path may include directory separators such as '/' or '\\'.
        /// </param>
        /// <returns>
        /// A string representing the file name, including its extension, extracted from the provided file path.
        /// </returns>
        public static string GetFileName(this string path)
        {
            return path.Substring(path.LastIndexOfAny(separators) + 1);
        }
    }
}
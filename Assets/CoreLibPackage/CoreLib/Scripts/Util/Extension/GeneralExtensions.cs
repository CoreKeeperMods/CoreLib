// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: GeneralExtensions.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides a set of general-purpose extension methods for string manipulation,
//              data conversion, and Unity object traversal used throughout CoreLib.
// ========================================================

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Pug.UnityExtensions;
using PugMod;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extension
{
    /// Provides a collection of extension methods that offer general-purpose
    /// utility functionality across CoreLib and mod development.
    /// <remarks>
    /// Includes helper methods for generating GUIDs, retrieving Unity child transforms,
    /// copying fixed array data, and working with file paths or object identifiers.
    /// </remarks>
    /// <seealso cref="ObjectID"/>
    /// <seealso cref="UnityEngine.Transform"/>
    /// <seealso cref="System.Guid"/>
    public static class GeneralExtensions
    {
        #region Fields

        /// Character separator definitions used for file and directory path operations.
        /// <remarks>
        /// This array allows compatibility with both UNIX (<c>/</c>) and Windows (<c>\\</c>) file systems.
        /// </remarks>
        private static readonly char[] Separators =
        {
            '/',
            '\\'
        };

        #endregion

        #region String and GUID Utilities

        /// Generates a deterministic globally unique identifier (GUID) from a given input string.
        /// <param name="objectId">
        /// A string value used to compute the GUID. The input is hashed via MD5 to produce a unique, consistent identifier.
        /// </param>
        /// <returns>
        /// A 32-character lowercase hexadecimal string representing the generated GUID (without dashes).
        /// </returns>
        /// <remarks>
        /// This method ensures stable GUID generation for any string input, making it suitable for mapping
        /// between string-based identifiers and unique object references.
        /// </remarks>
        /// <example>
        /// <code>
        /// string guid = "CoreLib.Audio".GetGuid();
        /// Debug.Log(guid); // Outputs a consistent unique ID string for "CoreLib.Audio"
        /// </code>
        /// </example>
        /// <seealso cref="System.Guid"/>
        /// <seealso cref="System.Security.Cryptography.MD5"/>
        public static string GetGuid(this string objectId)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(objectId));
            var result = new Guid(hash);
            return result.ToString("N");
        }

        #endregion

        #region Unity Transform Utilities

        /// Retrieves all child transforms (including nested descendants) from a given Unity <see cref="Transform"/>.
        /// <param name="parent">The parent <see cref="Transform"/> whose children are to be collected.</param>
        /// <param name="transformList">
        /// An optional list to populate with the child transforms. If omitted, a new list will be created.
        /// </param>
        /// <returns>
        /// A list containing all descendant <see cref="Transform"/> objects under the specified parent.
        /// </returns>
        /// <remarks>
        /// This method recursively traverses all child transforms to ensure full hierarchy retrieval.
        /// </remarks>
        /// <example>
        /// <code>
        /// List&lt;Transform&gt; children = transform.GetAllChildren();
        /// Debug.Log($"Found {children.Count} total child objects.");
        /// </code>
        /// </example>
        /// <seealso cref="UnityEngine.Transform"/>
        public static List<Transform> GetAllChildren(this Transform parent, List<Transform> transformList = null)
        {
            transformList ??= new List<Transform>();

            foreach (object o in parent)
            {
                var child = (Transform)o;
                transformList.Add(child);
                child.GetAllChildren(transformList);
            }

            return transformList;
        }

        #endregion

        #region Data Copy Utilities

        /// Copies data from a byte array into the specified <see cref="FixedArray64"/> structure.
        /// <param name="fixedArray">The target <see cref="FixedArray64"/> to populate.</param>
        /// <param name="bytes">The byte array containing the data to copy.</param>
        /// <param name="startIndex">The starting index in the byte array from which copying begins.</param>
        /// <remarks>
        /// The amount of data copied is limited by the smaller of the fixed array’s size
        /// or the remaining length of the source byte array.
        /// </remarks>
        /// <seealso cref="Unity.Mathematics.math.min(float,float)"/>
        public static void CopyFrom(this ref FixedArray64 fixedArray, byte[] bytes, int startIndex)
        {
            int size = math.min(fixedArray.Size, bytes.Length - startIndex);
            byte[] dataBytes = new byte[size];
            Array.Copy(bytes, startIndex, dataBytes, 0, size);
            fixedArray.Set(dataBytes);
        }

        /// Copies data from a <see cref="FixedArray64"/> into a byte array at a specified index.
        /// <param name="fixedArray">The source <see cref="FixedArray64"/> containing data to copy.</param>
        /// <param name="bytes">The destination byte array to copy data into.</param>
        /// <param name="startIndex">The position in the destination array where copying begins.</param>
        /// <remarks>
        /// Only data that fits within the available space in the destination array will be copied.
        /// </remarks>
        /// <seealso cref="Array.Copy(Array,int,Array,int,int)"/>
        public static void CopyTo(this ref FixedArray64 fixedArray, byte[] bytes, int startIndex)
        {
            int size = math.min(fixedArray.Size, bytes.Length - startIndex);
            byte[] dataBytes = fixedArray.ToArray<byte>(size);
            Array.Copy(dataBytes, 0, bytes, startIndex, size);
        }

        #endregion

        #region Object and File Utilities

        /// Converts a string into an <see cref="ObjectID"/> either by direct enum parsing or by using an external authoring lookup.
        /// <param name="value">The string representation of an object to convert.</param>
        /// <returns>
        /// A corresponding <see cref="ObjectID"/> value if found; otherwise, a default or fallback value retrieved from <see cref="API.Authoring"/>.
        /// </returns>
        /// <remarks>
        /// This method first attempts to parse the string as a known <see cref="ObjectID"/> enum.
        /// If parsing fails, it calls <see cref="ModAPIAuthoring.GetObjectID(string)"/> for external resolution.
        /// </remarks>
        /// <seealso cref="ObjectID"/>
        /// <seealso cref="ModAPIAuthoring.GetObjectID(string)"/>
        public static ObjectID GetObjectID(this string value)
        {
            return Enum.TryParse(value, true, out ObjectID objectID)
                ? objectID
                : API.Authoring.GetObjectID(value);
        }

        /// Extracts the file name (including extension) from a given file path.
        /// <param name="path">The full path to extract the file name from.</param>
        /// <returns>The extracted file name, including its extension.</returns>
        /// <remarks>
        /// This method supports both Windows and UNIX path separators.
        /// </remarks>
        /// <example>
        /// <code>
        /// string fileName = "Assets/Audio/Music/Theme.wav".GetFileName();
        /// Debug.Log(fileName); // Outputs: "Theme.wav"
        /// </code>
        /// </example>
        public static string GetFileName(this string path)
        {
            return path[(path.LastIndexOfAny(Separators) + 1)..];
        }

        #endregion
    }
}
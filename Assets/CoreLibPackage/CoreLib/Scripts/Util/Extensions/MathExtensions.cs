using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Provides extension methods for seamless conversion between Unity.Mathematics types and UnityEngine types.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Converts a float2 to a Vector2.
        /// </summary>
        /// <param name="vec">The float2 input to be converted.</param>
        /// <returns>A Vector2 with x and y values derived from the float2.</returns>
        public static Vector2 ToVector2(this float2 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        /// <summary>
        /// Converts a <see cref="float3"/> to a <see cref="Vector2"/> by taking the X and Z components of the <see cref="float3"/>.
        /// </summary>
        /// <param name="vec">The <see cref="float3"/> to convert.</param>
        /// <returns>A <see cref="Vector2"/> containing the X and Z components of the input <see cref="float3"/>.</returns>
        public static Vector2 ToVector2(this float3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }

        /// <summary>
        /// Converts a UnityEngine.Vector2 to a Unity.Mathematics.float2.
        /// </summary>
        /// <param name="vec">The Vector2 instance to convert.</param>
        /// <returns>A float2 representing the x and y values of the input Vector2.</returns>
        public static float2 ToFloat2(this Vector2 vec)
        {
            return new float2(vec.x, vec.y);
        }

        /// <summary>
        /// Converts a Vector3 to a float2 by taking the X and Z components.
        /// </summary>
        /// <param name="vec">The Vector3 to convert.</param>
        /// <returns>A float2 containing the X and Z components of the input Vector3.</returns>
        public static float2 ToFloat2(this Vector3 vec)
        {
            return new float2(vec.x, vec.z);
        }

        /// <summary>
        /// Converts a float3 to a Vector3.
        /// </summary>
        /// <param name="vec">The float3 input to be converted.</param>
        /// <returns>A Vector3 with x, y, and z values derived from the float3.</returns>
        public static Vector3 ToVector3(this float3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// Converts a Vector3 to a float3.
        /// </summary>
        /// <param name="vec">The input Vector3 to be converted.</param>
        /// <returns>A float3 representation of the input Vector3.</returns>
        public static float3 ToFloat3(this Vector3 vec)
        {
            return new float3(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// Converts an int3 to a Vector3.
        /// </summary>
        /// <param name="vec">The int3 input to be converted.</param>
        /// <returns>A Vector3 with x, y, and z values derived from the int3.</returns>
        public static Vector3 ToVector3(this int3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        
    }
}
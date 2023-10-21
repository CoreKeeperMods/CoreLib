using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Util.Extensions
{
    public static class MathExtensions
    {
        public static Vector2 ToVector2(this float2 vec)
        {
            return new Vector2(vec.x, vec.y);
        }
        
        public static Vector2 ToVector2(this float3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }
        
        public static float2 ToFloat2(this Vector2 vec)
        {
            return new float2(vec.x, vec.y);
        }
        
        public static float2 ToFloat2(this Vector3 vec)
        {
            return new float2(vec.x, vec.z);
        }
        
        public static Vector3 ToVector3(this float3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }
        
        public static float3 ToFloat3(this Vector3 vec)
        {
            return new float3(vec.x, vec.y, vec.z);
        }
        
        public static Vector3 ToVector3(this int3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        
    }
}
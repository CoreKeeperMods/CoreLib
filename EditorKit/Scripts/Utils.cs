using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLibKit
{
    public static class Utils
    {
        public static void ClearChildren(this GameObject thatObject)
        {
            List<GameObject> allChildren = new List<GameObject>(thatObject.transform.childCount);

            foreach (Transform child in thatObject.transform)
            {
                allChildren.Add(child.gameObject);
            }

            if (allChildren.Count == 0) return;
            
            foreach (GameObject child in allChildren)
            {
                Object.DestroyImmediate(child);
            }
        }

        public static Vector3Int ToVector(this int3 value)
        {
            return new Vector3Int(value.x, value.y, value.z);
        }

        public static Vector4 ToVector(this Rect rect)
        {
            return new Vector4(rect.x, rect.y, rect.width, rect.height);
        }
        
        public static float4 ToFloat4(this Rect rect)
        {
            return new float4(rect.x, rect.y, rect.width, rect.height);
        }
    }
}
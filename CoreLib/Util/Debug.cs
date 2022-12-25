using System.Collections.Generic;
using CoreLib.Util.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib
{
    /// <summary>
    /// Utility to display dots at specific points in the world
    /// </summary>
    public static class Debug
    {
        public struct MarkerData
        {
            public GameObject gameObject;
            public MeshRenderer renderer;

            public MarkerData(GameObject gameObject, MeshRenderer renderer)
            {
                this.gameObject = gameObject;
                this.renderer = renderer;
            }

            public void Set(Vector3 pos, Color color)
            {
                renderer.material.color = color;
                gameObject.transform.localPosition = pos;
                gameObject.SetActive(true);
            }

            public void Clear()
            {
                gameObject.SetActive(false);
            }
        }
        
        private static List<MarkerData> activeMarkers = new List<MarkerData>();
        private static Queue<MarkerData> freeMarkers = new Queue<MarkerData>();

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayPos(float3 pos, Color color)
        {
            DisplayPos(pos.ToVector3(), color);
        }
        
        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayPos(Vector3 pos, Color color)
        {
            MarkerData marker = CreateMarker();
            marker.Set(pos, color);

            activeMarkers.Add(marker);
        }

        /// <summary>
        /// Clear all dots
        /// </summary>
        public static void ClearDisplay()
        {
            foreach (MarkerData marker in activeMarkers)
            {
                marker.Clear();
                freeMarkers.Enqueue(marker);
            }

            activeMarkers.Clear();
        }
        
        private static MarkerData CreateMarker()
        {
            if (freeMarkers.Count > 0)
            {
                return freeMarkers.Dequeue();
            }
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
            Object.Destroy(sphere.GetComponent<SphereCollider>());
            meshRenderer.material.shader = Shader.Find("Radical/SpritesDefault");
            sphere.transform.parent = Manager.camera.OrigoTransform;
            sphere.transform.localScale *= 0.2f;

            MarkerData data = new MarkerData(sphere, meshRenderer);
            return data;
        }
    }
}
using System.Collections.Generic;
using CoreLib.Util.Extension;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Util
{
    /// <summary>
    /// Utility to display dots at specific points in the world
    /// </summary>
    public static class PosDebug
    {
        private static readonly List<MarkerData> _activeMarkers = new List<MarkerData>();
        
        private static readonly Queue<MarkerData> _freePointMarkers = new Queue<MarkerData>();
        private static readonly Queue<MarkerData> _freeLineMarkers = new Queue<MarkerData>();

        private static Transform _myAnchor;

        private static Transform GetMyAnchor()
        {
            if (_myAnchor != null) return _myAnchor;

            _myAnchor = Manager.camera.GetRenderAnchor();
            return _myAnchor;
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void Display(float3 pos, Color color)
        {
            Display(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void Display(int3 pos, Color color)
        {
            Display(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void Display(Vector3 pos, Color color)
        {
            Vector3 localPos = pos - Manager.camera.RenderOrigo;
            DisplayLocal(localPos, color);
        }


        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayLocal(float3 pos, Color color)
        {
            DisplayLocal(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayLocal(int3 pos, Color color)
        {
            DisplayLocal(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayLocal(Vector3 pos, Color color)
        {
            MarkerData marker = CreateMarker(false);
            pos.y += 1f;
            pos.z -= 1f;
            marker.Set(pos, color);

            _activeMarkers.Add(marker);
        }

        public static void DisplayLine(float3 pos1, float3 pos2, Color color)
        {
            DisplayLine(pos1.ToVector3(), pos2.ToVector3(), color);
        }
        
        public static void DisplayLine(int3 pos1, int3 pos2, Color color)
        {
            DisplayLine(pos1.ToVector3(), pos2.ToVector3(), color);
        }

        public static void DisplayLine(Vector3 pos1, Vector3 pos2, Color color)
        {
            MarkerData marker = CreateMarker(true);

            pos1.y += 1f;
            pos1.z -= 1f;
            
            pos2.y += 1f;
            pos2.z -= 1f;
            marker.SetLine(pos1, pos2, color);

            _activeMarkers.Add(marker);
        }

        /// <summary>
        /// Clear all dots
        /// </summary>
        public static void ClearDisplay()
        {
            foreach (MarkerData marker in _activeMarkers)
            {
                marker.Clear();

                if (marker.isLine)
                {
                    _freeLineMarkers.Enqueue(marker);
                }
                else
                {
                    _freePointMarkers.Enqueue(marker);
                }
            }

            _activeMarkers.Clear();
        }

        private static MarkerData CreateMarker(bool line)
        {
            if (!line)
            {
                if (_freePointMarkers.Count > 0)
                    return _freePointMarkers.Dequeue();
                
                return CreatePointMarker();
            }
            else
            {
                if (_freeLineMarkers.Count > 0)
                    return _freeLineMarkers.Dequeue();
                
                return CreateLineMarker();
            }
        }

        private static MarkerData CreatePointMarker()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "DebugPoint";

            MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
            Object.Destroy(sphere.GetComponent<SphereCollider>());
            meshRenderer.material.shader = Shader.Find("Radical/SpritesDefault");
            sphere.transform.parent = GetMyAnchor();
            sphere.transform.localScale *= 0.2f;

            MarkerData data = new MarkerData(sphere, meshRenderer, null);
            return data;
        }

        private static MarkerData CreateLineMarker()
        {
            
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
            line.name = "DebugLine";
            
            MeshRenderer meshRenderer = line.GetComponent<MeshRenderer>();
            Object.Destroy(line.GetComponent<MeshCollider>());
            meshRenderer.material.shader = Shader.Find("Radical/SpritesDefault");

            line.transform.parent = GetMyAnchor();

            MarkerData data = new MarkerData(line, null, meshRenderer);
            return data;
        }
        
    }
}
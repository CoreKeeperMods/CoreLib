using System;
using System.Collections.Generic;
using CoreLib.Util.Extensions;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Util
{
    /// <summary>
    /// Utility to display dots at specific points in the world
    /// </summary>
    public static class Debug
    {
        [Serializable]
        public struct MarkerData
        {
            public bool isLine;

            public GameObject gameObject;
            public MeshRenderer meshRenderer;
            public MeshRenderer quadRenderer;
            //public LineRenderer lineRenderer;

            public MarkerData(GameObject gameObject, MeshRenderer meshRenderer, MeshRenderer quadRenderer)
            {
                this.gameObject = gameObject;
                this.meshRenderer = meshRenderer;
                this.quadRenderer = quadRenderer;

                isLine = this.quadRenderer != null;
            }

            public void Set(Vector3 pos, Color color)
            {
                if (isLine) return;
                
                meshRenderer.material.color = color;
                gameObject.transform.localPosition = pos;
                gameObject.SetActive(true);
            }

            public void SetLine(Vector3 pos1, Vector3 pos2, Color color)
            {
                if (!isLine) return;
                
                quadRenderer.material.color = color;

                var dist = Vector3.Distance(pos1, pos2);
                var mid = (pos1 + pos2) / 2;
                var dir = (pos2 - pos1).normalized;
                var rotation = Vector2.SignedAngle(Vector2.right, new Vector2(dir.x, dir.z));
                
                gameObject.transform.localPosition = mid;
                gameObject.transform.localScale = new Vector3(dist, 0.1f, 1);
                gameObject.transform.localRotation = Quaternion.Euler(-90, -rotation, 0);
                
                //lineRenderer.SetPosition(0, pos1);
                //lineRenderer.SetPosition(1, pos2);
                gameObject.SetActive(true);
            } 

            public void Clear()
            {
                gameObject.SetActive(false);
            }
        }

        private static List<MarkerData> activeMarkers = new List<MarkerData>();
        
        private static Queue<MarkerData> freePointMarkers = new Queue<MarkerData>();
        private static Queue<MarkerData> freeLineMarkers = new Queue<MarkerData>();

        private static Transform myAnchor;

        private static Transform GetMyAnchor()
        {
            if (myAnchor != null) return myAnchor;

            myAnchor = Manager.camera.GetRenderAnchor();
            return myAnchor;
        }

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
        public static void DisplayPos(int3 pos, Color color)
        {
            DisplayPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayPos(Vector3 pos, Color color)
        {
            Vector3 localPos = pos - Manager.camera.RenderOrigo;
            DisplayLocalPos(localPos, color);
        }


        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayLocalPos(float3 pos, Color color)
        {
            DisplayLocalPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayLocalPos(int3 pos, Color color)
        {
            DisplayLocalPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Place a colored dot at specified position
        /// </summary>
        public static void DisplayLocalPos(Vector3 pos, Color color)
        {
            MarkerData marker = CreateMarker(false);
            pos.y += 1f;
            pos.z -= 1f;
            marker.Set(pos, color);

            activeMarkers.Add(marker);
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

                if (marker.isLine)
                {
                    freeLineMarkers.Enqueue(marker);
                }
                else
                {
                    freePointMarkers.Enqueue(marker);
                }
            }

            activeMarkers.Clear();
        }

        private static MarkerData CreateMarker(bool line)
        {
            if (!line)
            {
                if (freePointMarkers.Count > 0)
                    return freePointMarkers.Dequeue();
                
                return CreatePointMarker();
            }
            else
            {
                if (freeLineMarkers.Count > 0)
                    return freeLineMarkers.Dequeue();
                
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
            //GameObject line = new GameObject("DebugLine");
            
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
            line.name = "DebugLine";
            
            /*LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
            
            var material = new Material(Shader.Find("Radical/SpritesDefault"));
            lineRenderer.material = material;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;*/
            
            MeshRenderer meshRenderer = line.GetComponent<MeshRenderer>();
            Object.Destroy(line.GetComponent<MeshCollider>());
            meshRenderer.material.shader = Shader.Find("Radical/SpritesDefault");

            line.transform.parent = GetMyAnchor();
            //line.transform.localScale *= 0.2f;

            MarkerData data = new MarkerData(line, null, meshRenderer);
            return data;
        }
        
    }
}
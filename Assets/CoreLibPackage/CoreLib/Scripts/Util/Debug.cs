using System;
using System.Collections.Generic;
using CoreLib.Util.Extensions;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Util
{
    /// <summary>
    /// Provides debugging utilities for displaying visual markers and lines in a 3D world space.
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Represents visual marker data used for debugging purposes in a 3D world space.
        /// </summary>
        /// <remarks>
        /// This struct is designed to encapsulate the functionality of visual markers,
        /// which can represent either points or lines. It includes properties to manage
        /// associated game objects, renderers, and methods to configure or clear the marker state.
        /// </remarks>
        [Serializable]
        public struct MarkerData
        {
            /// <summary>
            /// Indicates whether the marker data represents a line.
            /// </summary>
            /// <remarks>
            /// This boolean value is used to differentiate between markers that represent points
            /// and those that represent lines in the 3D world space. A value of <c>true</c> means
            /// the marker represents a line, while <c>false</c> means it represents a point.
            /// Typically set based on the presence of a quad renderer.
            /// </remarks>
            public bool isLine;

            /// <summary>
            /// Represents a GameObject used as part of the marker data for debugging visualization.
            /// </summary>
            /// <remarks>
            /// This GameObject is utilized to represent markers or lines in a 3D space for debugging purposes.
            /// It can be manipulated to display at specific positions, set active or inactive, and scaled or rotated
            /// depending on the type of visualization being rendered (e.g., points or lines).
            /// </remarks>
            public GameObject gameObject;

            /// <summary>
            /// Represents the <see cref="MeshRenderer"/> component associated with a 3D object in the Unity engine.
            /// It is used to render the mesh of the object and allows customization of material properties, appearance,
            /// and visibility within the scene.
            /// </summary>
            public MeshRenderer meshRenderer;

            /// <summary>
            /// Represents the renderer component of a quad used for visualizing lines or markers in the debug system.
            /// </summary>
            /// <remarks>
            /// The <c>quadRenderer</c> is responsible for rendering a rectangular mesh that can be used to simulate lines
            /// or visual aids in the debugging system. It is typically used when visualizing line-like structures
            /// (e.g., connecting two points in 3D space) and is manipulated to match the line's position, scale, and orientation.
            /// </remarks>
            public MeshRenderer quadRenderer;
            //public LineRenderer lineRenderer;

            /// <summary>
            /// Represents a structure to hold data for a debug marker, which includes a GameObject and its associated renderers.
            /// </summary>
            public MarkerData(GameObject gameObject, MeshRenderer meshRenderer, MeshRenderer quadRenderer)
            {
                this.gameObject = gameObject;
                this.meshRenderer = meshRenderer;
                this.quadRenderer = quadRenderer;

                isLine = this.quadRenderer != null;
            }

            /// <summary>
            /// Updates the position and color of the marker, and activates the associated GameObject,
            /// unless the marker is set as a line.
            /// </summary>
            /// <param name="pos">The new position to set for the marker.</param>
            /// <param name="color">The color to apply to the marker's material.</param>
            public void Set(Vector3 pos, Color color)
            {
                if (isLine) return;
                
                meshRenderer.material.color = color;
                gameObject.transform.localPosition = pos;
                gameObject.SetActive(true);
            }

            /// <summary>
            /// Configures and displays a line between two specified positions with a given color.
            /// </summary>
            /// <param name="pos1">The starting position of the line.</param>
            /// <param name="pos2">The ending position of the line.</param>
            /// <param name="color">The color of the line.</param>
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

            /// <summary>
            /// Deactivates the GameObject associated with the marker.
            /// </summary>
            public void Clear()
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Maintains a list of active markers currently displayed in the 3D world space.
        /// </summary>
        /// <remarks>
        /// This list stores marker data representing either points or lines being rendered for debug purposes.
        /// It is used internally to manage, update, and clear visual markers during runtime. Each marker
        /// contains metadata such as whether it represents a line or a point and references to its
        /// associated game objects and renderers.
        /// </remarks>
        private static List<MarkerData> activeMarkers = new List<MarkerData>();

        /// <summary>
        /// A queue that holds reusable point marker data for optimization purposes.
        /// </summary>
        /// <remarks>
        /// This collection is used to store instances of <see cref="MarkerData"/> that represent point markers,
        /// allowing them to be recycled instead of frequently creating and destroying new instances.
        /// It helps improve performance by reducing allocations and garbage collection in scenarios
        /// where a large number of point markers are displayed and cleared dynamically.
        /// </remarks>
        private static Queue<MarkerData> freePointMarkers = new Queue<MarkerData>();

        /// <summary>
        /// A queue that stores reusable line marker data for efficient rendering and management.
        /// </summary>
        /// <remarks>
        /// This collection holds instances of <c>MarkerData</c> that represent line markers
        /// in the 3D world space. It is designed to recycle unused markers, reducing the overhead
        /// of creating and destroying marker instances. Markers can be enqueued when no longer needed
        /// and dequeued when a new marker is required, promoting efficient resource management.
        /// </remarks>
        private static Queue<MarkerData> freeLineMarkers = new Queue<MarkerData>();

        /// <summary>
        /// A reference to a specific Unity transform used as an anchor for rendering and positioning debug visuals.
        /// </summary>
        /// <remarks>
        /// This transform is primarily used for debugging purposes to anchor visual elements
        /// such as markers and lines in the 3D world space. It is dynamically initialized
        /// to correspond to the render anchor of the current camera via the <c>Manager.camera.GetRenderAnchor()</c> method.
        /// Subsequent access to this property returns the cached transform reference.
        /// </remarks>
        private static Transform myAnchor;

        /// <summary>
        /// Retrieves the anchor Transform used as the parent for debug markers. If the anchor does not exist, it is initialized and returned.
        /// </summary>
        /// <returns>
        /// The Transform instance that represents the anchor for debug markers.
        /// </returns>
        private static Transform GetMyAnchor()
        {
            if (myAnchor != null) return myAnchor;

            myAnchor = Manager.camera.GetRenderAnchor();
            return myAnchor;
        }

        /// <summary>
        /// Displays a colored dot at the specified position in world space.
        /// </summary>
        /// <param name="pos">The position where the dot will be displayed.</param>
        /// <param name="color">The color of the dot.</param>
        public static void DisplayPos(float3 pos, Color color)
        {
            DisplayPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Places a colored visual marker at a specified position in world space.
        /// </summary>
        /// <param name="pos">The position in 3D space where the marker will be placed.</param>
        /// <param name="color">The color of the marker to be displayed.</param>
        public static void DisplayPos(int3 pos, Color color)
        {
            DisplayPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Displays a visual marker, represented as a colored dot, at a specified position in 3D world space.
        /// </summary>
        /// <param name="pos">The 3D position (in world space) where the marker will be displayed.</param>
        /// <param name="color">The color of the marker to be displayed.</param>
        public static void DisplayPos(Vector3 pos, Color color)
        {
            Vector3 localPos = pos - Manager.camera.RenderOrigo;
            DisplayLocalPos(localPos, color);
        }


        /// <summary>
        /// Displays a colored dot at the specified local position relative to the rendering origin.
        /// </summary>
        /// <param name="pos">The position in local space where the dot will be displayed.</param>
        /// <param name="color">The color of the dot to be displayed.</param>
        public static void DisplayLocalPos(float3 pos, Color color)
        {
            DisplayLocalPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Displays a visual marker at a specified local position relative to the render origin, using a specified color.
        /// </summary>
        /// <param name="pos">The local position to display the marker at.</param>
        /// <param name="color">The color of the marker.</param>
        public static void DisplayLocalPos(int3 pos, Color color)
        {
            DisplayLocalPos(pos.ToVector3(), color);
        }

        /// <summary>
        /// Displays a marker as a colored dot at the specified local position.
        /// </summary>
        /// <param name="pos">The local position in world space where the marker will be placed.</param>
        /// <param name="color">The color of the marker to be displayed.</param>
        public static void DisplayLocalPos(Vector3 pos, Color color)
        {
            MarkerData marker = CreateMarker(false);
            pos.y += 1f;
            pos.z -= 1f;
            marker.Set(pos, color);

            activeMarkers.Add(marker);
        }

        /// <summary>
        /// Renders a line between two specified positions in 3D world space with a given color.
        /// </summary>
        /// <param name="pos1">The starting point of the line in 3D space.</param>
        /// <param name="pos2">The ending point of the line in 3D space.</param>
        /// <param name="color">The color of the line.</param>
        public static void DisplayLine(float3 pos1, float3 pos2, Color color)
        {
            DisplayLine(pos1.ToVector3(), pos2.ToVector3(), color);
        }

        /// <summary>
        /// Displays a line between two specified positions in world space with a given color.
        /// </summary>
        /// <param name="pos1">The starting position of the line in world space.</param>
        /// <param name="pos2">The ending position of the line in world space.</param>
        /// <param name="color">The color of the line.</param>
        public static void DisplayLine(int3 pos1, int3 pos2, Color color)
        {
            DisplayLine(pos1.ToVector3(), pos2.ToVector3(), color);
        }

        /// <summary>
        /// Displays a debug line between two specified positions with a given color in the 3D world space.
        /// </summary>
        /// <param name="pos1">The starting position of the line in world coordinates.</param>
        /// <param name="pos2">The ending position of the line in world coordinates.</param>
        /// <param name="color">The color of the line to be displayed.</param>
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
        /// Clears all active markers, including both point and line markers,
        /// and returns them to their respective pools for reuse.
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

        /// <summary>
        /// Creates a new debug marker or reuses an existing one from the pool based on whether it represents a line or a point.
        /// </summary>
        /// <param name="line">Indicates whether the marker to be created is for a line (true) or a point (false).</param>
        /// <returns>A <see cref="MarkerData"/> instance representing the created or reused debug marker.</returns>
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

        /// <summary>
        /// Creates a new point marker in the form of a sphere GameObject, attaches it to a predetermined anchor,
        /// and initializes its properties for debugging purposes.
        /// </summary>
        /// <returns>A MarkerData object containing the created sphere GameObject and its associated renderer.</returns>
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

        /// <summary>
        /// Creates a new line marker in the 3D world space for debugging purposes.
        /// </summary>
        /// <returns>
        /// A <see cref="MarkerData"/> structure containing information about the created line marker, including its GameObject and associated renderer.
        /// </returns>
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
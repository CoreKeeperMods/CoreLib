using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreLib.Util
{
    /// <summary>
    /// Tracks the full names of components attached to a GameObject, excluding its Transform- and self-type.
    /// This ensures consistency across game versions and helps in debugging and version control.
    /// </summary>
    public class ComponentTracker : MonoBehaviour
    {
        /// <summary>
        /// A list of full names of components attached to the GameObject, excluding the Transform component
        /// and the ComponentTracker itself. This is designed to help with tracking, debugging, and ensuring
        /// version control consistency.
        /// </summary>
        public List<string> components = new();

        /// <summary>
        /// Called when the script instance is being loaded.
        /// This method destructs the <see cref="ComponentTracker"/> immediately upon the script's initialization.
        /// Ensures that the ComponentTracker does not persist on the GameObject.
        /// </summary>
        private void Awake()
        {
            Destroy(this);
        }

        /// <summary>
        /// Ensures the list of component names is updated whenever modifications to the GameObject occur in the editor.
        /// </summary>
        private void OnValidate()
        {
            LoadComponents();
        }

        /// <summary>
        /// Populates the list of components attached to the GameObject with their fully qualified type names,
        /// excluding the Transform and ComponentTracker components.
        /// </summary>
        public void LoadComponents()
        {
            components.Clear();
            
            var allComponents = gameObject.GetComponents<Component>();
            components = allComponents
                .Where(c => c.GetType() != typeof(Transform) && c.GetType() != typeof(ComponentTracker))   
                .Select(c => c.GetType().FullName)
                .ToList();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// A custom editor for the ComponentTracker class, providing additional functionality in the Unity Inspector.
    /// </summary>
    [CustomEditor(typeof(ComponentTracker))]
    public class ComponentTrackerEditor : Editor
    {
        /// <summary>
        /// Draws the custom inspector GUI for the <see cref="ComponentTracker"/> component.
        /// Adds a button to the inspector that allows the user to trigger the loading of the component list.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Load info"))
            {
                var component = (ComponentTracker)target;
                component.LoadComponents();
            }
        }
    }
#endif
}
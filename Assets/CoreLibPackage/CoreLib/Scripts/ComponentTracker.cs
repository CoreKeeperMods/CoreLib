using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace CoreLib.Util
{
    /// <summary>
    /// This component allows to track added component full names to prevent issues between game versions
    /// </summary>
    public class ComponentTracker : MonoBehaviour
    {
        public List<string> components = new List<string>();

        private void Awake()
        {
            Destroy(this);
        }

        private void OnValidate()
        {
            LoadComponents();
        }

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
    [CustomEditor(typeof(ComponentTracker))]
    public class ComponentTrackerEditor : Editor
    {
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
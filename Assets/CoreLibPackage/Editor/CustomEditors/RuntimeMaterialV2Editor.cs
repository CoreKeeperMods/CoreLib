using System.Linq;
using CoreLib.Submodule.Entity.Component;
using Pug.Sprite;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomEditor(typeof(RuntimeMaterial))]
    public class RuntimeMaterialV2Editor : UnityEditor.Editor
    {
        public RuntimeMaterial TargetObject => (RuntimeMaterial)target;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Use Material Name", GUILayout.Height(25)))
                UseMaterialName();
            EditorGUILayout.Separator();
            if (GUILayout.Button("Reassign Material", GUILayout.Height(25)))
                ReassignMaterial();
        }
        
        
        /// <summary>
        /// Reassigns the material of the component's sprite renderer or particle system renderer to match
        /// the specified material name. Searches the project's assets for a material with the given name
        /// and assigns it if found. Marks the object as dirty in the editor to ensure the change is saved.
        /// Logs a message if no matching material is found.
        /// </summary>
        public void ReassignMaterial()
        {
            string[] results = AssetDatabase.FindAssets($"t:material {TargetObject.materialName}");
            if (results.Length <= 0)
            {
                Debug.LogWarning($"No material matches found for: {TargetObject.materialName}");
                return;
            }
            string result = results.First();
            string path = AssetDatabase.GUIDToAssetPath(result);
            if (TargetObject.TryGetComponent(out Renderer renderer))
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (TargetObject.TryGetComponent(out SpriteObject spriteObject))
                spriteObject.material = AssetDatabase.LoadAssetAtPath<Material>(path);
            EditorUtility.SetDirty(this);
        }


        /// <summary>
        /// Updates the material name field of the component by retrieving the name of the currently assigned
        /// material from either a SpriteRenderer or ParticleSystemRenderer component attached to the GameObject.
        /// If no applicable renderer component is present or a material is not assigned, the materialName field
        /// remains unchanged. Marks the object as dirty in the editor to ensure the change is saved.
        /// </summary>
        /// <param name="o"></param>
        public void UseMaterialName()
        {
            if (TargetObject.TryGetComponent(out Renderer renderer))
                TargetObject.materialName = renderer.sharedMaterial.name;
            if (TargetObject.TryGetComponent(out SpriteObject spriteObject))
                TargetObject.materialName = spriteObject.material.name;
            EditorUtility.SetDirty(this);
        }
    }
}
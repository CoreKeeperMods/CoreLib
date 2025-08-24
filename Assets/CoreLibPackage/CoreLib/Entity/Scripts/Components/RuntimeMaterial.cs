using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Components
{
    /// <summary>
    /// Represents a runtime material handler for a GameObject in Unity.
    /// This class allows dynamic application of materials identified by a material name,
    /// providing utilities for interaction and reassignment of materials at runtime or in the editor.
    /// </summary>
    [ExecuteAlways]
    public class RuntimeMaterial : ModCDAuthoringBase
    {
        /// <summary>
        /// The name of the material to be applied to the associated object.
        /// This string is used as a key to retrieve the material from a pre-defined dictionary of materials.
        /// </summary>
        public string materialName;
        /// <summary>
        /// Applies a material to the relevant renderer of the current GameObject based on the specified `materialName`.
        /// Attempts to find a material in the shared `PrefabCrawler.materials` dictionary. If a match is found,
        /// it applies the material to the `SpriteRenderer` or `ParticleSystemRenderer` component of the GameObject.
        /// Provides error logging if the material cannot be found or if a valid target component is missing.
        /// </summary>
        /// <param name="data">A MonoBehaviour instance that might be required for contextual application logic.
        /// Currently, not utilized within this method implementation.</param>
        /// <returns>Returns true upon execution, indicating the method processed the material application logic.</returns>
        public override bool Apply(MonoBehaviour data)
        {
            if (PrefabCrawler.materials.ContainsKey(materialName)) {
                if (gameObject.TryGetComponent(out SpriteRenderer spriteRenderer)) {
                    spriteRenderer.sharedMaterial = PrefabCrawler.materials[materialName];
                } else if (gameObject.TryGetComponent(out ParticleSystemRenderer particleSystemRenderer)) {
                    particleSystemRenderer.sharedMaterial = PrefabCrawler.materials[materialName];
                } else {
                    CoreLibMod.Log.LogInfo($"Error applying material {materialName}, found no valid target!");
                }
            } else {
                CoreLibMod.Log.LogInfo($"Error applying material {materialName}, such material is not found!");
            }

            return true;
        }
#if UNITY_EDITOR
        /// <summary>
        /// Automatically invoked by Unity when the script instance is being loaded.
        /// Ensures that the `materialName` field is initialized with a valid value by invoking
        /// the `UseMaterialName` method if the `materialName` field is null or empty.
        /// This helps maintain consistent material assignment behavior.
        /// </summary>
        private void Awake()
        {
            if (string.IsNullOrEmpty(materialName))
            {
                UseMaterialName();
            }
        }

        /// <summary>
        /// Reassigns the material of the component's sprite renderer or particle system renderer to match
        /// the specified material name. Searches the project's assets for a material with the given name
        /// and assigns it if found. Marks the object as dirty in the editor to ensure the change is saved.
        /// Logs a message if no matching material is found.
        /// </summary>
        public void ReassignMaterial()
        {
            string[] results = AssetDatabase.FindAssets($"t:material {materialName}");
            if (results.Length > 0)
            {
                string result = results.First();
                string path = AssetDatabase.GUIDToAssetPath(result);
                if (TryGetComponent(out SpriteRenderer spriteRenderer))
                    spriteRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (TryGetComponent(out ParticleSystemRenderer particleSystemRenderer))
                    particleSystemRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);

                EditorUtility.SetDirty(this);
            }
            else
            {
                Debug.LogWarning($"No material matches found for: {materialName}");
            }
        }

        /// <summary>
        /// Updates the material name field of the component by retrieving the name of the currently assigned
        /// material from either a SpriteRenderer or ParticleSystemRenderer component attached to the GameObject.
        /// If no applicable renderer component is present or a material is not assigned, the materialName field
        /// remains unchanged. Marks the object as dirty in the editor to ensure the change is saved.
        /// </summary>
        public void UseMaterialName()
        {
            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                materialName = spriteRenderer.sharedMaterial.name;
            }
            
            if (TryGetComponent(out ParticleSystemRenderer particleSystemRenderer))
            {
                materialName = particleSystemRenderer.sharedMaterial.name;
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}
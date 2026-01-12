using System;
using Pug.Sprite;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    /// Represents a runtime material handler for a GameObject in Unity.
    /// This class allows dynamic application of materials identified by a material name,
    /// providing utilities for interaction and reassignment of materials at runtime or in the editor.
    public class RuntimeMaterial : MonoBehaviour
    {
        /// The name of the material to be applied to the associated object.
        /// This string is used as a key to retrieve the material from a pre-defined dictionary of materials.
        public string materialName;

        /// Automatically invoked by Unity when the script instance is being loaded.
        /// Ensures that the `materialName` field is initialized with a valid value by invoking
        /// the `UseMaterialName` method if the `materialName` field is null or empty.
        /// This helps maintain consistent material assignment behavior.
        private void Awake()
        {
            try
            {
                MaterialCrawler.Materials.TryGetValue(materialName, out var newMaterial);
                if (TryGetComponent(out Renderer component))
                    component.sharedMaterial = newMaterial;
                else if (TryGetComponent(out SpriteObject spriteObject))
                    spriteObject.material = newMaterial;
            }
            catch (Exception e)
            {
                EntityModule.Log.LogError($"Error applying material: {materialName}!\n{e}");
            }
        }
    }
}
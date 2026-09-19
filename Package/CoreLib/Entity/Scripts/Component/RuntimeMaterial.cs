using System;
using Pug.Sprite;
using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    /// Represents a runtime material handler for a GameObject in Unity.
    /// This class allows dynamic application of materials identified by a material name
    public class RuntimeMaterial : MonoBehaviour
    {
        /// The name of the material to be applied to the associated object.
        public string materialName;

        public bool useModApi = false;
        
        private void Awake()
        {
            try
            {
                Material newMaterial;
                
                if (useModApi)
                    newMaterial = API.Rendering.GetMaterial(materialName);
                else
                    MaterialCrawler.materials.TryGetValue(materialName, out newMaterial);
                
                if (TryGetComponent(out Renderer component))
                    component.sharedMaterial = newMaterial;
                else if (TryGetComponent(out SpriteObject spriteObject))
                {
                    spriteObject.material = newMaterial;
                    spriteObject.ApplyVisualChange();
                }
            }
            catch (Exception e)
            {
                EntityModule.log.LogError($"Error applying material: {materialName}!\n{e}");
            }
        }
    }
}
using System;
using CoreLib.Submodules;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Unity.Collections;
using UnityEngine;

namespace CoreLib.Components
{
    public class RuntimeMaterialV2 : ModCDAuthoringBase
    {
        public Il2CppValueField<FixedString64Bytes> materialName;

        public RuntimeMaterialV2(IntPtr ptr) : base(ptr) { }
        
        public override bool Apply(EntityMonoBehaviourData data)
        {
            string matName = materialName.Value.ToString();
            ApplyMaterial(gameObject, matName);
            return true;
        }

        internal static void ApplyMaterial(GameObject gameObject, string matName)
        {
            if (PrefabCrawler.materials.ContainsKey(matName))
            {
                bool anyWorked = false;
                SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sharedMaterial = PrefabCrawler.materials[matName];
                    anyWorked = true;
                }

                ParticleSystemRenderer particleSystem = gameObject.GetComponent<ParticleSystemRenderer>();
                if (particleSystem != null)
                {
                    particleSystem.sharedMaterial = PrefabCrawler.materials[matName];
                    anyWorked = true;
                }

                if (!anyWorked)
                {
                    CoreLibPlugin.Logger.LogInfo($"Error applying material {matName}, found no valid target!");
                }
            }
            else
            {
                CoreLibPlugin.Logger.LogInfo($"Error applying material {matName}, such material is not found!");
            }
        }
    }
}
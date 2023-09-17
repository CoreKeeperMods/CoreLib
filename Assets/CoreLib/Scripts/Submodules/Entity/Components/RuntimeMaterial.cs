using System;
using CoreLib.Submodules;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Components
{
    public class RuntimeMaterial : ModCDAuthoringBase
    {
        public String materialName;
        public override bool Apply(MonoBehaviour data)
        {
            if (PrefabCrawler.materials.ContainsKey(materialName))
            {
                bool anyWorked = false;
                SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sharedMaterial = PrefabCrawler.materials[materialName];
                    anyWorked = true;
                }

                ParticleSystemRenderer particleSystem = gameObject.GetComponent<ParticleSystemRenderer>();
                if (particleSystem != null)
                {
                    particleSystem.sharedMaterial = PrefabCrawler.materials[materialName];
                    anyWorked = true;
                }

                if (!anyWorked)
                {
                    CoreLibMod.Log.LogInfo($"Error applying material {materialName}, found no valid target!");
                }
            }
            else
            {
                CoreLibMod.Log.LogInfo($"Error applying material {materialName}, such material is not found!");
            }

            return true;
        }
    }
}
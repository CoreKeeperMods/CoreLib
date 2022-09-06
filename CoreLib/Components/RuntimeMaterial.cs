using System.Runtime.InteropServices;
using CoreLib.Submodules.CustomEntity;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem;
using UnityEngine;


namespace CoreLib.Components
{
    public class RuntimeMaterial : ModCDAuthoringBase
    {
        public Il2CppReferenceField<String> materialName;

        private GCHandle materialNameHandle;
        
        public RuntimeMaterial(System.IntPtr ptr) : base(ptr) { }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                materialNameHandle = GCHandle.Alloc(materialName.Value);
            }
            return alloc;
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            string matName = materialName.Value;
            if (PrefabCrawler.materials.ContainsKey(matName))
            {
                bool anyWorked = false;
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sharedMaterial = PrefabCrawler.materials[matName];
                    anyWorked = true;
                }

                ParticleSystemRenderer particleSystem = GetComponent<ParticleSystemRenderer>();
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
            return true;
        }

        private void OnDestroy()
        {
            materialNameHandle.Free();
        }
    }
}
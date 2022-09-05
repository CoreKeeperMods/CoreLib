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
        private SpriteRenderer spriteRenderer;
        
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
                spriteRenderer = GetComponent<SpriteRenderer>();
                spriteRenderer.sharedMaterial = PrefabCrawler.materials[matName];
                CoreLibPlugin.Logger.LogInfo($"Applied material {matName}");
            }
            else
            {
                CoreLibPlugin.Logger.LogInfo($"Error applying material {matName}");
            }
            return true;
        }

        private void OnDestroy()
        {
            materialNameHandle.Free();
        }
    }
}
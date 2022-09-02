using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem;
using UnityEngine;


namespace CoreLib.Submodules.CustomEntity
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
            if (PrefabCrawler.materials.ContainsKey(materialName.Value))
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                spriteRenderer.sharedMaterial = PrefabCrawler.materials[materialName.Value];
            }
            else
            {
                CoreLibPlugin.Logger.LogInfo($"Error applying material {materialName.Value.ToString()}");
            }
            return true;
        }

        private void OnDestroy()
        {
            materialNameHandle.Free();
        }
    }
}
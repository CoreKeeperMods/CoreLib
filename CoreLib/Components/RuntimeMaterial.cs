using System.Runtime.InteropServices;
using CoreLib.Submodules.ModEntity;
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
            RuntimeMaterialV2.ApplyMaterial(gameObject, matName);
            return true;
        }

        private void OnDestroy()
        {
            materialNameHandle.Free();
        }
    }
}
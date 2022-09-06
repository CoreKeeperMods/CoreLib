using System;
using System.Runtime.InteropServices;
using CoreLib.Submodules.CustomEntity;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using String = Il2CppSystem.String;

namespace CoreLib.Components
{
    public class ModRangeWeaponCDAuthoring : ModCDAuthoringBase
    {
        public Il2CppReferenceField<String> projectileID;
        private GCHandle projectileIDHandle;
        public Il2CppValueField<float> spawnOffsetDistance;
 
        public ModRangeWeaponCDAuthoring(IntPtr ptr) : base(ptr) { }
        
        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                projectileIDHandle = GCHandle.Alloc(projectileID.Value);
            }
            return alloc;
        }

        private void OnDestroy()
        {
            projectileIDHandle.Free();
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            RangeWeaponCDAuthoring rangeWeaponCdAuthoring = gameObject.AddComponent<RangeWeaponCDAuthoring>();
            rangeWeaponCdAuthoring.projectileID = CustomEntityModule.GetObjectId(projectileID.Value);
            rangeWeaponCdAuthoring.spawnOffsetDistance = spawnOffsetDistance;
            Destroy(this);
            return true;
        }
    }
}
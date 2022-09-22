using System;
using System.Runtime.InteropServices;
using CoreLib.Submodules.DropTables;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using String = Il2CppSystem.String;

namespace CoreLib.Components
{
    public class ModDropsLootFromTableCDAuthoring : ModCDAuthoringBase
    {
        public Il2CppReferenceField<String> lootTableId;
        private GCHandle lootTableIdHandle;

        public ModDropsLootFromTableCDAuthoring(IntPtr ptr) : base(ptr) { }
        
        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                lootTableIdHandle = GCHandle.Alloc(lootTableId.Value);
            }
            return alloc;
        }

        private void OnDestroy()
        {
            lootTableIdHandle.Free();
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            DropsLootFromLootTableCDAuthoring authoring = gameObject.AddComponent<DropsLootFromLootTableCDAuthoring>();
            authoring.lootTableID = DropTablesModule.GetLootTableID(lootTableId.Value);
            Destroy(this);
            return true;
        }
    }
}
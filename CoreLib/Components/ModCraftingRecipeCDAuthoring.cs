using System;
using System.Runtime.InteropServices;
using CoreLib.Submodules.ModEntity;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;

namespace CoreLib.Components
{
    public class ModCraftingRecipeCDAuthoring : ModCDAuthoringBase
    {
        public Il2CppReferenceField<List<ModCraftData>> requiredToCraft;
        public GCHandle requiredToCraftHandle;
        
        public ModCraftingRecipeCDAuthoring(IntPtr ptr) : base(ptr) { }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                requiredToCraftHandle = GCHandle.Alloc(requiredToCraft.Value);   
            }
            return alloc;
        }
        
        private void OnDestroy()
        {
            requiredToCraftHandle.Free();
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            List<CraftingObject> items = data.objectInfo.requiredObjectsToCraft;
            items.Clear();

            foreach (ModCraftData modCraftData in requiredToCraft.Value)
            {
                ObjectID objectID = EntityModule.GetObjectId(modCraftData.item);
                if (objectID != ObjectID.None)
                {
                    items.Add(new CraftingObject()
                    {
                        objectID = objectID,
                        amount = modCraftData.amount
                    });
                }
            }
            Destroy(this);
            return true;
        }
    }

    [Serializable]
    public class ModCraftData
    {
        public string item;
        public int amount;
    }
}
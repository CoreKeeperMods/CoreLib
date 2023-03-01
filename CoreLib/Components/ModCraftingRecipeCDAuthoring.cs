using System;
using System.Runtime.InteropServices;
using CoreLib.Submodules.ModEntity;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using String = Il2CppSystem.String;

namespace CoreLib.Components
{
    public class ModCraftingRecipeCDAuthoring : ModCDAuthoringBase
    {
        [TreatAsObjectId] 
        public Il2CppReferenceField<String> item;
        public Il2CppValueField<int> amount;

        public GCHandle itemHandle;

        public ModCraftingRecipeCDAuthoring(IntPtr ptr) : base(ptr) { }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                itemHandle = GCHandle.Alloc(item.Value);
            }

            return alloc;
        }

        private void OnDestroy()
        {
            itemHandle.Free();
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            List<CraftingObject> items = data.objectInfo.requiredObjectsToCraft;

            if (Enum.TryParse(item.Value, true, out ObjectID objectID))
            {
                items.Add(new CraftingObject()
                {
                    objectID = objectID,
                    amount = amount.Value
                });
                Destroy(this);
                return true;
            }

            ObjectID objectID1 = EntityModule.GetObjectId(item.Value);
            if (objectID1 != ObjectID.None)
            {
                items.Add(new CraftingObject()
                {
                    objectID = objectID1,
                    amount = amount.Value
                });
            }

            Destroy(this);
            return true;
        }
    }

    public class TreatAsObjectIdAttribute : Attribute { }
}
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace CoreLib.Components
{
    public class ModCraftingRecipeCDAuthoring : ModCDAuthoringBase
    {
        public List<ModCraftData> requiredToCraft;
        public bool Allocate()
        {
            return default(bool);
        }

        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }
    }

    [Serializable]
    public class ModCraftData : System.Object
    {
        public FixedString64Bytes item;
        public int amount;
    }
}
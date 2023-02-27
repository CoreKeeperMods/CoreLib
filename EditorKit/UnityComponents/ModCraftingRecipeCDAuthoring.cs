using System;
using System.Collections.Generic;

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
    public class ModCraftData
    {
        public string item;
        public int amount;
    }
}
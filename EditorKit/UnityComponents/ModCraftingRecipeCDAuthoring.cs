using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using String = System.String;

namespace CoreLib.Components
{
    public class ModCraftingRecipeCDAuthoring : ModCDAuthoringBase
    {
        [TreatAsObjectId]
        public String item;
        public int amount;
        public bool Allocate()
        {
            return default(bool);
        }

        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }
    }

    public class TreatAsObjectIdAttribute : PropertyAttribute
    {
    }
}
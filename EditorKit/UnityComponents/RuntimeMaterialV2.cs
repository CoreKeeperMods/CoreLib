using System;
using Unity.Collections;
using UnityEngine;

namespace CoreLib.Components
{
    public class RuntimeMaterialV2 : ModCDAuthoringBase
    {
        public FixedString64Bytes materialName;
        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }

        internal static void ApplyMaterial(GameObject gameObject, string matName)
        {
        }
    }
}
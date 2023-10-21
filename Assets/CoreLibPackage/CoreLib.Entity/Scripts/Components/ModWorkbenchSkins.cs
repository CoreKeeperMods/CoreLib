using System.Collections.Generic;
using PugMod;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Components
{
    public class ModWorkbenchSkins : MonoBehaviour
    {
        private bool hasBeenApplied;
        [SerializeField] 
        internal SerializableDictionary<string, EntityMonoBehaviour.ReskinInfo> modReskinInfos = 
            new SerializableDictionary<string, EntityMonoBehaviour.ReskinInfo>();
        
        internal void Apply()
        {
            if (hasBeenApplied) return;

            var craftingBuilding = gameObject.GetComponent<SimpleCraftingBuilding>();
            foreach (var pair in modReskinInfos)
            {
                pair.Value.objectIDToUseReskinOn = API.Authoring.GetObjectID(pair.Key);
                foreach (EntityMonoBehaviour.ReskinOptions reskinOption in craftingBuilding.reskinOptions)
                {
                    reskinOption.reskins.Add(pair.Value);
                }
            }
            hasBeenApplied = true;
        }
    }
}
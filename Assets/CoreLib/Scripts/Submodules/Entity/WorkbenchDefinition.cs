using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CoreLib.Submodules.ModEntity
{
    [CreateAssetMenu(fileName = "WorkbenchDefinition", menuName = "New WorkbenchDefinition", order = 2)]
    public class WorkbenchDefinition : ScriptableObject
    {
        public string itemId;
        
        [FormerlySerializedAs("icon")] 
        public Sprite bigIcon;
        public Sprite smallIcon;
        [FormerlySerializedAs("variations")] 
        public Texture2D skinsTexture;

        public bool bindToRootWorkbench;
        
        [FormerlySerializedAs("requiredObjectsToCraft")] 
        public List<InventoryItemAuthoring.CraftingObject> recipe;

        public List<CraftingAuthoring.CraftableObject> canCraft;
        public List<string> relatedWorkbenches;
    }
}
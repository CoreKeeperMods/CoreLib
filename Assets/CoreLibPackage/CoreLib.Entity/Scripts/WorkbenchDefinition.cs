using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extensions;
using Pug.Sprite;
using Pug.UnityExtensions;
using PugMod;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace CoreLib.Submodules.ModEntity
{
    [CreateAssetMenu(fileName = "WorkbenchDefinition", menuName = "CoreLib/New WorkbenchDefinition", order = 2)]
    public class WorkbenchDefinition : ScriptableObject
    {
        public string itemId;
        
        [FormerlySerializedAs("icon")] 
        public Sprite bigIcon;
        public Sprite smallIcon;
        [FormerlySerializedAs("variations")] 
        public Texture2D texture;

        public SpriteAssetSkin assetSkin;

        public bool bindToRootWorkbench;
        
        [FormerlySerializedAs("requiredObjectsToCraft")] 
        public List<InventoryItemAuthoring.CraftingObject> recipe;
        
        public List<InventoryItemAuthoring.CraftingObject> canCraft;
        
        [PickStringFromEnum(typeof(ObjectID))]
        public List<string> relatedWorkbenches;
        public bool refreshRelatedWorkbenchTitles;

        public string title;
        public string leftTitle;
        public string rightTitle;
        public UIManager.CraftingUIThemeType skin;
    }
}
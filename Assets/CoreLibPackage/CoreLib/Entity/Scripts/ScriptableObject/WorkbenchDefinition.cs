using System;
using System.Collections.Generic;
using I2.Loc;
using Pug.Sprite;
using Pug.UnityExtensions;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity
{
    [CreateAssetMenu(fileName = "WorkbenchDefinition", menuName = "CoreLib/Entity/Workbench Definition")]
    public class WorkbenchDefinition : ScriptableObject
    {
        [Tooltip("The type of Workbench")]
        public WorkbenchType workbenchType;

        [Tooltip("Format: <ModID>:<ItemID>\nExample: CoreLib:RootModWorkbench")]
        public string itemID;

        [Tooltip("Large Icon: 16x16 PNG")]
        public Sprite bigIcon;

        [Tooltip("Small Icon: 16x16 PNG\nRecommended Sprite Size: 10x10")]
        public Sprite smallIcon;

        [Tooltip("Sprite Asset Skin: Target Asset should be Workbench.asset in CoreLib")]
        public DataBlockRef<SpriteAssetSkin> assetRef;

        [Tooltip("Bind this Workbench to the Root Workbench in CoreLib")]
        public bool bindToRootWorkbench;
        
        [Tooltip("The required items to create this Workbench")]
        public List<InventoryItemAuthoring.CraftingObject> recipe;

        [Tooltip("The items this Workbench can craft")]
        public List<InventoryItemAuthoring.CraftingObject> canCraft;
        
        [PickStringFromEnum(typeof(ObjectID)), Tooltip("The Workbenches that this Workbench can switch to.\nThe items of that Workbench will be added to this one.")]
        public List<string> relatedWorkbenches;
        
        [Tooltip("Refresh all the Window Titles when switching to a different related Workbench")]
        public bool refreshRelatedWorkbenchTitles;
        
        [Tooltip("Workbench Titles")]
        public List<LocalizedString> titles;

        [Tooltip("The skin of the Windows of the Workbench")]
        public UIManager.CraftingUIThemeType skin;
        
        [Obsolete, HideInInspector]
        public string title;
        
        [Obsolete, HideInInspector]
        public string leftTitle;

        [Obsolete, HideInInspector]
        public string rightTitle;

        internal void SetupNewTitles()
        {
            if (titles.Count > 0) return;
            titles.Clear();
            LocalizedString leftTitleLoc = leftTitle;
            if( !string.IsNullOrEmpty(leftTitle) ) titles.Add(leftTitleLoc);
            LocalizedString titleLoc = title;
            if( !string.IsNullOrEmpty(title) ) titles.Add(titleLoc);
            LocalizedString rightTitleLoc = rightTitle;
            if( !string.IsNullOrEmpty(rightTitle) ) titles.Add(rightTitleLoc);
            title = leftTitle = rightTitle = null;
        }
        
        private void OnValidate() => SetupNewTitles();
        
        
    }
    
    public enum WorkbenchType { Simple, Wide }
}
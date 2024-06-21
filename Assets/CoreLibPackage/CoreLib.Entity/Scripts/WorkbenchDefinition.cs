using System.Collections.Generic;
using SpriteInstancing;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;

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
        public List<string> relatedWorkbenches;

        public string title;
        public string leftTitle;
        public string rightTitle;
        public UIManager.CraftingUIThemeType skin;
    }
}
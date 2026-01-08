using CoreLib.Util.Extension;
using I2.Loc;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    [DisallowMultipleComponent]
    public class ModCraftingUISetting : MonoBehaviour
    {
        [Header("Crafting Building Settings")]
        public GameObject EntityPrefab => gameObject.GetComponent<ObjectAuthoring>().graphicalPrefab;
        
        [Tooltip("Center Window Title in Crafting Buildings")]
        public LocalizedString craftingUITitle;
        
        [Tooltip("Left Window Title in Crafting Buildings")]
        public LocalizedString craftingUITitleLeftBox;
        
        [Tooltip("Right Window Title in Crafting Buildings")]
        public LocalizedString craftingUITitleRightBox;
        
        [Tooltip("Background Skin Variation")]
        public UIManager.CraftingUIThemeType craftingUIBackgroundVariation;
        
        public CraftingBuilding.CraftingUISettings GetCraftingUISettings() => new(
            this.GetEntityObjectID(), craftingUITitle, craftingUITitleLeftBox, craftingUITitleRightBox, craftingUIBackgroundVariation);
    }
}
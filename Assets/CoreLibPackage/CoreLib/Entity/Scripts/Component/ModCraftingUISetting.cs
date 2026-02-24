using System.Collections.Generic;
using CoreLib.Util.Extension;
using I2.Loc;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    [DisallowMultipleComponent]
    public class ModCraftingUISetting : MonoBehaviour
    {
        [Header("Crafting Building Settings")] public GameObject EntityPrefab => gameObject.GetComponent<ObjectAuthoring>().graphicalPrefab;

        [Tooltip("Crafting UI Titles")]
        public List<LocalizedString> titles;

        [Tooltip("Background Skin Variation")] public UIManager.CraftingUIThemeType craftingUIBackgroundVariation;

        public CraftingBuilding.CraftingUISettings GetCraftingUISetting()
        {
            return new CraftingBuilding.CraftingUISettings(
                craftingUIBackgroundVariation, titles.ToArray());
        }


        public CraftingBuilding.CraftingUISettingsOverride GetCraftingUISettingOverride()
        {
            return new CraftingBuilding.CraftingUISettingsOverride
            {
                usedForBuilding = this.GetEntityObjectID(),
                settings = GetCraftingUISetting()
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extension;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    [DisallowMultipleComponent]
    public class ModCraftingUISetting : MonoBehaviour
    {
        [Header("Crafting Building Settings")] public GameObject EntityPrefab => gameObject.GetComponent<ObjectAuthoring>().graphicalPrefab;

        [Tooltip("Center Window Title in Crafting Buildings")]
        public LocalizedString craftingUITitle;

        [Tooltip("Left Window Title in Crafting Buildings")]
        public LocalizedString craftingUITitleLeftBox;

        [Tooltip("Right Window Title in Crafting Buildings")]
        public LocalizedString craftingUITitleRightBox;

        [Tooltip("Background Skin Variation")] public UIManager.CraftingUIThemeType craftingUIBackgroundVariation;

        public CraftingBuilding.CraftingUISettings GetCraftingUISetting()
        {
            var titles = new List<LocalizedString>();
            if(!string.IsNullOrEmpty(craftingUITitleLeftBox.mTerm) && titles.FindIndex(x => x.mTerm.Equals(craftingUITitleLeftBox.mTerm)) == -1)
                titles.Add(craftingUITitleLeftBox);
            if(!string.IsNullOrEmpty(craftingUITitle.mTerm) && titles.FindIndex(x => x.mTerm.Equals(craftingUITitle.mTerm)) == -1)
                titles.Add(craftingUITitle);
            if(!string.IsNullOrEmpty(craftingUITitleRightBox.mTerm) && titles.FindIndex(x => x.mTerm.Equals(craftingUITitleRightBox.mTerm)) == -1)
                titles.Add(craftingUITitleRightBox);
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
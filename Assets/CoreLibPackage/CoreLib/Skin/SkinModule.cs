// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: ModPlayerCustomizationTable.cs
// Author: Minepatcher
// Created: 2025-11-17
// Description: Handles loading and managing player customization skins.
// ========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Skin
{
    public class SkinModule : BaseSubmodule
    {
        #region Fields
        /// Human-readable module name used for logging and identification.
        public new const string Name = "Core Library - Skin";
        
        /// <summary>Module-scoped logger instance for EntityModule.</summary>
        internal new static Logger Log = new(Name);
        
        /// <summary>Table containing player customization entries used by the module.</summary>
        internal static PlayerCustomizationTable CustomizationTable => Resources.Load<PlayerCustomizationTable>(nameof (PlayerCustomizationTable));
        
        /// <summary>Convenience accessor for the loaded instance of this module.</summary>
        internal static SkinModule Instance => CoreLibMod.GetModuleInstance<SkinModule>();
        
        /// <summary>Dictionary containing all loaded skin assets from other mods.</summary>
        internal static Dictionary<string, ModPlayerCustomizationTable> SkinDictionary = new();
        #endregion
        
        #region BaseSubmodule Implementation
        /// <summary>
        /// Called when the module is loaded.
        /// </summary>
        internal override void Load()
        {
            base.Load();
            foreach (var mod in DependentMods)
            {
                var modPlayerCustomizationTables = mod.Assets.OfType<ModPlayerCustomizationTable>().ToList();
                if(modPlayerCustomizationTables.Count == 0) continue;
                SkinDictionary.TryAdd(mod.Metadata.name, modPlayerCustomizationTables[0]);
            }

            foreach ((string modName, var asset) in SkinDictionary)
            {
                asset.bodySkins.ForEach(AddToCustomizationTable);
                asset.skinColors.ForEach(x => AddToCustomizationTable(x, "skin"));
                asset.hairSkins.ForEach(AddToCustomizationTable);
                asset.hairColors.ForEach(x => AddToCustomizationTable(x, "hair"));
                asset.hairShadeColors.ForEach(x => AddToCustomizationTable(x, "hairShade"));
                asset.eyeSkins.ForEach(AddToCustomizationTable);
                asset.eyeColors.ForEach(x => AddToCustomizationTable(x, "eyes"));
                asset.shirtSkins.ForEach(AddToCustomizationTable);
                asset.pantsSkins.ForEach(AddToCustomizationTable);
                Log.LogInfo($"Added {asset.name} to Player Skin Table from {modName}.");
            }
            CustomizationTable.OnBeforeSerialize();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Adds a skin to the customization table.
        /// </summary>
        /// <param name="skin">The skin to add.</param>
        /// <exception cref="ArgumentException">Thrown if an unknown skin type is encountered.</exception>
        private static void AddToCustomizationTable(SkinBase skin)
        {
            switch (skin)
            {
                case BodySkin bodySkin:
                    bodySkin.id = (byte)CustomizationTable.bodySkins.Count;
                    CustomizationTable.bodySkins.Add(bodySkin);
                    break;
                case HairSkin hairSkin:
                    hairSkin.id = (byte)CustomizationTable.hairSkins.Count;
                    CustomizationTable.hairSkins.Add(hairSkin);
                    break;
                case EyesSkin eyesSkin:
                    eyesSkin.id = (byte)CustomizationTable.eyeSkins.Count;
                    CustomizationTable.eyeSkins.Add(eyesSkin);
                    break;
                case ShirtSkin shirtSkin:
                    shirtSkin.id = (byte)CustomizationTable.shirtSkins.Count;
                    CustomizationTable.shirtSkins.Add(shirtSkin);
                    break;
                case PantsSkin pantsSkin:
                    pantsSkin.id = (byte)CustomizationTable.pantsSkins.Count;
                    CustomizationTable.pantsSkins.Add(pantsSkin);
                    break;
                default:
                    throw new ArgumentException($"[{Name}] Unknows skin type: {skin.GetType().Name}");
            }
        }
        /// <summary>
        /// Adds a color replacement to the customization table.
        /// </summary>
        /// <param name="colorReplacement">The color replacement to add.</param>
        /// <param name="type">The type of color replacement to add.</param>
        /// <exception cref="ArgumentException">Thrown if an unknown skin type is encountered.</exception>
        private static void AddToCustomizationTable(ReorderableColorList colorReplacement, string type)
        {
            switch (type)
            {
                case "skin":
                    colorReplacement.id = (byte)CustomizationTable.skinColors.replacementColors.Count;
                    CustomizationTable.skinColors.replacementColors.Add(colorReplacement);
                    break;
                case "hair":
                    colorReplacement.id = (byte)CustomizationTable.hairColors.replacementColors.Count;
                    CustomizationTable.hairColors.replacementColors.Add(colorReplacement);
                    break;
                case "hairShade":
                    colorReplacement.id = (byte)CustomizationTable.hairShadeColors.replacementColors.Count;
                    CustomizationTable.hairShadeColors.replacementColors.Add(colorReplacement);
                    break;
                case "eyes":
                    colorReplacement.id = (byte)CustomizationTable.eyeColors.replacementColors.Count;
                    CustomizationTable.eyeColors.replacementColors.Add(colorReplacement);
                    break;
                default:
                    throw new ArgumentException($"[{Name}] Unknows skin type: {type}");
            }
        }
        #endregion
    }
}
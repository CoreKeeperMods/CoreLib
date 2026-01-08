// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: ModPlayerCustomizationTable.cs
// Author: Minepatcher
// Created: 2025-11-17
// Description: Contains all player customization data.
// ========================================================

using System;
using Pug.UnityExtensions;
using System.Collections.Generic;
using CoreLib.Util.Extension;
using UnityEngine;
using UnityEngine.Rendering;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Skin
{
    [CreateAssetMenu(menuName = "CoreLib/Skin/Player Customization Table", fileName = "New Player Customization Table")]
    public class ModPlayerCustomizationTable : ScriptableObject
    {
        [ArrayElementTitle("id"), Tooltip("Custom Body Skins to add.")]
        public List<BodySkin> bodySkins;
        [ReadOnly, Tooltip("Default Skin Colors. [READ-ONLY]")]
        public List<Color> skinSourceColors = new() { 
            ColorUtils.ToRGBA(Convert.ToUInt32("EBC3BB", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("D29A7C", 16)), 
            ColorUtils.ToRGBA(Convert.ToUInt32("B57A47", 16)), 
            ColorUtils.ToRGBA(Convert.ToUInt32("915B26", 16)) 
        };
        [ArrayElementTitle("id"), Tooltip("Custom Skin colors to add. 4-element array of RGBA values.")]
        public List<ReorderableColorList> skinColors;
        [ArrayElementTitle("id"), Tooltip("Custom Hair Skins to add.")]
        public List<HairSkin> hairSkins;
        [ReadOnly, Tooltip("Default Hair Colors. [READ-ONLY]")]
        public List<Color> hairSourceColors = new() { 
            ColorUtils.ToRGBA(Convert.ToUInt32("0E2522", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("39341F", 16)), 
            ColorUtils.ToRGBA(Convert.ToUInt32("614822", 16)), 
            ColorUtils.ToRGBA(Convert.ToUInt32("915B26", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("B58047", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("142B5C", 16)), 
            ColorUtils.ToRGBA(Convert.ToUInt32("143EAB", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("165DD9", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("1885D8", 16))
        };
        [ArrayElementTitle("id"), Tooltip("Custom Hair colors to add. 9-element array of RGBA values.")]
        public List<ReorderableColorList> hairColors;
        [ReadOnly, Tooltip("Default Hair Shade Colors. [READ-ONLY]")]
        public List<Color> hairShadeSourceColors = new() { 
            ColorUtils.ToRGBA(Convert.ToUInt32("915B26", 16))
        };
        [ArrayElementTitle("id"), Tooltip("Custom Hair Shade colors to add. 1-element array of RGBA values.")]
        public List<ReorderableColorList> hairShadeColors;
        [ArrayElementTitle("id"), Tooltip("Custom Eyes Skins to add.")]
        public List<EyesSkin> eyeSkins;
        [ReadOnly, Tooltip("Default Eye Colors. [READ-ONLY]")]
        public List<Color> eyeSourceColors = new() { 
            ColorUtils.ToRGBA(Convert.ToUInt32("142B5C", 16)),
            ColorUtils.ToRGBA(Convert.ToUInt32("165DD9", 16))
        };
        [ArrayElementTitle("id"), Tooltip("Custom Eyes colors to add. 2-element array of RGBA values.")]
        public List<ReorderableColorList> eyeColors;
        [ArrayElementTitle("id"), Tooltip("Custom Shirt Skins to add.")]
        public List<ShirtSkin> shirtSkins;
        [ArrayElementTitle("id"), Tooltip("Custom Pants Skins to add.")]
        public List<PantsSkin> pantsSkins;
    }
}
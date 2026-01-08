using System;
using System.Collections.Generic;
using CoreLib.Submodule.ControlMapping.Extension;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.ControlMapping
{
    [CreateAssetMenu(menuName = "CoreLib/Control Mapping/Control Mapping Category", fileName = "Control Mapping Category")]
    
    public class ModControlMappingCategory : ScriptableObject
    {
        [Header("Category Settings")]
        [Tooltip("The ID Name of your mod's Category. Ex: 'CoreLibCategory'")]
        public string categoryName;

        public List<KeyBindData> keyboardActions = new();
        public List<KeyBindData> mouseActions = new();
        public List<KeyBindData> joystickActions = new();

        private UserData _userData = new();


        private void OnValidate()
        {
            int[] categoryID = _userData.AddNewCategory(categoryName);
            
            foreach (var data in keyboardActions)
            {
                
            }
            
        }

        [Serializable]
        public class KeyBindData
        {
            public string keyBindName;

            /// <summary>
            /// Specifies the type of gamepad element to be used for a key binding.
            /// </summary>
            public ControllerElementType gamepadElementType;
        
            /// <summary>
            /// Represents the identifier for a gamepad element, used to associate a specific
            /// hardware element (e.g., button, axis) with a game input binding.
            /// </summary>
            public int gamepadElementId;

            /// <summary>
            /// Defines the range of motion or activation for a gamepad axis.
            /// This can determine how input along the axis is interpreted, such as full-range or a specific directional-range.
            /// </summary>
            public AxisRange gamepadAxisRange;
        
            /// <summary>
            /// Indicates whether the associated gamepad axis input should be inverted.
            /// </summary>
            public bool gamepadInvert;
        
            public Pole gamepadAxisContribution;
        
            /// <summary>
            /// Stores the default keyboard key code associated with a key binding.
            /// </summary>
            public KeyboardKeyCode keyboardKeyCode;

            /// <summary>
            /// Represents a key modifier (e.g., Shift, Ctrl, Alt) associated with a key binding.
            /// </summary>
            public ModifierKey modifierKey1;
            public ModifierKey modifierKey2;
            public ModifierKey modifierKey3;

        }
        
    }
}
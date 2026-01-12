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

                /// Specifies the type of gamepad element to be used for a key binding.
                public ControllerElementType gamepadElementType;
        
                /// Represents the identifier for a gamepad element, used to associate a specific
            /// hardware element (e.g., button, axis) with a game input binding.
                public int gamepadElementId;

                /// Defines the range of motion or activation for a gamepad axis.
            /// This can determine how input along the axis is interpreted, such as full-range or a specific directional-range.
                public AxisRange gamepadAxisRange;
        
                /// Indicates whether the associated gamepad axis input should be inverted.
                public bool gamepadInvert;
        
            public Pole gamepadAxisContribution;
        
                /// Stores the default keyboard key code associated with a key binding.
                public KeyboardKeyCode keyboardKeyCode;

                /// Represents a key modifier (e.g., Shift, Ctrl, Alt) associated with a key binding.
                public ModifierKey modifierKey1;
            public ModifierKey modifierKey2;
            public ModifierKey modifierKey3;

        }
        
    }
}
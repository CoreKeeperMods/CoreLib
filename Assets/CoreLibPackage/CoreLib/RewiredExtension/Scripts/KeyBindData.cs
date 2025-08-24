using Rewired;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.RewiredExtension
{
    /// <summary>
    /// Represents the key binding data for input configurations, including keyboard,
    /// controller, and gamepad input settings. This class is used to define and store
    /// input binding configurations such as default key codes, modifiers, and gamepad
    /// properties.
    /// </summary>
    public class KeyBindData
    {
        /// <summary>
        /// Stores the default keyboard key code associated with a key binding.
        /// </summary>
        public KeyboardKeyCode defaultKeyCode;

        /// <summary>
        /// Represents a key modifier (e.g., Shift, Ctrl, Alt) associated with a key binding.
        /// </summary>
        public ModifierKey modifierKey;

        /// <summary>
        /// Specifies the type of gamepad element to be used for a key binding.
        /// </summary>
        /// <remarks>
        /// Defines the <see cref="ControllerElementType"/> associated with the gamepad input.
        /// This indicates whether the element is a button, axis, or other supported types
        /// within the Rewired framework.
        /// </remarks>
        public ControllerElementType gamepadElementType;

        /// <summary>
        /// Defines the range of motion or activation for a gamepad axis.
        /// This can determine how input along the axis is interpreted, such as full range or a specific directional range.
        /// </summary>
        public AxisRange gamepadAxisRange;

        /// <summary>
        /// Indicates whether the associated gamepad axis input should be inverted.
        /// </summary>
        public bool gamepadInvert;

        /// <summary>
        /// Represents the identifier for a gamepad element, used to associate a specific
        /// hardware element (e.g., button, axis) with a game input binding.
        /// </summary>
        /// <remarks>
        /// This ID is utilized in conjunction with a <see cref="ControllerMap_Editor"/> or other
        /// relevant configuration to establish the mapping between a game control and the physical
        /// gamepad input. It typically corresponds to the identifier of the hardware element as recognized
        /// by the input library being used.
        /// </remarks>
        public int gamepadElementId;

        /// <summary>
        /// Represents the unique identifier for the input action associated with a key binding.
        /// This ID is used internally to map a specific input action to its corresponding key bind.
        /// </summary>
        public int actionId;

        /// Encapsulates key binding configurations, such as default key codes, modifiers, and other related properties.
        public KeyBindData(KeyboardKeyCode defaultKeyCode, ModifierKey modifierKey)
        {
            this.defaultKeyCode = defaultKeyCode;
            this.modifierKey = modifierKey;
        }
    }
}
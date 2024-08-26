using Rewired;

namespace CoreLib.RewiredExtension
{

    public class KeyBindData
    {
        public KeyboardKeyCode defaultKeyCode;
        public ModifierKey modifierKey;

        public ControllerElementType gamepadElementType;
        public AxisRange gamepadAxisRange;
        public bool gamepadInvert;
        public int gamepadElementId;

        public int actionId;

        public KeyBindData(KeyboardKeyCode defaultKeyCode, ModifierKey modifierKey)
        {
            this.defaultKeyCode = defaultKeyCode;
            this.modifierKey = modifierKey;
        }
    }
}
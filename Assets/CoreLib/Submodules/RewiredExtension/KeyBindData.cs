using Rewired;

namespace CoreLib.Submodules.RewiredExtension
{

    public class KeyBindData
    {
        public KeyboardKeyCode defaultKeyCode;
        public ModifierKey modifierKey;
        public int actionId;

        public KeyBindData(KeyboardKeyCode defaultKeyCode, ModifierKey modifierKey)
        {
            this.defaultKeyCode = defaultKeyCode;
            this.modifierKey = modifierKey;
        }
    }
}
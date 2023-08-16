# Rewired Extension Module
Rewired Extension Module is a submodule that allows to register new Rewired key binds. Users will be able to rebind them to what they want.

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(RewiredExtensionModule))]` to your plugin attributes. This will load the submodule.

Now in your plugin `Load()` method write:
```cs
RewiredExtensionModule.AddKeybind("KeyBindID", "My Amazing Key Bind", KeyboardKeyCode.C, ModifierKey.Control);
```
`KeyBindID` must be a unique identifier, which was not already added. Description is automatically added as a I2 term. Last parameter is optional and allows the default binding to be a key combination.

If you need to translate your key bind description to more languages use verbose version:
```cs
RewiredExtensionModule.AddKeybind("KeyBindID", new Dictionary<string, string> { { "en", "My Amazing Key Bind" }, { "zh-CN", "My Amazing Key Bind (In Chinese)" }, /*...*/ }, KeyboardKeyCode.C, ModifierKey.Control);
```

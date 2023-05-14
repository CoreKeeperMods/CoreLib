
### v2.1.2
- Allow to manually request any module to be loaded from config options
- Added MigrationModule. Module is intended to be opt in functionality to fix issues with missing items. Enable in the config file.

<details>
<summary>Changelog</summary>

### v2.1.1
- Fixed error on launch due to reflection error

### v2.1.0
- Fixed compatibility with game version 0.6.0 and higher
- Chat command handlers can be registered separately
- Added Dump chat command
- Added `drop` and `modify` JSON loader
- Added Prefab modification API to `EntityModule`
- Changed method signatures for workbench registration

### v2.0.1
**WARNING: Update all of your mods when installing version 2.0.0 or higher!**
- Fixed crash on dedicated servers when Entity module tried to register visual prefabs
- Fixed IDynamicItemHandler color replacement logic being broken

### v2.0.0
**WARNING: Update all of your mods when installing version 2.0.0 or higher!**
- Use Il2CppInterop from my PR branch to allow for advanced DOTS features
- Added `Component` module, which allows to create custom ECS components
- Added `System` module, which allows to create pseudo systems and state requesters
- Renamed `CustomEntity` module to `Entity` module. Mods which used it need to update
- Added `ModCraftingRecipeCDAuthoring` to allow to assign custom recipes from Unity editor
- Json Loader module now can create custom blocks using JSON
- Fix Entity module impaired functionality

### v1.4.0
**WARNING: Update all of your mods when installing this version!**
- Fixed compatibility with game version 0.5.2.0 and higher
- Custom Entity module may not function fully due to Unity version bump. Use at your own risk.

### v1.3.1
- Fixed a crash when using NativeTranspiler (Also AudioModule)
- Allow to load AudioClip from a `.wav` file

### v1.3.0
- Public release of JSON loader module
- Added Equipment Slot Module
- Added RuntimeMaterialV2 which uses FixedString
- Added ModObjectTypeAuthoring

### v1.2.4
- Add IDynamicItemHandler interface

### v1.2.3
- Add unboxed variant of NativeList
- Another missed GCHandle in ModProjectile added

### v1.2.2
- Add missing GCHandles

### v1.2.1
- Fixed compatibility with game version 0.5.1.0 and higher
- Stop adding mod workbench when no one uses it

### v1.2.0
- Fixed compatibility with game version 0.5.0.0 and higher
- Chat Commands Module now uses Rewired Keybinds

### v1.1.2
- Fixed crashes and issues when using advanced features of CustomEntityModule

### v1.1.1
- @ `CaptainStupid#8539`: Fix for Localization failing on vanilla ObjectIDs

### v1.1.0
- Update to BepInEx 6.0.0-be.656
- Added Audio Module
- Added Drop Tables module
- Added Mod Resources module
- Added Utils Module
- Significant improvements to Custom Entity Module. Custom almost anything is possible now.

### v1.0.0
**WARNING!** This version contains breaking changes. Make sure to update ALL of your mods before proceeding!
- Refactor project structure. Now using submodules.
- Localization, Rewired keybinds are moved into their own submodule
- Added Chat commands submodule
- Added Custom Entity submodule

### v0.1.1
- Now supports dedicated servers

### v0.1.0
- Added Localization helper class
- Added RewiredKeybinds helper class
- Improve README

### v0.0.1
- Initial Release
</details>
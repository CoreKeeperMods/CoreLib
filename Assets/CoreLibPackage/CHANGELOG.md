# Changelog
# CoreLib v3

## v3.6.4 (2025-07-22)
[Release](https://github.com/CoreKeeperMods/CoreLib/releases/tag/3.6.4)

[3.6.3...3.6.4](https://github.com/CoreKeeperMods/CoreLib/compare/3.6.3...3.6.4)

### Changes
- Fixed `Skin target asset mismatch` error in CoreLib.Entity

## v3.6.3

### Changes
- Updated to work with 1.1.2 and higher

## v3.6.2

### Changes
- Improved Chain Builder tool to batch update mod tags.
- No changes to submodules

## v3.6.1
### Changes
- Allow to load assets from mod asset bundles as addressable
- Minor tweak to AddMusicToRoster function

## v3.6.0
### Changes
- Fix issues with placement icon
- Fix and improve CoreLib.Audio

## v3.5.0
### Changes
- Added Component Tracker script to help track entity prefab components on game updates
- Fixed issues where workbenches created using this module would be invisible
- Updated CoreLib.Equipment module for 1.0+ version of the game

## v3.4.6
### Changes
- Updated to work with 1.1.0 and higher

## v3.4.5.1
### Changes
- Fix compile errors in Equipment module

## v3.4.5
### Changes
CoreLib.Entity:
- Updated to work with game version 1.0.1

## v3.4.4
### Changes
CoreLib:
- Add thread-safe static cache for ConfigFile instances (Thanks to DrSalzstreuer)

## v3.4.3
### Changes
CoreLib.Drops:
- Allows to specify whether to allow duplicate drops when adding a custom loot table (Thanks to ReishyouSose)

CoreLib.Commands:
- Fixed for game version 1.0.0.6

## v3.4.2
### Changes
Editor Scripts:
- Fix that compiled burst assembly would cause crashes when PPC's are used

## v3.4.1
### Changes
CoreLib.Localization:
- Add a method to add entity localization for modded items

CoreLib.Entity:
- Fix it being impossible to use Interactable Authoring Component with Object Authoring objects
- Fix custom workbenches not being interactable

## v3.4.0
### Changes
CoreLib:
- Update to work with 0.9.9.9

CoreLib.Audio:
- Update to work with 0.9.9.9

CoreLib.Commands:
- Update to work with 0.9.9.9

CoreLib.Equipment:
- Disabled broken code. Module is BROKEN.

CoreLib.RewiredExtension:
- Attempt to improve friendly name acquisition

CoreLib.UserInterface:
- Allow controller tree mapping

NOT COMPATIBLE WITH CoreLib.Equipment. MODULE IS BROKEN!

## v3.3.0
### Changes
CoreLib:
- Fixed incorrect version being reported in submodule build version check message
- Expanded functionality of Debug utility to allow displaying lines

CoreLib.Equipment:
- Allow to quickly fade spammed Emote texts

CoreLib.Entity:

Breaking changes:
- ModCraftingAuthoring includeCraftedObjectsFromBuildings field type has been changed to string (object ID)

Bug fixes:
- WorkbenchDefinition relatedWorkbenches field is now functional
- Workbenches created with Entity submodules no longer glitch on clients or after world reload
- Root workbenches actually have all sub workbenches shown
Major version is bumped due to multiple API breaking changes

## v3.2.2
### Changes:
- Commands Module: Changed method of acquiring friendly names to ensure mod added items have them
- Entity Module: Allow to override graphical prefab to use pooling

## v3.2.1
### Changes
- Updated for game version 0.7.4

## v3.2.0
### Changes
- Updated for game version 0.7.3
- Fixed some issues in ChainBuilder
- Added ability to preview PugText

## v3.1.1
- Updated for 0.7.2 game version

## v3.1.0
- Added User Interface Module

## v3.0.1
### Changes
- Improved Chain Builder to support uploading to mod IO
- Added guide on developing with CoreLib

## v3.0.0
- Ported to mod SDK

# Corelib v2
## v2.1.3
- Fixed issues using ModProjectile

## v2.1.2
- Allow to manually request any module to be loaded from config options
- Added MigrationModule. Module is intended to be opt in functionality to fix issues with missing items. Enable in the config file.

## v2.1.1
- Fixed error on launch due to reflection error

## v2.1.0
- Fixed compatibility with game version 0.6.0 and higher
- Chat command handlers can be registered separately
- Added Dump chat command
- Added `drop` and `modify` JSON loader
- Added Prefab modification API to `EntityModule`
- Changed method signatures for workbench registration

## v2.0.1
**WARNING: Update all of your mods when installing version 2.0.0 or higher!**
- Fixed crash on dedicated servers when Entity module tried to register visual prefabs
- Fixed IDynamicItemHandler color replacement logic being broken

## v2.0.0
**WARNING: Update all of your mods when installing version 2.0.0 or higher!**
- Use Il2CppInterop from my PR branch to allow for advanced DOTS features
- Added `Component` module, which allows to create custom ECS components
- Added `System` module, which allows to create pseudo systems and state requesters
- Renamed `CustomEntity` module to `Entity` module. Mods which used it need to update
- Added `ModCraftingRecipeCDAuthoring` to allow to assign custom recipes from Unity editor
- Json Loader module now can create custom blocks using JSON
- Fix Entity module impaired functionality

# CoreLib v1
## v1.4.0
**WARNING: Update all of your mods when installing this version!**
- Fixed compatibility with game version 0.5.2.0 and higher
- Custom Entity module may not function fully due to Unity version bump. Use at your own risk.

## v1.3.1
- Fixed a crash when using NativeTranspiler (Also AudioModule)
- Allow to load AudioClip from a `.wav` file

## v1.3.0
- Public release of JSON loader module
- Added Equipment Slot Module
- Added RuntimeMaterialV2 which uses FixedString
- Added ModObjectTypeAuthoring

## v1.2.4
- Add IDynamicItemHandler interface

## v1.2.3
- Add unboxed variant of NativeList
- Another missed GCHandle in ModProjectile added

## v1.2.2
- Add missing GCHandles

## v1.2.1
- Fixed compatibility with game version 0.5.1.0 and higher
- Stop adding mod workbench when no one uses it

## v1.2.0
- Fixed compatibility with game version 0.5.0.0 and higher
- Chat Commands Module now uses Rewired Keybinds

## v1.1.2
- Fixed crashes and issues when using advanced features of CustomEntityModule

## v1.1.1
- @ `CaptainStupid#8539`: Fix for Localization failing on vanilla ObjectIDs

## v1.1.0
- Update to BepInEx 6.0.0-be.656
- Added Audio Module
- Added Drop Tables module
- Added Mod Resources module
- Added Utils Module
- Significant improvements to Custom Entity Module. Custom almost anything is possible now.

## v1.0.0
**WARNING!** This version contains breaking changes. Make sure to update ALL of your mods before proceeding!
- Refactor project structure. Now using submodules.
- Localization, Rewired keybinds are moved into their own submodule
- Added Chat commands submodule
- Added Custom Entity submodule

# CoreLib v0
## v0.1.1
- Now supports dedicated servers

## v0.1.0
- Added Localization helper class
- Added RewiredKeybinds helper class
- Improve README

## v0.0.1
- Initial Release
</details>

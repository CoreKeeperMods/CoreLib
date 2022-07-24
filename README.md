# CoreLib
A modding library for Core Keeper. Provides features that makes modding Core Keeper easier.

**WARNING!** Version 1.0.0 contains breaking changes. Make sure to update ALL of your mods before proceeding!

# List of features
- Get main objects of the game, like `Managers`, `Players`
- Add new localization terms
- Add custom Rewired keybinds
- Add custom chat commands
- Add new items

# Installation
### With Mod Manager

Simply open the mod manager (if you don't have it install it [here](https://dsp.thunderstore.io/package/ebkr/r2modman/)), select **CoreLib by CoreMods**, then **Download**.

If prompted to download with dependencies, select `Yes`.
Then just click **Start modded**, and the game will run with the mod installed.

### Manually
Install BepInEx Pack from [here](https://core-keeper.thunderstore.io/package/BepInEx/BepInExPack_Core_Keeper/)<br/>

Unzip all files into `Core Keeper\BepInEx\plugins\CoreLib/` (Create folder named `CoreLib`)<br/>

This library is still WIP.

## Changelog
<details>
<summary>Changelog</summary>

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

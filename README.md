# CoreLib
A modding library for Core Keeper. Provides features that makes modding Core Keeper easier.

**WARNING!** Version 1.0.0 contains breaking changes. Make sure to update ALL of your mods before proceeding!

# List of features
- Get main objects of the game, like `Managers`, `Players`
- Add new localization terms
- Add custom Rewired keybinds
- Add custom chat commands
- Add new items, blocks, enemies and more
- Use JSON to add custom items

## Note on multiplayer and save compatibility
If you are playing with friends MAKE SURE to sync your `CoreLib.ModEntityID.cfg` and `CoreLib.TilesetID.cfg` config files. If anything inside does not match you WILL encounter issues connecting, missing items, and errors.

The same applies if you are loading a save of another user. If your ID's don't match the ID's save was created with, the save will load corrupted.

I recommend any mods adding custom content warn users about this on their page.

This might get improved later, but right now this is best that you can do.

# How to support development
If you like what I do and would like to support development, you can [donate](https://boosty.to/kremnev8).

# Installation
### With Mod Manager

Simply open the mod manager (if you don't have it install it [here](https://dsp.thunderstore.io/package/ebkr/r2modman/)), select **CoreLib by CoreMods**, then **Download**.

If prompted to download with dependencies, select `Yes`.
Then just click **Start modded**, and the game will run with the mod installed.

### Manually
Install BepInEx Pack from [here](https://core-keeper.thunderstore.io/package/BepInEx/BepInExPack_Core_Keeper/)<br/>

Unzip all files into `Core Keeper\BepInEx\plugins\CoreLib/` (Create folder named `CoreLib`)<br/>

This library is still WIP.
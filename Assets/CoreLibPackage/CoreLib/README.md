# CoreLib
A modding library for Core Keeper. Provides features that makes modding Core Keeper easier.

**WARNING!** Version 3.0.0 contains breaking changes.

# List of features
- Custom items, blocks, enemies, NPC, etc.
- Easier access to Rewired input system, localization
- Custom chat commands

And much more!

# Developing with CoreLib
To develop with CoreLib, you will need to install CoreLib package into your mod SDK project.

## Preparation
CoreLib packages use git packages, this means you **MUST** install [git](https://git-scm.com/download/win) and add it to your PATH environment variable.  

Note:
- After installing git make sure to close Unity and Unity hub completely!
- Unity Hub does not close when you close the window and needs to be exited from the Windows Notification Area

## Installation
Navigate to your mod SDK project root, and open `Packages` folder. And edit `manifest.json` file with your favorite text editor. 

Add following lines at the beginning:
```
"ck.modding.corelib": "https://github.com/CoreKeeperMods/CoreLib.git?path=/Assets/CoreLibPackage#main",
"ck.modding.sdk-extensions": "https://github.com/CoreKeeperMods/CoreLib.git?path=/Assets/SDKExtensions#main",
"ck.modding.markdig": "https://github.com/CoreKeeperMods/CoreLib.git?path=/Assets/Markdig#main",
```

If you want to install latest version of CoreLib use `#main` at the end of every line. However it is highly recommended that you lock your version, and update it manually. To do that replace `#main` with `#tag`. For example `#3.0.1`.

## Done
Now you can open your project and you will have Core Lib ready to be used. 

You can access CoreLib documentation from within your editor, by navigating to CoreLib package folder and viewing `README.md`

### Utilities
With Core Lib also comes a package called `SDK Extensions`. This package adds a few useful editors:

- Select dependencies from a dropdown (Including CoreLib)
- Updating asmdef file accordingly to your dependency declaration
- Building Burst assembly with your mod

**Note:** some of these features require manual changes to mod SDK project. Reference [my fork](https://github.com/kremnev8/CoreKeeperModSDK) of mod SDK project to see what changes you need. 

# Documentation
Each submodule contains a markdown file with documentation.

Contents:
- [Audio Submodule](../CoreLib.Audio/README.md)
- [Commands Submodule](../CoreLib.Commands/README.md)
- [Drops Submodule](../CoreLib.Drops/README.md)
- [Entity Submodule](../CoreLib.Entity/README.md)
- [Equipment Submodule](../CoreLib.Equipment/README.md)
- [Localization Submodule](../CoreLib.Localization/README.md)
- [Resources Submodule](../CoreLib.Resources/README.md)
- [RewiredExtension Submodule](../CoreLib.RewiredExtension/README.md)
- [Tilesets Submodule](../CoreLib.Tilesets/README.md)
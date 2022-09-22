# Utils Module
This module contains other useful utilities, that need to be loaded. Currently it only contains `ThreadingHelper`, which allows to call code on the main thread.

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(UtilsModule))]` to your plugin attributes. This will load the submodule.

From here you can use the module

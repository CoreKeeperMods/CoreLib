# Migration Module
This is not a typical module. It does not provide any public API. Instead it allows users to opt in into functionality with side effects.

To use it go to CoreLib config file and set this option:

`ForceModuleLoad = MigrationModule`

At the moment, the module provides only one migration:
- Ensure that any missing items are removed from world or inventories to prevent lag.
# JSON Loader Module

JSON loader module allows to create custom items using the power of JSON.

This is a prerelease version of JSON loader module, and it can be subject to change. Although item format is unlikely to change.

## Usage example

### With code

Make sure to add `[CoreLibSubmoduleDependency(nameof(JsonLoaderModule))]` to your plugin attributes. This will load the
submodule.

Then in your plugin `Load()` method write:

```csharp
string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
JsonLoaderModule.LoadFolder(PluginInfo.PLUGIN_GUID, pluginfolder);
```

### Without code

Json Loader module can also be used without writing code at all. To do so use prebuilt
loader [assembly](../../../JsonModLoader/JsonModLoader/Binaries).

Create a new folder in the plugins folder, and move the loader assembly there. Make sure to include `manifest.json`
file, which must contain `author`, `name` and `version_number` properties. This is the same file used by Thunderstore.

## Creating items using JSON

To start creating items create a new folder named `resources` in your plugin folder. You can include here all textures
and item json files. You are allowed to create any subfolders, so it's recommended to structure your folder.

Each item is contained it its own json file. Here is an example of a simple item.

```json
{
  "type": "item",
  "itemId": "MyMod:MyItem",
  "icon": "icons/my-item-big-icon.png",
  "smallIcon": "icons/my-item-small-icon.png",
  "localizedName": "My Item",
  "localizedDescription": "This item was added using JSON!",
  "isStackable": true
}
```

To be able to craft the item you also must add a mod workbench (Or write code that would make it obtainable):

```json
{
  "type": "modWorkbench",
  "itemId": "MyMod:MyWorkbench",
  "icon": "icons/wb-big-icon.png",
  "smallIcon": "icons/wb-small-icon.png",
  "localizedName": "Test Workbench",
  "localizedDescription": "This workbench was added using JSON!",
  "isStackable": true,
  "requiredObjectsToCraft": [
    {
      "objectID": "GoldBar",
      "amount": 6
    }
  ],
  "canCraft": [
    "MyMod:MyItem"
  ]
}
```

You might need to look at the game code to determine what fields you need to set, and what values exist. To do so you
can use basic dnSpy [project](https://core-keeper-modding.gitbook.io/modding-wiki/modding/view-source-code#using-cpp2il)

### Sprites

The paths to the icons are relative to your `resources` folder. Sprites can be defined in two ways: `string`
and `object`. An example of the second:

```json
{
  "icon": {
    "path": "icons/icon.png",
    "type": "icon-top"
  }
}
```

Supported types include `icon-top` and `icon-bottom`, which allow to contain both small and big icons in one file. This
does imply your texture is `16x32` px.

You can also define the rect manually:

```json
{
  "icon": {
    "path": "icons/icon.png",
    "rect": {
      "x": 0,
      "y": 16,
      "width": 16,
      "height": 16
    }
  }
}
```

This would identical to `icon-top` icon type above.

### Components

Your JSON item can have ECS components attached to it. To do so use `components` property. Each object must contain
a `type` field, which must be a valid class name. This includes modded components too.
<details><summary>Melee Weapon Example</summary>

```json
{
  "type": "item",
  "itemId": "MyMod:TestMace",
  "icon": {
    "path": "icons/mace.png",
    "rect": {
      "x": 0,
      "y": 80,
      "width": 40,
      "height": 40
    }
  },
  "smallIcon": {
    "path": "icons/mace.png",
    "rect": {
      "x": 0,
      "y": 80,
      "width": 40,
      "height": 40
    }
  },
  "localizedName": "My Test Mace",
  "localizedDescription": "This item was added using JSON!",
  "craftingTime": 2.5,
  "initialAmount": 666,
  "objectType": "MeleeWeapon",
  "rarity": "Epic",
  "iconOffset": {
    "x": 0,
    "y": -0.125
  },
  "requiredObjectsToCraft": [
    {
      "objectID": "IronBar",
      "amount": 5
    },
    {
      "objectID": "MyMod:Iridium",
      "amount": 5
    }
  ],
  "components": [
    {
      "type": "DurabilityCDAuthoring",
      "maxDurability": 666,
      "repairMultiplier": 0.5,
      "reinforceCostMultiplier": 1
    },
    {
      "type": "GivesConditionsWhenEquippedCDAuthoring",
      "givesConditionsWhenEquipped": [
        {
          "id": "MovementSpeedDecrease",
          "valueMultiplier": 1,
          "value": -150
        },
        {
          "id": "MeleeDamageIncrease",
          "valueMultiplier": 1,
          "value": 400
        }
      ]
    },
    {
      "type": "CooldownCDAuthoring",
      "cooldown": 1.5
    },
    {
      "type": "WeaponDamageCDAuthoring",
      "damage": 250,
      "damageMultiplier": 1
    },
    {
      "type": "WeaponCDAuthoring",
      "baseHitColliderSize": 2,
      "extraHitColliderReachSize": 0
    }
  ]
}
```
This JSON file defines a melee mace weapon, that has certain effects.
Do note that the `mace.png` file contains a sprite as explained in the [item guide](../CustomEntity/Guides/Items.md)

</details>

### Blocks and other entities
Currently JSON loader module does not support adding custom blocks. This is being worked on though.
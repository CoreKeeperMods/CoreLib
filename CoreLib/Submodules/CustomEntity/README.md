# Custom Entity Module
Custom Entity Module is a submodule that allows to add new entities. This includes items, blocks, NPCs and other things. 

## Note on multiplayer and save compatibility
If you are playing with friends MAKE SURE to sync your `CoreLib.ModItemID.cfg` and `CoreLib.TilesetID.cfg` config files. If anything inside does not match you WILL encounter issues connecting, missing items, and errors.

The same applies if you are loading a save of another user. If your ID's don't match the ID's save was created with, the save will load corrupted.

I recommend any mods adding custom content warn users about this on their page.

This might get improved later, but right now this is best that you can do.

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(CustomEntityModule))]` to your plugin attributes. This will load the submodule.

Before continuing follow guide on [Resource Module](../ModResources/README.md) page to setup your asset bundle.

After setting up Unity project make sure to add the Editor Kit to your project. It contains dummies for all custom components amd some custom editors. You can find it [here](../../../EditorKit/)

### Making Entity Prefab
In your Unity Project make a new prefab (Or copy one of the original ones). It should contain only the root object with `EntityMonoBehaviorData` component attached. This looks like this:

![EntityMonoBehaviorData In Unity Editor](./documentation/EntityMonoBehaviorData.png)<br>
In this component you can set all kind of properties that affect what the entity is. Most important properties are:

- `ObjectType` - defines what kind of entity is it. Here you can make it an item, block, enemy, etc
- `Icon` and `SmallIcon` - defines icon of your entity. This is mostly used for items. Small icon is often used when you hold your item in hand. Normal icon is shown in inventory and on pedestals.
- `PrefabInfos` here you need to have one entry with reference to prefab itself. The `Prefab` field allows to define custom visual for entity. This is used to make blocks, enemies, etc. For items it needs to be null.

On your entity prefab you can attach other ECS components which alter entity behavior or properties. You can inspect vanilla entities to find out what components do what.

For more specific guides on different types of custom entities check [guides](Guides/) folder.

Once you are done setting up your prefab place it in a folder with the name of your mod and pack a asset bundle. Don't forget to add the prefab to the bundle.

### Adding entity

With entity prefab made adding it is really easy. In your plugin `Load()` method add this code:
```cs
// Register your prefab. Use a UNIQUE string id to identify your entity. I recommend to include your mod name in the ID.
ObjectID entityID = CustomEntityModule.AddEntity($"{MODNAME}:MyAmazingEntity", "Assets/myamazingmod/Prefab/MyAmazingEntity");

// Add localization terms for your item
CustomEntityModule.AddEntityLocalization(entityID,
    "My Amazing Item",
    "This amazing item will change the world!");
```

If your entity need to have multiple variations use this method. Include list of all needed entity prefabs. Their variation fields have to be set to correct variations:
```cs
ObjectID entityID = CustomEntityModule.AddEntityWithVariations($"{MODNAME}:MyAmazingEntity", new[]
{
    "Assets/myamazingmod/Prefab/MyAmazingBackwardEntity",
    "Assets/myamazingmod/Prefab/MyAmazingForwardEntity",
    "Assets/myamazingmod/Prefab/MyAmazingLeftEntity",
    "Assets/myamazingmod/Prefab/MyAmazingRightEntity",
});
```

To allow player to obtain the added entity (if it's an item), you will need to either add it to a mob drop loot pool, or use custom workbenches:
```cs
// You only need to supply single texture, which was set multiple mode
// Also you can specify the recipe and even disable automatic addition to root mod workbenches
ObjectID workbench = CustomEntityModule.AddModWorkbench($"{MODNAME}:MyWorkbench",
    "Assets/myamazingmodTextures/myworkbench-texture", 
    new List<CraftingData> {new CraftingData(ObjectID.ScarletBar, 4)});

// Now you can add up to 18 items to this workbench
CustomEntityModule.AddWorkbenchItem(workbench, entityID);
```

You should cache or remember `entityID` variable. It's the ObjectID that the game uses to identify the entity.
If you ever need to get this ID you can use `CustomEntityModule.GetObjectId(string itemID)` method to access it again.

Also please note that you can't hardcode this ID. It will change depending on user mods installed. It can also be changed by user themselves by editing `CoreLib.ModItemID.cfg` config file found in `config` folder.
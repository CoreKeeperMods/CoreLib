# Custom Entity Module
Custom Entity Module is a submodule that allows to add new entities. This includes items, blocks, NPCs and other things. 

## Important Note
Please note that currently this submodule <b>DOES NOT</b> support adding anything except items. This is a technical limitation that we still need to fix. 

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(CustomEntityModule))]` to your plugin attributes. This will load the submodule.

### Making Item Prefab
To actually make the item you will need a set up Unity Project. You can follow this [guide](https://github.com/CoreKeeperMods/Getting-Started/wiki/Getting-The-Assets-In-Unity).
In your Unity Project make a new prefab (Or copy one of the original ones). It should contain only the root object with `EntityMonoBehaviorData` component attached. This looks like this:
![EntityMonoBehaviorData In Unity Editor](./documentation/EntityMonoBehaviorData.png)
In this component you can set all kind of properties that affect what the item is. Most important properties are:

- `ObjectType` - defines what kind of entity is it. Here you can make it an armor piece or sword.
- `Icon` and `SmallIcon` - defines visual aspects of your item. Small icon is often used when you hold your item in hand. Normal icon is shown in inventory and on pedestals.
- `IsStakable` - defines if you can stack your item
- `Required Objects To Craft` allows you to define your item crafting recipe.
- `PrefabInfos` here you need to have one entry with reference to prefab itself. Note that `Prefab` field need to be empty for entity to be a item. Currently filling this field <b>IS NOT SUPPORTED</b>!

On your item prefab you can attach other ECS components which alter item behavior or properties. You can inspect vanilla items to find out what components do what.

For example here I have a `DurabilityCDAuthoring` component added. With it item will now have durability. Use this in combination with `InitialAmount` property to make item with durability.
![DurabilityCDAuthoring In Unity Editor](./documentation/DurabilityComponent.png)
Once you are done setting up your prefab place it in a folder with the name of your mod and pack a asset bundle. Don't forget to add the prefab to the bundle.

### Adding item in code

With item prefab made adding it is really easy. In your plugin `Load()` method add this code:
```c#
// Get path to your plugin folder
string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

// Create a new ResourceData class with information about the bundle
resource = new ResourceData(MODNAME, "myamazingmod", pluginfolder);

// Load the aseet bundle and add the resource.
resource.LoadAssetBundle("myamazingmodbundle");
CustomEntityModule.AddResource(resource);

// Register your prefab. Use a UNIQUE string id to identify your item. I recommend to include your mod name in the ID.
ObjectID itemIndex = CustomEntityModule.AddEntity($"{MODNAME}:MyAmazingItem", "Assets/myamazingmod/Prefab/MyAmazingItem.prefab");

// Add localization terms for your item
Localization.AddTerm($"Items/{itemIndex}", "My Amazing Item");
Localization.AddTerm($"Items/{itemIndex}Desc", "This amazing item will change the world!");
```
Note that here `myamazingmod` is a <b>keyword</b>. This keyword is later used in the prefab path. This is important, as this is how I find which asset bundle contains the prefab. If you forget to include a keyword in your asset path it <b>WILL NOT LOAD</b>

You should cache or remember `itemIndex` variable. It contains numerical ID that the game uses to identify the item. You can cast it to `ObjectID` enum and pass to game code.
If you ever need to get this ID you can use `CustomEntityModule.GetItemIndex(string itemID)` method to access it again.

Also please note that you can't hardcode this ID. It will change depending on user mods installed. It can also be changed by user themselves by editing `CoreLib.ModItemID.cfg` config file found in `config` folder. 
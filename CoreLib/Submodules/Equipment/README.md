# Equipment Slot Module
This module allows to create custom equipment slots, which define how user should be using the item. Equipment slots drive every tool in Core Keeper.

## Usage example
Make sure to add `[CoreLibSubmoduleDependency(nameof(JsonLoaderModule))]` to your plugin attributes. This will load the
submodule.

### Adding custom slot
Then create a new class inheriting either from `EquipmentSlot` or `PlaceObjectSlot` classes. You can try to inherit other relevant classes. You also must implement an interface `IModEquipmentSlot`.

<details><summary>Custom Slot Example</summary>

```csharp
public class MyCustomSlot : PlaceObjectSlot, IModEquipmentSlot
{
    public const string MyObjectType = "MyMod:MyObjectType";

    public MyCustomSlot(IntPtr ptr) : base(ptr) { }

    public override EquipmentSlotType slotType => EquipmentSlotModule.GetEquipmentSlotType<MyCustomSlot>();

    public override void OnEquip(PlayerController player)
    {
        this.CallBase<PlaceObjectSlot, Action<PlayerController>>(nameof(OnEquip), player);

		// this method is executed when player start using the tool
    }

    public override void PlaceItem() {	}

    public override void HandleInput(
        bool interactPressed,
        bool interactReleased,
        bool secondInteractPressed,
        bool secondInteractReleased,
        bool interactIsHeldDown,
        bool secondInteractIsHeldDown)
    {
        // this method is executed when player presses one of the interact button. Check for them and do your thing.
    }

    public ObjectType GetSlotObjectType()
    {
		// return ObjectType that this should should be used for
        return CustomEntityModule.GetObjectType(MyObjectType);
    }

    public void UpdateSlotVisuals(PlayerController controller)
    {
		// here you need to set how item in player hand should look. If you want the default, don't change anything here
        ObjectDataCD objectDataCd = controller.GetHeldObject();
        ObjectInfo objectInfo = PugDatabase.GetObjectInfo(objectDataCd.objectID, objectDataCd.variation);
        
        controller.ActivateCarryableItemSpriteAndSkin(
            controller.carryablePlaceItemSprite,
            controller.carryableSwingItemSkinSkin,
            objectInfo);
        controller.carryablePlaceItemSprite.sprite = objectInfo.smallIcon;
        controller.carryablePlaceItemColorReplacer.UpdateColorReplacerFromObjectData(objectDataCd);
    }
}
```
For more examples you can look at my recent `Bucket Mod`

</details>

Then in your plugin `Load()` method write:

```csharp
EquipmentSlotModule.RegisterEquipmentSlot<MyCustomSlot>(EquipmentSlotModule.PLACEMENT_PREFAB);
```

### Presets
You can either use one of the preset prefabs, or create your own prefab in Unity Editor. References other slot prefabs to do so.

Placement Prefab preset includes `PlacementHandler` and `PlaceIcon` (The blue square target), and is suitable if you are inheriting from `PlaceObjectSlot`.

### Item with custom ObjectType
To use the equipment slot you must also add an item, which uses a custom ObjectType. Here is an example of such item using `JsonLoaderModule`:

```json
{
	"type" : "item",
	"itemId" : "MyMod:MyCustomItem",
	"icon" : {
		"path" : "icons/icon.png",
		"type" : "icon-top"
	},
	"smallIcon" : {
		"path" : "icons/icon.png",
		"type" : "icon-bottom"
	},
	"variationIsDynamic" : true,
	"localizedName" : "My Item",
	"localizedDescription" : "This item is added using JSON",
	"objectType" : "MyMod:MyObjectType",
	"requiredObjectsToCraft" : [
		{
			"objectID" : "IronBar",
			"amount" : 4
		}
	]
}
```

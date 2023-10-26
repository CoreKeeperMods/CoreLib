# Equipment Slot Module
Equipment Slot Module is a CoreLib submodule that allows to create custom equipment slots, which define how user should be using the item. Equipment slots drive every tool in Core Keeper.

## Usage example
Make sure to call `CoreLibMod.LoadModules(typeof(EquipmentSlotModule));` to in your mod `EarlyInit()` function, before using the module. This will load the submodule.

### Adding custom slot
Then create a new class inheriting either from `EquipmentSlot` or `PlaceObjectSlot` classes. You can try to inherit other relevant classes. You also must implement an interface `IModEquipmentSlot`.

<details><summary>Custom Slot Example</summary>

```csharp
public class MyCustomSlot : PlaceObjectSlot, IModEquipmentSlot
{
    public const string MyObjectType = "MyMod:MyObjectType";

    public override EquipmentSlotType slotType => EquipmentSlotModule.GetEquipmentSlotType<MyCustomSlot>();

    public override void OnEquip(PlayerController player)
    {
        base.OnEquip(player);

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
        ObjectDataCD objectDataCd = controller.GetHeldObject();
        ObjectInfo objectInfo = PugDatabase.GetObjectInfo(objectDataCd.objectID, objectDataCd.variation);

        ContainedObjectsBuffer objectsBuffer = AsBuffer(objectDataCd);

        controller.InvokeVoid("ActivateCarryableItemSpriteAndSkin", new object[]
        {
            controller.carryablePlaceItemSprite,
            controller.carryablePlaceItemPugSprite,
            controller.carryableSwingItemSkinSkin,
            objectInfo,
            objectsBuffer
        });

        controller.carryablePlaceItemSprite.sprite = objectInfo.smallIcon;
        controller.carryablePlaceItemColorReplacer.UpdateColorReplacerFromObjectData(objectsBuffer);
    }
}
```
For more examples you can look at my recent `Bucket Mod`

</details>

Then in your mod `ModObjectLoaded()` method write:

```csharp
var slot = gameObject.GetComponent<EquipmentSlot>();

if (slot != null)
{
    EntityModule.AddToAuthoringList(gameObject);
    EquipmentModule.RegisterEquipmentSlot<CrowbarEquipmentSlot>(gameObject);
}
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

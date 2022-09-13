# Tips on making items
If you don't know how to make a certain type of item, find it in the Unity Editor and copy the prefab. There you should see everything that makes it tick.

Properties of `EntityMonoBehaviourData` tha are relevant:

- `Initial Amount` - useful together with `DurabilityCDAuthoring` to define initial durability
- `Icon` and `Small Icon` - defines icon of your entity. This is mostly used for items. Small icon is often used when you hold your item in hand. Normal icon is shown in inventory and on pedestals.
- `Is Stackable` - defines if you can stack your item
- `Required Objects To Craft` allows you to define your item crafting recipe.


## Swords, Tools, etc
To make a equipable item with use animation you need to:
- Set the `ObjectType` to tool or weapon type
- Add `DurabilityCDAuthoring`, `GivesConditionsWhenEquipedCDAuthoring`, `CooldownCDAuthoring`, `WeaponDamageCDAuthoring` and `LevelCDAuthoring` component authorings and configure them correctly
- Assign both icons to first sprite in item animation sheet.

Example of the sprite sheet. It should be 120x120 px and have 7 sprites showing item in different states. You can find such sheets for all weapons and tools in the Unity Editor

![Example Item Sheet](../documentation/SwordExample.png)<br>

### Ranged weapon 
To make a ranged weapon you mostly need to do the same as with any other weapon. Except for the fact that you will need a custom projectile entity added.
To hook modded projectile entity use `ModRangeWeaponCDAuthoring` component instead of `RangeWeaponCDAuthoring`

## Armor

To make armor you need to:
- Set the `ObjectType` to armor type
- Add `DurabilityCDAuthoring`, `ModEquipmentSkinCDAuthoring`, `GivesConditionsWhenEquipedCDAuthoring` and `LevelCDAuthoring` component authorings and configure them correctly

Make a armor spite sheet. Examples of such sheets can be found in the Unity Editor.
Now assign your texture to `ModEquipmentSkinCDAuthoring`. The component will automatically set everything up.
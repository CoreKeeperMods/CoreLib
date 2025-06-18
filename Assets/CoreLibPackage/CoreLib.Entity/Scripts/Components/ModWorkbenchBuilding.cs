using System.Linq;
using Pug.Sprite;
using Pug.UnityExtensions;
using PugMod;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Components
{
    public class ModWorkbenchBuilding : CraftingBuilding
    {

        public SpriteObject mainObject;
        public SpriteObject shadowObject;

        public Transform particleSpawnLocation;

        public override void OnOccupied()
        {
            int variation = DirectionBasedOnVariationCD.GetVariationFromDirection(direction.RoundToInt2());

            var info = objectInfo;

            var skin = EntityModule.modWorkbenches.FirstOrDefault(definition => API.Authoring.GetObjectID(definition.itemId) == info.objectID);

            mainObject.skin = skin.assetSkin;
            mainObject.ApplyVisualChange();
            mainObject.SetVariantByIndex((variation + 2) % 4);

            shadowObject.skin = skin.assetSkin;
            shadowObject.ApplyVisualChange();

            craftingUITitle = skin.title;
            craftingUITitleLeftBox = skin.leftTitle;
            craftingUITitleRightBox = skin.rightTitle;
            craftingUIBackgroundVariation = skin.skin;
            
            foreach (var workbench in skin.relatedWorkbenches)
            {
                var modObjectPrefab = EntityModule.modWorkbenches
                    .FirstOrDefault(definition => definition.itemId == workbench);
                if (modObjectPrefab)
                {
                    var newItem = new CraftingUISettings(
                        API.Authoring.GetObjectID(workbench),
                        modObjectPrefab.title,
                        modObjectPrefab.leftTitle,
                        modObjectPrefab.rightTitle,
                        modObjectPrefab.skin);
                    if(!craftingUIOverrideSettings.Contains(newItem))
                        craftingUIOverrideSettings.Add(newItem);
                    continue;
                }
                
                var otherObject = PugDatabase.entityMonobehaviours.Find(mono =>
                    mono.ObjectInfo.objectID == API.Authoring.GetObjectID(workbench));
                if (otherObject is null) continue;

                var objectPrefab = otherObject.ObjectInfo?.prefabInfos[0]?.prefab;
                if(objectPrefab is null) continue;
                
                switch (objectPrefab)
                {
                    case SimpleWideCraftingBuilding simpleWideCraftingBuilding:
                        var wideSetting = simpleWideCraftingBuilding.craftingUIOverrideSettings.Find(x => 
                            x.usedForBuilding == otherObject.ObjectInfo.objectID) ?? new CraftingUISettings(
                            otherObject.ObjectInfo.objectID,
                            simpleWideCraftingBuilding.craftingUITitle,
                            simpleWideCraftingBuilding.craftingUITitleLeftBox,
                            simpleWideCraftingBuilding.craftingUITitleRightBox,
                            simpleWideCraftingBuilding.craftingUIBackgroundVariation);
                        if(!craftingUIOverrideSettings.Contains(wideSetting))
                            craftingUIOverrideSettings.Add(wideSetting);
                        break;
                    case SimpleCraftingBuilding simpleCraftingBuilding:
                        var simpleSetting = simpleCraftingBuilding.craftingUIOverrideSettings.Find(x => 
                            x.usedForBuilding == otherObject.ObjectInfo.objectID) ?? new CraftingUISettings(
                            otherObject.ObjectInfo.objectID,
                            simpleCraftingBuilding.craftingUITitle,
                            simpleCraftingBuilding.craftingUITitleLeftBox,
                            simpleCraftingBuilding.craftingUITitleRightBox,
                            simpleCraftingBuilding.craftingUIBackgroundVariation);
                        if(!craftingUIOverrideSettings.Contains(simpleSetting))
                            craftingUIOverrideSettings.Add(simpleSetting);
                        break;
                }
            }
            
            base.OnOccupied();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            Manager.effects.PlayPuff(PuffID.DirtBlockDust, this.particleSpawnLocation.position, 5);
        }
    }
}
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
            var variationFromDirection = DirectionBasedOnVariationCD.GetVariationFromDirection(direction.RoundToInt2());

            var info = objectInfo;

            var skin = EntityModule.modWorkbenches.FirstOrDefault(definition => API.Authoring.GetObjectID(definition.itemId) == info.objectID);

            mainObject.skin = skin.assetSkin;
            mainObject.ApplyVisualChange();
            mainObject.SetVariantByIndex((variationFromDirection + 2) % 4);

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
                
                if (modObjectPrefab is not null)
                {
                    var newItem = new CraftingUISettings(
                        API.Authoring.GetObjectID(modObjectPrefab.itemId),
                        modObjectPrefab.title,
                        modObjectPrefab.leftTitle,
                        modObjectPrefab.rightTitle,
                        modObjectPrefab.skin);
                    if(!craftingUIOverrideSettings.Contains(newItem))
                        craftingUIOverrideSettings.Add(newItem);
                    continue;
                }
                
                var monoObject = PugDatabase.entityMonobehaviours.Find(mono =>
                    mono.ObjectInfo.objectID == API.Authoring.GetObjectID(workbench));

                CraftingBuilding craftingBuilding;
                CraftingUISettings craftingSetting;
                switch (monoObject)
                {
                    case EntityMonoBehaviourData entityAuthoring:
                        craftingBuilding = (CraftingBuilding) entityAuthoring.ObjectInfo.prefabInfos[0].prefab;
                        if (craftingBuilding is null) continue;
                        craftingSetting = craftingBuilding.craftingUIOverrideSettings.Find(x => 
                            x.usedForBuilding == entityAuthoring.ObjectInfo.objectID) ?? new CraftingUISettings(
                            entityAuthoring.ObjectInfo.objectID,
                            craftingBuilding.craftingUITitle,
                            craftingBuilding.craftingUITitleLeftBox,
                            craftingBuilding.craftingUITitleRightBox,
                            craftingBuilding.craftingUIBackgroundVariation);
                        if(!craftingUIOverrideSettings.Contains(craftingSetting))
                            craftingUIOverrideSettings.Add(craftingSetting);
                        break;
                    case ObjectAuthoring objectAuthoring:
                        craftingBuilding = (CraftingBuilding) objectAuthoring.graphicalPrefab.GetComponentAtIndex(0);
                        if (craftingBuilding is null) continue;
                        craftingSetting = craftingBuilding.craftingUIOverrideSettings.Find(x => 
                            x.usedForBuilding == API.Authoring.GetObjectID(objectAuthoring.objectName)) ?? new CraftingUISettings(
                            API.Authoring.GetObjectID(objectAuthoring.objectName),
                            craftingBuilding.craftingUITitle,
                            craftingBuilding.craftingUITitleLeftBox,
                            craftingBuilding.craftingUITitleRightBox,
                            craftingBuilding.craftingUIBackgroundVariation);
                        if(!craftingUIOverrideSettings.Contains(craftingSetting))
                            craftingUIOverrideSettings.Add(craftingSetting);
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
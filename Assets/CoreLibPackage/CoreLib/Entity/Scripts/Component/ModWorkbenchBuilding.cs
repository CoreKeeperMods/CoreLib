using System;
using CoreLib.Util.Extension;
using Pug.Sprite;
using PugMod;
using UnityEngine;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    /// Represents a ModWorkbenchBuilding, a modular crafting building connected
    /// with dynamic visual representation and modifiable crafting components.
    /// Provides functionalities for customization and interaction during gameplay.
    public class ModWorkbenchBuilding : CraftingBuilding
    {
        public SpriteObject mainObject;
        public SpriteObject shadowObject;
        public Transform particleSpawnLocation;

        internal GameObject ModdedEntity;
        // ReSharper disable once UnusedMember.Local
        private static Logger Log => EntityModule.Log;

        public override void OnOccupied()
        {
            ModdedEntity = EntityModule.ModdedEntities.Find(x => x.GetEntityObjectID() == objectInfo.objectID ).gameObject;
            if (ModdedEntity is not null)
            {
                
                if (ModdedEntity.TryGetComponent(out ModReskinCondition reskinCondition))
                {
                    if (gameObject.TryGetComponent(out SpriteSkinFromEntityAndSeason skin))
                    {
                        var newSkin = reskinCondition.GetReskinCondition();
                        // ReSharper disable once UsageOfDefaultStructEquality
                        if (!skin.reskinConditions.Contains(newSkin))
                            skin.reskinConditions.Add(newSkin);
                        skin.UpdateGraphicsFromObjectInfo(objectInfo);
                    }
                }
                /* TODO rework
                if (ModdedEntity.TryGetComponent(out ModCraftingUISetting modCraftingUISetting))
                {
                    craftingUITitle = modCraftingUISetting.craftingUITitle;
                    craftingUITitleLeftBox = modCraftingUISetting.craftingUITitleLeftBox;
                    craftingUITitleRightBox = modCraftingUISetting.craftingUITitleRightBox;
                    craftingUIBackgroundVariation = modCraftingUISetting.craftingUIBackgroundVariation;
                }

                if (ModdedEntity.TryGetComponent(out ModCraftingAuthoring modCraftingAuthoring))
                {
                    foreach (string item in modCraftingAuthoring.includeCraftedObjectsFromBuildings)
                    {
                        var buildingID = API.Authoring.GetObjectID(item);
                        var monoObject = PugDatabase.entityMonobehaviours.Find(mono => mono.ObjectInfo.objectID == buildingID).GameObject;
                        if (monoObject.TryGetComponent(out ModCraftingUISetting craftingUISetting))
                        {
                            if (!craftingUIOverrideSettings.Contains(craftingUISetting.GetCraftingUISettings())) 
                                craftingUIOverrideSettings.Add(craftingUISetting.GetCraftingUISettings());
                        }
                        else if (monoObject.TryGetComponent(out EntityMonoBehaviourData entityMonoBehaviourData))
                        {
                            var craftingBuilding = (CraftingBuilding)entityMonoBehaviourData.objectInfo.prefabInfos[0].prefab;
                            if (craftingBuilding is null) continue;
                            var craftingSetting = 
                                craftingBuilding.craftingUIOverrideSettings.Find(x => x.usedForBuilding == entityMonoBehaviourData.ObjectInfo.objectID) 
                                ?? new CraftingUISettings(entityMonoBehaviourData.ObjectInfo.objectID, craftingBuilding.craftingUITitle, craftingBuilding.craftingUITitleLeftBox, craftingBuilding.craftingUITitleRightBox, craftingBuilding.craftingUIBackgroundVariation);
                            if (!craftingUIOverrideSettings.Contains(craftingSetting))
                                craftingUIOverrideSettings.Add(craftingSetting);
                        }
                    }
                }*/
                
            }
            
            base.OnOccupied();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            Manager.effects.PlayPuff(PuffID.DirtBlockDust, particleSpawnLocation.position, 5);
        }
    }
}
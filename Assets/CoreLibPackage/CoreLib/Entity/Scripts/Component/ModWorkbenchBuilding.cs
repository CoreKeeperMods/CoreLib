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

        internal GameObject moddedEntity;
        // ReSharper disable once UnusedMember.Local
        private static Logger Log => EntityModule.log;

        public override void OnOccupied()
        {
            moddedEntity = EntityModule.moddedEntities.Find(x => x.GetEntityObjectID() == objectInfo.objectID ).gameObject;
            if (moddedEntity is not null)
            {
                
                if (moddedEntity.TryGetComponent(out ModReskinCondition reskinCondition))
                {
                    if (gameObject.TryGetComponent(out SpriteSkinFromEntityAndSeason skin))
                    {
                        var newSkin = reskinCondition.GetReskinCondition();
                        // ReSharper disable once UsageOfDefaultStructEquality
                        if (skin.reskinConditions.FindIndex(x => x.objectID == newSkin.objectID) == -1)
                            skin.reskinConditions.Add(newSkin);
                        skin.UpdateGraphicsFromObjectInfo(objectInfo);
                    }
                }
                if (moddedEntity.TryGetComponent(out ModCraftingUISetting modCraftingUISetting)) 
                    defaultUISettings = modCraftingUISetting.GetCraftingUISetting();

                if (moddedEntity.TryGetComponent(out ModCraftingAuthoring modCraftingAuthoring))
                {
                    foreach (string item in modCraftingAuthoring.includeCraftedObjectsFromBuildings)
                    {
                        var buildingID = API.Authoring.GetObjectID(item);
                        var monoObject = PugDatabase.entityMonobehaviours.Find(mono => mono.ObjectInfo.objectID == buildingID).GameObject;
                        if (monoObject.TryGetComponent(out ModCraftingUISetting craftingUISetting))
                        {
                            buildingSpecificUISettings.Add(craftingUISetting.GetCraftingUISettingOverride());
                        }
                        else if (monoObject.TryGetComponent(out EntityMonoBehaviourData entityMonoBehaviourData))
                        {
                            var craftingBuilding = (CraftingBuilding)entityMonoBehaviourData.objectInfo.prefabInfos?[0]?.prefab;
                            if (craftingBuilding is null) continue;
                            var craftingSetting = 
                                craftingBuilding.buildingSpecificUISettings.Find(x => x.usedForBuilding == entityMonoBehaviourData.ObjectInfo.objectID) 
                                ?? new CraftingUISettingsOverride
                                {
                                    usedForBuilding = buildingID,
                                    settings = craftingBuilding.defaultUISettings
                                };
                            buildingSpecificUISettings.Add(craftingSetting);
                        }
                    }
                }
                
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
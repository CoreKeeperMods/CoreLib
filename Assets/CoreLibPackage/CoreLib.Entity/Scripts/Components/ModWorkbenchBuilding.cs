using System.Collections.Generic;
using System.Linq;
using Pug.Sprite;
using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    /// <summary>
    /// Represents a ModWorkbenchBuilding, a modular crafting building connected
    /// with dynamic visual representation and modifiable crafting components.
    /// Provides functionalities for customization and interaction during gameplay.
    /// </summary>
    public class ModWorkbenchBuilding : CraftingBuilding
    {
        /// <summary>
        /// Represents the primary visual object displayed for the ModWorkbenchBuilding.
        /// Typically used to define and modify the graphical appearance of the workbench
        /// in the game environment.
        /// </summary>
        public SpriteObject mainObject;

        /// <summary>
        /// Represents a secondary SpriteObject associated with the crafting building.
        /// This object is typically used to define a visual shadow or complementary visual
        /// component for the main object of the workbench.
        /// </summary>
        public SpriteObject shadowObject;

        /// <summary>
        /// Represents the location where particle effects are spawned for the modded workbench building.
        /// This transform serves as the reference point for positioning visual effects, such as dust or smoke,
        /// during events such as building destruction or interaction.
        /// </summary>
        public Transform particleSpawnLocation;

        /// <summary>
        /// Invoked when the building becomes occupied by an entity or user.
        /// This method updates the building's visual representation and crafting UI to match
        /// the workbench definition associated with the occupying entity, if available.
        /// Customizes the workbench appearance and properties, including title and background,
        /// based on predefined mod workbench definitions.
        /// </summary>
        public override void OnOccupied()
        {
            WorkbenchDefinition skin = EntityModule.modWorkbenches.FirstOrDefault(definition => API.Authoring.GetObjectID(definition.itemId) == objectInfo.objectID);
            if (skin is not null)
            {
                bool hasReskin = TryGetComponent(out SpriteSkinFromEntityAndSeason reskin);
                if (hasReskin)
                {
                    SpriteSkinFromEntityAndSeason.ReskinCondition newSkin = new()
                    {
                        objectID = API.Authoring.GetObjectID(skin.itemId),
                        dependsOnVariation = false,
                        variation = 0,
                        season = Season.None,
                        reskin = new List<SpriteSkinFromEntityAndSeason.SkinAndGradientMap>
                        { new() { skin = skin.assetSkin }, new() { skin = skin.assetSkin } }
                    };
                    if(reskin.reskinConditions.FindIndex(x => x.objectID == API.Authoring.GetObjectID(skin.itemId)) == -1)
                        reskin.reskinConditions.Add(newSkin);
                    reskin.UpdateGraphicsFromObjectInfo(objectInfo);
                }
                
                craftingUITitle = skin.title;
                craftingUITitleLeftBox = skin.leftTitle;
                craftingUITitleRightBox = skin.rightTitle;
                craftingUIBackgroundVariation = skin.skin;
            }
            base.OnOccupied();
        }

        /// <summary>
        /// Called when the building associated with this object is destroyed or "killed."
        /// This method handles cleanup and invokes specific destruction effects or processes.
        /// </summary>
        protected override void OnDeath()
        {
            base.OnDeath();
            Manager.effects.PlayPuff(PuffID.DirtBlockDust, this.particleSpawnLocation.position, 5);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Pug.Sprite;
using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    public class ModWorkbenchBuilding : CraftingBuilding
    {

        public SpriteObject mainObject;
        public SpriteObject shadowObject;

        public Transform particleSpawnLocation;

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

        protected override void OnDeath()
        {
            base.OnDeath();
            Manager.effects.PlayPuff(PuffID.DirtBlockDust, this.particleSpawnLocation.position, 5);
        }
    }
}
using System.Linq;
using Pug.Sprite;
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
            base.OnOccupied();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            Manager.effects.PlayPuff(PuffID.DirtBlockDust, this.particleSpawnLocation.position, 5);
        }
    }
}
using System.Collections.Generic;
using CoreLib.Submodules.ModEntity.Components;
using PugConversion;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#pragma warning disable CS0649

namespace CoreLib.JsonLoader.Components
{
    public struct TemplateBlockCD : IComponentData
    {
        public Color lightColor;
        public float3 verticalSpriteOffset;
        public float3 horizontalSpriteOffset;
        public float3 shadowOffset;
        public float3 prefabOffset;
        public int interactHandlerId;
        public float interactRadius;
    }
    
    public class TemplateBlockCDAuthoring : ModCDAuthoringBase, IHasDefaultValue
    {
        public Sprite verticalSprite;
        public Sprite horizontalSprite;
        public Sprite verticalEmissiveSprite;
        public Sprite horizontalEmissiveSprite;
        public Sprite shadowSprite;
        public Color lightColor;
        public float3 verticalSpriteOffset;
        public float3 horizontalSpriteOffset;
        public float3 shadowOffset;
        public float3 prefabOffset;
        public string interactHandler;
        public float interactRadius;

        public void InitDefaultValues()
        {
            verticalSpriteOffset = new float3(0, 0.625f, 0);
            horizontalSpriteOffset = new float3(0, 0.1f, 0.5f);
            prefabOffset = new float3(0, 0, -0.5f);
            shadowOffset = new float3(0, 0.0625f, 0.4375f);
            interactRadius = 1.3f;
        }

        public override bool Apply(MonoBehaviour data)
        {
            List<Sprite> sprites = new List<Sprite>(5)
            {
                verticalSprite,
                verticalEmissiveSprite,
                horizontalSprite,
                horizontalEmissiveSprite,
                shadowSprite
            };
            
            if (data is EntityMonoBehaviourData oldData)
                oldData.objectInfo.additionalSprites = sprites;
            else if (data is ObjectAuthoring newData)
                newData.additionalSprites = sprites;

            return true;
        }
    }

    public class TemplateBlockConverter : SingleAuthoringComponentConverter<TemplateBlockCDAuthoring>
    {
        protected override void Convert(TemplateBlockCDAuthoring authoring)
        {
            AddComponentData(new TemplateBlockCD()
            {
                lightColor = authoring.lightColor,
                verticalSpriteOffset = authoring.verticalSpriteOffset,
                horizontalSpriteOffset = authoring.horizontalSpriteOffset,
                shadowOffset = authoring.shadowOffset,
                prefabOffset = authoring.prefabOffset,
                interactHandlerId = JsonLoaderModule.GetInteractHandlerId(authoring.interactHandler),
                interactRadius = authoring.interactRadius
            });
        }
    }
}
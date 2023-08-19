using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#pragma warning disable CS0649

namespace CoreLib.Components
{
    public struct TemplateBlockCD : IComponentData
    {
        public Color lightColor;
        public float3 verticalSpriteOffset;
        public float3 horizontalSpriteOffset;
        public float3 shadowOffset;
        public float3 prefabOffset;
        public int interactionId;
    }
    
    public class TemplateBlockCDAuthoring : ModCDAuthoringBase
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

        internal int interactMethodIndex;

        public void InitDefaultValues()
        {
            verticalSpriteOffset = new float3(0, 0.625f, 0);
            horizontalSpriteOffset = new float3(0, 0.1f, 0.5f);
            prefabOffset = new float3(0, 0, -0.5f);
            shadowOffset = new float3(0, 0.0625f, 0.4375f);
            interactMethodIndex = -1;
        }

        public void PostInit()
        {
            //if (!string.IsNullOrEmpty(interactHandler))
            //    interactMethodIndex = JsonLoaderModule.RegisterInteractHandler(interactHandler);
        }

        public override bool Apply(MonoBehaviour data)
        {
            List<Sprite> sprites = new List<Sprite>(5);
            sprites.Add(verticalSprite);
            sprites.Add(verticalEmissiveSprite);
            sprites.Add(horizontalSprite);
            sprites.Add(horizontalEmissiveSprite);
            sprites.Add(shadowSprite);
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
                interactionId = authoring.interactMethodIndex,
            });
        }
    }
}
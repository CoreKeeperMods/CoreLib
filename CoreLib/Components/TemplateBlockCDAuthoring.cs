using System.Runtime.InteropServices;
using CoreLib.Submodules.JsonLoader;
using CoreLib.Util;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using IntPtr = System.IntPtr;

#pragma warning disable CS0649

namespace CoreLib.Components
{
    [Il2CppImplements(typeof(IComponentData))]
    public struct TemplateBlockCD
    {
        public Color lightColor;
        public float3 verticalSpriteOffset;
        public float3 horizontalSpriteOffset;
        public float3 shadowOffset;
        public float3 prefabOffset;
        public int interactionId;
    }
    

    [Il2CppImplements(typeof(IConvertGameObjectToEntity))]
    public class TemplateBlockCDAuthoring : ModCDAuthoringBase, IHasDefaultValue
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddModComponentData(entity, new TemplateBlockCD()
            {
                lightColor = lightColor,
                verticalSpriteOffset = verticalSpriteOffset,
                horizontalSpriteOffset = horizontalSpriteOffset,
                shadowOffset = shadowOffset,
                prefabOffset = prefabOffset,
                interactionId = interactMethodIndex
            });
        }

        public Il2CppReferenceField<Sprite> verticalSprite;
        public Il2CppReferenceField<Sprite> horizontalSprite;
        public Il2CppReferenceField<Sprite> verticalEmissiveSprite;
        public Il2CppReferenceField<Sprite> horizontalEmissiveSprite;
        public Il2CppReferenceField<Sprite> shadowSprite;
        public Il2CppValueField<Color> lightColor;
        public Il2CppValueField<float3> verticalSpriteOffset;
        public Il2CppValueField<float3> horizontalSpriteOffset;
        public Il2CppValueField<float3> shadowOffset;
        public Il2CppValueField<float3> prefabOffset;
        public string interactHandler;

        private Il2CppValueField<int> interactMethodIndex;

        private GCHandle mainSpriteHandle;
        private GCHandle floorSpriteHandle;
        private GCHandle mainEmissiveSpriteHandle;
        private GCHandle floorEmissiveSpriteHandle;
        private GCHandle shadowSpriteHandle;


        public TemplateBlockCDAuthoring(IntPtr ptr) : base(ptr) { }
        
        public void InitDefaultValues()
        {
            verticalSpriteOffset.Value = new float3(0, 0.625f, 0);
            horizontalSpriteOffset.Value = new float3(0, 0.1f, 0.5f);
            prefabOffset.Value = new float3(0, 0, -0.5f);
            shadowOffset.Value = new float3(0, 0.0625f, 0.4375f);
            interactMethodIndex.Value = -1;
        }

        public void PostInit()
        {
            if (!string.IsNullOrEmpty(interactHandler))
                interactMethodIndex.Value = JsonLoaderModule.RegisterInteractHandler(interactHandler);
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            List<Sprite> sprites = new List<Sprite>(5);
            sprites.Add(verticalSprite.Value);
            sprites.Add(verticalEmissiveSprite.Value);
            sprites.Add(horizontalSprite.Value);
            sprites.Add(horizontalEmissiveSprite.Value);
            sprites.Add(shadowSprite.Value);
            data.objectInfo.additionalSprites = sprites;

            return true;
        }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                mainSpriteHandle = GCHandle.Alloc(verticalSprite.Value);
                floorSpriteHandle = GCHandle.Alloc(horizontalSprite.Value);
                mainEmissiveSpriteHandle = GCHandle.Alloc(verticalEmissiveSprite.Value);
                floorEmissiveSpriteHandle = GCHandle.Alloc(horizontalEmissiveSprite.Value);
                shadowSpriteHandle = GCHandle.Alloc(shadowSprite.Value);
            }
            return alloc;
        }

        private void OnDestroy()
        {
            mainSpriteHandle.Free();
            floorSpriteHandle.Free();
            mainEmissiveSpriteHandle.Free();
            floorEmissiveSpriteHandle.Free();
            shadowSpriteHandle.Free();
        }
    }
}
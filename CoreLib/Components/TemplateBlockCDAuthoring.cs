using System.Runtime.InteropServices;
using CoreLib.Util;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using IntPtr = System.IntPtr;

namespace CoreLib.Components
{
    [Il2CppImplements(typeof(IComponentData))]
    public struct TemplateBlockCD
    {
        public Color lightColor;
    }
    

    [Il2CppImplements(typeof(IConvertGameObjectToEntity))]
    public class TemplateBlockCDAuthoring : ModCDAuthoringBase
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddModComponentData(entity, new TemplateBlockCD()
            {
                lightColor = lightColor.Value
            });
        }

        public Il2CppReferenceField<Sprite> mainSprite;
        public Il2CppReferenceField<Sprite> floorSprite;
        public Il2CppReferenceField<Sprite> mainEmissiveSprite;
        public Il2CppReferenceField<Sprite> floorEmissiveSprite;
        public Il2CppReferenceField<Sprite> shadowSprite;
        public Il2CppValueField<Color> lightColor;

        private GCHandle mainSpriteHandle;
        private GCHandle floorSpriteHandle;
        private GCHandle mainEmissiveSpriteHandle;
        private GCHandle floorEmissiveSpriteHandle;
        private GCHandle shadowSpriteHandle;


        public TemplateBlockCDAuthoring(IntPtr ptr) : base(ptr) { }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            List<Sprite> sprites = new List<Sprite>(5);
            sprites.Add(mainSprite.Value);
            sprites.Add(mainEmissiveSprite.Value);
            sprites.Add(floorSprite.Value);
            sprites.Add(floorEmissiveSprite.Value);
            sprites.Add(shadowSprite.Value);
            data.objectInfo.additionalSprites = sprites;
            
            return true;
        }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                mainSpriteHandle = GCHandle.Alloc(mainSprite.Value);
                floorSpriteHandle = GCHandle.Alloc(floorSprite.Value);
                mainEmissiveSpriteHandle = GCHandle.Alloc(mainEmissiveSprite.Value);
                floorEmissiveSpriteHandle = GCHandle.Alloc(floorEmissiveSprite.Value);
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
using System;
using CoreLib.Util;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CoreLib.Components
{
    public class TemplateBlock : ModEntityMonoBehavior
    {
        public Il2CppReferenceField<SpriteRenderer> verticalRenderer;
        public Il2CppReferenceField<SpriteRenderer> horizontalRenderer;

        public Il2CppReferenceField<SpriteRenderer> verticalEmmisiveRenderer;
        public Il2CppReferenceField<SpriteRenderer> horizontalEmmisiveRenderer;
        
        public Il2CppReferenceField<SpriteRenderer> shadowSpriteRenderer;

        public Il2CppReferenceField<GameObject> lightGO;

        public TemplateBlock(IntPtr ptr) : base(ptr) { }

        public override void OnOccupied()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(OnOccupied));
            List<Sprite> sprites = objectInfo.additionalSprites;
            if (sprites._items[0] != null)
            {
                verticalRenderer.Value.sprite = sprites._items[0];
                verticalRenderer.Value.gameObject.SetActive(true);
            }
            if (sprites._items[1] != null)
            {
                verticalEmmisiveRenderer.Value.sprite = sprites._items[1];
                verticalEmmisiveRenderer.Value.gameObject.SetActive(true);
            }
            if (sprites._items[2] != null)
            {
                horizontalRenderer.Value.sprite = sprites._items[2];
                horizontalRenderer.Value.gameObject.SetActive(true);
            }
            if (sprites._items[3] != null)
            {
                horizontalEmmisiveRenderer.Value.sprite = sprites._items[3];
                horizontalEmmisiveRenderer.Value.gameObject.SetActive(true);
            }
            if (sprites._items[4] != null)
            {
                shadowSpriteRenderer.Value.sprite = sprites._items[4];
                shadowSpriteRenderer.Value.gameObject.SetActive(true);
            }
            
            TemplateBlockCD templateBlockCd = world.EntityManager.GetModComponentData<TemplateBlockCD>(entity);
            if (templateBlockCd.lightColor != Color.black)
            {
                lightGO.Value.SetActive(true);
                optionalLightOptimizer.lightToOptimize.color = templateBlockCd.lightColor;
            }
        }

        public override void OnFree()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(OnFree));
            verticalRenderer.Value.gameObject.SetActive(false);
            verticalEmmisiveRenderer.Value.gameObject.SetActive(false);
            horizontalRenderer.Value.gameObject.SetActive(false);
            horizontalEmmisiveRenderer.Value.gameObject.SetActive(false);
            shadowSpriteRenderer.Value.gameObject.SetActive(false);
            lightGO.Value.SetActive(false);
        }
        
    }
}
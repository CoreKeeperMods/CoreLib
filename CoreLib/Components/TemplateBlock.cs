using System;
using CoreLib.Submodules.CustomEntity;
using CoreLib.Submodules.JsonLoader;
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
        public Il2CppReferenceField<Transform> SRPivot;

        public TemplateBlock(IntPtr ptr) : base(ptr) { }

        public override void OnOccupied()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(OnOccupied));
            List<Sprite> sprites = objectInfo.additionalSprites;
            TemplateBlockCD templateBlockCd = world.EntityManager.GetModComponentData<TemplateBlockCD>(entity);
            
            if (sprites._items[0] != null)
            {
                verticalRenderer.Value.sprite = sprites._items[0];
                verticalRenderer.Value.gameObject.SetActive(true);
                verticalRenderer.Value.transform.localPosition = (Vector3)templateBlockCd.verticalSpriteOffset;
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
                horizontalRenderer.Value.transform.localPosition = templateBlockCd.horizontalSpriteOffset;
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
                shadowSpriteRenderer.Value.transform.localPosition = templateBlockCd.shadowOffset;
            }

            SRPivot.Value.localPosition = templateBlockCd.prefabOffset;
            
            if (templateBlockCd.lightColor != Color.black)
            {
                lightGO.Value.SetActive(true);
                optionalLightOptimizer.lightToOptimize.color = templateBlockCd.lightColor;
            }

            if (templateBlockCd.interactionId >= 0)
            {
                interactable.gameObject.SetActive(true);
                interactable.optionalOutlineController.gameObject.SetActive(true);
                interactable.additionalOutlineControllers._items[0].gameObject.SetActive(true);
            }
        }

        //TODO crash when interactible object calls a method.
        public override void OnFree()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(OnFree));
            verticalRenderer.Value.gameObject.SetActive(false);
            verticalEmmisiveRenderer.Value.gameObject.SetActive(false);
            horizontalRenderer.Value.gameObject.SetActive(false);
            horizontalEmmisiveRenderer.Value.gameObject.SetActive(false);
            shadowSpriteRenderer.Value.gameObject.SetActive(false);
            lightGO.Value.SetActive(false);
            interactable.gameObject.SetActive(false);
            interactable.optionalOutlineController.gameObject.SetActive(false);
            interactable.additionalOutlineControllers._items[0].gameObject.SetActive(false);
        }

        public void OnUse()
        {
            CoreLibPlugin.Logger.LogInfo("get Component");
            TemplateBlockCD templateBlockCd = world.EntityManager.GetModComponentData<TemplateBlockCD>(entity);

            try
            {
                IInteractionHandler handler = JsonLoaderModule.GetInteractionHandler<IInteractionHandler>(templateBlockCd.interactionId);
                handler.OnInteraction(this);
            }
            catch (Exception e)
            {
                string stringId = CustomEntityModule.GetObjectStringId(objectInfo.objectID);
                CoreLibPlugin.Logger.LogWarning($"Exception while executing object {stringId} interaction handler:\n{e}");
            }
        }
        
    }
}
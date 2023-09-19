using System;
using System.Collections.Generic;
using CoreLib.Submodules.ModEntity;
using PugMod;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Components
{
    public class TemplateBlock : EntityMonoBehaviour
    {
        public SpriteRenderer verticalRenderer;
        public SpriteRenderer horizontalRenderer;
        
        public SpriteRenderer verticalEmmisiveRenderer;
        public SpriteRenderer horizontalEmmisiveRenderer;
        
        public SpriteRenderer shadowSpriteRenderer;
        
        public GameObject lightGO;
        public Transform SRPivot;
        
        private int InteractHandler => world.EntityManager.GetComponentData<TemplateBlockCD>(entity).interactHandlerId;

        public override void OnOccupied()
        {
            base.OnOccupied();
            List<Sprite> sprites = objectInfo.additionalSprites;
            TemplateBlockCD templateBlockCd = world.EntityManager.GetComponentData<TemplateBlockCD>(entity);
            
            if (sprites[0] != null)
            {
                verticalRenderer.sprite = sprites[0];
                verticalRenderer.gameObject.SetActive(true);
                verticalRenderer.transform.localPosition = templateBlockCd.verticalSpriteOffset;
            }
            if (sprites[1] != null)
            {
                verticalEmmisiveRenderer.sprite = sprites[1];
                verticalEmmisiveRenderer.gameObject.SetActive(true);
            }
            if (sprites[2] != null)
            {
                horizontalRenderer.sprite = sprites[2];
                horizontalRenderer.gameObject.SetActive(true);
                horizontalRenderer.transform.localPosition = templateBlockCd.horizontalSpriteOffset;
            }
            if (sprites[3] != null)
            {
                horizontalEmmisiveRenderer.sprite = sprites[3];
                horizontalEmmisiveRenderer.gameObject.SetActive(true);
            }
            if (sprites[4] != null)
            {
                shadowSpriteRenderer.sprite = sprites[4];
                shadowSpriteRenderer.gameObject.SetActive(true);
                shadowSpriteRenderer.transform.localPosition = templateBlockCd.shadowOffset;
            }

            SRPivot.localPosition = templateBlockCd.prefabOffset;
            
            if (templateBlockCd.lightColor != Color.black)
            {
                lightGO.SetActive(true);
                optionalLightOptimizer.lightToOptimize.color = templateBlockCd.lightColor;
            }

            if (templateBlockCd.interactHandlerId > 0)
            {
                interactable.gameObject.SetActive(true);
                interactable.optionalOutlineController.gameObject.SetActive(true);
                interactable.additionalOutlineControllers[0].gameObject.SetActive(true);
                interactable.radius = templateBlockCd.interactRadius;
            }
            else
            {
                interactable.gameObject.SetActive(false);
            }
        }

        public override void OnFree()
        {
            base.OnFree();
            verticalRenderer.gameObject.SetActive(false);
            verticalEmmisiveRenderer.gameObject.SetActive(false);
            horizontalRenderer.gameObject.SetActive(false);
            horizontalEmmisiveRenderer.gameObject.SetActive(false);
            shadowSpriteRenderer.gameObject.SetActive(false);
            lightGO.SetActive(false);
            interactable.gameObject.SetActive(false);
            interactable.optionalOutlineController.gameObject.SetActive(false);
            interactable.additionalOutlineControllers[0].gameObject.SetActive(false);
        }

        public void OnUse()
        {
            try
            {
                IInteractionHandler handler = JsonLoaderModule.GetInteractionHandler<IInteractionHandler>(InteractHandler);
                handler?.OnInteract(this);
            }
            catch (Exception e)
            {
                string stringId = API.Authoring.ObjectProperties.GetPropertyString(objectInfo.objectID, "name");
                CoreLibMod.Log.LogWarning($"Exception while executing object {stringId} interaction handler:\n{e}");
            }
        }

        public void OnEnter()
        {
            try
            {
                IInteractionHandler handler = JsonLoaderModule.GetInteractionHandler<IInteractionHandler>(InteractHandler);
                handler?.OnEnter(this);
            }
            catch (Exception e)
            {
                string stringId = API.Authoring.ObjectProperties.GetPropertyString(objectInfo.objectID, "name");
                CoreLibMod.Log.LogWarning($"Exception while executing object {stringId} interaction handler:\n{e}");
            }
        }
        
        public void OnExit()
        {
            try
            {
                IInteractionHandler handler = JsonLoaderModule.GetInteractionHandler<IInteractionHandler>(InteractHandler);
                handler?.OnExit(this);
            }
            catch (Exception e)
            {
                string stringId = API.Authoring.ObjectProperties.GetPropertyString(objectInfo.objectID, "name");
                CoreLibMod.Log.LogWarning($"Exception while executing object {stringId} interaction handler:\n{e}");
            }
        }
    }
}
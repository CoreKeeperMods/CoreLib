using System;
using System.Runtime.InteropServices;

namespace CoreLib.Components
{
    public class ModEntityMonoBehavior : EntityMonoBehaviour, IAllocate
    {
        private GCHandle optionalColorReplacersHandle;
        private GCHandle optionalColorSpriteSheetSkinsHandle;
        private GCHandle colorSpriteSheetsHandle;
        private GCHandle optionalColorSpriteRenderersHandle;
        private GCHandle defaultColorSpritesHandle;
        private GCHandle colorSpritesPerSpriteRendererHandle;
        private GCHandle optionalLightsToPaintHandle;
        private GCHandle optionalLightsDefaultColorsHandle;
        private GCHandle lightPaintColorsHandle;
        private GCHandle optionalSpriteSheetSkinsHandle;
        private GCHandle skinSpriteSheetsHandle;
        private GCHandle seasonalSkinSpriteSheetsHandle;
        private GCHandle conditionsEffectsSettingsHandle;
        private GCHandle spritesToRandomlyOffsetSlightlyOnZHandle;
        private GCHandle sfxParamsHandle;
        private GCHandle puffParamsHandle;
        private GCHandle objectVariationsHandle;
        private GCHandle particleSpawnLocationsHandle;
        private GCHandle spritesToRandomlyFlipHandle;
        private GCHandle particlesToDisableOnLowQualityHandle;
        private GCHandle defaultSpritePositionsHandle;

        protected bool allocated;

        public ModEntityMonoBehavior(IntPtr ptr) : base(ptr) { }

        public override void Awake()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(Awake));
            Allocate();
        }

        public virtual bool Allocate()
        {
            if (allocated) return false;

            optionalColorReplacersHandle = GCHandle.Alloc(optionalColorReplacers);
            optionalColorSpriteSheetSkinsHandle = GCHandle.Alloc(optionalColorSpriteSheetSkins);
            colorSpriteSheetsHandle = GCHandle.Alloc(colorSpriteSheets);
            optionalColorSpriteRenderersHandle = GCHandle.Alloc(optionalColorSpriteRenderers);
            defaultColorSpritesHandle = GCHandle.Alloc(defaultColorSprites);
            colorSpritesPerSpriteRendererHandle = GCHandle.Alloc(colorSpritesPerSpriteRenderer);
            optionalLightsToPaintHandle = GCHandle.Alloc(optionalLightsToPaint);
            optionalLightsDefaultColorsHandle = GCHandle.Alloc(optionalLightsDefaultColors);
            lightPaintColorsHandle = GCHandle.Alloc(lightPaintColors);
            optionalSpriteSheetSkinsHandle = GCHandle.Alloc(optionalSpriteSheetSkins);
            skinSpriteSheetsHandle = GCHandle.Alloc(skinSpriteSheets);
            seasonalSkinSpriteSheetsHandle = GCHandle.Alloc(seasonalSkinSpriteSheets);
            spritesToRandomlyOffsetSlightlyOnZHandle = GCHandle.Alloc(spritesToRandomlyOffsetSlightlyOnZ);
            sfxParamsHandle = GCHandle.Alloc(sfxParams);
            puffParamsHandle = GCHandle.Alloc(puffParams);
            objectVariationsHandle = GCHandle.Alloc(objectVariations);
            particleSpawnLocationsHandle = GCHandle.Alloc(particleSpawnLocations);
            spritesToRandomlyFlipHandle = GCHandle.Alloc(spritesToRandomlyFlip);
            particlesToDisableOnLowQualityHandle = GCHandle.Alloc(particlesToDisableOnLowQuality);
            conditionsEffectsSettingsHandle = GCHandle.Alloc(conditionsEffectsSettings);
            defaultSpritePositionsHandle = GCHandle.Alloc(defaultSpritePositions);

            allocated = true;
            return true;
        }

        public override void OnDestroy()
        {
            this.CallBase<PoolableSimple>(nameof(OnDestroy));

            optionalColorReplacersHandle.Free();
            optionalColorSpriteSheetSkinsHandle.Free();
            colorSpriteSheetsHandle.Free();
            optionalColorSpriteRenderersHandle.Free();
            defaultColorSpritesHandle.Free();
            colorSpritesPerSpriteRendererHandle.Free();
            optionalLightsToPaintHandle.Free();
            optionalLightsDefaultColorsHandle.Free();
            lightPaintColorsHandle.Free();
            optionalSpriteSheetSkinsHandle.Free();
            skinSpriteSheetsHandle.Free();
            seasonalSkinSpriteSheetsHandle.Free();
            spritesToRandomlyOffsetSlightlyOnZHandle.Free();
            sfxParamsHandle.Free();
            puffParamsHandle.Free();
            objectVariationsHandle.Free();
            particleSpawnLocationsHandle.Free();
            spritesToRandomlyFlipHandle.Free();
            particlesToDisableOnLowQualityHandle.Free();
            conditionsEffectsSettingsHandle.Free();
            defaultSpritePositionsHandle.Free();
        }
    }
}
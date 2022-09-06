using System;
using System.Runtime.InteropServices;

namespace CoreLib.Components
{
    public class ModProjectile : Projectile
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
        private GCHandle spritesToRandomlyOffsetSlightlyOnZHandle;
        private GCHandle sfxParamsHandle;
        private GCHandle puffParamsHandle;
        private GCHandle objectVariationsHandle;
        private GCHandle particleSpawnLocationsHandle;
        private GCHandle spritesToRandomlyFlipHandle;
        private GCHandle particlesToDisableOnLowQualityHandle;
        private GCHandle conditionsEffectsSettingsHandle;
        private GCHandle defaultSpritePositionsHandle;
        private GCHandle conditionsHandle;
        private GCHandle piercesWallTypesHandle;

        protected bool allocated;

        public ModProjectile(IntPtr ptr) : base(ptr) { }

        public override void Awake()
        {
            base.Awake();
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
            spritesToRandomlyOffsetSlightlyOnZHandle = GCHandle.Alloc(spritesToRandomlyOffsetSlightlyOnZ);
            sfxParamsHandle = GCHandle.Alloc(sfxParams);
            puffParamsHandle = GCHandle.Alloc(puffParams);
            objectVariationsHandle = GCHandle.Alloc(objectVariations);
            particleSpawnLocationsHandle = GCHandle.Alloc(particleSpawnLocations);
            spritesToRandomlyFlipHandle = GCHandle.Alloc(spritesToRandomlyFlip);
            particlesToDisableOnLowQualityHandle = GCHandle.Alloc(particlesToDisableOnLowQuality);
            conditionsEffectsSettingsHandle = GCHandle.Alloc(conditionsEffectsSettings);
            defaultSpritePositionsHandle = GCHandle.Alloc(defaultSpritePositions);
            conditionsHandle = GCHandle.Alloc(conditions);
            piercesWallTypesHandle = GCHandle.Alloc(piercesWallTypesHandle);

            allocated = true;
            return true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

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
            spritesToRandomlyOffsetSlightlyOnZHandle.Free();
            sfxParamsHandle.Free();
            puffParamsHandle.Free();
            objectVariationsHandle.Free();
            particleSpawnLocationsHandle.Free();
            spritesToRandomlyFlipHandle.Free();
            particlesToDisableOnLowQualityHandle.Free();
            conditionsEffectsSettingsHandle.Free();
            defaultSpritePositionsHandle.Free();
            conditionsHandle.Free();
            piercesWallTypesHandle.Free();
        }
    }
}
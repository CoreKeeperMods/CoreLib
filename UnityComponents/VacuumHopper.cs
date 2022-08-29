using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using CoreLib.Submodules.CustomEntity;

#if IL2CPP

namespace CK_ChunkLoader.Enitity
{
    public class VacuumHopper : ModEntityMonoBehavior
    {
        public Il2CppReferenceField<SpriteRenderer> beltRenderer;
        public Il2CppReferenceField<SpriteRenderer> mainRenderer;

        public Il2CppReferenceField<SpriteSheetSkin> beltSkin;
        public Il2CppReferenceField<SpriteSheetSkin> mainSkin;

        public Il2CppReferenceField<List<Sprite>> beltSprites;
        public Il2CppReferenceField<List<Sprite>> mainSprites;

        public Il2CppReferenceField<List<Texture2D>> frames;

        private GCHandle beltSpritesHandle;
        private GCHandle mainSpritesHandle;
        private GCHandle framesHandle;


        public VacuumHopper(IntPtr ptr) : base(ptr) { }

        public override bool Allocate()
        {
            bool shouldAllocate = base.Allocate();
            if (shouldAllocate)
            {
                beltSpritesHandle = GCHandle.Alloc(beltSprites.Value);
                mainSpritesHandle = GCHandle.Alloc(mainSprites.Value);
                framesHandle = GCHandle.Alloc(frames.Value);
            }

            return shouldAllocate;
        }

        public override void OnOccupied()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(OnOccupied));
            ChunkLoaderPlugin.logger.LogDebug($"Vacuum hooper occupied!, Variation {variation}");

            UpdateVisual();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            beltSpritesHandle.Free();
            mainSpritesHandle.Free();
            framesHandle.Free();
        }

        public void OnUse()
        {
            int newVariation = (variation + 1) % 4;
            SetVariation(newVariation);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (mainSprites.Value != null && variation < mainSprites.Value.Count)
            {
                Sprite sprite = mainSprites.Value._items[variation];
                mainRenderer.Value.sprite = sprite;
                sprite = beltSprites.Value._items[variation];
                beltRenderer.Value.sprite = sprite;
            }
        }


        public override void ManagedLateUpdate()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(ManagedLateUpdate));

            if (entityExist)
            {
                int frame = (int)(Time.time * 15) % frames.Value.Count;
                Texture2D currentFrame = frames.Value._items[frame];
                beltSkin.Value.skin = currentFrame;
                mainSkin.Value.skin = currentFrame;
            }
        }
    }
}
#endif
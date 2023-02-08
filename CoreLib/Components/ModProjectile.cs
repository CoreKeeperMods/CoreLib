using System;
using System.Runtime.InteropServices;

namespace CoreLib.Components
{
    public class ModProjectile : Projectile, IAllocate
    { 
        private GCHandle reskinOptionsHandle;
        private GCHandle paintableOptionssHandle;
        private GCHandle soundOptionsHandle;
        private GCHandle particleOptionsHandle;
        private GCHandle objectVariationsHandle;
        private GCHandle spritesToRandomlyFlipHandle;
        private GCHandle conditionsEffectsSettingsHandle;
        
        private GCHandle conditionsHandle;
        private GCHandle piercesWallTypesHandle;
        private GCHandle tilesToCheckHandle;

        protected bool allocated;

        public ModProjectile(IntPtr ptr) : base(ptr) { }

        public override void Awake()
        {
            this.CallBase<EntityMonoBehaviour>(nameof(Awake));
            Allocate();
        }

        public virtual bool Allocate()
        {
            if (allocated) return false;

            reskinOptionsHandle = GCHandle.Alloc(reskinOptions);
            paintableOptionssHandle = GCHandle.Alloc(paintableOptions);
            soundOptionsHandle = GCHandle.Alloc(soundOptions);
            particleOptionsHandle = GCHandle.Alloc(particleOptions);
            objectVariationsHandle = GCHandle.Alloc(objectVariations);
            spritesToRandomlyFlipHandle = GCHandle.Alloc(spritesToRandomlyFlip);
            conditionsEffectsSettingsHandle = GCHandle.Alloc(conditionsEffectsSettings);
            
            conditionsHandle = GCHandle.Alloc(conditions);
            piercesWallTypesHandle = GCHandle.Alloc(piercesWallTypesHandle);
            tilesToCheckHandle = GCHandle.Alloc(tilesToCheck);

            allocated = true;
            return true;
        }

        public override void OnDestroy()
        {
            this.CallBase<PoolableSimple>(nameof(OnDestroy));

            reskinOptionsHandle.Free();
            paintableOptionssHandle.Free();
            soundOptionsHandle.Free();
            particleOptionsHandle.Free();
            objectVariationsHandle.Free();
            spritesToRandomlyFlipHandle.Free();
            conditionsEffectsSettingsHandle.Free();
            
            conditionsHandle.Free();
            piercesWallTypesHandle.Free();
            tilesToCheckHandle.Free();
        }
    }
}
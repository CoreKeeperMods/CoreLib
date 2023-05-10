using System;
using System.Runtime.InteropServices;
using Il2CppSystem.Collections.Generic;
using Unity.Mathematics;

namespace CoreLib.Components
{
    public class ModProjectile : Projectile, IAllocate
    { 
        private GCHandle spriteObjectsHandle;
        private GCHandle reskinOptionsHandle;
        private GCHandle paintableOptionssHandle;
        private GCHandle soundOptionsHandle;
        private GCHandle particleOptionsHandle;
        private GCHandle objectVariantsHandle;
        private GCHandle spritesToRandomlyFlipHandle;
        private GCHandle conditionsEffectsSettingsHandle;
        
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

            spriteObjectsHandle = GCHandle.Alloc(spriteObjects);
            reskinOptionsHandle = GCHandle.Alloc(reskinOptions);
            paintableOptionssHandle = GCHandle.Alloc(paintableOptions);
            soundOptionsHandle = GCHandle.Alloc(soundOptions);
            particleOptionsHandle = GCHandle.Alloc(particleOptions);
            objectVariantsHandle = GCHandle.Alloc(objectVariants);
            spritesToRandomlyFlipHandle = GCHandle.Alloc(spritesToRandomlyFlip);
            conditionsEffectsSettingsHandle = GCHandle.Alloc(conditionsEffectsSettings);

            tilesToCheck ??= new HashSet<int2>();
            tilesToCheckHandle = GCHandle.Alloc(tilesToCheck);

            allocated = true;
            return true;
        }

        public override void OnDestroy()
        {
            this.CallBase<PoolableSimple>(nameof(OnDestroy));

            spriteObjectsHandle.Free();
            reskinOptionsHandle.Free();
            paintableOptionssHandle.Free();
            soundOptionsHandle.Free();
            particleOptionsHandle.Free();
            objectVariantsHandle.Free();
            spritesToRandomlyFlipHandle.Free();
            conditionsEffectsSettingsHandle.Free();
            
            tilesToCheckHandle.Free();
        }
    }
}
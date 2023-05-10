using System;
using System.Runtime.InteropServices;

namespace CoreLib.Components
{
    public class ModEntityMonoBehavior : EntityMonoBehaviour, IAllocate
    {
        private GCHandle spriteObjectsHandle;
        private GCHandle reskinOptionsHandle;
        private GCHandle paintableOptionssHandle;
        private GCHandle soundOptionsHandle;
        private GCHandle particleOptionsHandle;
        private GCHandle objectVariantsHandle;
        private GCHandle spritesToRandomlyFlipHandle;
        private GCHandle conditionsEffectsSettingsHandle;

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

            spriteObjectsHandle = GCHandle.Alloc(spriteObjects);
            reskinOptionsHandle = GCHandle.Alloc(reskinOptions);
            paintableOptionssHandle = GCHandle.Alloc(paintableOptions);
            soundOptionsHandle = GCHandle.Alloc(soundOptions);
            particleOptionsHandle = GCHandle.Alloc(particleOptions);
            objectVariantsHandle = GCHandle.Alloc(objectVariants);
            spritesToRandomlyFlipHandle = GCHandle.Alloc(spritesToRandomlyFlip);
            conditionsEffectsSettingsHandle = GCHandle.Alloc(conditionsEffectsSettings);

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
        }
    }
}
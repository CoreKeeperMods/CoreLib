using System;

namespace CoreLib.Components
{
    public class ModProjectile : Projectile, IAllocate
    {
        public void Awake()
        {
        }

        public virtual bool Allocate()
        {
            return default(bool);
        }

        public void OnDestroy()
        {
        }
    }
}
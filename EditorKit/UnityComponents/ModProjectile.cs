using System;

namespace CoreLib.Components
{
    public class ModProjectile : Projectile
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
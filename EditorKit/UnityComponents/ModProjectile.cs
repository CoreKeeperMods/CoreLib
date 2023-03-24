using System;
using System.Collections.Generic;
using Unity.Mathematics;

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
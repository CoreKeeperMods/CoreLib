using System;

namespace CoreLib.Submodules.CustomEntity
{
    public class ModEntityMonoBehavior : EntityMonoBehaviour
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
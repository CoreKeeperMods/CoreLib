using System;

namespace CoreLib.Components
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
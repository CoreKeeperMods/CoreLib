using UnityEngine;

namespace CoreLib.Components
{
    public class ModCDAuthoringBase : MonoBehaviour, IAllocate
    {
        public virtual void Awake()
        {
        }

        public virtual bool Allocate()
        {
            return default(bool);
        }

        public virtual bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }
    }
}
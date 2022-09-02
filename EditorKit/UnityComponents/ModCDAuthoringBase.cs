using UnityEngine;

namespace CoreLib
{
    public class ModCDAuthoringBase : MonoBehaviour
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
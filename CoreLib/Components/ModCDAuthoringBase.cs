using UnityEngine;

namespace CoreLib.Components
{
    public class ModCDAuthoringBase : MonoBehaviour
    {
        protected bool allocated;
    
        public ModCDAuthoringBase(System.IntPtr ptr) : base(ptr) { }
    
        public virtual void Awake()
        {
            Allocate();
        }
    
        public virtual bool Allocate()
        {
            if (allocated) return false;
            allocated = true;
            return true;
        }

        public virtual bool Apply(EntityMonoBehaviourData data)
        {
            return true;
        }

    }
}
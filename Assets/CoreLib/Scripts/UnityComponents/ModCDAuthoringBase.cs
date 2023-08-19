using UnityEngine;

namespace CoreLib.Components
{
    public class ModCDAuthoringBase : MonoBehaviour
    {
        public virtual bool Apply(MonoBehaviour data)
        {
            return default(bool);
        }
    }
}
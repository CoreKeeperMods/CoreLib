using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    public class ModCDAuthoringBase : MonoBehaviour
    {
        public virtual bool Apply(MonoBehaviour data)
        {
            return default(bool);
        }
    }
}
using System;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity
{
    public class EntityPrefabOverride : MonoBehaviour
    {
        public Il2CppValueField<ObjectID> sourceEntity;
        
        public EntityPrefabOverride(IntPtr ptr) : base(ptr) { }
    }
}


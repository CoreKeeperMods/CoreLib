using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CoreLib.Submodules.ModResources;

public class ObjectRetainer : MonoBehaviour
{
    public Il2CppReferenceField<List<Il2CppSystem.Object>> loadedObjects;
    private GCHandle loadedObjectsHandle;
    
    public ObjectRetainer(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        List<Il2CppSystem.Object> list = new List<Il2CppSystem.Object>();
        loadedObjectsHandle = GCHandle.Alloc(list, GCHandleType.Normal);
        loadedObjects.Set(list);
    }

    private void OnDestroy()
    {
        loadedObjectsHandle.Free();
    }


    public void Add(Il2CppSystem.Object o)
    {
        loadedObjects.Value.Add(o);
    }
}


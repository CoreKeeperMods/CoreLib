using System.Runtime.InteropServices;
using Il2CppSystem;

namespace CoreLib;

public struct GCHandleObject<T>
where T : Object
{
    public T obj;
    public GCHandle handle;

    public GCHandleObject(T obj) : this()
    {
        this.obj = obj;
        handle = GCHandle.Alloc(obj);
    }
    
    public static implicit operator GCHandleObject<T>(T o) => new GCHandleObject<T>(o);
    public static implicit operator T(GCHandleObject<T> o) => o.obj;
}
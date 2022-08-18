using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib;

namespace CoreLib.Util.Extensions;

public static class CollectionUtils
{
    public static int FindIndex<T>(this List<T> list, System.Predicate<T> predicate)
    {
        return list.FindIndex(DelegateSupport.ConvertDelegate<Predicate<T>>(predicate));
    }

    public static int RemoveAll<T>(this List<T> list, System.Predicate<T> predicate)
    {
        return list.RemoveAll(DelegateSupport.ConvertDelegate<Predicate<T>>(predicate));
    }

    public static bool TrueForAll<T>(this List<T> list, System.Predicate<T> predicate)
    {
        return list.TrueForAll(DelegateSupport.ConvertDelegate<Predicate<T>>(predicate));
    }

    public static bool Exists<T>(this List<T> list, System.Predicate<T> predicate)
    {
        return list.Exists(DelegateSupport.ConvertDelegate<Predicate<T>>(predicate));
    }

    public static T Find<T>(this List<T> list, System.Predicate<T> predicate)
    {
        return list.Find(DelegateSupport.ConvertDelegate<Predicate<T>>(predicate));
    }

}
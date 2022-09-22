using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;

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

    public static List<T> ToIl2CppList<T>(this System.Collections.Generic.IEnumerable<T> enumerable)
    {
        System.Collections.Generic.ICollection<T> collection;   
        
        if (enumerable is System.Collections.Generic.ICollection<T> objs)
        {
            collection = objs;
        }
        else
        {
            collection = enumerable.ToArray();
        }
        
        int count = collection.Count;
        List<T> list = new List<T>(count);
        if (count == 0)
            return list;
        foreach (T obj in collection)
        {
            list.Add(obj);
        }
        return list;
    }

    public static void AddDelegate<TKey, TDel>(this System.Collections.Generic.Dictionary<TKey, TDel> entityModifyFunctions, TKey key, TDel modifyDelegate )
    where TDel : System.Delegate
    {
        if (entityModifyFunctions.ContainsKey(key))
        {
            entityModifyFunctions[key] = (TDel)System.Delegate.Combine(entityModifyFunctions[key], modifyDelegate);
        }
        else
        {
            entityModifyFunctions.Add(key, modifyDelegate);
        }
    }

}
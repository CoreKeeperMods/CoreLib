
namespace CoreLib.Util.Extensions
{
    public static class CollectionUtils
    {

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
}
namespace CoreLib.Util.Extensions
{
    /// Provides utility methods for working with collections.
    public static class CollectionUtils
    {
        /// Creates or updates a delegate entry in the dictionary by combining it with the existing delegate if one exists.
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TDel">The type of delegates stored in the dictionary, constrained to be a delegate type.</typeparam>
        /// <param name="entityModifyFunctions">The dictionary to which the delegate will be added or updated.</param>
        /// <param name="key">The key associated with the delegate to be added or updated.</param>
        /// <param name="modifyDelegate">The delegate to be added or combined with an existing delegate at the specified key.</param>
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
// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: CollectionUtils.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides extension methods for working with collection objects,
//              including safe delegate combination and dictionary manipulation utilities.
// ========================================================

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extension
{
    /// Provides extension methods for enhancing and simplifying operations on collection types.
    /// <remarks>
    /// The <see cref="CollectionUtils"/> class focuses on extending the functionality
    /// of built-in collection types (e.g., <see cref="Dictionary{TKey,TValue}"/>)
    /// with safe, modular, and reusable operations — particularly around delegate management.
    /// </remarks>
    /// <seealso cref="Delegate"/>
    /// <seealso cref="Dictionary{TKey,TValue}"/>
    public static class CollectionUtils
    {
        #region Delegate Handling

        /// Adds or updates a delegate entry in a dictionary, combining it with an existing delegate if one already exists.
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TDel">The type of delegates stored in the dictionary. Must inherit from <see cref="Delegate"/>.</typeparam>
        /// <param name="entityModifyFunctions">The dictionary containing delegate entries, indexed by <typeparamref name="TKey"/>.</param>
        /// <param name="key">The key associated with the delegate entry to add or update.</param>
        /// <param name="modifyDelegate">The delegate to add or merge into the existing one at the given key.</param>
        /// <remarks>
        /// This method ensures that if a delegate already exists for a given key, the new delegate is combined
        /// using <see cref="Delegate.Combine(Delegate, Delegate)"/> rather than replaced outright.
        /// This allows multiple handlers or callbacks to be registered for the same key without loss of functionality.
        /// </remarks>
        /// <example>
        /// <code>
        /// var handlers = new Dictionary&lt;string, Action&gt;();
        /// handlers.AddDelegate("onLoad", () => Console.WriteLine("Loaded!"));
        /// handlers.AddDelegate("onLoad", () => Console.WriteLine("Again!"));
        /// // The "onLoad" key now references a combined delegate that executes both handlers.
        /// </code>
        /// </example>
        /// <seealso cref="Delegate.Combine(Delegate, Delegate)"/>
        public static void AddDelegate<TKey, TDel>(
            this Dictionary<TKey, TDel> entityModifyFunctions,
            TKey key,
            TDel modifyDelegate)
            where TDel : Delegate
        {
            if (!entityModifyFunctions.TryAdd(key, modifyDelegate))
            {
                entityModifyFunctions[key] = (TDel)Delegate.Combine(entityModifyFunctions[key], modifyDelegate);
            }
        }

        #endregion
    }
}
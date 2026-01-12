// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: TopologicalSortExtension.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides extension methods for performing topological sorting operations
//              on generic collections while detecting and handling cyclic dependencies.
// ========================================================

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extension
{
    /// Provides extension methods for performing topological sorting on collections of elements
    /// and resolving dependency relationships.
    /// <remarks>
    /// These extensions help organize elements in dependency order and can safely handle circular
    /// references using a custom handler.
    /// </remarks>
    /// <seealso cref="CyclicDependencyException"/>
    public static class TopologicalSortExtension
    {
        #region Nested Types

        /// Represents an exception thrown when a cyclic dependency is detected during
        /// a topological sort operation.
        /// <remarks>
        /// A cyclic dependency occurs when an element indirectly depends on itself through a
        /// chain of dependencies. This prevents a valid topological order from being established.
        /// </remarks>
        /// <seealso cref="TopologicalSort{T}(IEnumerable{T}, Func{T, IEnumerable{T}}, Action{T, T})"/>
        public class CyclicDependencyException : Exception
        {
                /// Initializes a new instance of the <see cref="CyclicDependencyException"/> class
            /// with a default message indicating a cyclic dependency was detected.
                public CyclicDependencyException()
                : base("Cyclic dependency found.")
            {
            }
        }

        #endregion

        #region Public Methods

        /// Performs a topological sort on a collection of elements based on their dependencies.
        /// <typeparam name="T">The type of elements being sorted.</typeparam>
        /// <param name="source">The collection of elements to sort.</param>
        /// <param name="dependencies">
        /// A function that returns the dependencies of a given element.
        /// </param>
        /// <param name="circularDepHandler">
        /// An action invoked when a circular dependency is detected, providing the elements involved.
        /// </param>
        /// <returns>
        /// An enumerable collection of elements sorted in topological order.
        /// </returns>
        /// <remarks>
        /// This method recursively visits each element in the dependency graph and builds a list
        /// ordered by dependency resolution. When a circular dependency is found, the handler is called.
        /// </remarks>
        /// <example>
        /// <code>
        /// var sorted = items.TopologicalSort(
        ///     item => item.Dependencies,
        ///     (a, b) => Debug.LogWarning($"Circular dependency between {a} and {b}")
        /// );
        /// </code>
        /// </example>
        /// <seealso cref="GetDependants{T}(T, Func{T, IEnumerable{T}}, Action{T, T})"/>
        public static IEnumerable<T> TopologicalSort<T>(
            this IEnumerable<T> source,
            Func<T, IEnumerable<T>> dependencies,
            Action<T, T> circularDepHandler)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            foreach (var item in source)
                Visit(item, visited, sorted, dependencies, circularDepHandler);

            return sorted;
        }

        /// Retrieves a collection of dependants for a given source element, performing a topological sort.
        /// <typeparam name="T">The type of elements in the dependency graph.</typeparam>
        /// <param name="source">The initial element whose dependants are to be determined.</param>
        /// <param name="dependencies">A function that returns the dependencies for a given element.</param>
        /// <param name="circularDepHandler">
        /// An action invoked when a circular dependency is detected between two elements.
        /// </param>
        /// <returns>
        /// A collection of elements in topological order, beginning from the given source.
        /// </returns>
        /// <seealso cref="TopologicalSort{T}(IEnumerable{T}, Func{T, IEnumerable{T}}, Action{T, T})"/>
        public static IEnumerable<T> GetDependants<T>(
            this T source,
            Func<T, IEnumerable<T>> dependencies,
            Action<T, T> circularDepHandler)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            Visit(source, visited, sorted, dependencies, circularDepHandler);
            return sorted;
        }

        #endregion

        #region Private Helpers

        /// Recursively visits an element in the dependency graph, marking it as visited and
        /// adding it to the sorted output list after processing its dependencies.
        /// <typeparam name="T">The type of elements being processed.</typeparam>
        /// <param name="item">The current element being visited.</param>
        /// <param name="visited">
        /// A set of already visited elements to prevent redundant processing and detect cycles.
        /// </param>
        /// <param name="sorted">The list collecting the elements in dependency order.</param>
        /// <param name="dependencies">A function that retrieves an element’s dependencies.</param>
        /// <param name="circularDepHandler">
        /// An action invoked when a cyclic dependency is detected.
        /// </param>
        /// <exception cref="CyclicDependencyException">
        /// Thrown when a circular reference is detected during traversal.
        /// </exception>
        private static void Visit<T>(
            T item,
            HashSet<T> visited,
            List<T> sorted,
            Func<T, IEnumerable<T>> dependencies,
            Action<T, T> circularDepHandler)
        {
            if (visited.Add(item))
            {
                T lastDep = default;

                try
                {
                    foreach (var dep in dependencies(item))
                    {
                        lastDep = dep;
                        Visit(dep, visited, sorted, dependencies, circularDepHandler);
                    }

                    sorted.Add(item);
                }
                catch (CyclicDependencyException)
                {
                    circularDepHandler(item, lastDep);
                }
            }
            else if (!sorted.Contains(item))
            {
                throw new CyclicDependencyException();
            }
        }

        #endregion
    }
}
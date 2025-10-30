using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Provides extension methods for performing topological sorting operations on collections of elements and resolving dependencies.
    /// </summary>
    /// <remarks>
    /// This class contains static methods designed to facilitate the organization of elements based on their dependencies.
    /// It can address scenarios involving circular dependencies using custom handlers.
    /// </remarks>
    public static class TopologicalSortExtension
    {
        /// <summary>
        /// Represents an exception that is thrown when a cyclic dependency is detected.
        /// </summary>
        /// <remarks>
        /// This exception is typically used in scenarios where dependencies among elements need to be resolved and a cyclic dependency is encountered.
        /// A common use case is during a topological sort operation, where a cycle in the dependencies prevents a valid sorting order.
        /// </remarks>
        public class CyclicDependencyException : Exception
        {
            /// Represents an exception that is thrown when a cyclic dependency is detected while performing a topological sort or handling dependencies.
            public CyclicDependencyException() : base("Cyclic dependency found")
            {
            }
        }

        /// Performs a topological sort on a collection of elements based on their dependencies, and provides a mechanism to handle circular dependencies if detected.
        /// <typeparam name="T">The type of the elements in the collection to be sorted.</typeparam>
        /// <param name="source">The collection of elements on which the topological sort is to be performed.</param>
        /// <param name="dependencies">A function that returns the dependencies for a given element in the collection.</param>
        /// <param name="circularDepHandler">An action that handles circular dependencies, given the pair of elements causing the circular reference.</param>
        /// <return>Returns an enumerable collection of elements sorted in topological order.</return>
        public static IEnumerable<T> TopologicalSort<T>( this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, Action<T, T> circularDepHandler)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            foreach( var item in source )
                Visit( item, visited, sorted, dependencies, circularDepHandler);

            return sorted;
        }

        /// Retrieves a collection of dependants for a given source element based on its dependencies, while also providing a mechanism to handle circular dependencies.
        /// <typeparam name="T">The type of the elements in the dependency graph.</typeparam>
        /// <param name="source">The initial element whose dependants are to be determined.</param>
        /// <param name="dependencies">A function that specifies the dependencies of each element in the graph.</param>
        /// <param name="circularDepHandler">An action that handles circular dependencies, given the pair of elements causing the circular reference.</param>
        /// <return>Returns an enumerable collection of elements in the dependency graph sorted in a topological order, starting from the given source element.</return>
        public static IEnumerable<T> GetDependants<T>( this T source, Func<T, IEnumerable<T>> dependencies, Action<T, T> circularDepHandler)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            Visit( source, visited, sorted, dependencies, circularDepHandler);

            return sorted;
        }


        /// Visits a specific item in the dependency graph, marking it as visited and adding it to the sorted list while handling dependencies and circular references.
        /// <typeparam name="T">The type of the elements in the dependency graph.</typeparam>
        /// <param name="item">The current element being visited in the dependency graph.</param>
        /// <param name="visited">A set of elements that have already been visited to ensure no duplication.</param>
        /// <param name="sorted">A list of elements that will store the sorted order of the graph.</param>
        /// <param name="dependencies">A function that specifies the dependencies of each element in the graph.</param>
        /// <param name="circularDepHandler">An action that handles circular dependencies, given the pair of elements causing the circular reference.</param>
        private static void Visit<T>(T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies , Action<T, T> circularDepHandler)
        {
            if( visited.Add( item ) )
            {
                T lastDep = default;
                
                try
                {
                    foreach (var dep in dependencies(item))
                    {
                        lastDep = dep;
                        Visit(dep, visited, sorted, dependencies, circularDepHandler);
                    }

                    sorted.Add( item );
                }
                catch (CyclicDependencyException)
                {
                    circularDepHandler(item, lastDep);
                }
            }
            else
            {
                if( !sorted.Contains( item ) )
                    throw new CyclicDependencyException();
            }
        }
    }
}
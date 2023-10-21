using System;
using System.Collections.Generic;

namespace CoreLib.Util.Extensions
{
    public static class TopologicalSortExtension
    {
        public class CyclicDependencyException : Exception
        {
            public CyclicDependencyException() : base("Cyclic dependency found")
            {
            }
        }

        public static IEnumerable<T> TopologicalSort<T>( this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, Action<T, T> circularDepHandler)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            foreach( var item in source )
                Visit( item, visited, sorted, dependencies, circularDepHandler);

            return sorted;
        }
        
        public static IEnumerable<T> GetDependants<T>( this T source, Func<T, IEnumerable<T>> dependencies, Action<T, T> circularDepHandler)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            Visit( source, visited, sorted, dependencies, circularDepHandler);

            return sorted;
        }


        private static void Visit<T>(T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies , Action<T, T> circularDepHandler)
        {
            if( !visited.Contains( item ) )
            {
                visited.Add( item );

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
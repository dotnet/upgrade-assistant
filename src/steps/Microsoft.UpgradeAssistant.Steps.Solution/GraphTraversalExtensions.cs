using System;
using System.Collections.Generic;

namespace Microsoft.UpgradeAssistant.Steps.Solution
{
    internal static class GraphTraversalExtensions
    {
        public static IEnumerable<T> PostOrderTraversal<T>(this T initial, Func<T, IEnumerable<T>> selector, IEqualityComparer<T>? comparer = null)
            => new[] { initial }.PostOrderTraversal(selector, comparer);

        public static IEnumerable<T> PostOrderTraversal<T>(this IEnumerable<T> initial, Func<T, IEnumerable<T>> selector, IEqualityComparer<T>? comparer = null)
        {
            var result = new List<T>();
            var visited = new HashSet<T>(comparer);

            void Visit(T item)
            {
                if (!visited.Add(item))
                {
                    return;
                }

                foreach (var inner in selector(item))
                {
                    Visit(inner);
                }

                result.Add(item);
            }

            foreach (var item in initial)
            {
                Visit(item);
            }

            return result;
        }
    }
}

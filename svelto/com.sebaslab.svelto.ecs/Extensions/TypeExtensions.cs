using System;
using System.Collections.Generic;
using System.Linq;

namespace Svelto.ECS.Extensions
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Used to create a deterministic id from an enumerable of types reguardless
        /// of the order the types are recieved.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public static int SortAndHash(this IEnumerable<Type> types)
        {
            // Sort the types
            Type[] sorted = types.OrderBy(x => x.AssemblyQualifiedName).ThenBy(x => x.GetHashCode()).ToArray();

            // Generate unique id from sorted types
            int id = 0;
            for (var i = 0; i < sorted.Length; i++)
            {
                id = HashCode.Combine(sorted[i], id);
            }

            return id;
        }
    }
}

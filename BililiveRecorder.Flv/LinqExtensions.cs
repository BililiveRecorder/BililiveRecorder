using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BililiveRecorder.Flv
{
    public static class LinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any2<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> predicate)
        {
            using var iterator = source.GetEnumerator();

            if (!iterator.MoveNext())
                return false;

            var lastItem = iterator.Current;

            while (iterator.MoveNext())
            {
                var current = iterator.Current;

                if (predicate(lastItem, current))
                    return true;

                lastItem = current;
            }
            return false;
        }
    }
}

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

            var previousItem = iterator.Current;

            while (iterator.MoveNext())
            {
                var currentItem = iterator.Current;

                if (predicate(previousItem, currentItem))
                    return true;

                previousItem = currentItem;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any2<TIn, TFunction>(this IEnumerable<TIn> source, ref TFunction function)
            where TFunction : ITwoInputFunction<TIn, bool>
        {
            using var iterator = source.GetEnumerator();

            if (!iterator.MoveNext())
                return false;

            var previousItem = iterator.Current;

            while (iterator.MoveNext())
            {
                var currentItem = iterator.Current;

                if (function.Eval(previousItem, currentItem))
                    return true;

                previousItem = currentItem;
            }
            return false;
        }
    }

    public interface ITwoInputFunction<in TIn, out TOut>
    {
        TOut Eval(TIn a, TIn b);
    }
}

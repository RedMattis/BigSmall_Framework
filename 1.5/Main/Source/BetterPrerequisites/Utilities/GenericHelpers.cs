using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class ListHelpers
    {
        public static List<T> IntersectNullableLists<T>(this List<T> list1, List<T> list2) =>
            (list1 != null && list2 != null) ? list1.Intersect(list2).ToList() : list1 ?? list2;

        public static List<T> UnionNullableLists<T>(this List<T> list1, List<T> list2) =>
            (list1 != null && list2 != null) ? list1.Union(list2).ToList() : list1 ?? list2;

        // Runs Where and Select in sequence.
        //public static IEnumerable<TResult> SelectWhere<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        //{
        //    foreach (var item in source.Where(x=>x is TResult))
        //    {
        //        yield return selector(item);
        //    }
        //}
        //public static IEnumerable<TResult> SelectManyWhere<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        //{
        //    foreach (var item in source.Where(x => x is TResult))
        //    {
        //        foreach (var subItem in selector(item))
        //        {
        //            yield return subItem;
        //        }
        //    }
        //}
    }

    public static class MathHelpers
    {
        public static bool ApproximatelyEquals(this float f1, float f2, float tolerance = 0.01f)
        {
            return Math.Abs(f1 - f2) < tolerance;
        }
        public static bool Approx(this float f1, float f2, float tolerance = 0.01f)
        {
            return ApproximatelyEquals(f1, f2, tolerance);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

static class AmteUtils
{
    public static void Foreach<T>(this IEnumerable<T> self, Action<T> function)
    {
        foreach (var e in self)
            function(e);
    }

    public static T Clamp<T>(this T input, T min, T max) where T : IComparable<T>
    {
        T val = input;

        if (input.CompareTo(min) < 0)
            val = min;
        if (input.CompareTo(max) > 0)
            val = max;
        return val;
    }

    /// <summary>
    /// In Minutes
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static long ToTimerMilliseconds(this long time)
    {
        long val = time * 60 * 1000;

        if (val > 0 && val < long.MaxValue)
        {
            return val;
        }

        return 0;
    }
}
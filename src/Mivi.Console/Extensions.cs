using System.Collections.Generic;
using System.Linq;

namespace Mivi.Console
{
    public static class Extensions
    {
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> source)
            => source
                .Select((a, i) => (a, i));
    }
}

using System.Collections.Generic;
using System.Linq;

namespace NHWebConsole.Utils {
    public static class EnumerableExtensions {
        public static string Join(this IEnumerable<string> s, string separator) {
            return string.Join(separator, s.ToArray());
        }
    }
}
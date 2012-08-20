using System;

namespace NHWebConsole.Tests {
    internal static class L {
        public static Action<T> A<T>(Action<T> f) {
            return f;
        }

        public static Action<T1, T2> A<T1, T2>(Action<T1, T2> f) {
            return f;
        }
    }
}
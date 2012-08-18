using System.Reflection;
using Fuchu;

namespace NHWebConsole.Tests {
    public static class Runner {
        public static int Main(string[] args) {
            return Test.List(new[] {Tests.AllTests, IntellisenseTests.AllTests}).Run();
        }
    }
}
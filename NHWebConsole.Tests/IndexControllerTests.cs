using System;
using MiniMVC;
using NUnit.Framework;

namespace NHWebConsole.Tests {
    [TestFixture]
    public class IndexControllerTests {
        [Test]
        public void NoEmptyScripts() {
            var x = X.E("script", X.A("src", "http://ajax.googleapis.com/ajax/libs/jquery/1.4.3/jquery.min.js"));
            var n = IndexController.NoEmptyScripts(x);
            Console.WriteLine(n.ToString());
            StringAssert.DoesNotContain("/>", n.ToString());
        }
    }
}
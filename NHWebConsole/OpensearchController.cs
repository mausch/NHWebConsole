using System;
using System.Linq;
using System.Web;
using MiniMVC;
using NHWebConsole.Utils;

namespace NHWebConsole {
    public class OpensearchController : Controller {
        public override void Execute(HttpContextBase context) {
            var url = context.Request.Url.ToString();
            url = url.Split('/').Reverse().Skip(1).Reverse().Join("/");
            var v = Views.Views.OpenSearch(url);
            context.XDocument(v.MakeHTML5Doc(), "application/opensearchdescription+xml");
        }
    }
}
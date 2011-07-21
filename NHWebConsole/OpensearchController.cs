using System;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using MiniMVC;
using NHWebConsole.Utils;

namespace NHWebConsole {
    public class OpensearchController : Controller {
        public override IResult Execute(HttpContextBase context) {
            var url = context.Request.Url.ToString();
            url = url.Split('/').Reverse().Skip(1).Reverse().Join("/");
            var v = Views.Views.OpenSearch(url);
            return new XDocResult(new XDocument(X.XHTML1_0_Transitional, v)) {
                ContentType = "application/opensearchdescription+xml"
            };
        }
    }
}
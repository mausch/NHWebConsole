using System;
using System.Web;

namespace MiniMVC {
    public class RedirectResult : IResult {
        private readonly string url;

        public RedirectResult(string url) {
            this.url = url;
        }

        public void Execute(HttpContextBase context) {
            context.Response.Redirect(url);
        }
    }
}
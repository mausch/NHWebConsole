using System.Web;

namespace MiniMVC {
    public class HttpContextStub : HttpContextWrapper {
        private readonly HttpResponseStub response = new HttpResponseStub();

        public HttpContextStub(HttpContext httpContext) : base(httpContext) {}

        public override HttpResponseBase Response {
            get { return response; }
        }

        public string ResponseAsString {
            get { return response.ToString();  }
        }
    }
}
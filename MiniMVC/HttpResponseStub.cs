using System.Text;
using System.Web;

namespace MiniMVC {
    public class HttpResponseStub : HttpResponseBase {
        private readonly StringBuilder sb = new StringBuilder();

        public override void Write(char ch) {
            sb.Append(ch);
        }

        public override void Write(object obj) {
            sb.Append(obj);
        }

        public override void Write(string s) {
            sb.Append(s);
        }

        public override string ToString() {
            return sb.ToString();
        }
    }
}
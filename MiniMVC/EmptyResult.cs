using System.Web;

namespace MiniMVC {
    public class EmptyResult : IResult {
        public void Execute(HttpContextBase context) {}
    }
}
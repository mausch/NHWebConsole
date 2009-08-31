using System.Web;

namespace NHWebConsole {
    public interface IController {
        object Execute(HttpContextBase context);
    }
}
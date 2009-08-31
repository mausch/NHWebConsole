using System;
using System.Web;
using NHibernate;

namespace NHWebConsole {
    public abstract class NHController : Controller {
        public ISession Session { get; set;}

        public override void ProcessRequest(HttpContext context) {
            using (Session = NHibernateFunctions.OpenSession()) {
                Session.FlushMode = FlushMode.Never;
                base.ProcessRequest(context);
            }
        }
    }
}
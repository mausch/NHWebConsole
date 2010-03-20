using System.Web;
using Commons.Collections;
using NVelocity.App;
using NVelocity.Runtime;

namespace MiniMVC {
    public class ExternalVelocityEngine : VelocityEngine {
        public ExternalVelocityEngine() {
            var props = new ExtendedProperties();
            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "file");
            var path = HttpContext.Current.Server.MapPath("~/Resources");
            props.SetProperty(RuntimeConstants.FILE_RESOURCE_LOADER_PATH, path);
            Init(props);
        }
    }
}
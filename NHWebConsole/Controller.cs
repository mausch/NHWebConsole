using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using Commons.Collections;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;

namespace NHWebConsole {
    public abstract class Controller : IController, IHttpHandler {
        public abstract object Execute(HttpContextBase context);

        public virtual void ProcessRequest(HttpContext context) {
            var contextWrapper = new HttpContextWrapper(context);
            var result = Execute(contextWrapper);
            context.Response.Write(FillResponseTemplate(contextWrapper, result));
        }

        protected readonly IDictionary<string, object> ViewParams = new Dictionary<string, object>();

        protected virtual object FillResponseTemplate(HttpContextBase context, object result) {
            var vcontext = new VelocityContext();
            vcontext.Put("model", result);
            foreach (var h in ViewParams)
                vcontext.Put(h.Key, h.Value);
            using (var writer = new StringWriter()) {
                TemplateEngine.GetTemplate(ViewName).Merge(vcontext, writer);
                return writer.GetStringBuilder().ToString();
            }
        }

        private static readonly VelocityEngine TemplateEngine;

        public virtual string ViewName {
            get { return string.Format("{0}.Resources.{1}.html", GetType().Assembly.FullName.Split(',')[0], ControllerName); }
            set {}
        }

        public string ControllerName {
            get { return Regex.Replace(GetType().Name, "Controller$", ""); }
        }

        static Controller() {
            var engine = new VelocityEngine();
            var props = new ExtendedProperties();
            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "assembly");
            props.AddProperty("assembly.resource.loader.class",
                              "NVelocity.Runtime.Resource.Loader.AssemblyResourceLoader, NVelocity");
            props.AddProperty("assembly.resource.loader.assembly", Assembly.GetExecutingAssembly().FullName.Split(',')[0]);
            engine.Init(props);
            TemplateEngine = engine;
        }

        public bool IsReusable {
            get { return false; }
        }
    }
}
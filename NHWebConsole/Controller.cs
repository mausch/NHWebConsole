#region license
// Copyright (c) 2009 Mauricio Scheffer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using Commons.Collections;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;
using NVelocity.Runtime.Directive;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;

namespace NHWebConsole {
    public abstract class Controller : IController, IHttpHandler {
        public abstract object Execute(HttpContext context);

        public virtual void ProcessRequest(HttpContext context) {
            var result = Execute(context);
            context.Response.Write(FillResponseTemplate(context, result));
        }

        protected readonly IDictionary<string, object> ViewParams = new Dictionary<string, object>();

        protected virtual object FillResponseTemplate(HttpContext context, object result) {
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
            props.AddProperty("directive.manager", typeof(NVDirectiveManager).AssemblyQualifiedName);
            props.AddProperty(RuntimeConstants.RESOURCE_MANAGER_CLASS, typeof(ResourceManagerImpl).AssemblyQualifiedName);
            props.AddProperty("assembly.resource.loader.class", typeof (AssemblyResourceLoader).AssemblyQualifiedName);
            props.AddProperty("assembly.resource.loader.assembly", Assembly.GetExecutingAssembly().FullName.Split(',')[0]);
            var directives = new[] {
                typeof (Foreach),
                typeof(Include),
                typeof(Parse),
                typeof(Macro),
                typeof(Literal),
            };
            foreach (var i in Enumerable.Range(0, directives.Length))
                props.AddProperty("directive." + i, directives[i].AssemblyQualifiedName);
            engine.Init(props);
            TemplateEngine = engine;
        }

        public bool IsReusable {
            get { return false; }
        }
    }
}
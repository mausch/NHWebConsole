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
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Web;
using Commons.Collections;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;
using NVelocity.Runtime.Directive;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;

namespace MiniMVC {
    public class NVHelper {
        public string XmlEncode(string s) {
            return SecurityElement.Escape(s);
        }

        public string HtmlEncode(string s) {
            return HttpUtility.HtmlEncode(s);
        }

        public DateTime Now() {
            return DateTime.Now;
        }

        private static readonly VelocityEngine TemplateEngine;

        static NVHelper() {
            var engine = new VelocityEngine();
            var props = new ExtendedProperties();
            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "assembly");
            props.AddProperty("directive.manager", typeof(NVDirectiveManager).AssemblyQualifiedName);
            props.AddProperty(RuntimeConstants.RESOURCE_MANAGER_CLASS, typeof(ResourceManagerImpl).AssemblyQualifiedName);
            props.AddProperty("assembly.resource.loader.class", typeof(AssemblyResourceLoader).AssemblyQualifiedName);
            props.AddProperty("assembly.resource.loader.assembly", Assembly.GetExecutingAssembly().FullName.Split(',')[0]);
            var directives = new[] {
                typeof (Foreach),
                typeof (Include),
                typeof (Parse),
                typeof (Macro),
                typeof (Literal),
            };
            foreach (var i in Enumerable.Range(0, directives.Length))
                props.AddProperty("directive." + i, directives[i].AssemblyQualifiedName);
            engine.Init(props);
            TemplateEngine = engine;
        }

        public string Render(string template, IDictionary parameters) {
            var context = new VelocityContext();
            if (parameters != null) {
                foreach (string k in parameters.Keys) {
                    context.Put(k, parameters[k]);
                }
            }
            using (var writer = new StringWriter()) {
                TemplateEngine.Evaluate(context, writer, "", template);
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}
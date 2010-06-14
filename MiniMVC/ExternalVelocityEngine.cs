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

using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using Commons.Collections;
using NVelocity.App;
using NVelocity.Runtime;
using NVelocity.Runtime.Directive;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;

namespace MiniMVC {
    public class ExternalVelocityEngine : VelocityEngine {
        public ExternalVelocityEngine(string resourcePath) {
            var props = new ExtendedProperties();
            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "file");
            props.AddProperty("directive.manager", typeof(NVDirectiveManager).AssemblyQualifiedName);
            props.AddProperty(RuntimeConstants.RESOURCE_MANAGER_CLASS, typeof(ResourceManagerImpl).AssemblyQualifiedName);
            props.AddProperty("file.resource.loader.class", typeof(FileResourceLoader).AssemblyQualifiedName);
            props.AddProperty("file.resource.loader.assembly", Assembly.GetExecutingAssembly().FullName.Split(',')[0]);
            var directives = new[] {
                typeof (Foreach),
                typeof (Include),
                typeof (Parse),
                typeof (Macro),
                typeof (Literal),
            };
            foreach (var i in Enumerable.Range(0, directives.Length))
                props.AddProperty("directive." + i, directives[i].AssemblyQualifiedName);
            props.SetProperty(RuntimeConstants.FILE_RESOURCE_LOADER_PATH, resourcePath);
            Init(props);
        }

        public ExternalVelocityEngine() : this(HostingEnvironment.MapPath("~/Resources")) {}
    }
}
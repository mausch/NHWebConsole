using System.Linq;
using System.Reflection;
using Commons.Collections;
using NVelocity.App;
using NVelocity.Runtime;
using NVelocity.Runtime.Directive;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;

namespace MiniMVC {
    public class EmbeddedVelocityEngine : VelocityEngine {
        public EmbeddedVelocityEngine() {
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
            Init(props);
        }
    }
}
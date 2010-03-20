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

using System.IO;
using System.Web;
using NVelocity;
using NVelocity.App;

namespace MiniMVC {
    public class ViewResult : IResult {
        private readonly object model;
        private readonly string name;
        public string ContentType { get; set; }

        public ViewResult(object model, string name) {
            this.model = model;
            this.name = name;
        }

        public void Execute(HttpContext context) {
            if (ContentType != null)
                context.Response.ContentType = ContentType;
            var vcontext = new VelocityContext();
            vcontext.Put("model", model);
            vcontext.Put("helper", new NVHelper());
            using (var writer = new StringWriter()) {
                TemplateEngine.GetTemplate(name).Merge(vcontext, writer);
                context.Response.Write(writer.GetStringBuilder().ToString());
            }
        }

        private static readonly VelocityEngine TemplateEngine = new ExternalVelocityEngine();
    }
}
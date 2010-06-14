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
using System.Security;
using System.Web;
using NVelocity;
using NVelocity.Context;

namespace MiniMVC {
    public class NVHelper {
        public string XmlEncode(string s) {
            return SecurityElement.Escape(s);
        }

        public string HtmlEncode(string s) {
            return HttpUtility.HtmlEncode(s);
        }

        public string UrlEncode(string s) {
            return HttpUtility.UrlEncode(s);
        }

        public DateTime Now() {
            return DateTime.Now;
        }

        public string Nbsp(string s) {
            return s == null ? null : s.Replace(" ", "&nbsp;");
        }

        public string YesNo(bool b) {
            return b ? "Yes" : "No";
        }

        public string RenderTemplate(string template, IDictionary parameters) {
            var context = BuildContext(parameters);
            using (var writer = new StringWriter()) {
                Setup.TemplateEngine().GetTemplate(template).Merge(context, writer);
                return writer.GetStringBuilder().ToString();
            }            
        }

        public string Render(string template, IDictionary parameters) {
            var context = BuildContext(parameters);
            using (var writer = new StringWriter()) {
                Setup.TemplateEngine().Evaluate(context, writer, "", template);
                return writer.GetStringBuilder().ToString();
            }
        }

        private IContext BuildContext(IDictionary parameters) {
            var context = new VelocityContext();
            if (parameters != null) {
                foreach (string k in parameters.Keys) {
                    context.Put(k, parameters[k]);
                }
            }
            return context;
        }
    }
}
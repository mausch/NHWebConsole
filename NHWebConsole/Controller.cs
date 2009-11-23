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

using System.Text.RegularExpressions;
using System.Web;

namespace NHWebConsole {
    /// <summary>
    /// Handles view plumbing
    /// </summary>
    public abstract class Controller : IController, IHttpHandler {
        public abstract IResult Execute(HttpContext context);

        public virtual void ProcessRequest(HttpContext context) {
            var result = Execute(context);
            result.Execute(context);
        }

        public string GetEmbeddedViewName(string name) {
            return string.Format("{0}.Resources.{1}.html", GetType().Assembly.FullName.Split(',')[0], name);
        }

        private string viewName;

        public virtual string ViewName {
            get { return viewName ?? GetEmbeddedViewName(ControllerName); }
            set { viewName = value; }
        }

        public string ControllerName {
            get { return Regex.Replace(GetType().Name, "Controller$", ""); }
        }

        public bool IsReusable {
            get { return false; }
        }
    }
}
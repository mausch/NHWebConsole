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

using System.Web;
using MiniMVC;

namespace NHWebConsole {
    public class StaticController : Controller {
        public override IResult Execute(HttpContextBase context) {
            var resource = context.Request.QueryString["r"];
            var contentType = context.Request.QueryString["t"];
            if (contentType != null)
                context.Response.ContentType = contentType;
            var fullResourceName = string.Format("{0}.Resources.{1}", GetType().Assembly.FullName.Split(',')[0], resource);
            var resourceStream = GetType().Assembly.GetManifestResourceStream(fullResourceName);
            const int size = 32768;
            var buffer = new byte[size];
            var read = 0;
            while ((read = resourceStream.Read(buffer, 0, size)) > 0)
                context.Response.OutputStream.Write(buffer, 0, read);
            return new EmptyResult();
        }

        public class EmptyResult : IResult {
            public void Execute(HttpContextBase context) {}
        }
    }
}
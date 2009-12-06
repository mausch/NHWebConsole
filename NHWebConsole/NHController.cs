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
using System.Web;
using MiniMVC;
using NHibernate;

namespace NHWebConsole {
    /// <summary>
    /// Handles NHibernate session
    /// </summary>
    public abstract class NHController : Controller {
        public ISession Session { get; set;}

        public override void ProcessRequest(HttpContext context) {
            Session = NHWebConsoleSetup.OpenSession();
            try {
                Session.FlushMode = FlushMode.Never;
                base.ProcessRequest(context);                
            } finally {
                if (NHWebConsoleSetup.DisposeSession)
                    Session.Dispose();
            }
        }
    }
}
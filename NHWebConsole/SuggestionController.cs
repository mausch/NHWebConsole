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
using System.Linq;
using System.Web;
using HqlIntellisense;
using MiniMVC;

namespace NHWebConsole {
    public class SuggestionController : NHController, IHQLCompletionRequestor {
        private string error;
        private readonly IList<string> suggestions = new List<string>();

        public override void Execute(HttpContextBase context) {
            var q = context.Request.QueryString["q"];
            var p = int.Parse(context.Request.QueryString["p"]);
            new HQLCodeAssist(new NHConfigDataProvider(NHWebConsoleSetup.Configuration())).CodeComplete(q, p, this);
            if (error != null) {
                context.Raw(error);
                return;
            }
            var sugg = string.Join(",", suggestions.Select(s => string.Format("\"{0}\"", s)).ToArray());
            var json = "{\"suggestions\": [$]}".Replace("$", sugg);
            context.Raw(json);
        }

        public bool accept(HQLCompletionProposal proposal) {
            suggestions.Add(proposal.GetCompletion());
            return true;
        }

        public void completionFailure(string errorMessage) {
            error = errorMessage;
        }
    }
}
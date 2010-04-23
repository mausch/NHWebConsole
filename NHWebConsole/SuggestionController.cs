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

        public override IResult Execute(HttpContext context) {
            var q = context.Request.QueryString["q"];
            var p = int.Parse(context.Request.QueryString["p"]);
            new HQLCodeAssist(new NHConfigDataProvider(NHWebConsoleSetup.Configuration())).CodeComplete(q, p, this);
            return new ViewResult(new SuggestionResponse {
                Error = error,
                Suggestions = string.Format("[{0}]", string.Join(",", suggestions.Select(s => string.Format("'{0}'", s)).ToArray())),
            }, ViewName);
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
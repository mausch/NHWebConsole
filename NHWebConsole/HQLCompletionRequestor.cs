using System;
using System.Collections.Generic;
using HqlIntellisense;

namespace NHWebConsole {
    public class HQLCompletionRequestor : IHQLCompletionRequestor {
        private string error;
        private readonly IList<string> suggestions = new List<string>();

        public string Error {
            get { return error; }
        }

        public IEnumerable<string> Suggestions {
            get { return suggestions; }
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
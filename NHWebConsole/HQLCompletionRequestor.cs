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

        public bool Accept(HQLCompletionProposal proposal) {
            suggestions.Add(proposal.Completion);
            return true;
        }

        public void CompletionFailure(string errorMessage) {
            error = errorMessage;
        }
    }
}
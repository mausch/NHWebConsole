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
using HqlIntellisense;
using NUnit.Framework;
using SampleApp;

namespace NHWebConsole.Tests {
    [TestFixture]
    public class IntellisenseTests {
        private IConfigurationDataProvider cfg;

        [TestFixtureSetUp]
        public void Setup() {
            var nhcfg = Global.BuildNHConfiguration("test.db");
            cfg = new NHConfigDataProvider(nhcfg);
        }

        [Test]
        public void tt() {
            var q = "from ";
            var requestor = new CompletionRequestor();
            new HQLCodeAssist(cfg).CodeComplete(q, q.Length, requestor);
            PrintResult(requestor);
        }

        [Test]
        public void ttt() {
            var q = "from Customer x where ";
            var requestor = new CompletionRequestor();
            new HQLCodeAssist(cfg).CodeComplete(q, q.Length, requestor);
            PrintResult(requestor);            
        }

        [Test]
        public void Error() {
            var q = "from Nada ";
            var requestor = new CompletionRequestor();
            new HQLCodeAssist(cfg).CodeComplete(q, q.Length, requestor);
            PrintResult(requestor);
        }

        [Test]
        public void SelectFromWhere() {
            var q = "select x from SampleModel.Customer x where x.";
            var requestor = new CompletionRequestor();
            new HQLCodeAssist(cfg).CodeComplete(q, q.Length, requestor);
            PrintResult(requestor);
        }

        public void PrintResult(CompletionRequestor r) {
            if (r.Error != null)
                Console.WriteLine("Error: {0}", r.Error);
            if (r.Proposals.Count == 0)
                Console.WriteLine("No proposals");
            foreach (var p in r.Proposals) {
                Console.WriteLine("completion: {0}", p.GetCompletion());
                Console.WriteLine("kind: {0}", p.GetCompletionKind());
                Console.WriteLine("location: {0}", p.GetCompletionLocation());
                Console.WriteLine("relevance: {0}", p.GetRelevance());
                Console.WriteLine("simple name: {0}", p.GetSimpleName());
                Console.WriteLine();
            }
        }

        public class CompletionRequestor : IHQLCompletionRequestor {
            public IList<HQLCompletionProposal> Proposals { get; private set; }
            public string Error { get; private set; }

            public CompletionRequestor() {
                Proposals = new List<HQLCompletionProposal>();
            }

            public bool accept(HQLCompletionProposal proposal) {
                Proposals.Add(proposal);
                return true;
            }

            public void completionFailure(string errorMessage) {
                Error = errorMessage;
            }
        }
    }
}
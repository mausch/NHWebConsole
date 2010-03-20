using System;
using System.Collections.Generic;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using HqlIntellisense;
using NHibernate.Cfg;
using NUnit.Framework;
using SampleModel;

namespace NHWebConsole.Tests {
    [TestFixture]
    public class IntellisenseTests {
        private Configuration cfg;

        [TestFixtureSetUp]
        public void Setup() {
            cfg = Fluently.Configure()
                .Database(SQLiteConfiguration.Standard
                              .ConnectionString("Data Source=test.db;Version=3;New=True;"))
                .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Customer>()))
                .BuildConfiguration();
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

        public void PrintResult(CompletionRequestor r) {
            if (r.Error != null)
                Console.WriteLine(r.Error);
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
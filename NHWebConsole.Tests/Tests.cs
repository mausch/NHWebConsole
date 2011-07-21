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
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using NHWebConsole.Views;
using NHibernate;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using SampleApp;
using SampleModel;
using Environment = NHibernate.Cfg.Environment;

namespace NHWebConsole.Tests {
    [TestFixture]
    public class Tests {
        private ISessionFactory sessionFactory;
        private SingleSessionWrapper session;

        [SetUp]
        public void Setup() {
            var cfg = Global.FluentNHConfig(":memory:")
                .ExposeConfiguration(c => c.SetProperty(Environment.ReleaseConnections, "on_close"))
                .BuildConfiguration();
            sessionFactory = cfg.BuildSessionFactory();
            session = new SingleSessionWrapper(sessionFactory.OpenSession());
            NHWebConsoleSetup.OpenSession = () => session;
            NHWebConsoleSetup.DisposeSession = false;
            NHWebConsoleSetup.Configuration = () => cfg;
            new SchemaExport(cfg).Execute(false, true, false, session.Connection, null);
            Global.CreateSampleData(null);
        }

        [TearDown]
        public void Teardown() {
            //session.Dispose();
            session.Kill();
            sessionFactory.Dispose();
        }

        [Test]
        public void ExecQuery() {
            var c = new IndexController {
                Session = session,
                Cfg = NHWebConsoleSetup.Configuration(),
                RawUrl = "/pepe.aspx",
            };
            var model = new Context {
                Query = "from System.Object",
                QueryType = QueryType.HQL,
                ImageFields = new string[0],
            };
            c.ExecQuery(model);
            Assert.IsNotNull(model.Results);
            Assert.Greater(model.Results.Count, 0);
            foreach (var r in model.Results)
                foreach (var m in r)
                    Console.WriteLine("{0}: {1}", m.Key, m.Value);
        }

        [Test]
        public void ManyToMany() {
            var c = new IndexController {
                Session = session,
                Cfg = NHWebConsoleSetup.Configuration(),
                RawUrl = "/pepe.aspx",
            };
            var link = c.BuildCollectionLink(typeof (Territory), typeof (Employee), 1);
            Console.WriteLine(link);
            Assert.IsNotNull(link);
        }

        [Test]
        public void IsCollectionOf() {
            var types = new[] {
                KV(typeof (IEnumerable<string>), typeof (string)),
                KV(typeof (IEnumerable<int>), typeof (int)),
                KV(typeof (ICollection<int>), typeof (int)),
                KV(typeof (List<int>), typeof (int)),
                KV(typeof (ISet<int>), typeof (int)),
                KV(typeof (HashedSet<int>), typeof (int)),
            };

            foreach (var t in types) {
                Assert.IsTrue(IndexController.IsCollectionOf(t.Key, t.Value), "Expected {0} is collection of {1}", t.Key, t.Value);
            }
        }

        [Test]
        public void IsNotCollectionOf() {
            var types = new[] {
                KV(typeof (IEnumerable<string>), typeof (int)),
                KV(typeof (IEnumerable), typeof (int)),
                KV(typeof (ArrayList), typeof (int)),
                KV(typeof (string), typeof (int)),
                KV(typeof (string), typeof (char)),
            };

            foreach (var t in types) {
                Assert.IsFalse(IndexController.IsCollectionOf(t.Key, t.Value), "Expected {0} is NOT collection of {1}", t.Key, t.Value);
            }
        }

        public KeyValuePair<K, V> KV<K, V>(K key, V value) {
            return new KeyValuePair<K, V>(key, value);
        }

        [Test]
        public void NextPage() {
            var c = new IndexController {
                RawUrl = "/pepe.aspx?hql=from+System.Object&",
            };
            Console.WriteLine(c.BuildNextPageUrl(new Context {
                MaxResults = 10,
                Results = Enumerable.Range(1, 11).Select(i => new Row()).ToList(),
            }));
        }

        [Test]
        public void Component() {
            var c = new IndexController {
                RawUrl = "/pepe?",
            };
            var employee = new Employee {
                Address = new Address {
                    City = "",
                }
            };
            var ctx = new Context {
                ImageFields = new string[0],
            };
            c.ConvertResult(employee, ctx);
        }

        [Test]
        public void NullComponent() {
            var c = new IndexController {
                RawUrl = "/pepe?",
            };
            var employee = new Employee {
                Address = null,
            };
            var ctx = new Context {
                ImageFields = new string[0],
            };
            c.ConvertResult(employee, ctx);
        }

        [Test]
        public void QueryScalarIncludesNamespace() {
            var c = new IndexController();
            var prop = new Property {Name = "FirstName"};
            var query = c.QueryScalar(prop, typeof (Employee), new Employee());
            Assert.AreEqual("select x.FirstName from SampleModel.Employee x where x.Id = '0'", query);
        }
    }
}
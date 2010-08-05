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
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using SampleApp;
using SampleModel;

namespace NHWebConsole.Tests {
    [TestFixture]
    public class Tests {
        [SetUp]
        public void Setup() {
            var cfg = Global.BuildNHConfiguration("test.db");
            var sessionFactory = cfg.BuildSessionFactory();
            NHWebConsoleSetup.OpenSession = () => sessionFactory.OpenSession();
            NHWebConsoleSetup.Configuration = () => cfg;
            new SchemaExport(cfg).Execute(false, true, false);
            Global.CreateSampleData(null);
        }

        [Test]
        public void ExecQuery() {
            using (var session = NHWebConsoleSetup.OpenSession()) {
                var c = new IndexController {
                    Session = session,
                    Cfg = NHWebConsoleSetup.Configuration(),
                    RawUrl = "/pepe.aspx",
                };
                var model = new Context {
                    Query = "from System.Object",
                };
                c.ExecQuery(model);
                Assert.IsNotNull(model.Results);
                Assert.Greater(model.Results.Count, 0);
                foreach (var r in model.Results)
                    foreach (var m in r)
                        Console.WriteLine("{0}: {1}", m.Key, m.Value);
            }
        }

        [Test]
        public void ManyToMany() {
            using (var session = NHWebConsoleSetup.OpenSession()) {
                var c = new IndexController {
                    Session = session,
                    Cfg = NHWebConsoleSetup.Configuration(),
                    RawUrl = "/pepe.aspx",
                };
                var link = c.BuildCollectionLink(typeof (Territory), typeof (Employee), 1);
                Console.WriteLine(link);
                Assert.IsNotNull(link);
            }
        }

        [Test]
        public void IsCollectionOf() {
            var types = new[] {
                KV(typeof(IEnumerable<string>), typeof(string)),
                KV(typeof(IEnumerable<int>), typeof(int)),
                KV(typeof(ICollection<int>), typeof(int)),
                KV(typeof(List<int>), typeof(int)),
                KV(typeof(ISet<int>), typeof(int)),
                KV(typeof(HashedSet<int>), typeof(int)),
            };

            foreach (var t in types) {
                Assert.IsTrue(IndexController.IsCollectionOf(t.Key, t.Value), "Expected {0} is collection of {1}", t.Key, t.Value);
            }
        }

        [Test]
        public void IsNotCollectionOf() {
            var types = new[] {
                KV(typeof(IEnumerable<string>), typeof(int)),
                KV(typeof(IEnumerable), typeof(int)),
                KV(typeof(ArrayList), typeof(int)),
                KV(typeof(string), typeof(int)),
                KV(typeof(string), typeof(char)),
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
    }
}
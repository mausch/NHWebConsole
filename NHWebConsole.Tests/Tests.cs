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
using Fuchu;
using Iesi.Collections.Generic;
using NHibernate.Cfg;
using NHWebConsole.Views;
using NHibernate;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using SampleApp;
using SampleModel;
using Environment = NHibernate.Cfg.Environment;

namespace NHWebConsole.Tests {
    public static class Tests {

        public static readonly Lazy<Configuration> Cfg =
            new Lazy<Configuration>(() => Global.FluentNHConfig(":memory:")
                    .ExposeConfiguration(c => c.SetProperty(Environment.ReleaseConnections, "on_close"))
                    .BuildConfiguration());

        public static readonly Func<Tuple<SingleSessionWrapper, ISessionFactory, Configuration>> Setup =
            () => {
                var sessionFactory = Cfg.Value.BuildSessionFactory();
                var session = new SingleSessionWrapper(sessionFactory.OpenSession());
                new SchemaExport(Cfg.Value).Execute(false, true, false, session.Connection, null);
                Global.CreateSampleData(() => session, null);
                return Tuple.Create(session, sessionFactory, Cfg.Value);
            };

        public static readonly Action<SingleSessionWrapper, ISessionFactory> Teardown =
            (session, sessionFactory) => {
                session.Kill();
                sessionFactory.Dispose();
            };

        public static readonly Func<Action<ISession, Configuration>, Action> sessionSetup =
            f => () => {
                var s = Setup();
                try {
                    f(s.Item1, s.Item3);                    
                } finally {
                    Teardown(s.Item1, s.Item2);
                }
            };

        public static readonly Action<ISession, Configuration> ExecQuery =
            (session, cfg) => {
                var model = new Context {
                    Query = "from System.Object",
                    QueryType = QueryType.HQL,
                    ImageFields = new string[0],
                };
                ControllerFactory.ExecQuery(model, cfg, session, "/pepe.aspx");
                Assert.NotNull("results", model.Results);
                Assert.Equal("results count > 0", true, model.Results.Any());
                //foreach (var r in model.Results)
                //    foreach (var m in r)
                //        Console.WriteLine("{0}: {1}", m.Key, m.Value);
            };

        public static readonly Action<ISession, Configuration> ManyToMany =
            (session, cfg) => {
                var link = ControllerFactory.BuildCollectionLink(typeof (Territory), typeof (Employee), 1, cfg, "/pepe.aspx");
                //Console.WriteLine(link);
                Assert.NotNull("link", link);
            };

        public static readonly Action<ISession, Configuration> QueryScalarWithNamespace =
            (session, cfg) => {
                var prop = new Property { Name = "FirstName" };
                var query = ControllerFactory.QueryScalar(prop, typeof(Employee), new Employee(), cfg);
                Assert.Equal("query", "select x.FirstName from SampleModel.Employee x where x.Id = '0'", query);
            };

        public static readonly Action<ISession, Configuration> Component =
            (session, cfg) => {
                var employee = new Employee {
                    Address = new Address {
                        City = "",
                    }
                };
                var ctx = new Context {
                    ImageFields = new string[0],
                };
                ControllerFactory.ConvertResult(employee, ctx, cfg, "/pepe?");
            };

        public static readonly Action<ISession, Configuration> NullComponent =
            (session, cfg) => {
                var employee = new Employee {
                    Address = null,
                };
                var ctx = new Context {
                    ImageFields = new string[0],
                };
                ControllerFactory.ConvertResult(employee, ctx, cfg, "/pepe?");
            };

        public static readonly Test SessionTests = Test.List("Session tests", new[] {
            new {name = "exec query", test = ExecQuery},
            new {name = "many to many", test = ManyToMany},
            new {name = "query scalar includes namespace", test = QueryScalarWithNamespace},
            new {name = "null component", test = NullComponent},
            new {name = "component", test = Component},
        }.Select(x => Test.Case(x.name, sessionSetup(x.test))).ToArray());

        public static readonly Test AllTests =
            Test.List(new[] {
                SessionTests,
                Test.Case("next page", () => {
                    var context = new Context {
                        MaxResults = 10, 
                        Results = Enumerable.Range(1, 11).Select(i => new Row()).ToList(),
                    };
                    var url = ControllerFactory.BuildNextPageUrl(context, "/pepe.aspx?hql=from+System.Object&");
                    Console.WriteLine(url);
                }),
                Test.Case("is not collection of", () => {
                    var types = new[] {
                        Tuple.Create(typeof (IEnumerable<string>), typeof (int)),
                        Tuple.Create(typeof (IEnumerable), typeof (int)),
                        Tuple.Create(typeof (ArrayList), typeof (int)),
                        Tuple.Create(typeof (string), typeof (int)),
                        Tuple.Create(typeof (string), typeof (char)),
                    };

                    foreach (var t in types) {
                        Assert.Equal(t.Item1.ToString(), false, ControllerFactory.IsCollectionOf(t.Item1, t.Item2));
                    }
                }),
                Test.Case("is collection of", () => {
                    var types = new[] {
                        Tuple.Create(typeof (IEnumerable<string>), typeof (string)),
                        Tuple.Create(typeof (IEnumerable<int>), typeof (int)),
                        Tuple.Create(typeof (ICollection<int>), typeof (int)),
                        Tuple.Create(typeof (List<int>), typeof (int)),
                        Tuple.Create(typeof (Iesi.Collections.Generic.ISet<int>), typeof (int)),
                        Tuple.Create(typeof (HashedSet<int>), typeof (int)),
                    };

                    foreach (var t in types) {
                        Assert.Equal(t.Item1.ToString(), true, ControllerFactory.IsCollectionOf(t.Item1, t.Item2));
                    }
                })
            }).Wrap(t => {
                NHWebConsoleSetup.OpenSession = () => null;
                NHWebConsoleSetup.Configuration = () => null;
                return t;
            });
    }
}
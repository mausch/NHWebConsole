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
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Iesi.Collections.Generic;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using SampleModel;

namespace NHWebConsole.Tests {
    [TestFixture]
    public class Tests {
        [SetUp]
        public void Setup() {
            var cfg = Fluently.Configure()
                .Database(SQLiteConfiguration.Standard
                              .ConnectionString("Data Source=test.db;Version=3;New=True;"))
                .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Customer>()))
                .BuildConfiguration();
            var sessionFactory = cfg.BuildSessionFactory();
            NHWebConsoleSetup.OpenSession = () => sessionFactory.OpenSession();
            NHWebConsoleSetup.Configuration = () => cfg;
            new SchemaExport(cfg).Execute(false, true, false);
            CreateSampleData();
        }

        private void CreateSampleData() {
            using (var session = NHWebConsoleSetup.OpenSession()) {
                var customer = new Customer {
                    Name = "John Doe",
                    Title = "CEO",
                };
                session.Save(customer);
                var employee = new Employee {
                    FirstName = "Employee",
                    LastName = "of the Month",
                };
                session.Save(employee);
                var territory = new Territory {
                    Name = "North east",
                };
                session.Save(territory);
                session.Save(new Order {
                    Customer = customer,
                    Employee = employee,
                    OrderDate = DateTime.Now,
                });
                session.Save(new Order {
                    Customer = customer,
                    Employee = employee,
                    OrderDate = DateTime.Now.AddMonths(1),
                });
                session.Save(new Order {
                    Customer = customer,
                    Employee = employee,
                    OrderDate = DateTime.Now.AddDays(1),
                });
                territory.Employees = new HashedSet<Employee> { employee };
                employee.Territories = new HashedSet<Territory> {territory};
                session.Flush();
            }
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
        public void NextPage() {
            var c = new IndexController {
                RawUrl = "/pepe.aspx?hql=from+System.Object&",
            };
            Console.WriteLine(c.BuildNextPageUrl(new Context {
                MaxResults = 10,
            }));
        }
    }
}
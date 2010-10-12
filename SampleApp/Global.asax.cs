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
using System.IO;
using System.Linq;
using System.Web;
using FluentNHibernate;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Iesi.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NHWebConsole;
using NLipsum.Core;
using SampleModel;

namespace SampleApp {
    public class Global : HttpApplication {
        public static FluentConfiguration FluentNHConfig(string dbFile) {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard
                              .ConnectionString(string.Format("Data Source={0};Version=3;New=True;", dbFile)))
                .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Customer>(new NHFluentConfig())));
        }

        protected void Application_Start(object sender, EventArgs e) {
            var cfg = FluentNHConfig(Server.MapPath("/test.db")).BuildConfiguration();
            var sessionFactory = cfg.BuildSessionFactory();
            NHWebConsoleSetup.OpenSession = () => sessionFactory.OpenSession();
            NHWebConsoleSetup.Configuration = () => cfg;
            new SchemaExport(cfg).Execute(false, true, false);
            CreateSampleData(Server.MapPath("/maxi_yacht_sail9062928.jpg"));
        }

        public class NHFluentConfig : DefaultAutomappingConfiguration {
            public override bool IsComponent(Type type) {
                return type == typeof (Address);
            }

            public override bool ShouldMap(Member member) {
                return member.CanWrite;
            }
        }

        public static void CreateSampleData(string pictureFile) {
            using (var session = NHWebConsoleSetup.OpenSession()) {
                var customer = new Customer {
                    Name = "John Doe",
                    Title = "CEO",
                    History = LipsumGenerator.Generate(5),
                    SomeHtml = LipsumGenerator.GenerateHtml(5),
                    Picture = pictureFile == null ? null : File.ReadAllBytes(pictureFile),
                    Address = new Address {
                        City = "Buenos Aires",
                        Country = "Argentina",
                        State = "Buenos Aires",
                        Street = "El Cuco 123",
                    }
                };
                session.Save(customer);
                var employee = new Employee {
                    FirstName = "Employee",
                    LastName = "of the Month",
                    Address = new Address {
                        City = "Düsseldorf",
                        Country = "Deutschland",
                        State = "Nordrhein-Westfalen",
                        Street = "Königsallee 44",
                    }
                };
                session.Save(employee);
                foreach (var i in Enumerable.Range(1, 100))
                    session.Save(new Employee {
                        FirstName = "Juan",
                        LastName = "Perez",
                    });
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
                var territory = new Territory {
                    Name = "America",
                };
                session.Save(territory);
                employee.Territories = new HashedSet<Territory> {
                    territory,
                };
                session.Save(employee);
                session.Flush();
            }
        }

        protected void Session_Start(object sender, EventArgs e) {}

        protected void Application_BeginRequest(object sender, EventArgs e) {}

        protected void Application_AuthenticateRequest(object sender, EventArgs e) {}

        protected void Application_Error(object sender, EventArgs e) {}

        protected void Session_End(object sender, EventArgs e) {}

        protected void Application_End(object sender, EventArgs e) {}
    }
}
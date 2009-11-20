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
using System.Drawing;
using System.IO;
using System.Web;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using NHWebConsole;
using SampleModel;

namespace SampleApp {
    public class Global : HttpApplication {
        protected void Application_Start(object sender, EventArgs e) {
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
                    History = NLipsum.Core.LipsumGenerator.Generate(10),
                    SomeHtml = NLipsum.Core.LipsumGenerator.GenerateHtml(10),
                    Picture = File.ReadAllBytes(Server.MapPath("/maxi_yacht_sail9062928.jpg")),
                };
                session.Save(customer);
                var employee = new Employee {
                    FirstName = "Employee",
                    LastName = "of the Month",
                };
                session.Save(employee);
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
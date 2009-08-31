using System;
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
            NHibernateFunctions.OpenSession = () => sessionFactory.OpenSession();
            NHibernateFunctions.Configuration = () => cfg;
            new SchemaExport(cfg).Execute(false, true, false);
            CreateSampleData();
        }

        private void CreateSampleData() {
            using (var session = NHibernateFunctions.OpenSession()) {
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
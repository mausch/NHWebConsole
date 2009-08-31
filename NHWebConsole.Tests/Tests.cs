using System;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
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


        [Test]
        public void ExecQuery() {
            using (var session = NHibernateFunctions.OpenSession()) {
                var c = new IndexController {
                    Session = session,
                    Cfg = NHibernateFunctions.Configuration(),
                    RawUrl = "/pepe.aspx",
                };
                var results = c.ExecQuery(new ViewModel {
                    Hql = "from System.Object",
                });
                Assert.IsNotNull(results);
                Assert.Greater(results.Count, 0);
                foreach (var r in results)
                    foreach (var m in r)
                        Console.WriteLine("{0}: {1}", m.Key, m.Value);
            }
        }

        [Test]
        public void NextPage() {
            var c = new IndexController {
                RawUrl = "/pepe.aspx?hql=from+System.Object&",
            };
            Console.WriteLine(c.BuildNextPageUrl(new ViewModel {
                MaxResults = 10,
            }));
        }
    }
}
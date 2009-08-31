using System;
using NHibernate;
using NHibernate.Cfg;

namespace NHWebConsole {
    public static class NHibernateFunctions {
        public static Func<ISession> OpenSession { get; set; }

        public static Func<ISessionFactory> SessionFactory { get; set; }

        public static Func<Configuration> Configuration { get; set; }

        static NHibernateFunctions() {
            OpenSession = () => SessionFactory().OpenSession();

            SessionFactory = () => {
                throw new Exception("Define NHWebConsole.NHibernateFunctions.SessionFactory");
            };

            Configuration = () => {
                throw new Exception("Define NHWebConsole.NHibernateFunctions.Configuration");
            };
        }
    }
}
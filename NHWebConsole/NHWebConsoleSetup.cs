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
using NHibernate;
using NHibernate.Cfg;

namespace NHWebConsole {
    /// <summary>
    /// Defines how to access NHibernate's session.
    /// Either <see cref="SessionFactory"/> or <see cref="OpenSession"/> need to be defined.
    /// <see cref="Configuration"/> is also required.
    /// </summary>
    public static class NHWebConsoleSetup {
        /// <summary>
        /// Defines how to open/get an <see cref="ISession"/>.
        /// By default it opens a session from the <see cref="SessionFactory"/>
        /// </summary>
        public static Func<ISession> OpenSession { get; set; }

        /// <summary>
        /// Defines how to get an <see cref="ISessionFactory"/>. 
        /// Required
        /// </summary>
        public static Func<ISessionFactory> SessionFactory { get; set; }

        /// <summary>
        /// Defines how to get NHibernate's <see cref="Configuration"/>
        /// </summary>
        public static Func<Configuration> Configuration { get; set; }

        /// <summary>
        /// If true (default), NHWebConsole will dispose the <see cref="ISession"/> at the end of the request.
        /// </summary>
        public static bool DisposeSession = true;

        static NHWebConsoleSetup() {
            OpenSession = () => SessionFactory().OpenSession();

            SessionFactory = () => {
                throw new Exception("Define NHWebConsole.NHWebConsoleSetup.SessionFactory");
            };

            Configuration = () => {
                throw new Exception("Define NHWebConsole.NHWebConsoleSetup.Configuration");
            };
        }
    }
}
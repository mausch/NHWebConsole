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
using NVelocity.App;

namespace MiniMVC {
    public static class Setup {
        public static Func<VelocityEngine> TemplateEngine { get; set; }

        public static Func<string, Controller> ControllerFactory { get; set; }

        static Setup() {
            var engine = new EmbeddedVelocityEngine();
            TemplateEngine = () => engine;
            ControllerFactory = controller => {
                var controllerType = Type.GetType(controller, false, false);
                if (controllerType == null)
                    throw new Exception(string.Format("Type '{0}' not found", controller));
                return (Controller) Activator.CreateInstance(controllerType);
            };
        }
    }
}
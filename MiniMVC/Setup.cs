using System;
using NVelocity.App;

namespace MiniMVC {
    public static class Setup {
        public static Func<VelocityEngine> TemplateEngine { get; set; }

        static Setup() {
            var engine = new EmbeddedVelocityEngine();
            TemplateEngine = () => engine;
        }
    }
}
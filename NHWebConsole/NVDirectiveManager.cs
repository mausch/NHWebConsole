using System;
using System.Text.RegularExpressions;
using NVelocity.Runtime.Directive;

namespace NHWebConsole {
    public class NVDirectiveManager : DirectiveManager {
        public override void Register(String directiveTypeName) {
            directiveTypeName = Regex.Replace(directiveTypeName, directiveTypeName.Split(',')[1] + "$", GetType().Assembly.FullName.Split(',')[0]);
            base.Register(directiveTypeName);
        }
    }
}
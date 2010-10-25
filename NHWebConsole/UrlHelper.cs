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
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NHWebConsole {
    public static class UrlHelper {
        public static string SetParameters(string url, IDictionary<string, object> parameters) {
            var parts = url.Split('?');
            IDictionary<string, string> qs = new Dictionary<string, string>();
            if (parts.Length > 1)
                qs = ParseQueryString(parts[1]);
            foreach (var p in parameters)
                qs[p.Key] = Convert.ToString(p.Value);
            return parts[0] + "?" + DictToQuerystring(qs);
        }

        public static string DictToQuerystring(IDictionary<string, string> qs) {
            return string.Join("&", qs
                                        .Where(k => !string.IsNullOrEmpty(k.Key))
                                        .Select(k => string.Format("{0}={1}", HttpUtility.UrlEncode(k.Key), HttpUtility.UrlEncode(k.Value)))
                                        .ToArray());
        }


        /// <summary>
        /// Parses a query string. If duplicates are present, the last key/value is kept.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IDictionary<string, string> ParseQueryString(string s) {
            var d = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (s == null)
                return d;
            if (s.StartsWith("?"))
                s = s.Substring(1);
            foreach (var kv in s.Split('&')) {
                var v = kv.Split('=');
                if (string.IsNullOrEmpty(v[0]))
                    continue;
                d[HttpUtility.UrlDecode(v[0])] = HttpUtility.UrlDecode(v[1]);
            }
            return d;
        }

        public static string Link(string url, string text) {
            return string.Format("<a href=\"{0}\">{1}</a>", url, text);
        }

    }
}
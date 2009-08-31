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
                                        .Select(k => string.Format("{0}={1}", HttpUtility.UrlEncode(k.Key), HttpUtility.UrlEncode(k.Value))).ToArray());
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
    }
}
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Property;
using NHibernate.Proxy;
using NHibernate.Type;

namespace NHWebConsole {
    /// <summary>
    /// Entry-point
    /// </summary>
    public class IndexController : NHController {
        private const int maxLen = 100;
        private string rawUrl;
        private Configuration cfg = NHWebConsoleSetup.Configuration();

        public Configuration Cfg {
            get { return cfg; }
            set { cfg = value; }
        }

        public string RawUrl {
            get { return rawUrl; }
            set { rawUrl = value; }
        }

        /// <summary>
        /// Request entry-point
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IResult Execute(HttpContext context) {
            rawUrl = context.Request.RawUrl;
            var model = new ViewModel {
                Url = rawUrl.Split('?')[0],
                LimitLength = string.IsNullOrEmpty(context.Request.QueryString["limitLength"]),
                Raw = !string.IsNullOrEmpty(context.Request.QueryString["raw"]),
                ImageFields = (context.Request.QueryString["image"] ?? "").Split(','),
                ContentType = context.Request.QueryString["contentType"],
            };
            try {
                model.MaxResults = TryParse(context.Request["MaxResults"]);
                model.FirstResult = TryParse(context.Request["FirstResult"]);
                model.Query = context.Request["q"];
                model.QueryType = GetQueryType(context.Request["type"]);
                ExecQuery(model);
                model.NextPageUrl = BuildNextPageUrl(model);
                model.PrevPageUrl = BuildPrevPageUrl(model);
            } catch (HibernateException e) {
                model.Error = e.ToString();
            }
            if (model.Raw)
                return new RawResult(model.RawResult) {ContentType = model.ContentType};
            return new ViewResult(model, ViewName);
        }

        public QueryType GetQueryType(string s) {
            if (string.IsNullOrEmpty(s))
                return QueryType.HQL;
            return (QueryType) Enum.Parse(typeof (QueryType), s, true);
        }

        public string BuildPrevPageUrl(ViewModel model) {
            if (!model.MaxResults.HasValue || !model.FirstResult.HasValue || model.FirstResult.Value <= 0)
                return null;
            return UrlHelper.SetParameters(rawUrl, new Dictionary<string, object> {
                {"FirstResult", Math.Max(0, model.FirstResult.Value-model.MaxResults.Value)},
            });
        }

        public string BuildNextPageUrl(ViewModel model) {
            if (!model.MaxResults.HasValue || model.Results.Count < model.MaxResults)
                return null;
            var first = model.FirstResult ?? 0;
            return UrlHelper.SetParameters(rawUrl, new Dictionary<string, object> {
                {"FirstResult", first + model.MaxResults.Value},
            });
        }

        public int? TryParse(string s) {
            if (string.IsNullOrEmpty(s))
                return null;
            int r;
            if (int.TryParse(s.Trim(), out r))
                return r;
            return null;
        }

        public IQuery CreateQuery(ViewModel model) {
            if (model.QueryType == QueryType.HQL)
                return Session.CreateQuery(model.Query);
            return Session.CreateSQLQuery(model.Query);
        }

        public void ExecQuery(ViewModel model) {
            if (cfg == null)
                throw new ApplicationException("NHibernate configuration not supplied");
            if (string.IsNullOrEmpty(model.Query))
                return;
            var q = CreateQuery(model);
            if (model.MaxResults.HasValue)
                q.SetMaxResults(model.MaxResults.Value);
            if (model.FirstResult.HasValue)
                q.SetFirstResult(model.FirstResult.Value);
            ExecQueryByType(q, model);
        }

        private static readonly Regex updateRx = new Regex(@"\s*(insert|update|delete)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void ExecQueryByType(IQuery q, ViewModel model) {
            if (!updateRx.IsMatch(model.Query)) {
                if (model.Raw)
                    model.RawResult = q.UniqueResult();
                else
                    model.Results = ConvertResults(q.List(), model);
            } else {
                var count = q.ExecuteUpdate();
                model.Results = new List<ICollection<KeyValuePair<string, string>>> {
                    new Dictionary<string, string> {
                        {"count", count.ToString()},
                    },
                };                
            }
        }

        public KeyValuePair<K, V> KV<K, V>(K key, V value) {
            return new KeyValuePair<K, V>(key, value);
        }

        public ICollection<KeyValuePair<string, string>> ConvertResult(object o, ViewModel model) {
            var r = new List<KeyValuePair<string, string>>();
            var trueType = NHibernateProxyHelper.GetClass(o);
            var mapping = cfg.GetClassMapping(trueType);
            r.Add(KV("Type", BuildTypeLink(trueType)));
            if (mapping == null) {
                if (o is object[]) {
                    r.AddRange(ConvertObjectArray((object[])o, model));
                } else {
                    r.Add(KV("Value", HttpUtility.HtmlEncode(Convert.ToString(o))));
                }
            } else {
                r.Add(KV(mapping.IdentifierProperty.Name, Convert.ToString(mapping.IdentifierProperty.GetGetter(trueType).Get(o))));
                r.AddRange(mapping.PropertyCollection.Cast<Property>()
                               .Select(p => ConvertProperty(o, trueType, p, model)));
            }
            return r;
        }

        public IEnumerable<KeyValuePair<string, string>> ConvertObjectArray(object[] o, ViewModel model) {
            return o.SelectMany((x, i) => ConvertResult(x, model)
                .Select(k => KV(string.Format("{0}[{1}]", k.Key, i), k.Value)));
        }

        public string BuildCollectionLink(Type ct, Type fk, object fkValue) {
            var fkp = cfg.GetClassMapping(ct).PropertyCollection.Cast<Property>()
                .FirstOrDefault(p => p.Type.IsAssociationType && p.GetGetter(ct).ReturnType == fk);
            if (fkp == null)
                return null;
            var hql = string.Format("from {0} x where x.{1} = '{2}'", ct.Name, fkp.Name, fkValue);
            return string.Format("<a href=\"{0}?q={1}&MaxResults=10\">collection</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
        }

        public string BuildEntityLink(Type entityType, object pkValue) {
            var hql = string.Format("from {0} x where x.{1} = '{2}'", entityType.Name, GetPkGetter(entityType).PropertyName, pkValue);
            return string.Format("<a href=\"{0}?q={1}\">{2}#{3}</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql), entityType.Name, pkValue);
        }

        public string BuildTypeLink(Type entityType) {
            if (cfg.GetClassMapping(entityType) == null)
                return entityType.Name;
            var hql = string.Format("from {0}", entityType.Name);
            return string.Format("<a href='{0}?q={1}&MaxResults=10'>{2}</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql), entityType.Name);
        }

        public IGetter GetPkGetter(Type entityType) {
            return cfg.GetClassMapping(entityType).IdentifierProperty.GetGetter(entityType);
        }

        public object GetPkValue(Type entityType, object o) {
            return GetPkGetter(entityType).Get(o);
        }

        public KeyValuePair<string, string> ConvertProperty(object o, Type entityType, Property p, ViewModel model) {
            var getter = p.GetGetter(entityType);
            var value = getter.Get(o);
            if (p.Type.IsCollectionType) {
                var fkType = getter.ReturnType.GetGenericArguments()[0];
                var fk = GetPkValue(entityType, o);
                return KV(p.Name, BuildCollectionLink(fkType, entityType, fk));
            }
            if (p.Type.IsEntityType) {
                var assocType = (EntityType) p.Type;
                var mapping = cfg.GetClassMapping(assocType.AssociatedClass);
                var o1 = p.GetGetter(entityType).Get(o);
                if (o1 == null)
                    return KV(p.Name, null as string);
                var pk = GetPkValue(mapping.MappedClass, o1);
                return KV(p.Name, BuildEntityLink(getter.ReturnType, pk));
            }
            var valueAsString = Convert.ToString(value);
            if (model.ImageFields.Contains(p.Name)) {
                var query = QueryScalar(p, entityType, o);
                var imgUrl = string.Format("{0}?raw=1&q={1}", rawUrl.Split('?')[0], HttpUtility.UrlEncode(query));
                valueAsString = string.Format("<img src=\"{0}\"/>", imgUrl);
            } else if (model.LimitLength && valueAsString.Length > maxLen) {
                var sb = new StringBuilder();
                sb.Append(HttpUtility.HtmlEncode(valueAsString.Substring(0, maxLen)));
                var query = QueryScalar(p, entityType, o);
                sb.AppendFormat("<a href=\"{0}?q={1}&limitLength=0\">...</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(query));
                valueAsString = sb.ToString();
            } else {
                valueAsString = HttpUtility.HtmlEncode(valueAsString);
            }
            if (p.Type == NHibernateUtil.BinaryBlob || p.Type == NHibernateUtil.Binary) {
                var urlParts = rawUrl.Split('?');
                IDictionary<string, string> qs = new Dictionary<string, string>();
                if (urlParts.Length > 1)
                    qs = UrlHelper.ParseQueryString(urlParts[1]);
                if (!qs.ContainsKey("image") || !qs["image"].Contains(p.Name)) {
                    if (qs.ContainsKey("image"))
                        qs["image"] += "," + p.Name;
                    else
                        qs["image"] = p.Name;
                    valueAsString += string.Format("<a href=\"{0}?{1}\">(as image)</a>", urlParts[0], UrlHelper.DictToQuerystring(qs));
                }
            }
            return KV(p.Name, valueAsString);
        }

        public string QueryScalar(Property p, Type entityType, object o) {
            return string.Format("select {0} from {1} x where x.{2} = '{3}'", p.Name, entityType.Name, GetPkGetter(entityType).PropertyName, GetPkValue(entityType, o));
        }

        public ICollection<ICollection<KeyValuePair<string, string>>> ConvertResults(IList results, ViewModel model) {
            return results.Cast<object>().Select(x => ConvertResult(x, model)).ToList();
        }
    }
}
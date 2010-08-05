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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using MiniMVC;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Proxy;
using NHibernate.Type;

namespace NHWebConsole {
    /// <summary>
    /// Entry-point
    /// </summary>
    public class IndexController : NHController {
        private const int maxLen = 100;

        public Configuration Cfg { get; set; }

        public string RawUrl { get; set; }

        public IndexController() {
            Cfg = NHWebConsoleSetup.Configuration();
        }

        /// <summary>
        /// Request entry-point
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IResult Execute(HttpContextBase context) {
            RawUrl = context.Request.RawUrl;
            var model = new Context {
                Url = RawUrl.Split('?')[0],
                LimitLength = string.IsNullOrEmpty(context.Request.QueryString["limitLength"]),
                Raw = !string.IsNullOrEmpty(context.Request.QueryString["raw"]),
                ImageFields = (context.Request.QueryString["image"] ?? "").Split(','),
                ContentType = context.Request.QueryString["contentType"],
                Output = context.Request.QueryString["output"],
                ExtraRowTemplate = context.Request["extraRowTemplate"],
            };
            try {
                model.MaxResults = TryParse(context.Request["MaxResults"]);
                model.FirstResult = TryParse(context.Request["FirstResult"]);
                model.Query = context.Request["q"];
                model.QueryType = GetQueryType(context.Request["type"]);
                ExecQuery(model);
                model.NextPageUrl = BuildNextPageUrl(model);
                model.PrevPageUrl = BuildPrevPageUrl(model);
                model.FirstPageUrl = BuildFirstPageUrl(model);
                model.AllEntities = GetAllEntities()
                    .OrderBy(e => e)
                    .Select(e => KV(e, BuildEntityUrl(e)))
                    .ToList();
                model.RssUrl = BuildRssUrl(model);
            } catch (HibernateException e) {
                model.Error = e.ToString();
            }
            if (model.Raw)
                return new RawResult(model.RawResult) {ContentType = model.ContentType};
            if (model.Output != null)
                ViewName = GetEmbeddedViewName(model.Output);
            return new ViewResult(model, ViewName) { ContentType = model.ContentType };
        }

        public string BuildRssUrl(Context model) {
            if (string.IsNullOrEmpty(model.Query) || updateRx.IsMatch(model.Query))
                return null;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"contentType", "application/rss+xml"},
                {"output", "RSS"},
            });
        }

        public IEnumerable<string> GetAllEntities() {
            return Cfg.ClassMappings.Select(c => c.EntityName);
        }

        public QueryType GetQueryType(string s) {
            if (string.IsNullOrEmpty(s))
                return QueryType.HQL;
            return (QueryType) Enum.Parse(typeof (QueryType), s, true);
        }

        public string BuildFirstPageUrl(Context model) {
            if (!HasPrevPage(model))
                return null;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"FirstResult", 0},
            });
        }

        public bool HasPrevPage(Context model) {
            return !(!model.MaxResults.HasValue || !model.FirstResult.HasValue || model.FirstResult.Value <= 0);
        }

        public string BuildPrevPageUrl(Context model) {
            if (!HasPrevPage(model))
                return null;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"FirstResult", Math.Max(0, model.FirstResult.Value-model.MaxResults.Value)},
            });
        }

        public string BuildNextPageUrl(Context model) {
            if (!model.MaxResults.HasValue || model.Total <= model.MaxResults)
                return null;
            var first = model.FirstResult ?? 0;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
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

        public IQuery CreateQuery(Context model) {
            if (model.QueryType == QueryType.HQL)
                return Session.CreateQuery(model.Query);
            return Session.CreateSQLQuery(model.Query);
        }

        public void ExecQuery(Context model) {
            if (Cfg == null)
                throw new ApplicationException("NHibernate configuration not supplied");
            if (string.IsNullOrEmpty(model.Query))
                return;
            var q = CreateQuery(model);
            if (model.MaxResults.HasValue)
                q.SetMaxResults(model.MaxResults.Value+1);
            if (model.FirstResult.HasValue)
                q.SetFirstResult(model.FirstResult.Value);
            ExecQueryByType(q, model);
        }

        private static readonly Regex updateRx = new Regex(@"^\s*(insert|update|delete)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void ExecQueryByType(IQuery q, Context model) {
            if (!updateRx.IsMatch(model.Query)) {
                if (model.Raw)
                    model.RawResult = q.UniqueResult();
                else {
                    var results = q.List();
                    model.Total = results.Count;
                    model.Results = ConvertResults(results, model);
                }
            } else {
                var count = q.ExecuteUpdate();
                model.Results = new List<Row> {
                    new Row {
                        KV("count", count.ToString()),
                    },
                };
            }
        }

        public KeyValuePair<K, V> KV<K, V>(K key, V value) {
            return new KeyValuePair<K, V>(key, value);
        }

        /// <summary>
        /// Converts a single result row
        /// </summary>
        /// <param name="o"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public Row ConvertResult(object o, Context model) {
            var row = new Row();
            var trueType = NHibernateProxyHelper.GetClassWithoutInitializingProxy(o);
            var mapping = Cfg.GetClassMapping(trueType);
            row.Add(KV("Type", BuildTypeLink(trueType)));
            if (mapping == null) {
                if (o is object[]) {
                    row.AddRange(ConvertObjectArray((object[])o, model));
                } else {
                    row.Add(KV("Value", HttpUtility.HtmlEncode(Convert.ToString(o))));
                }
            } else {
                row.Add(KV(mapping.IdentifierProperty.Name, Convert.ToString(mapping.IdentifierProperty.GetGetter(trueType).Get(o))));
                row.AddRange(mapping.PropertyClosureIterator
                               .SelectMany(p => ConvertProperty(o, trueType, p, model)));
            }
            return row;
        }

        public IEnumerable<KeyValuePair<string, string>> ConvertObjectArray(object[] o, Context model) {
            return o.SelectMany((x, i) => ConvertResult(x, model)
                .Select(k => KV(string.Format("{0}[{1}]", HttpUtility.UrlEncode(k.Key), i), k.Value)));
        }

        public string BuildCollectionLink(Type ct, Type fk, object fkValue) {
            var classMapping = Cfg.GetClassMapping(ct);
            var associations = classMapping.PropertyClosureIterator.Where(p => p.Type.IsAssociationType);
            var fkp = associations.FirstOrDefault(p => p.GetGetter(ct).ReturnType == fk);
            if (fkp != null) {
                var hql = string.Format("from {0} x where x.{1} = '{2}'", classMapping.EntityName, fkp.Name, fkValue);
                return string.Format("<a href=\"{0}?q={1}&MaxResults=10\">collection</a>", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
            }
            // try many-to-many
            var collection = associations.FirstOrDefault(p => IsCollectionOf(p.GetGetter(ct).ReturnType, fk));
            if (collection != null) {
                var fkType = collection.GetGetter(ct).ReturnType.GetGenericArguments()[0];
                var fkTypePK = GetPkGetter(fkType).PropertyName;
                var hql = string.Format("select x from {0} x join x.{1} y where y.{2} = '{3}'", classMapping.EntityName, collection.Name, fkTypePK, fkValue);
                return string.Format("<a href=\"{0}?q={1}&MaxResults=10\">collection</a>", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
            }
            return null;
        }

        public static bool IsCollectionOf(Type collectionType, Type elementType) {
            if (!collectionType.IsGenericType)
                return false;
            if (!typeof(IEnumerable).IsAssignableFrom(collectionType))
                return false;
            var typeArgs = collectionType.GetGenericArguments();
            if (typeArgs.Length > 1)
                return false;
            return typeArgs[0] == elementType;
        }

        public string BuildEntityUrl(string entityName) {
            return string.Format("{0}?q=from+{1}&MaxResults=10", RawUrl.Split('?')[0], HttpUtility.UrlEncode(entityName));
        }

        public string BuildEntityLink(Type entityType, object pkValue) {
            var hql = string.Format("from {0} x where x.{1} = '{2}'", Cfg.GetClassMapping(entityType).EntityName, GetPkGetter(entityType).PropertyName, pkValue);
            return string.Format("<a href=\"{0}?q={1}\">{2}#{3}</a>", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql), entityType.Name, pkValue);
        }

        public string BuildTypeLink(Type entityType) {
            if (Cfg.GetClassMapping(entityType) == null)
                return entityType.Name;
            var hql = string.Format("from {0}", Cfg.GetClassMapping(entityType).EntityName);
            return string.Format("<a href='{0}?q={1}&MaxResults=10'>{2}</a>", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql), entityType.Name);
        }

        public IGetter GetPkGetter(Type entityType) {
            return Cfg.GetClassMapping(entityType).IdentifierProperty.GetGetter(entityType);
        }

        public object GetPkValue(Type entityType, object o) {
            return GetPkGetter(entityType).Get(o);
        }

        public KeyValuePair<string, string> ConvertCollection(object o, Type entityType, Property p) {
            var getter = p.GetGetter(entityType);
            var fkType = getter.ReturnType.GetGenericArguments()[0];
            var fk = GetPkValue(entityType, o);
            return KV(p.Name, BuildCollectionLink(fkType, entityType, fk));
        }

        public IEnumerable<KeyValuePair<string, string>> ConvertComponent(object o, Property p, string name) {
            if (o == null)
                return new[] {KV<string, string>(name, null),};
            var compType = (ComponentType) p.Type;
            var t = o.GetType();
            return from propName in compType.PropertyNames
                   let prop = t.GetProperty(propName)
                   let v = prop.GetValue(o, null)
                   select KV(string.Format("{0}.{1}", name, propName), v == null ? null : v.ToString());
        }

        public KeyValuePair<string, string> ConvertEntity(object o, Type entityType, Property p) {
            var assocType = (EntityType)p.Type;
            var mapping = Cfg.GetClassMapping(assocType.GetAssociatedEntityName());
            var o1 = p.GetGetter(entityType).Get(o);
            if (o1 == null)
                return KV(p.Name, null as string);
            var pk = GetPkValue(mapping.MappedClass, o1);
            var getter = p.GetGetter(entityType);
            return KV(p.Name, BuildEntityLink(getter.ReturnType, pk));
        }

        public IEnumerable<KeyValuePair<string, string>> ConvertProperty(object o, Type entityType, Property p, Context model) {
            if (p.Type.IsCollectionType) {
                yield return ConvertCollection(o, entityType, p);
                yield break;
            }
            if (p.Type.IsEntityType) {
                yield return ConvertEntity(o, entityType, p);
                yield break;
            }
            var getter = p.GetGetter(entityType);
            var value = getter.Get(o);
            if (p.Type.IsComponentType) {
                foreach (var r in ConvertComponent(value, p, p.Name))
                    yield return r;
                yield break;
            }
            string valueAsString = null;
            if (value != null)
                valueAsString = Convert.ToString(value);
            if (model.ImageFields.Contains(p.Name)) {
                var query = QueryScalar(p, entityType, o);
                var imgUrl = string.Format("{0}?raw=1&q={1}", RawUrl.Split('?')[0], HttpUtility.UrlEncode(query));
                valueAsString = string.Format("<img src=\"{0}\"/>", imgUrl);
            } else if (model.LimitLength && value != null && valueAsString.Length > maxLen) {
                var sb = new StringBuilder();
                sb.Append(HttpUtility.HtmlEncode(valueAsString.Substring(0, maxLen)));
                var query = QueryScalar(p, entityType, o);
                sb.AppendFormat("<a href=\"{0}?q={1}&limitLength=0\">...</a>", RawUrl.Split('?')[0], HttpUtility.UrlEncode(query));
                valueAsString = sb.ToString();
            } else {
                valueAsString = HttpUtility.HtmlEncode(valueAsString);
            }
            if (p.Type == NHibernateUtil.BinaryBlob || p.Type == NHibernateUtil.Binary) {
                var urlParts = RawUrl.Split('?');
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
            yield return KV(p.Name, valueAsString);
        }

        public string QueryScalar(Property p, Type entityType, object o) {
            return string.Format("select x.{0} from {1} x where x.{2} = '{3}'", p.Name, entityType.Name, GetPkGetter(entityType).PropertyName, GetPkValue(entityType, o));
        }

        public ICollection<Row> ConvertResults(IList results, Context model) {
            var r = results.Cast<object>()
                .Select(x => ConvertResult(x, model));
            if (model.MaxResults.HasValue)
                r = r.Take(model.MaxResults.Value);
            return r.ToList();
        }
    }
}
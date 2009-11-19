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
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Proxy;
using NHibernate.Type;

namespace NHWebConsole {
    public class IndexController : NHController {
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

        public override object Execute(HttpContext context) {
            rawUrl = context.Request.RawUrl;
            var model = new ViewModel {
                Url = rawUrl.Split('?')[0],
            };
            try {
                model.MaxResults = TryParse(context.Request["MaxResults"]);
                model.FirstResult = TryParse(context.Request["FirstResult"]);
                model.Query = context.Request["q"];
                model.QueryType = GetQueryType(context.Request["type"]);
                model.Results = ExecQuery(model);
                model.NextPageUrl = BuildNextPageUrl(model);
                model.PrevPageUrl = BuildPrevPageUrl(model);
            } catch (HibernateException e) {
                model.Error = e.ToString();
            }
            return model;
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
            if (!model.MaxResults.HasValue)
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

        public ICollection<ICollection<KeyValuePair<string, string>>> ExecQuery(ViewModel model) {
            if (cfg == null)
                throw new ApplicationException("NHibernate configuration not supplied");
            if (string.IsNullOrEmpty(model.Query))
                return null;
            var q = CreateQuery(model);
            if (model.MaxResults.HasValue)
                q.SetMaxResults(model.MaxResults.Value);
            if (model.FirstResult.HasValue)
                q.SetFirstResult(model.FirstResult.Value);
            return ExecQueryByType(q, model);
        }

        private static readonly Regex updateRx = new Regex(@"\s*(insert|update|delete)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ICollection<ICollection<KeyValuePair<string, string>>> ExecQueryByType(IQuery q, ViewModel model) {
            if (!updateRx.IsMatch(model.Query))
                return ConvertResults(q.List());
            var count = q.ExecuteUpdate();
            return new List<ICollection<KeyValuePair<string, string>>> {
                new Dictionary<string, string> {
                    {"count", count.ToString()},
                },
            };
        }

        public KeyValuePair<K, V> KV<K, V>(K key, V value) {
            return new KeyValuePair<K, V>(key, value);
        }

        public ICollection<KeyValuePair<string, string>> ConvertResult(object o) {
            var r = new List<KeyValuePair<string, string>>();
            var trueType = NHibernateProxyHelper.GetClassWithoutInitializingProxy(o);
            var mapping = cfg.GetClassMapping(trueType);
            r.Add(KV("Type", trueType.Name));
            if (mapping == null) {
                if (o is object[]) {
                    r.AddRange(ConvertObjectArray((object[])o));
                } else {
                    r.Add(KV("Value", Convert.ToString(o)));
                }
            } else {
                r.Add(KV(mapping.IdentifierProperty.Name, Convert.ToString(mapping.IdentifierProperty.GetGetter(trueType).Get(o))));
                r.AddRange(mapping.PropertyIterator
                               .Select(p => ConvertProperty(o, trueType, p)));
            }
            return r;
        }

        public IEnumerable<KeyValuePair<string, string>> ConvertObjectArray(object[] o) {
            return o.SelectMany((x, i) => ConvertResult(x)
                .Select(k => KV(string.Format("{0}[{1}]", k.Key, i), k.Value)));
        }

        public string BuildCollectionLink(Type ct, Type fk, object fkValue) {
            var fkp = cfg.GetClassMapping(ct).PropertyIterator
                .FirstOrDefault(p => p.Type.IsAssociationType && p.GetGetter(ct).ReturnType == fk);
            if (fkp == null)
                return null;
            var hql = string.Format("from {0} x where x.{1} = {2}", ct.Name, fkp.Name, fkValue);
            return string.Format("<a href='{0}?q={1}'>collection</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
        }

        public string BuildEntityLink(Type entityType, object pkValue) {
            var hql = string.Format("from {0} x where x.{1} = {2}", entityType.Name, GetPkGetter(entityType).PropertyName, pkValue);
            return string.Format("<a href='{0}?q={1}'>{2}#{3}</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql), entityType.Name, pkValue);
        }

        public IGetter GetPkGetter(Type entityType) {
            return cfg.GetClassMapping(entityType).IdentifierProperty.GetGetter(entityType);
        }

        public object GetPkValue(Type entityType, object o) {
            return GetPkGetter(entityType).Get(o);
        }

        public KeyValuePair<string, string> ConvertProperty(object o, Type entityType, Property p) {
            var getter = p.GetGetter(entityType);
            var value = getter.Get(o);
            if (p.Type.IsCollectionType) {
                var fkType = getter.ReturnType.GetGenericArguments()[0];
                var fk = GetPkValue(entityType, o);
                return KV(p.Name, BuildCollectionLink(fkType, entityType, fk));
            }
            if (p.Type.IsEntityType) {
                var assocType = (EntityType) p.Type;
                var mapping = cfg.GetClassMapping(assocType.GetAssociatedEntityName());
                var o1 = p.GetGetter(entityType).Get(o);
                if (o1 == null)
                    return KV(p.Name, null as string);
                var pk = GetPkValue(mapping.MappedClass, o1);
                return KV(p.Name, BuildEntityLink(getter.ReturnType, pk));
            }
            return KV(p.Name, HttpUtility.HtmlEncode(Convert.ToString(value)));
        }

        public ICollection<ICollection<KeyValuePair<string, string>>> ConvertResults(IList results) {
            return results.Cast<object>().Select(x => ConvertResult(x)).ToList();
        }
    }
}
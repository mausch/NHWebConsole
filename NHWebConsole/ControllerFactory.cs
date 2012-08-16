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
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using HqlIntellisense;
using MiniMVC;
using NHWebConsole.Views;
using NHibernate;
using NHWebConsole.Utils;
using NHibernate.Cfg;
using NHibernate.Properties;
using NHibernate.Proxy;
using NHibernate.Mapping;
using NHibernate.Type;

namespace NHWebConsole {
    public class ControllerFactory : HttpHandlerFactory {
        public override IHttpHandler GetHandler(HttpContextBase context) {
            var lastUrlSegment = context.Request.Url.Segments.Last().Split('.')[0];
            return routes.Where(k => k.Key == lastUrlSegment).Select(k => k.Value).FirstOrDefault();
        }

        private readonly IEnumerable<KeyValuePair<string, IHttpHandler>> routes =
            new[] {
                KV("opensearch", OpensearchHandler),
                KV("index", IndexHandler),
                KV("suggestion", SuggestionHandler),
            };

        public static Action<HttpContextBase> WithNHSession(Action<HttpContextBase, ISession, Configuration> action) {
            return ctx => {
                var cfg = NHWebConsoleSetup.Configuration();
                var session = NHWebConsoleSetup.OpenSession();
                session.FlushMode = FlushMode.Never;
                try {
                    action(ctx, session, cfg);
                } finally {
                    if (NHWebConsoleSetup.DisposeSession)
                        session.Dispose();
                }            
            };
        }

        public static readonly IHttpHandler OpensearchHandler = new HttpHandler(Opensearch);

        public static void Opensearch(HttpContextBase context) {
            var url = context.Request.Url.ToString();
            url = url.Split('/').Reverse().Skip(1).Reverse().Join("/");
            var v = Views.Views.OpenSearch(url);
            context.XDocument(v.MakeHTML5Doc(), "application/opensearchdescription+xml");
        }

        public static readonly IHttpHandler StaticHandler = new HttpHandler(Static);

        public static void Static(HttpContextBase context) {
            var resource = context.Request.QueryString["r"];
            var contentType = context.Request.QueryString["t"];
            var cache = context.Request.QueryString["cache"];
            if (!string.IsNullOrEmpty(cache))
                context.Response.Cache.SetExpires(DateTime.Now.AddYears(10));
            if (contentType != null)
                context.Response.ContentType = contentType;
            var assembly = typeof (ControllerFactory).Assembly;
            var fullResourceName = string.Format("{0}.Resources.{1}", assembly.FullName.Split(',')[0], resource);
            using (var resourceStream = assembly.GetManifestResourceStream(fullResourceName))
                Copy(resourceStream, context.Response.OutputStream);
        }

        public static void Copy(Stream source, Stream dest) {
            const int size = 32768;
            var buffer = new byte[size];
            var read = 0;
            while ((read = source.Read(buffer, 0, size)) > 0)
                dest.Write(buffer, 0, read);
        }

        public static readonly IHttpHandler SuggestionHandler = new HttpHandlerWithReadOnlySession(WithNHSession(Suggestion));

        public static void Suggestion(HttpContextBase context, ISession session, Configuration cfg) {
            var q = context.Request.QueryString["q"];
            var p = int.Parse(context.Request.QueryString["p"]);
            var hqlAssist = new HQLCompletionRequestor();
            new HQLCodeAssist(new SimpleConfigurationProvider(cfg)).CodeComplete(q, p, hqlAssist);
            if (hqlAssist.Error != null) {
                context.Raw(hqlAssist.Error);
                return;
            }
            var sugg = string.Join(",", hqlAssist.Suggestions.Select(s => string.Format("\"{0}\"", s)).ToArray());
            var json = "{\"suggestions\": [$]}".Replace("$", sugg);
            context.Raw(json);            
        }

        public static readonly IHttpHandler IndexHandler = new HttpHandlerWithReadOnlySession(WithNHSession(Index));

        public static Context InitialContext(HttpRequestBase request) {
            return new Context {
                Version = Setup.AssemblyDate.Ticks.ToString(),
                Url = request.RawUrl.Split('?')[0],
                LimitLength = string.IsNullOrEmpty(request.QueryString["limitLength"]),
                Raw = !string.IsNullOrEmpty(request.QueryString["raw"]),
                ImageFields = (request.QueryString["image"] ?? "").Split(','),
                ContentType = request.QueryString["contentType"],
                Output = request.QueryString["output"],
                ExtraRowTemplate = request["extraRowTemplate"],
            };
        }

        public static void Index(HttpContextBase context, ISession session, Configuration cfg) {
            var model = InitialContext(context.Request);
            try {
                model.MaxResults = TryParse(context.Request["MaxResults"]);
                model.FirstResult = TryParse(context.Request["FirstResult"]);
                model.Query = context.Request["q"];
                model.QueryType = GetQueryType(context.Request["type"]);
                ExecQuery(model, cfg, session, context.Request.RawUrl);
                model.NextPageUrl = BuildNextPageUrl(model, context.Request.RawUrl);
                model.PrevPageUrl = BuildPrevPageUrl(model, context.Request.RawUrl);
                model.FirstPageUrl = BuildFirstPageUrl(model, context.Request.RawUrl);
                model.AllEntities = GetAllEntities(cfg)
                    .OrderBy(e => e)
                    .Select(e => KV(e, BuildEntityUrl(e, context.Request.RawUrl)))
                    .ToList();
                model.RssUrl = BuildRssUrl(model, context.Request.RawUrl);
            } catch (HibernateException e) {
                model.Error = e.ToString();
            }
            if (model.Raw) {
                context.Raw(model.Error ?? model.RawResult, model.ContentType);
            } else {
                var v = GetView(model);
                context.XDocument(v, model.ContentType);
            }            
        }

        public static IEnumerable<string> GetAllEntities(Configuration Cfg) {
            return Cfg.ClassMappings.Select(c => c.EntityName);
        }

        public static bool HasPrevPage(Context model) {
            return !(!model.MaxResults.HasValue || !model.FirstResult.HasValue || model.FirstResult.Value <= 0);
        }

        public static string BuildFirstPageUrl(Context model, string RawUrl) {
            if (!HasPrevPage(model))
                return null;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"FirstResult", 0},
            });
        }

        public static string BuildPrevPageUrl(Context model, string RawUrl) {
            if (!HasPrevPage(model))
                return null;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"FirstResult", Math.Max(0, model.FirstResult.Value-model.MaxResults.Value)},
            });
        }

        public static string BuildNextPageUrl(Context model, string RawUrl) {
            if (!model.MaxResults.HasValue || model.Total <= model.MaxResults)
                return null;
            var first = model.FirstResult ?? 0;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"FirstResult", first + model.MaxResults.Value},
            });
        }

        public static XDocument GetView(Context model) {
            if (model.Output != null && model.Output.ToLowerInvariant() == "rss")
                return new XDocument(Views.Views.RSS(model));
            return Views.Views.Index(model).MakeHTML5Doc();
        }

        public static string BuildRssUrl(Context model, string RawUrl) {
            if (string.IsNullOrEmpty(model.Query) || updateRx.IsMatch(model.Query))
                return null;
            return UrlHelper.SetParameters(RawUrl, new Dictionary<string, object> {
                {"contentType", "application/rss+xml"},
                {"output", "RSS"},
            });
        }

        public static string BuildEntityUrl(string entityName, string RawUrl) {
            var hql = HttpUtility.UrlEncode("from " + entityName);
            return string.Format("{0}?q={1}&MaxResults=10", RawUrl.Split('?')[0], hql);
        }

        public static void ExecQuery(Context model, Configuration Cfg, ISession session, string rawUrl) {
            if (Cfg == null)
                throw new ApplicationException("NHibernate configuration not supplied");
            if (string.IsNullOrEmpty(model.Query))
                return;
            var q = CreateQuery(model, session);
            if (model.MaxResults.HasValue)
                q.SetMaxResults(model.MaxResults.Value + 1);
            if (model.FirstResult.HasValue)
                q.SetFirstResult(model.FirstResult.Value);
            ExecQueryByType(q, model, Cfg, rawUrl);
        }

        private static readonly Regex updateRx = new Regex(@"^\s*(insert|update|delete)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void ExecQueryByType(IQuery q, Context model, Configuration cfg, string rawUrl) {
            if (!updateRx.IsMatch(model.Query)) {
                if (model.Raw)
                    model.RawResult = q.UniqueResult();
                else {
                    var results = q.List();
                    model.Total = results.Count;
                    model.Results = ConvertResults(results, model, cfg, rawUrl).ToList();
                }
            } else {
                var count = q.ExecuteUpdate();
                model.Results = new List<Row> {
                    new Row {
                        KV("count", new[] {X.T(count.ToString())} ),
                    },
                };
            }
        }

        public static IEnumerable<Row> ConvertResults(IList results, Context model, Configuration cfg, string rawUrl) {
            var r = results.Cast<object>()
                .Select(x => ConvertResult(x, model, cfg, rawUrl));
            if (model.MaxResults.HasValue)
                r = r.Take(model.MaxResults.Value);
            return r;
        }

        public static Row ConvertResult(object o, Context model, Configuration Cfg, string rawUrl) {
            var row = new Row();
            var trueType = NHibernateProxyHelper.GetClassWithoutInitializingProxy(o);
            var mapping = Cfg.GetClassMapping(trueType);
            row.Add(KV("Type", new[] { BuildTypeLink(trueType, Cfg, rawUrl) }));
            if (mapping == null) {
                // not a mapped type
                if (o is object[]) {
                    row.AddRange(ConvertObjectArray((object[])o, model, Cfg, rawUrl));
                } else {
                    row.Add(KV("Value", new[] { X.T(Convert.ToString(o)) }));
                }
            } else {
                var idProp = mapping.IdentifierProperty;
                var id = idProp.GetGetter(trueType).Get(o);
                row.Add(KV(idProp.Name, new[] { X.T(Convert.ToString(id)) }));
                row.AddRange(mapping.PropertyClosureIterator
                               .SelectMany(p => ConvertProperty(o, trueType, p, model, Cfg, rawUrl)));
            }
            return row;
        }

        public static IGetter GetPkGetter(Type entityType, Configuration Cfg) {
            return Cfg.GetClassMapping(entityType).IdentifierProperty.GetGetter(entityType);
        }

        public static object GetPkValue(Type entityType, object o, Configuration cfg) {
            return GetPkGetter(entityType, cfg).Get(o);
        }

        public static KeyValuePair<string, XElement> ConvertCollection(object o, Type entityType, Property p, Configuration cfg, string rawUrl) {
            var getter = p.GetGetter(entityType);
            var fkType = getter.ReturnType.GetGenericArguments()[0];
            var fk = GetPkValue(entityType, o, cfg);
            return KV(p.Name, BuildCollectionLink(fkType, entityType, fk, cfg, rawUrl));
        }

        public static XElement BuildCollectionLink(Type ct, Type fk, object fkValue, Configuration Cfg, string RawUrl) {
            var classMapping = Cfg.GetClassMapping(ct);
            var associations = classMapping.PropertyClosureIterator.Where(p => p.Type.IsAssociationType);
            var fkp = associations.FirstOrDefault(p => p.GetGetter(ct).ReturnType == fk);
            if (fkp != null) {
                var hql = string.Format("from {0} x where x.{1} = '{2}'", classMapping.EntityName, fkp.Name, fkValue);
                var url = string.Format("{0}?q={1}&MaxResults=10", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
                return Views.Views.Link(url, "collection");
            }
            // try many-to-many
            var collection = associations.FirstOrDefault(p => IsCollectionOf(p.GetGetter(ct).ReturnType, fk));
            if (collection != null) {
                // assume generic collection
                var fkType = collection.GetGetter(ct).ReturnType.GetGenericArguments()[0];
                var fkTypePK = GetPkGetter(fkType, Cfg).PropertyName;
                var hql = string.Format("select x from {0} x join x.{1} y where y.{2} = '{3}'", classMapping.EntityName, collection.Name, fkTypePK, fkValue);
                var url = string.Format("{0}?q={1}&MaxResults=10", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
                return Views.Views.Link(url, "collection");
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

        public static XNode[] NonNullArray(XNode e) {
            if (e == null)
                return new XNode[0];
            return new[] { e };
        }

        public static KeyValuePair<string, XNode> ConvertEntity(object o, Type entityType, Property p, Configuration Cfg, string rawUrl) {
            var assocType = (EntityType)p.Type;
            var o1 = p.GetGetter(entityType).Get(o);
            if (o1 == null)
                return KV(p.Name, null as XNode);
            var mapping = Cfg.GetClassMapping(assocType.GetAssociatedEntityName());
            var pk = GetPkValue(mapping.MappedClass, o1, Cfg);
            var getter = p.GetGetter(entityType);
            return KV(p.Name, BuildEntityLink(getter.ReturnType, pk, Cfg, rawUrl) as XNode);
        }

        public static XElement BuildEntityLink(Type entityType, object pkValue, Configuration Cfg, string RawUrl) {
            var hql = string.Format("from {0} x where x.{1} = '{2}'", Cfg.GetClassMapping(entityType).EntityName, GetPkGetter(entityType, Cfg).PropertyName, pkValue);
            var url = string.Format("{0}?q={1}", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
            var text = string.Format("{0}#{1}", entityType.Name, pkValue);
            return Views.Views.Link(url, text);
        }

        public static IEnumerable<KeyValuePair<string, XNode[]>> ConvertProperty(object o, Type entityType, Property p, Context model, Configuration cfg, string rawUrl) {
            if (p.Type.IsCollectionType) {
                var c = ConvertCollection(o, entityType, p, cfg, rawUrl);
                yield return KV(c.Key, NonNullArray(c.Value));
                yield break;
            }
            if (p.Type.IsEntityType) {
                var c = ConvertEntity(o, entityType, p, cfg, rawUrl);
                yield return KV(c.Key, NonNullArray(c.Value));
                yield break;
            }
            var getter = p.GetGetter(entityType);
            var value = getter.Get(o);
            if (p.Type.IsComponentType) {
                foreach (var r in ConvertComponent(value, p, p.Name))
                    yield return KV(r.Key, NonNullArray(r.Value));
                yield break;
            }
            var container = X.E("x", ConvertPropertyValue(value, p, model, o, entityType, cfg, rawUrl, 100));
            yield return KV(p.Name, container.Nodes().ToArray());
        }

        public static string QueryScalar(Property p, Type entityType, object o, Configuration cfg) {
            return string.Format("select x.{0} from {1} x where x.{2} = '{3}'", p.Name, entityType.FullName, GetPkGetter(entityType, cfg).PropertyName, GetPkValue(entityType, o, cfg));
        }

        public static IEnumerable<XNode> ConvertPropertyValue(object value, Property p, Context model, object obj, Type entityType, Configuration cfg, string RawUrl, int maxLen) {
            if (model.ImageFields.Contains(p.Name)) {
                var query = QueryScalar(p, entityType, obj, cfg);
                var imgUrl = string.Format("{0}?raw=1&q={1}", RawUrl.Split('?')[0], HttpUtility.UrlEncode(query));
                yield return Views.Views.Img(imgUrl);
            } else if (model.LimitLength && value != null && Convert.ToString(value).Length > maxLen) {
                yield return new XText(Convert.ToString(value).Substring(0, maxLen));
                var query = QueryScalar(p, entityType, obj, cfg);
                var url = string.Format("{0}?q={1}&limitLength=0", RawUrl.Split('?')[0], HttpUtility.UrlEncode(query));
                yield return Views.Views.Link(url, "...");
            } else if (value != null) {
                yield return new XText(Convert.ToString(value));
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
                    var url = string.Format("{0}?{1}", urlParts[0], UrlHelper.DictToQuerystring(qs));
                    yield return Views.Views.Link(url, "(as image)");
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, XNode>> ConvertComponent(object o, Property p, string name) {
            if (o == null)
                return new[] { KV(name, null as XNode), };
            var compType = (ComponentType)p.Type;
            var t = o.GetType();
            return from propName in compType.PropertyNames
                   let prop = t.GetProperty(propName)
                   let v = prop.GetValue(o, null)
                   let k = string.Format("{0}.{1}", name, propName)
                   select KV(k, v == null ? null : X.T(v.ToString()));
        }

        /// <summary>
        /// Converts an array of unmapped objects
        /// </summary>
        /// <param name="o"></param>
        /// <param name="model"></param>
        /// <param name="cfg"></param>
        /// <param name="rawUrl"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, XNode[]>> ConvertObjectArray(object[] o, Context model, Configuration cfg, string rawUrl) {
            return o.SelectMany((x, i) => ConvertResult(x, model, cfg, rawUrl)
                .Select(k => KV(string.Format("{0}[{1}]", k.Key, i), k.Value)));
        }

        public static XNode BuildTypeLink(Type entityType, Configuration Cfg, string RawUrl) {
            var mapping = Cfg.GetClassMapping(entityType);
            if (mapping == null)
                return new XText(entityType.Name);
            var hql = string.Format("from {0}", mapping.EntityName);
            var url = string.Format("{0}?q={1}&MaxResults=10", RawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
            return Views.Views.Link(url, entityType.Name);
        }

        public static KeyValuePair<K, V> KV<K, V>(K key, V value) {
            return new KeyValuePair<K, V>(key, value);
        }

        public static QueryType GetQueryType(string s) {
            if (string.IsNullOrEmpty(s))
                return QueryType.HQL;
            return (QueryType)Enum.Parse(typeof(QueryType), s, true);
        }

        public static IQuery CreateQuery(Context model, ISession Session) {
            if (model.QueryType == QueryType.HQL)
                return Session.CreateQuery(model.Query);
            return Session.CreateSQLQuery(model.Query);
        }

        public static int? TryParse(string s) {
            if (string.IsNullOrEmpty(s))
                return null;
            int r;
            if (int.TryParse(s.Trim(), out r))
                return r;
            return null;
        }

    }
}
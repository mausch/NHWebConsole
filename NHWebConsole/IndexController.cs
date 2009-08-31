using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private Configuration cfg = NHibernateFunctions.Configuration();

        public Configuration Cfg {
            get { return cfg; }
            set { cfg = value; }
        }

        public string RawUrl {
            get { return rawUrl; }
            set { rawUrl = value; }
        }

        public override object Execute(HttpContextBase context) {
            rawUrl = context.Request.RawUrl;
            var model = new ViewModel {
                Url = rawUrl,
            };
            try {
                model.MaxResults = TryParse(context.Request["MaxResults"]);
                model.FirstResult = TryParse(context.Request["FirstResult"]);
                model.Hql = context.Request["hql"];
                model.Results = ExecQuery(model);
                model.NextPageUrl = BuildNextPageUrl(model);
                model.PrevPageUrl = BuildPrevPageUrl(model);
            } catch (HibernateException e) {
                model.Error = e.ToString();
            }
            return model;
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
            int r;
            if (int.TryParse(s, out r))
                return r;
            return null;
        }

        public ICollection<ICollection<KeyValuePair<string, string>>> ExecQuery(ViewModel model) {
            if (string.IsNullOrEmpty(model.Hql))
                return null;
            var q = Session.CreateQuery(model.Hql);
            if (model.MaxResults.HasValue)
                q.SetMaxResults(model.MaxResults.Value);
            if (model.FirstResult.HasValue)
                q.SetFirstResult(model.FirstResult.Value);
            var results = q.List();
            return ConvertResults(results);
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
            return o.Select((x,i) => KV(string.Format("[{0}]", i), Convert.ToString(x)));
        }

        public string BuildCollectionLink(Type ct, Type fk, object fkValue) {
            var fkp = cfg.GetClassMapping(ct).PropertyIterator
                .FirstOrDefault(p => p.Type.IsAssociationType && p.GetGetter(ct).ReturnType == fk);
            if (fkp == null)
                return null;
            var hql = string.Format("from {0} x where x.{1} = {2}", ct.Name, fkp.Name, fkValue);
            return string.Format("<a href='{0}?hql={1}'>collection</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql));
        }

        public string BuildEntityLink(Type entityType, object pkValue) {
            var hql = string.Format("from {0} x where x.{1} = {2}", entityType.Name, GetPkGetter(entityType).PropertyName, pkValue);
            return string.Format("<a href='{0}?hql={1}'>{2}#{3}</a>", rawUrl.Split('?')[0], HttpUtility.UrlEncode(hql), entityType.Name, pkValue);
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
                var pk = GetPkValue(mapping.MappedClass, p.GetGetter(entityType).Get(o));
                return KV(p.Name, BuildEntityLink(getter.ReturnType, pk));
            }
            return KV(p.Name, Convert.ToString(value));
        }

        public ICollection<ICollection<KeyValuePair<string, string>>> ConvertResults(IList results) {
            return results.Cast<object>().Select(x => ConvertResult(x)).ToList();
        }
    }
}
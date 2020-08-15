using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;

namespace WWW.Helpers
{
    public static class RouteUtils
    {
        public static RouteValueDictionary ToRouteValues(this NameValueCollection queryString)
        {
            if (queryString == null || queryString.HasKeys() == false) return new RouteValueDictionary();

            var routeValues = new RouteValueDictionary();
            foreach (string key in queryString.AllKeys)
                routeValues.Add(key, queryString[key]);

            return routeValues;
        }

        public static RouteValueDictionary Extend(this RouteValueDictionary dest,
            IEnumerable<KeyValuePair<string, object>> src)
        {
            src.ToList().ForEach(x => { dest[x.Key] = x.Value; });
            return dest;
        }
        public static RouteValueDictionary ToRouteValues(this NameValueCollection col, Object obj)
        {
            var values = new RouteValueDictionary(obj);
            if (col != null)
            {
                foreach (string key in col)
                {
                    //values passed in object override those already in collection
                    if (key != null && !values.ContainsKey(key)) values[key] = col[key];
                }
            }
            return values;
        }

    }
    public class SeoFriendlyRoute : Route
    {
        public SeoFriendlyRoute(string url, RouteValueDictionary defaults, IRouteHandler routeHandler) : base(url, defaults, routeHandler)
        {
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            var routeData = base.GetRouteData(httpContext);

            if (routeData != null)
            {
                if (routeData.Values.ContainsKey("id"))
                    routeData.Values["id"] = GetIdValue(routeData.Values["id"]);
            }

            return routeData;
        }

        private object GetIdValue(object id)
        {
            if (id != null)
            {
                string idValue = id.ToString();
                
                //var regex = new Regex(@"^(?<id>\d+).*$");
                var regex = new Regex(@"^.*-(?<id>\d+)$");
                var match = regex.Match(idValue);

                if (match.Success)
                {
                    return match.Groups["id"].Value;
                }
            }

            return id;
        }
    }
}
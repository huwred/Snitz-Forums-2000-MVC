using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WWW.Filters
{
    public class RedirectAspPermanentRoute : RouteBase
    {
        private readonly IDictionary<string, object> urlMap;

        public RedirectAspPermanentRoute(IDictionary<string, object> urlMap)
        {
            this.urlMap = urlMap ?? throw new ArgumentNullException("urlMap");
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            var path = httpContext.Request.Path.ToLower();
            if (path.EndsWith(".asp"))
            {
                if (!urlMap.ContainsKey(path))
                    return null;

                var routeValues = urlMap[path];
                var routeData = new RouteData(this, new MvcRouteHandler());

                routeData.Values["controller"] = "Error";
                routeData.Values["action"] = "Status301";
                routeData.DataTokens["routeValues"] = routeValues;

                return routeData;
            }

            return null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }
    }
}
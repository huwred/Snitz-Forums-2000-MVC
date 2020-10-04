using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SnitzConfig;
using WWW.Filters;
using WWW.Helpers;

namespace WWW
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{filename}.ashx");
            routes.IgnoreRoute("{filename}.aspx");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
            #region Uncomment to protect Content folder
            routes.IgnoreRoute("Content/{*relpath}");
            routes.IgnoreRoute("Scripts/{*relpath}");
            routes.RouteExistingFiles = true;
            routes.MapRoute(
                name: "protectedphoto",
                url: "ProtectedContent/{*path}",
                defaults: new { controller = "Common", action = "Content" }
            );
            #endregion
            routes.Add(new RedirectAspPermanentRoute(
                new Dictionary<string, object>()
                {
                    { @"/active.asp", new { controller = "Topic", action = "Active" } },
                    { @"/topic.asp", new { controller = "Topic", action = "Posts" }  },
                    { @"/forum.asp", new { controller = "Forum", action = "Posts" }  },
                    { @"/pop_profile.asp", new { controller = "Account", action = "UserProfile" }  },
                    { @"/pop_slideshow.asp", new { controller = "PhotoAlbum", action = "Album" }  }
                })
            );
            routes.MapRoute(
                name: "Search",
                url: "Search/{id}",
                defaults: new
                {
                    controller = "Search",
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );
            routes.MapRoute(
                name: "Public",
                url: "Public/{viewName}",
                defaults: new
                {
                    controller = "Home",
                    action = "Public"
                }
            );
            routes.MapRoute(
                name: "DeleteFile",
                url: "Explorer/DeleteFile",
                defaults: new
                {
                    controller = "Explorer",
                    action = "DeleteFile"
                }
            );
            routes.MapRoute(
                name: "UploadFile",
                url: "Explorer/UploadFile",
                defaults: new
                {
                    controller = "Explorer",
                    action = "UploadFile"
                }
            );
            routes.MapRoute(
                name: "Explorer",
                url: "Explorer/{*path}",
                defaults: new
                {
                    controller = "Explorer",
                    action = "Index",
                    path = UrlParameter.Optional
                }
            );
            routes.MapRoute(
                name: "MyView",
                url: "Forum/MyView",
                defaults: new
                {
                    controller = "Forum",
                    action = "MyView"
                }
            );
            routes.MapRoute(
                name: "ForumSearch",
                url: "Forum/Search",
                defaults: new
                {
                    controller = "Forum",
                    action = "Search"
                }
            );

            routes.MapRoute(
                name: "Active",
                url: "Topic/Active",
                defaults: new
                {
                    controller = "Topic",
                    action = "Active"
                }
            );
            routes.MapRoute(
                name: "Moderation",
                url: "{controller}/ModeratePost",
                defaults: new
                {
                    action = "ModeratePost"
                }
            );
            routes.MapRoute(
                name: "PostPreview",
                url: "Forum/ProcessPreview",
                defaults: new
                {
                    controller = "Forum",
                    action = "ProcessPreview"
                }
            );
            routes.MapRoute(
                name: "About",
                url: "{controller}/About",
                defaults: new
                {
                    controller = "Home",
                    action = "About"
                }
            );
            routes.MapRoute(
                name: "Privacy",
                url: "{controller}/Privacy",
                defaults: new
                {
                    controller = "Home",
                    action = "Privacy"
                }
            );
            routes.MapRoute(
                name: "License",
                url: "{controller}/License",
                defaults: new
                {
                    controller = "Home",
                    action = "License"
                }
            );

            //routes.MapRoute(
            //    name: "TopicActions",
            //    url: "Topic/{action}/{id}",
            //    defaults: new {controller = "Topic", action = "Index", id = UrlParameter.Optional },
            //    constraints: new {action = new NotEqual(new[] {"Posts","Index"})}
            //); 

            //routes.Add("CategoryDetails", new SeoFriendlyRoute("Category/{action}/{slug}_{id}",
            //    new RouteValueDictionary(new { controller = "Category", action = "Index" }),
            //    new MvcRouteHandler()));
            //routes.Add("ForumDetails", new SeoFriendlyRoute("Forum/{action}/{slug}_{id}",
            //    new RouteValueDictionary(new { controller = "Forum", action = "Posts" }),
            //    new MvcRouteHandler()));
            //routes.Add("TopicDetails", new SeoFriendlyRoute("Topic/{action}/{slug}_{id}",
            //    new RouteValueDictionary(new { controller = "Topic", action = "Posts" }),
            //    new MvcRouteHandler()));

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults:
                Config.RunSetup
                    ? new { controller = "Setup", action = "Index", id = UrlParameter.Optional }
                    : new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );  
        }
    }
    public class NotEqual : IRouteConstraint
    {
        private string[] _match;

        public NotEqual(string[] match)
        {
            _match = match;
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var match = false;
            foreach (var m in _match)
            {
                match = match || String.Compare(values[parameterName].ToString(), m, StringComparison.OrdinalIgnoreCase) != 0;
            }
            return match;
        }
    }
}

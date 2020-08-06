// /*
// ####################################################################################################################
// ##
// ## Global.asax
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		19/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using Hangfire;
using LangResources.Utility;
using log4net;
using SnitzConfig;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;
using SnitzMembership;
using WWW.Controllers;

namespace WWW
{
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //we add the filter to handle "offline for maintenance"
            GlobalFilters.Filters.Add(new OfflineActionFilter());

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            //GlobalConfiguration.Configure(WebApiConfig.Register);

            RouteConfig.RegisterRoutes(RouteTable.Routes);

            BundleConfig.RegisterBundles(BundleTable.Bundles);
            if (!Config.RunSetup)
            {
                //AuthConfig.RegisterAuth();
                WebSecurity.Register();
            }

            DataAnnotationsModelValidatorProvider.RegisterAdapter(
               typeof(RequiredIfAttribute),
               typeof(RequiredIfValidator));
            DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(StringLength), typeof(StringLengthAttributeAdapter));
            ModelMetadataProviders.Current = new IgnoreValidationModelMetaDataProvider();

            ViewEngines.Engines.Clear();

            ViewEngines.Engines.Add(new RazorViewEngine());

            var logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            logger.Info("Application started.");
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            //Context.Items["loadstarttime"] = DateTime.Now;
            if (Config.RunSetup)
            {
                return;
            }
            // Extract the forms authentication cookie
            string cookieName = FormsAuthentication.FormsCookieName;
            HttpCookie authCookie = Context.Request.Cookies[cookieName];
            if (null == authCookie)
            {
                string user = null;
                string pword = null;

                var cookie = SnitzCookie.GetMultipleUsingSingleKeyCookies(Config.UniqueId + "User");
                foreach (KeyValuePair<string, string> pair in cookie)
                {
                    if (pair.Key == "Name")
                    {
                        user = pair.Value;
                    }
                    if (pair.Key == "Pword")
                    {
                        pword = pair.Value;
                    }
                }

                if (!String.IsNullOrWhiteSpace(user) && !String.IsNullOrWhiteSpace(pword))
                {
                    FormsAuthentication.SetAuthCookie(user, true);
                    if (!Config.AnonymousMembers.Contains(user))
                        OnlineUsersInstance.OnlineUsers.SetUserOnline(user);
                }

            }

        }

        protected void Application_EndRequest(Object sender, EventArgs e)
        {
            //DateTime end = (DateTime) Context.Items["loadstarttime"];
            //TimeSpan loadtime = DateTime.Now - end;
            //Response.Write("<h4> loaded in" + loadtime + " ms</h4>");

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var httpContext = ((Global)sender).Context;
            var currentController = " ";
            var currentAction = " ";
            var currentRouteData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(httpContext));

            if (currentRouteData != null)
            {
                if (currentRouteData.Values["controller"] != null && !String.IsNullOrEmpty(currentRouteData.Values["controller"].ToString()))
                {
                    currentController = currentRouteData.Values["controller"].ToString();
                }

                if (currentRouteData.Values["action"] != null && !String.IsNullOrEmpty(currentRouteData.Values["action"].ToString()))
                {
                    currentAction = currentRouteData.Values["action"].ToString();
                }
            }

            Exception exception = Server.GetLastError();
            // Log the exception.

            //var logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            //logger.Error("App_Error", exception);

            var routeData = new RouteData();
            var action = "Index";


            if (exception is HttpException)
            {
                var httpException = exception as HttpException;

                switch (httpException.GetHttpCode())
                {
                    case 401:
                    case 403:
                        // Page not found.
                        action = "Forbidden";
                        break;
                    case 404:
                        // Page not found.
                        action = "NotFound";
                        break;
                    case 500:
                        // Server error.
                        action = "Index";
                        break;

                    // Here you can handle Views to other error codes.
                    // I choose a General error template  
                    default:
                        action = "Index";
                        break;
                }
            }
            httpContext.ClearError();
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = exception is HttpException ? ((HttpException)exception).GetHttpCode() : 500;
            httpContext.Response.TrySkipIisCustomErrors = true;

            // Pass exception details to the target error View.
            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = action;
            routeData.Values["exception"] = new HandleErrorInfo(exception, currentController, currentAction);


            // Call target Controller and pass the routeData.
            IController errormanagerController = new ErrorController();
            HttpContextWrapper wrapper = new HttpContextWrapper(httpContext);
            var rc = new RequestContext(wrapper, routeData);
            errormanagerController.Execute(rc);

        }

        protected void Session_Start(Object sender, EventArgs e)
        {
            HttpContext.Current.Session.Add("__MyAppSession", string.Empty);
            try
            {
                #region No Hangfire Recycle AppPool

                if (User.Identity.IsAuthenticated)
                {
                    var serverList = JobStorage.Current.GetMonitoringApi().Servers();
                    if (serverList.Count == 0)
                    {
                        HttpRuntime.UnloadAppDomain();
                    }
                }

                #endregion

            }
            catch (Exception)
            {
                // ignored
            }
        }
        void Session_End(object sender, EventArgs e)
        {
            if (!Config.RunSetup)
                OnlineUsersInstance.OnlineUsers.UpdateForUserLeave();
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            //It's important to check whether session object is ready
            if (HttpContext.Current.Session != null)
            {
                CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
                //Checking first if there is no value in session 
                //and set default language 
                //this can happen for first user's request
                string langName = "en";
                if (ci == null)
                {
                    //Sets default culture to english invariant
                    var check = SnitzCookie.GetCookieValue("SnitzCulture");
                    //Try to get values from Accept lang HTTP header
                    if (check != null)
                    {
                        langName = check;
                    }
                    else if (HttpContext.Current.Request.UserLanguages != null &&
                             HttpContext.Current.Request.UserLanguages.Length != 0)
                    {
                        //Gets accepted list 
                        CultureInfo[] cultures = new CultureInfo[HttpContext.Current.Request.UserLanguages.Length + 1];
                        if (Request.UserLanguages != null)
                            for (int ctr = Request.UserLanguages.GetLowerBound(0);
                                ctr <= Request.UserLanguages.GetUpperBound(0);
                                ctr++)
                            {
                                langName = Request.UserLanguages[ctr];
                                if (!string.IsNullOrEmpty(langName))
                                {

                                    // Remove quality specifier, if present.
                                    if (langName.Contains(";"))
                                        langName = langName.Substring(0, langName.IndexOf(';'));
                                    try
                                    {
                                        cultures[ctr] = new CultureInfo(langName, false);
                                    }
                                    catch (Exception)
                                    {
                                        // ignored

                                    }
                                }
                                else
                                {
                                    cultures[ctr] = CultureInfo.CurrentCulture;
                                }
                            }

                        langName = cultures[0].TwoLetterISOLanguageName;
                        if (langName == "nn" || langName == "nb")
                        {
                            langName = "no";
                        }

                    }
                    //check if language supported
                    if (!Config.RunSetup)
                        langName = ResourceManager.IsSupported(langName);
                    //override if there is a cookie
                    ci = new CultureInfo(langName, false);
                    SnitzCookie.SetCookie("SnitzCulture", langName);
                    SessionData.Set("Culture", ci);

                }
                else
                {
                    langName = ci.TwoLetterISOLanguageName;
                }
                //Finally setting culture for each request
                Thread.CurrentThread.CurrentUICulture = ci;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(ci.Name);

                string[] rtlLangs = new string[] { "ar", "arc", "dv", "fa", "ha", "khw", "ks", "ku", "ps", "ur", "yi", "he" };
                //string[] arabic = new string[] { "ar", "arc", "dv", "fa", "ha", "khw", "ks", "ku", "ps", "ur" }; //Cairo
                //string[] aramaic = new string[] { "yi", "he" }; //Tinos

                bool isRighToLeft = (rtlLangs.Contains(langName));

                if (!SessionData.Contains("isRighToLeft") || isRighToLeft != SessionData.Get<bool>("isRighToLeft"))
                {
                    SessionData.Set("isRighToLeft", isRighToLeft);
                }
            }
        }

        protected void Application_PostAuthorizeRequest()
        {
            //if (IsWebApiRequest())
            //{
            //    HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
            //}
        }

        private bool IsWebApiRequest()
        {
            return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath != null && HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/api");
        }

        public override string GetVaryByCustomString(HttpContext context, string arg)
        {
            var lang = SnitzCookie.GetCookieValue("SnitzCulture");
            return arg.ToLower() == "snitzculture" ? lang : string.Empty;
        }
    }
}

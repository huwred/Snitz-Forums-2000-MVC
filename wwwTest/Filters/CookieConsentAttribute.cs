/*
 * ASP.NET ActionFilterAttribute to help implement EU Cookie-law 
 * MIT Licence (c) Maarten Sikkema, Macaw Nederland BV
 * 
 * Modified for Snitz Mvc by Huw Reddick 2017
 */

using System;
using System.Web;
using System.Web.Mvc;
using SnitzConfig;
using SnitzDataModel.Models;

namespace WWW.Filters
{
    public class CookieConsentAttribute : ActionFilterAttribute
    {
        public const string ConsentCookieName = "CookieConsent";
        public bool ConsentRequired = ClassicConfig.GetIntValue("INTREQUIRECONSENT") == 1;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var viewBag = filterContext.Controller.ViewBag;
            viewBag.AskCookieConsent = ConsentRequired;
            viewBag.HasCookieConsent = !ConsentRequired;

            var request = filterContext.HttpContext.Request;
            // do we require consent ??
            if (ConsentRequired)
            {
                // Check if the user has a conesent cookie
                var consentCookie = SnitzCookie.GetCookieValue(ConsentCookieName);// request.Cookies[ConsentCookieName];
                if (consentCookie == null )
                {
                    // No consent cookie. We first check the Do Not Track header value, this can have the value "0" or "1"
                    string dnt = request.Headers.Get("DNT");

                    // If we receive a DNT header, we accept its value and do not ask the user anymore
                    if (!String.IsNullOrEmpty(dnt))
                    {
                        viewBag.AskCookieConsent = false;
                        if (dnt == "0")
                        {
                            viewBag.HasCookieConsent = true;
                        }
                    }
                    else
                    {
                        if (IsSearchCrawler(request.Headers.Get("User-Agent")))
                        {
                            // don't ask consent from search engines, also don't set cookies
                            viewBag.AskCookieConsent = false;
                        }
                        else
                        {
                            // first request on the site and no DNT header. 
                            CookieConsent.SetTempCookieConsent(filterContext.HttpContext.Response, "asked");
                        }
                    }
                }
                else
                {
                    // we received a consent cookie
                    viewBag.AskCookieConsent = false;
                    if (consentCookie == "asked")
                    {
                        // consent is implicitly given
                        CookieConsent.SetCookieConsent(filterContext.HttpContext.Response, true);
                        
                        viewBag.HasCookieConsent = true;
                    }
                    else if (consentCookie == "true")
                    {
                        viewBag.HasCookieConsent = true;
                    }
                    else
                    {
                        // assume consent denied
                        viewBag.HasCookieConsent = false;
                    }
                }
            }
            base.OnActionExecuting(filterContext);
        }

        private bool IsSearchCrawler(string userAgent)
        {
            if (!String.IsNullOrEmpty(userAgent))
            {
                string[] crawlers = new string[] 
                { 
                    "Baiduspider", 
                    "Googlebot", 
                    "YandexBot", 
                    "YandexImages",
                    "bingbot", 
                    "msnbot", 
                    "Vagabondo", 
                    "SeznamBot",
                    "ia_archiver",
                    "AcoonBot",
                    "Yahoo! Slurp",
                    "AhrefsBot"
                };
                foreach (string crawler in crawlers)
                    if (userAgent.Contains(crawler))
                        return true;
            }
            return false;
        }
    }


    /// <summary>
    /// Helper class for easy/typesafe getting the cookie consent status
    /// </summary>
    public static class CookieConsent
    {
        public static void SetCookieConsent(HttpResponseBase response, bool consent)
        {
            var consentCookie = new HttpCookie(CookieConsentAttribute.ConsentCookieName)
            {
                Value = consent ? "true" : "false",
                Expires = DateTime.UtcNow.AddYears(1)
            };
            response.Cookies.Set(consentCookie);
        }
        public static void SetTempCookieConsent(HttpResponseBase response, string consent)
        {
            var consentCookie = new HttpCookie(CookieConsentAttribute.ConsentCookieName)
            {
                Value = consent,
                Expires = DateTime.UtcNow.AddMinutes(10)
            };
            response.Cookies.Set(consentCookie);
        }
        public static bool AskCookieConsent(ViewContext context)
        {
            return context.ViewBag.AskCookieConsent ?? false;
        }

        public static bool HasCookieConsent(ViewContext context)
        {
            return context.ViewBag.HasCookieConsent ?? false;
        }
    }

}

// /*
// ####################################################################################################################
// ##
// ## SnitzCookie
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
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
using System.Collections.Specialized;
using System.Web;
using Microsoft.Net.Http.Headers;
using SnitzConfig;

namespace SnitzDataModel.Models
{
    /// <summary>
    /// Snitz Cookie functions
    /// </summary>
    public static class SnitzCookie
    {
        private static readonly string SnitzUniqueId = Config.UniqueId;

        private static List<string> _cookieCollection = new List<string>
        {
            "snitztheme", "SnitzCulture", "pmModPaging", SnitzUniqueId + "User",
            "ActiveRefresh", "SinceDate", "TopicTracking", "GROUP", "RefreshFilter",
            "timezoneoffset", "preservedurl", "User"
        };

        public static void LogOut()
        {
            Clear();
        }

        public static void Clear()
        {
            foreach (string s in _cookieCollection)
            {
                ExpireCookie(s);
            }
        }

        public static void ClearAll()
        {
            string[] myCookies = GetHttpRequest().Cookies.AllKeys;
            foreach (string cookie in myCookies)
            {
                ExpireCookie(cookie);
            }
        }
        #region LastVisitDate
        public static string GetLastVisitDate()
        {
            return GetCookieValue("LastVisit");
        }

        public static void SetLastVisitCookie(string value)
        {
            string name = "LastVisit";
            HttpCookie cookie = GetHttpRequest().Cookies.Get(name) ?? new HttpCookie(name);
            cookie.Value = value;
            cookie.Secure = true;

            cookie.HttpOnly = false;
            cookie.Path = Config.CookiePath ?? ClassicConfig.CookiePath;
            cookie.Expires = DateTime.UtcNow.AddMonths(2);
            cookie.Domain = GetHttpRequest().Url.Host;
            GetHttpResponse().Cookies.Add(cookie);
        }
        #endregion

        #region Poll vote tracker
        public static void PollVote(int pollid)
        {
            Dictionary<string, string> pages = GetMultipleUsingSingleKeyCookies("votetracker");

            if (pages.ContainsKey(pollid.ToString()))
            {
                int lastpage = Convert.ToInt32(pages[pollid.ToString()]);
                pages[pollid.ToString()] = "1";
            }
            else
            {
                pages.Add(pollid.ToString(), "1");
            }
            SetMultipleUsingSingleKeyCookies("votetracker", pages);
        }
        public static bool HasVoted(int pollid)
        {
            var pages = GetMultipleUsingSingleKeyCookies("votetracker");
            if (pages.ContainsKey(pollid.ToString()))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region Topic read tracker
        public static void ClearTracking(string topicId)
        {
            Dictionary<string, string> cookie = GetMultipleUsingSingleKeyCookies("TopicTracking");
            if (cookie.ContainsKey(topicId))
            {
                cookie.Remove(topicId);
                SetMultipleUsingSingleKeyCookies("TopicTracking", cookie, true);
            }

            //SetMultipleUsingSingleKeyCookies("TopicTracking", cookie, true);
        }

        public static void UpdateTopicTrack(string topicId)
        {
            Dictionary<string, string> cookie = GetMultipleUsingSingleKeyCookies("TopicTracking");
            if (!cookie.ContainsKey(topicId))
            {
                cookie.Add(topicId, DateTime.UtcNow.ToString("ddHHmmss"));
            }
            else
            {
                cookie[topicId] = DateTime.UtcNow.ToString("ddHHmmss");
            }

            SetMultipleUsingSingleKeyCookies("TopicTracking", cookie, true);
        }

        public static string Tracked(string topicId)
        {
            var cookie = GetMultipleUsingSingleKeyCookies("TopicTracking");
            string lastvisit;
            if (!cookie.TryGetValue(topicId, out lastvisit))
                lastvisit = null;

            return lastvisit;

        }
        #endregion

        #region Active Topic cookies
        public static string GetActiveRefresh()
        {
            return GetCookieValue("ActiveRefreshSeconds");
        }

        public static void SetActiveRefresh(string value)
        {
            SetCookie("ActiveRefreshSeconds", value);
        }

        public static string GetTopicSince()
        {
            return GetCookieValue("SinceDateEnum");
        }

        public static void SetTopicSince(string value)
        {
            SetCookie("SinceDateEnum", value);
        }
        #endregion

        #region private methods


        private static HttpRequest GetHttpRequest()
        {
            return HttpContext.Current.Request;
        }


        private static HttpResponse GetHttpResponse()
        {
            return HttpContext.Current.Response;
        }

        public static void ClassicLogin(string user, string password)
        {
            Dictionary<string, string> logincookie = new Dictionary<string, string>();
            logincookie.Add("User", user);
            //logincookie.Add("Pword", Common.SHA256Hash(password));
            SetMultipleUsingSingleKeyCookies(SnitzUniqueId + "User", logincookie);
        }


        public static string GetCookieValue(string cookieKey)
        {
            var test = GetHttpRequest().Cookies.AllKeys;
            HttpCookie cookie = GetHttpRequest().Cookies.Get(cookieKey);
            return cookie != null ? cookie.Value : null;
        }


        public static void SetCookie(string name, string value, DateTime? expires = null)
        {
            if (!_cookieCollection.Contains(name))
            {
                _cookieCollection.Add(name);
            }
            HttpCookie cookie = GetHttpRequest().Cookies.Get(name) ?? new HttpCookie(name);
            // Set the secure flag, which Chrome's changes will require for Same
            cookie.Secure = true;
            cookie.HttpOnly = true;

// Add the SameSite attribute, this will emit the attribute with a value of none.
// To not emit the attribute at all set the SameSite property to -1.
            //cookie.SameSite = SameSiteMode.None;
            cookie.Value = value;
            cookie.Path = Config.CookiePath ?? ClassicConfig.CookiePath;
            cookie.Domain = GetHttpRequest().Url.Host;
            cookie.Expires = expires ?? DateTime.UtcNow.AddMonths(2);
            GetHttpResponse().Cookies.Add(cookie);
        }

        private static void ExpireCookie(string name)
        {

            if (GetHttpRequest().Cookies[name] != null)
            {
                HttpCookie myCookie = new HttpCookie(name) { Expires = DateTime.Now.AddDays(-30d) };
                GetHttpResponse().Cookies.Add(myCookie);
                GetHttpResponse().Cookies.Remove(name);
            }

        }

        public static Dictionary<string, string> GetMultipleUsingSingleKeyCookies(string cookieName)
        {

            //creating dic to return as collection.
            Dictionary<string, string> dicVal = new Dictionary<string, string>();


            //Check whether the cookie available or not.
            if (GetHttpRequest().Cookies[cookieName] != null)
            {

                //Creating a cookie.
                HttpCookie cookie = GetHttpRequest().Cookies[cookieName];

                if (cookie != null)
                {
                    //Getting multiple values from single cookie.
                    NameValueCollection nameValueCollection = cookie.Values;

                    //Iterate the unique keys.
                    foreach (string key in nameValueCollection.AllKeys)
                    {
                        if (key != null)
                            dicVal.Add(key, cookie[key]);
                    }
                }
            }
            return dicVal;
        }

        private static void SetMultipleUsingSingleKeyCookies(string cookieName, Dictionary<string, string> dic, bool persist = true)
        {
            HttpCookie cookie = GetHttpRequest().Cookies[cookieName];
            if (cookie == null)
            {
                cookie = new HttpCookie(cookieName);

                //This adds multiple cookies to the same key.
                foreach (KeyValuePair<string, string> val in dic)
                {
                    cookie[val.Key] = val.Value;
                }
                cookie.Secure = true;

                cookie.HttpOnly = true;
                cookie.Path = Config.CookiePath ?? ClassicConfig.CookiePath;
                cookie.Domain = GetHttpRequest().Url.Host;
                if (persist)
                    cookie.Expires = DateTime.UtcNow.AddDays(30);
                GetHttpResponse().Cookies.Add(cookie);
            }
            else
            {

                //This adds multiple cookies to the same key.
                foreach (KeyValuePair<string, string> val in dic)
                {
                    cookie[val.Key] = val.Value;
                }
                cookie.Secure = true;

                cookie.HttpOnly = false;
                cookie.Path = Config.CookiePath ?? ClassicConfig.CookiePath;
                cookie.Domain = GetHttpRequest().Url.Host;
                cookie.Expires = DateTime.UtcNow.AddDays(30);
                GetHttpResponse().Cookies.Add(cookie);
            }
        }

        #endregion

        public static string IsAuthenticated
        {
            get
            {
                string user = null;
                string pword = null;

                var cookie = GetMultipleUsingSingleKeyCookies(SnitzUniqueId + "User");
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
                    return user;
                }
                return null;
            }
        }


    }
}

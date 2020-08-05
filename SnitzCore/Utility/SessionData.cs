using System;
using System.Web;
using System.Web.SessionState;

namespace SnitzCore.Utility
{
    public static class SessionData
    {
        //Authenticated
        //IsAdministrator
        //IsModerator
        //ForumSubs
        //TopicSubs
        //CatSubs
        //_LastVisit
        //Culture

        const string ClientIdKey = "MyUserId";
        static HttpSessionState Session
        {
            get
            {
                if (HttpContext.Current == null)
                    throw new ApplicationException("No Http Context, No Session to Get!");

                return HttpContext.Current.Session;
            }
        }

        public static int MyUserId
        {
            get { return Get<int>(ClientIdKey) != 0 ? Get<int>(ClientIdKey) : 0; }
            set { Set(ClientIdKey, value); }
        }

        public static bool IsAuthenticated
        {
            get
            {
                if (!Contains("Authenticated"))
                {
                    Session.Add("Authenticated", HttpContext.Current.User.Identity.IsAuthenticated);
                }
                return Get<bool>("Authenticated");
                //return HttpContext.Current.Session["Authenticated"] != null ? (bool)HttpContext.Current.Session["Authenticated"] : false;
            }
            set { Set("Authenticated", value); }
        }

        public static T Get<T>(string key)
        {
            if (Session[key] == null)
                return default(T);
            return (T)Session[key];
        }

        public static void Set<T>(string key, T value)
        {
            try
            {
                if (!Contains(key))
                {
                    Session.Add(key,value);
                }
                else
                {
                    Session[key] = value;
                }
            }
            catch (Exception)
            {
                //If a restart occurs there will be no session so just trap it

            }

            
        }

        public static bool Contains(string key)
        {
            if (Session == null)
                return false;
            return Session[key] != null;
        }

        public static void Clear(string key)
        {
            if (Contains(key))
            {
                Session[key] = null;
                Session.Remove(key);
            }

        }

        public static void ClearAll()
        {
            Clear("Authenticated");
            Clear("Roles");
            Clear("IsAdministrator");
            Clear("IsModerator");
            Clear("ForumSubs");
            Clear("TopicSubs");
            Clear("CatSubs");
            //Clear("_LastVisit");
            Clear("Culture");
            Clear("SnitzMenu");
            Clear("Username");
            Clear("MyUserId");
            Clear("NewPM");
            Clear("MyBookmarks");
            Clear("MyThanks");
            Clear("AllowedForums");

        }
    }
}
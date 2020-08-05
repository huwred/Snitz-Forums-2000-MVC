// /*
// ####################################################################################################################
// ##
// ## UserQuestions
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
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Models;
using WebMatrix.WebData;


namespace SnitzDataModel.Extensions
{
    public static class UserExtensions
    {
        /// <summary>
        /// Copy properties from one entity to another
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="excludes">List of properties to exclude from copy</param>
        public static void CopyProperties(this object source, object destination, string[] excludes)
        {

            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsAssembly)
                {
                    continue;
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }
                if (excludes.Contains(targetProperty.Name))
                {
                    continue;
                }
                // Passed all tests, lets set the value
                try
                {
                    targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
                }
                catch (Exception)
                {

                    throw;
                }

            }
        }

        /// <summary>
        /// Get a list of Forum IDs the user has access to
        /// </summary>
        /// <param name="user"></param>
        /// <returns>List of Forum Id's</returns>
        public static List<int> AllowedForumIDs(this IPrincipal user)
        {
            if (!SessionData.Contains("AllowedForums"))
            {
                SessionData.Set("AllowedForums", user.Forums().Select(x => x.Id).ToList());

            }
            return SessionData.Get<List<int>>("AllowedForums");

        }

        /// <summary>
        /// Get the forums Member has access to.
        /// Used by Email Controller
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int> AllowedForumIDs(this Models.Member user)
        {

            List<int> allowedforums = new List<int>();
            if (user == null)
            {
                return allowedforums;
            }
            if (!SessionData.Contains("AllowedForums"))
            {
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    var forums = db.Fetch<Pair<int, int>>("SELECT FORUM_ID As 'Key', F_PRIVATEFORUMS As 'Value' FROM " + db.ForumTablePrefix + "FORUM");
                    foreach (var forum in forums)
                    {
                        if (IsAllowed(forum.Key, user.Username, (Enumerators.ForumAuthType)forum.Value))
                        {
                            if (!allowedforums.Contains(forum.Key))
                            {
                                allowedforums.Add(forum.Key);
                            }
                        }
                    }
                }
                SessionData.Set("AllowedForums", allowedforums);

            }
            return SessionData.Get<List<int>>("AllowedForums");

        }

        public static List<Models.Forum> Forums(this IPrincipal user)
        {
            List<Models.Forum> allowedforums = new List<Models.Forum>();
            if (user == null)
            {
                return allowedforums;
            }
            using (SnitzDataContext db = new SnitzDataContext())
            {
                var forums = db.Fetch<Models.Forum>("SELECT F.FORUM_ID,F.F_PRIVATEFORUMS,F.F_TOPICS,F.F_COUNT FROM " + db.ForumTablePrefix + "FORUM F");
                foreach (var forum in forums)
                {
                    if (IsAllowed(forum.Id, user.Identity.Name, forum.PrivateForums))
                    {
                        if (!allowedforums.Contains(forum))
                        {
                            allowedforums.Add(forum);
                        }
                    }
                }
            }
            return allowedforums;
        }

        private static bool IsAllowed(int forumid, string username, Enumerators.ForumAuthType type)
        {

            if (String.IsNullOrWhiteSpace(username))
            {
                return type == Enumerators.ForumAuthType.All;
            }
            if (Roles.IsUserInRole(username, "Administrator"))
            {
                return true;
            }

            if (type.In(Enumerators.ForumAuthType.All, Enumerators.ForumAuthType.PasswordProtected, Enumerators.ForumAuthType.Members, Enumerators.ForumAuthType.MembersPassword))
            {
                return true;
            }

            if (type.In(Enumerators.ForumAuthType.AllowedMembers, Enumerators.ForumAuthType.AllowedMemberPassword, Enumerators.ForumAuthType.AllowedMembersHidden))
            {
                if (Roles.IsUserInRole(username, "Forum_" + forumid))
                {
                    return true;
                }
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    int userid = WebSecurity.GetUserId(username);
                    var exists = db.Exists<Models.AllowedMembers>("FORUM_ID=@0 AND MEMBER_ID=@1", new object[] { forumid, userid });
                    return exists;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if user can has visibilty of a category
        /// </summary>
        /// <param name="user">user to check</param>
        /// <param name="catid">Id of Category</param>
        /// <param name="roles">extra role list for validation</param>
        /// <returns></returns>
        public static bool CanViewCategory(this IPrincipal user, int catid, IEnumerable<string> roles)
        {
            if (user.IsAdministrator())
                return true;

            var publicview = new[] { Enumerators.ForumAuthType.All, Enumerators.ForumAuthType.PasswordProtected };
            var hidden = new[] { Enumerators.ForumAuthType.MembersHidden, Enumerators.ForumAuthType.AllowedMembersHidden };
            var members = new[] { Enumerators.ForumAuthType.Members, Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.AllowedMembers, Enumerators.ForumAuthType.AllowedMemberPassword };
            Models.Category cat = null;
            using (SnitzDataContext db = new SnitzDataContext())
            {
                cat = db.FetchCategoryForumList(user).SingleOrDefault(c => c.Id == catid);
            }
            var canview = false;

            foreach (var forum in cat.Forums)
            {
                if (forum.PrivateForums.In(publicview))
                {
                    canview = true;
                }
                if (forum.PrivateForums.In(members))
                {
                    canview = true;
                }
                if (user.Identity.IsAuthenticated)
                {
                    if (roles != null)
                    {
                        foreach (string role in roles)
                        {
                            if (user.IsUserInRole(role))
                            {
                                canview = true;
                            }
                        }
                    }
                    else
                    {
                        if (forum.PrivateForums.In(hidden))
                        {
                            if (user.CanViewForum(forum, null))
                                canview = true; ;
                            //return false;
                        }
                    }
                }



            }

            return canview;
        }
        /// <summary>
        /// checks if a user has visibilty of a forum
        /// </summary>
        /// <param name="user">user to check</param>
        /// <param name="forum">Id of forum</param>
        /// <param name="roles">extra role list for validation</param>
        /// <returns></returns>
        public static bool CanViewForum(this IPrincipal user, Models.Forum forum, IEnumerable<string> roles)
        {
            if (user.IsAdministrator())
                return true;

            var publicview = new[] { Enumerators.ForumAuthType.All, Enumerators.ForumAuthType.Members, Enumerators.ForumAuthType.PasswordProtected };
            var hidden = new[] { Enumerators.ForumAuthType.MembersHidden, Enumerators.ForumAuthType.AllowedMembersHidden };
            var members = new[] { Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.AllowedMemberPassword };
            if (forum.PrivateForums.In(hidden))
            {
                return false;
            }

            switch (forum.PrivateForums)
            {
                case Enumerators.ForumAuthType.All:
                    return true;
                case Enumerators.ForumAuthType.AllowedMembers:
                case Enumerators.ForumAuthType.AllowedMemberPassword:
                    if (user.IsInRole("Forum_" + forum.Id) || user.IsForumModerator(forum.Id))
                    {
                        return true;
                    }
                    break;
                case Enumerators.ForumAuthType.Members:
                case Enumerators.ForumAuthType.MembersPassword:
                    return true;
                case Enumerators.ForumAuthType.PasswordProtected:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// checks if a user has access to a forum
        /// </summary>
        /// <param name="user">user to check</param>
        /// <param name="forum">forum Object</param>
        /// <param name="roles">extra role list for validation</param>
        /// <returns></returns>
        public static bool AllowedAccess(this IPrincipal user, Models.Forum forum, IEnumerable<string> roles)
        {
            if (user.IsAdministrator())
                return true;

            var publicview = new[] { Enumerators.ForumAuthType.All, Enumerators.ForumAuthType.PasswordProtected };
            var members = new[] { Enumerators.ForumAuthType.Members, Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.MembersHidden };
            var rolebased = new[] { Enumerators.ForumAuthType.AllowedMembers, Enumerators.ForumAuthType.AllowedMemberPassword, Enumerators.ForumAuthType.AllowedMembersHidden };

            if (forum.PrivateForums.In(publicview))
            {
                return true;
            }
            if (!user.Identity.IsAuthenticated) return false;
            if (forum.PrivateForums.In(members))
            {
                return true;
            }
            if (forum.PrivateForums.In(rolebased) && (user.IsUserInRole("Forum_" + forum.Id) || user.AllowedForumIDs().Contains(forum.Id)))
            {
                return true;
            }

            if (roles != null)
            {
                foreach (string role in roles)
                {
                    if (user.IsUserInRole(role))
                    {
                        return true;
                    }
                }
            }


            return false;
        }

        /// <summary>
        /// checks if a user has access to a forum
        /// </summary>
        /// <param name="user">user to check</param>
        /// <param name="forumid">Id of forum</param>
        /// <param name="roles">extra role list for validation</param>
        /// <returns></returns>
        public static bool AllowedAccess(this IPrincipal user, int forumid, IEnumerable<string> roles)
        {
            if (user.IsAdministrator())
                return true;

            var publicview = new[] { Enumerators.ForumAuthType.All, Enumerators.ForumAuthType.PasswordProtected };
            var members = new[] { Enumerators.ForumAuthType.Members, Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.MembersHidden };
            var rolebased = new[] { Enumerators.ForumAuthType.AllowedMembers, Enumerators.ForumAuthType.AllowedMemberPassword, Enumerators.ForumAuthType.AllowedMembersHidden };
            Models.Forum forum = null;
            using (SnitzDataContext db = new SnitzDataContext())
            {
                forum = Models.Forum.FetchForum(forumid);
            }
            if (forum.PrivateForums.In(publicview))
            {
                return true;
            }
            if (!user.Identity.IsAuthenticated) return false;
            if (forum.PrivateForums.In(members))
            {
                return true;
            }
            if (forum.PrivateForums.In(rolebased) && (user.IsUserInRole("Forum_" + forum.Id) || user.AllowedForumIDs().Contains(forum.Id)))
            {
                return true;
            }

            if (roles != null)
            {
                foreach (string role in roles)
                {
                    if (user.IsInRole(role))
                    {
                        return true;
                    }
                }
            }


            return false;
        }

        /// <summary>
        /// Check if currentuser is a forum moderator
        /// </summary>
        /// <param name="user"></param>
        /// <param name="forumid"></param>
        /// <returns></returns>
        public static bool IsForumModerator(this IPrincipal user, int forumid)
        {
            if (!SessionData.IsAuthenticated)
            {
                return false;
            }
            var forumlist = SessionData.Get<int[]>("ModeratedForums");

            if (forumlist == null)
            {
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    int userid = SessionData.MyUserId;

                    var res = db.Query<int>(
                                "SELECT FORUM_ID FROM " + db.ForumTablePrefix + "MODERATOR WHERE MEMBER_ID=@0", userid).ToArray();

                    SessionData.Set("ModeratedForums", res);
                }

            }
            if (forumlist != null && forumlist.Contains(forumid))
            {
                return true;
            }
            return false;
        }
        public static bool IsModerator(this IPrincipal user)
        {
            if (!SessionData.IsAuthenticated)
            {
                return false;
            }
            if (!SessionData.Contains("IsModerator"))
            {
                SessionData.Set<bool>("IsModerator", user.IsUserInRole("Moderator"));
            }

            return SessionData.Get<bool>("IsModerator");
        }
        /// <summary>
        /// Check if currentuser is a forum moderator
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsAdministrator(this IPrincipal user)
        {
            if (!SessionData.IsAuthenticated)
            {
                return false;
            }
            if (!SessionData.Contains("IsAdministrator"))
            {
                SessionData.Set<bool>("IsAdministrator", user.IsUserInRole("Administrator"));
            }

            return SessionData.Get<bool>("IsAdministrator");
        }
        public static bool IsUserInRole(this IPrincipal user, string rolename)
        {
            if (String.IsNullOrWhiteSpace(user.Identity.Name))
            {
                return false;
            }
            if (!SessionData.Contains("Roles"))
            {
                SessionData.Set<string[]>("Roles", Roles.Provider.GetRolesForUser(user.Identity.Name));
            }

            return SessionData.Get<string[]>("Roles").Contains(rolename);
        }
        /// <summary>
        /// Get a list of forums the user can moderate
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int> ModeratedForums(this IPrincipal user)
        {
            if (!SessionData.IsAuthenticated || !user.IsModerator())
            {
                return new List<int>() { 0 };
            }
            List<int> forums = new List<int>() { 0 };
            if (!SessionData.Contains("ModForums"))
            {
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    forums.AddRange(db.Fetch<int>("SELECT FORUM_ID FROM " + db.ForumTablePrefix + "MODERATOR WHERE MEMBER_ID=@0", SessionData.MyUserId));

                    SessionData.Set<List<int>>("ModForums", forums);
                }
            }
            return SessionData.Get<List<int>>("ModForums");
        }

        /// <summary>
        /// Get a list of Topics the user is subscribed to
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int> TopicSubscriptions(this IPrincipal user)
        {
            if (!SessionData.IsAuthenticated)
            {
                return new List<int>();
            }
            int userid = SessionData.MyUserId;
            if (!SessionData.Contains("TopicSubs"))
            {
                List<int> topics = new List<int>() { 0 };

                using (SnitzDataContext db = new SnitzDataContext())
                {
                    var subscribed =
                        db.Fetch<int>(
                            "SELECT DISTINCT TOPIC_ID FROM " + db.ForumTablePrefix +
                            "SUBSCRIPTIONS WHERE MEMBER_ID=@0 AND TOPIC_ID <> 0", userid);
                    if (subscribed.Count > 0)
                        topics.AddRange(subscribed);
                    SessionData.Set<List<int>>("TopicSubs", topics);
                }

            }
            return SessionData.Get<List<int>>("TopicSubs");
        }

        /// <summary>
        /// Get a list of Forums the user is subscribed to
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int> ForumSubscriptions(this IPrincipal user)
        {
            if (!SessionData.IsAuthenticated)
            {
                return new List<int>();
            }
            int userid = SessionData.MyUserId;
            if (!SessionData.Contains("ForumSubs"))
            {
                List<int> forums = new List<int>();// { 0 };
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    forums.AddRange(db.Fetch<int>("SELECT FORUM_ID FROM " + db.ForumTablePrefix + "SUBSCRIPTIONS WHERE MEMBER_ID=@0 AND TOPIC_ID = 0", userid));
                    SessionData.Set<List<int>>("ForumSubs", forums);
                }
            }
            return SessionData.Get<List<int>>("ForumSubs");
        }

        /// <summary>
        /// Get a list of Categories the user is subscribed to
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int> CategorySubscriptions(this IPrincipal user)
        {
            if (!SessionData.IsAuthenticated)
            {
                return new List<int>();
            }
            int userid = SessionData.MyUserId;
            if (!SessionData.Contains("CatSubs"))
            {
                List<int> cats = new List<int>() { 0 };
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    cats.AddRange(db.Fetch<int>("SELECT CAT_ID FROM " + db.ForumTablePrefix + "SUBSCRIPTIONS WHERE MEMBER_ID=@0 AND FORUM_ID=0 AND TOPIC_ID = 0", userid));
                    SessionData.Set<List<int>>("CatSubs", cats);
                }
            }
            return SessionData.Get<List<int>>("CatSubs");
        }

        public static string Theme(this IPrincipal user)
        {
            var theme = Config.DefaultTheme;

            var httpCookie = SnitzCookie.GetCookieValue("snitztheme");
            if (httpCookie != null && httpCookie != theme.ToLower())
            {
                var cookietheme = httpCookie;
                theme = cookietheme.FirstLetterToUpperCase();

            }
            return theme;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Security;
//using MySql.Web.Security;
using Snitz.Base;
using Snitz.WebMatrix.WebData;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership.Models;
using SnitzMembership.Repositories;
using WebMatrix.WebData;
using Membership = System.Web.Security.Membership;

namespace SnitzMembership
{
    public static class WebSecurity 
    {
        //private static SnitzMemberContext _context;

        public static bool IsAdministrator
        {
            get { return IsUserInRole(CurrentUserName, "Administrator"); }
        }
        public static bool IsModerator
        {
            get { return IsUserInRole(CurrentUserName, "Moderator"); }
        }


        /// <summary>
        /// Get userprofile record (FORUM_MEMBERS)
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static UserProfile GetUser(string username)
        {
            UnitOfWork uow = new UnitOfWork();
            return uow.UserProfileRepository.Get(u => u.UserName == username).SingleOrDefault();
        }

        public static UserProfile GetCurrentUser()
        {
            return GetUser(CurrentUserName);
        }

        public static void CreateUser(UserProfile user)
        {
            UserProfile dbUser = GetUser(user.UserName);
            if (dbUser != null)
                throw new Exception(LangResources.Utility.ResourceManager.GetLocalisedString("UserNameExists", "ErrorMessage") );
            UnitOfWork uow = new UnitOfWork();
            uow.UserProfileRepository.Insert(user);
            uow.Save();
        }

        public static void Register()
        {
            Database.SetInitializer<SnitzMemberContext>(null);
            string db = "ERROR";
            try
            {
                using (var context = SnitzMemberContext.CreateContext())
                {
                    db = context.Database.Connection.Database;
                    if (!context.Database.Exists())
                    {
                        db = db + " DOES NOT EXIST";
                        // Create the SimpleMembership database without Entity Framework migration schema 
                        ((IObjectContextAdapter)context).ObjectContext.CreateDatabase();
                        
                    }
                    //if (context.Provider.StartsWith("MySql"))
                    //{


                    //    MySqlWebSecurity.InitializeDatabaseConnection("SnitzMembership", context.MemberTablePrefix + "MEMBERS", "MEMBER_ID", "M_NAME", false);
                    //}else

                        WebMatrix.WebData.WebSecurity.InitializeDatabaseConnection("SnitzMembership", "SnitzSimpleMembershipProvider", context.MemberTablePrefix + "MEMBERS", "MEMBER_ID", "M_NAME", false,SimpleMembershipProviderCasingBehavior.RelyOnDatabaseCollation);
                }
                
                AddRoles();

            }
            catch (Exception ex)
            {
                Console.WriteLine(db + " database could not be initialized. " + ex.Message);
                throw new InvalidOperationException(db + " database could not be initialized. " + ex.Message, ex);
            } 
        }

        public static bool Login(string userName, string password, bool persistCookie = false)
        {
            if (Roles.Provider.IsUserInRole(userName, "Disabled"))
            {
                throw new Exception("Disabled");
            }

            var usr = GetUser(userName);
            if (usr == null)
            {
                throw new Exception("UsernameNotFound");
            }
            userName = usr.UserName;
            if (usr.UserLevel == 0 )
            {
                throw new Exception("UnApprovedAccount");
            }
            if (!IsConfirmed(usr.UserId))
            {

                var date = GetCreateDate(usr.UserId);

                //if the date equals minvalue then we have no record yet so need to try a legacy login
                if (date != DateTime.MinValue && usr.UserLevel!=3)
                {

                    throw new Exception("IncompleteReg");
                }
            }

            bool success = MemberManager.ValidateUser(userName, password);
            if (success)
            {

                FormsAuthentication.SetAuthCookie(userName, persistCookie);
                SessionData.ClearAll();
            }

            if (!success)
            {
                success = LegacyLogin(userName, password, usr.Password, persistCookie);
            }
            //bool success =  WebMatrix.WebData.WebSecurity.Login(usr.UserName, password, persistCookie);
            if (IsAccountLockedOut(userName, 5, 3600))
            {
                throw new Exception("LockedAccount");
            }
            if (success)
            {

                using (var db = new SnitzDataContext())
                {
                    
                    db.Execute("UPDATE " + db.MemberTablePrefix + "MEMBERS SET M_LASTHEREDATE=@0 WHERE M_NAME=@1",
                        DateTime.UtcNow.ToSnitzDate(), userName);

                }
                if (!Config.AnonymousMembers.Contains(userName))
                    OnlineUsersInstance.OnlineUsers.SetUserOnline(userName);
                SnitzCookie.ClassicLogin(userName, password);
            }
            return success;
        }

        public static bool ChangePassword(string userName, string oldPassword, string newPassword)
        {
            return ((SnitzSimpleMembershipProvider)Membership.Provider).ChangePassword(userName, oldPassword, newPassword);
        }

        public static bool ConfirmAccount(string accountConfirmationToken)
        {
            return ((SnitzSimpleMembershipProvider)Membership.Provider).ConfirmAccount(accountConfirmationToken);
        }

        public static string CreateAccount(string userName, string password, bool requireConfirmationToken = false)
        {
            return ((SnitzSimpleMembershipProvider) Membership.Provider).CreateAccount(userName, password,requireConfirmationToken);
            //return WebMatrix.WebData.WebSecurity.CreateAccount(userName, password, requireConfirmationToken);
        }

        public static string CreateUserAndAccount(string userName, string password, string email, bool requireConfirmationToken = false)
        {
            //if (SnitzMemberContext.CreateContext().Provider.StartsWith("MySql"))
            //{
            //    return MySql.Web.Security.MySqlWebSecurity.CreateUserAndAccount(userName, password, new { Email = email }, requireConfirmationToken);
            //}
            var props = new Dictionary<string, object>();
            props.Add("Email", email);
            return ((SnitzSimpleMembershipProvider)Membership.Provider).CreateUserAndAccount(userName, password, requireConfirmationToken,props);
        }

        public static string CreateUserAndAccount(string userName, string password, bool requireConfirmation, string email)
        {
            MemberManager.CreateUser(userName, password, email);
            return ((SnitzSimpleMembershipProvider)Membership.Provider).CreateAccount(userName, password, requireConfirmation);
            //return WebMatrix.WebData.WebSecurity.CreateUserAndAccount(userName, password, props, requireConfirmation);
        }

        public static int GetUserId(string userName)
        {
            return ((SnitzSimpleMembershipProvider)Membership.Provider).GetUserId(userName);
        }

        public static void Logout()
        {
            WebMatrix.WebData.WebSecurity.Logout();
        }

        public static bool IsAuthenticated
        {
            get
            {
                if (!SessionData.Contains("Authenticated"))
                {
                    SessionData.IsAuthenticated = WebMatrix.WebData.WebSecurity.IsAuthenticated;
                }
                return SessionData.IsAuthenticated; 
            }
        }

        public static string CurrentUserName
        {
            get
            {
                if (!SessionData.Contains("Username"))
                {
                    SessionData.Set("Username", WebMatrix.WebData.WebSecurity.CurrentUserName);
                }
                return SessionData.Get<string>("Username");
            }
        }

        public static int CurrentUserId
        {
            get
            {
                if (!SessionData.Contains("MyUserId"))
                {
                    if (!IsAuthenticated)
                    {
                        return -1;
                    }
                    SessionData.MyUserId = WebMatrix.WebData.WebSecurity.CurrentUserId;
                }
                return SessionData.MyUserId;

            }
        }

        /// <summary>
        /// Do we have a confirmed new membership record?
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static bool IsConfirmed(int userid)
        {
            return MemberManager.IsConfirmed(userid);
        }

        public static DateTime GetCreateDate(string userName)
        {
            return MemberManager.GetCreateDate(userName);
        }
        public static DateTime GetCreateDate(int userId)
        {
            return MemberManager.GetCreateDate(userId);
        }
        public static string GeneratePasswordResetToken(string userName, int i)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException(LangResources.Utility.ResourceManager.GetLocalisedString("Username_cannot_be_empty", "ErrorMessage"), "username");
            }
            var token = GenerateToken();
            var newRecord = false;
            using (var db = new SnitzMemberContext())
            {
                //bool throwException = true;

                var user = GetUser(userName);
                var membership = db.GetMembership(user.UserId);

                if (membership == null)
                {
                    membership = new SnitzSecurity();
                    membership.UserId = user.UserId;
                    membership.CreateDate = user.CreatedDate;
                    membership.Password = user.Password;
                    membership.IsConfirmed = true;
                    membership.PasswordSalt = String.Empty;
                    newRecord = true;
                }

                if (membership.PasswordVerificationTokenExpirationDate.HasValue &&
                    membership.PasswordVerificationTokenExpirationDate.Value > DateTime.UtcNow)
                {
                    return membership.PasswordVerificationToken;
                }

                membership.PasswordVerificationToken = token;
                membership.PasswordVerificationTokenExpirationDate =
                    DateTime.UtcNow.AddMinutes((double)24 * 60);
                if(newRecord)
                    db.Memberships.Add(membership);

                var res = db.SaveChanges();

                if (res == 0)
                {
                    throw new ProviderException(LangResources.Utility.ResourceManager.GetLocalisedString("GeneratePasswordResetToken_Fail", "ErrorMessage"));
                }
            }
            return token;
        }

        private static string GenerateToken()
        {
            using (var prng = new RNGCryptoServiceProvider()) 
 			{ 
 				return GenerateToken(prng); 
 			} 
        }
        internal static string GenerateToken(RandomNumberGenerator generator) 
 		{ 
 			byte[] tokenBytes = new byte[16]; 
 			generator.GetBytes(tokenBytes); 
 			return HttpServerUtility.UrlTokenEncode(tokenBytes); 
 		} 

        public static void ResetPassword(string token, string newPassword)
        {
            WebMatrix.WebData.WebSecurity.ResetPassword(token, newPassword);
        }

        /// <summary>
        /// Check user profile exists (FORUM_MEMBERS)
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static bool UserExists(string userName)
        {
            var test = MemberManager.GetUser(userName);

            return test != null;
        }

        public static bool IsAccountLockedOut(string userName, int p1, int p2)
        {
            return MemberManager.IsAccountLockedOut(userName);
        }


        public static int GetUserIdFromPasswordResetToken(string id)
        {
            
            return ((SnitzSimpleMembershipProvider)Membership.Provider).GetUserIdFromPasswordResetToken(id);
        }

        public static bool DeleteUser(string username)
        {
            var roles = Roles.Provider;
            var membership = Membership.Provider;
            if (roles.GetRolesForUser(username).Any())
            {
                roles.RemoveUsersFromRoles(new[] { username }, roles.GetRolesForUser(username));
            }
            bool wasDeleted = membership.DeleteUser(username, true);

            return wasDeleted;
        }

        public static bool LegacyLogin(string userName, string password, string passwordhashed, bool rememberMe)
        {
            var legacy = new LegacySecurity();

            return legacy.Login(userName, password, passwordhashed, rememberMe);
        }

        public static void AddRoles()
        {
            var roles = new SnitzSimpleRoleProvider();
               
            if (!Roles.RoleExists("Administrator"))
            {

                roles.CreateRole("Administrator");
                roles.CreateRole("Moderator");
                roles.CreateRole("Disabled");
                using (var db = new SnitzDataContext())
                {
                    try
                    {
                        db.Execute("INSERT INTO webpages_UsersInRoles SELECT MEMBER_ID, (SELECT RoleId FROM webpages_Roles  WHERE RoleName = 'Administrator') FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_LEVEL=3");
                    }
                    catch (Exception)
                    {
                        throw;
                        //trap errors for now
                    }
                    try
                    {
                        db.Execute("INSERT INTO webpages_UsersInRoles SELECT MEMBER_ID, (SELECT RoleId FROM webpages_Roles  WHERE RoleName = 'Moderator') FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_LEVEL=2");
                    }
                    catch (Exception)
                    {
                        //trap errors for now
                        throw;
                    }
                    try
                    { //Add locked users to Disabled Role
                        db.Execute("INSERT INTO webpages_UsersInRoles SELECT MEMBER_ID, (SELECT RoleId FROM webpages_Roles  WHERE RoleName = 'Disabled') FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_LEVEL=0");
                    }
                    catch (Exception)
                    {
                        //trap errors for now
                        throw;
                    }
                }
                MigrateAllowedMembers();
            }
        }

        /// <summary>
        /// Creates Roles for Private forums, then adds the allowed members to that role.
        /// 
        /// </summary>
        internal static void MigrateAllowedMembers()
        {
            var roles = Roles.Provider;
            using (var db = new SnitzDataContext())
            {
                var convert = "CONVERT(m.forum_id,char(10))";
                if (db.dbtype == "mssql")
                {
                    convert = "CONVERT(VARCHAR,m.FORUM_ID)";
                }
                var privateforums = db.Query<int>("SELECT DISTINCT FORUM_ID FROM " + db.ForumTablePrefix + "ALLOWED_MEMBERS");
                foreach (int privateforum in privateforums)
                {
                    if (!roles.RoleExists("Forum_" + privateforum))
                    {
                        roles.CreateRole("Forum_" + privateforum);
                    }
                }
                string sql = "INSERT INTO webpages_UsersInRoles SELECT m.MEMBER_ID, r.RoleId FROM " + db.ForumTablePrefix + "ALLOWED_MEMBERS m LEFT JOIN webpages_Roles r ON r.RoleName = 'Forum_' + " + convert;
                try
                {
                    db.Execute(sql);
                }
                catch (Exception)
                {
                    //trap errors for now
                }

            }
        }

        
        public static bool IsUserInRole(string username, string rolename)
        {

            if (username == WebSecurity.CurrentUserName)
            {
                return HttpContext.Current.User.IsUserInRole(rolename);
            }

            return Roles.Provider.IsUserInRole(username, rolename);
        }
        public static Dictionary<string, string> UserRoles
        {
            get
            {
                var cacheService = new InMemoryCache() { DoNotExpire = true };
                
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    return cacheService.GetOrSet("snitz.appvars", () => db.Fetch<Pair<string, string>>("SELECT C_VARIABLE As 'Key',C_VALUE As 'Value' FROM " + db.ForumTablePrefix + "CONFIG_NEW").ToDictionary(i => i.Key.ToUpper(), i => i.Value));
                }

            }
        }
        public static bool IsUserInRole(string username, List<string> rolenames)
        {
            var roles = Roles.Provider.GetRolesForUser(username);

            return roles.Any(x => rolenames.Contains(x));
        }

        public static void DeleteUser(UserProfile member)
        {

            UnitOfWork uow = new UnitOfWork();
            uow.UserProfileRepository.Delete(member);
            uow.Save();

            //using (var db = new SnitzDataContext())
            //{
            //    string sql = "DELETE FROM webpages_UsersInRoles SELECT m.MEMBER_ID, r.RoleId FROM ";
            //    try
            //    {
            //        db.Execute(sql);
            //    }
            //    catch (Exception)
            //    {
            //        //trap errors for now
            //    }
            //}
        }
    }

    public class EmailConfirmation
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }

}

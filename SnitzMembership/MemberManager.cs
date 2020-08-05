using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;
using System.Web.Helpers;
using System.Web.Mvc;
using BbCodeFormatter;
using PetaPoco;
using Postal;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;
using SnitzMembership.Models;
using SnitzMembership.Repositories;
using ForumTotals = SnitzDataModel.Models.ForumTotals;
using Member = SnitzDataModel.Models.Member;
using NameFilter = SnitzDataModel.Models.NameFilter;
using SpamFilter = SnitzDataModel.Models.SpamFilter;

namespace SnitzMembership
{
    public static class MemberManager
    {
        /// <summary>
        /// Fetch list of Members awaiting approval
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<UserProfile> PendingMembers()
        {

            using (var context = new SnitzMemberContext())
            {
                var pendingmembers =
                   from profile in context.UserProfiles
                   join member in context.Memberships on profile.UserId equals member.UserId
                   where (member.IsConfirmed == false && (profile.UserLevel > 0 && profile.Status == 0) == false) || (profile.UserLevel == 0)
                   select profile;

                return pendingmembers.ToList();                
            }

        }
        public static IEnumerable<string> CachedUsernames()
        {
            using (var db = new SnitzMemberContext())
            {
                var cacheService = new InMemoryCache(60);
                return cacheService.GetOrSet("lookup.usernames", () => (from r in db.UserProfiles
                    where r.Status == 1
                    select r.UserName).Distinct().ToArray());
            }
        }
        public static List<KeyValuePair<string,string>> ActiveUsersList(List<string> users)
        {
            using (var context = new SnitzMemberContext())
            {
                //var onlinelimit = DateTime.UtcNow.AddMinutes(-10).ToString("yyyyMMddHHmmss"); 
                var res = context.UserProfiles.Where(u => users.Contains(u.UserName)).ToList().Select(u=> new KeyValuePair<string, string>(u.UserName, u.PhotoUrl));

                return res.ToList();
            }
        }
        public static List<KeyValuePair<string, string>> RecentUsersList(List<string> users)
        {
            using (var context = new SnitzMemberContext())
            {
                //var onlinelimit = DateTime.UtcNow.AddMinutes(-10).ToString("yyyyMMddHHmmss"); 
                var res = context.UserProfiles.Where(u => u.Status == 1 && !users.Contains(u.UserName) && u.LastActivity != null).OrderByDescending(u=>u.LastActivity).Take(10).ToList().Select(u => new KeyValuePair<string, string>(u.UserName, u.LastActivity));

                return res.ToList();
            }
        }

        /// <summary>
        /// Get members profile
        /// </summary>
        /// <param name="id">Username or MemberId</param>
        /// <returns></returns>
        public static UserProfile GetUser(int id)
        {
            using (var context = new SnitzMemberContext())
            {
                return context.UserProfiles.SingleOrDefault(m => m.UserId == id);
            }
            
        }
        public static UserProfile GetUser(string id)
        {
            using (var context = new SnitzMemberContext())
            {
                return context.UserProfiles.SingleOrDefault(m => m.UserName == id);
            }
            
        }
        public static UserProfile GetUserByIP(string ipaddress)
        {
            using (var context = new SnitzMemberContext())
            {
                return context.UserProfiles.OrderByDescending(m=>m.LastActivity).FirstOrDefault(m => m.LastIp == ipaddress);
            }

        }
        public static UserProfile RegisterUser(string userName, string password, string email, UserProfile profile, out string status, bool admin=false)
        {
            string confirmationToken = null;
            UserProfile user = null;
            //check the email against Spam Filter
            if (ClassicConfig.GetValue("STRFILTEREMAILADDRESSES") == "1")
            {
                var check = SpamFilter.All().Find(s => email.EndsWith(s.SpamServer));
                if (check != null)
                {
                    status = LangResources.Utility.ResourceManager.GetLocalisedString("SpamFilterError", "ErrorMessage");
                    return null;
                }
            }

            try
            {
                using (SnitzMemberContext db = new SnitzMemberContext())
                {
                    if (ClassicConfig.GetValue("STRUNIQUEEMAIL") == "1")
                    {
                        var check = db.UserProfiles.SingleOrDefault(s => email.EndsWith(s.Email));
                        if (check != null)
                        {
                            status = LangResources.Utility.ResourceManager.GetLocalisedString("EmailExists", "ErrorMessage");
                            return null;
                        }
                    }
                    // need to check username filter
                    if (UsernameExists(userName, out status)) return null;

                    confirmationToken = WebSecurity.CreateUserAndAccount(userName, password, !admin && ClassicConfig.EmailValidation && ClassicConfig.AllowEmail, email);
                    user = db.UserProfiles.Single(u => u.UserName == userName);

                    if (user != null)
                    {
                        if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
                        {
                            user.IpAddress = Common.GetUserIP(System.Web.HttpContext.Current);
                            user.LastIp = Common.GetUserIP(System.Web.HttpContext.Current);
                        }
                        if (admin || ( ClassicConfig.EmailValidation && !ClassicConfig.RestrictReg) || !ClassicConfig.AllowEmail)
                        {
                            //unlock the user if reg not restricted;
                            user.UserLevel = 1;
                            user.Status = 1;
                        }
                        else if (!ClassicConfig.RestrictReg)
                        {
                            user.UserLevel = 1;
                            user.Status = 1;
                        }
                        if (profile != null)
                        {
                            foreach (var prop in profile.GetType()
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(p => !p.GetIndexParameters().Any())
                                .Where(p => p.CanRead && p.CanWrite && p.Name != "UserId"))
                            {
                                var value = prop.GetValue(profile, null);
                                var usrvalue = prop.GetValue(user, null);

                                if (usrvalue is string)
                                {
                                    if ((string)usrvalue == String.Empty)
                                        usrvalue = null;
                                }
                                if (usrvalue == null)
                                {
                                    var isRequired = Attribute.IsDefined(prop, typeof (RequiredIfAttribute));
                                    if (isRequired &&
                                        System.Web.HttpContext.Current.Request.RawUrl.ToLower().Contains("/api/member"))
                                    {
                                        var context = new ValidationContext("", null, null);
                                        var results = new List<ValidationResult>();
                                        var attributes = prop
                                            .GetCustomAttributes(true)
                                            .OfType<ValidationAttribute>()
                                            .ToArray();

                                        if (!Validator.TryValidateValue(value, context, results, attributes))
                                        {
                                            foreach (var result in results)
                                            {
                                                value = "--api--";
                                                status = "Failed Required Validation";
                                            }
                                        }

                                    }
                                    try
                                    {
                                        prop.SetValue(user, value, null);
                                    }
                                    catch (DbEntityValidationException ee)
                                    {
                                        status = "Error:" + ee.Message;
                                        return null;
                                    }

                                }

                            }
                        }

                        db.SaveChanges();
                    }
                }
            }
            catch (DbEntityValidationException ee)
            {
                status = "Error:" + ee.Message;
                return null;
            }
            catch (Exception e)
            {
                status = "Error:" + e.Message;
                return null;
            }

            if (admin)
            {
                status = "Success";
                if (user != null)
                {
                    ForumTotals.AddUser();
                } 
                return user;
            }
            if (ClassicConfig.RestrictReg)
            {
                status = "Success:Approval";
                return user;
            } 
            if (ClassicConfig.EmailValidation && ClassicConfig.AllowEmail)
            {
                //SendEmailConfirmation(user.Email, userName, confirmationToken, user.UserId);
                status = "Success:Confirmation";
                return user;
            }
            if (user != null)
            {
                ForumTotals.AddUser();
            } 
            
            status = "Success:OK";
            return user;
        }
        public static bool UsernameExists(string userName, out string status)
        {
            status = null;
            bool exists = Member.GetByName(userName) != null;
            if (!exists) //name doesn't exist, so check the dissallowed list
            {
                if (ClassicConfig.GetValue("STRUSERNAMEFILTER") == "1")
                {
                    try
                    {
                        exists = NameFilter.All().Any(m => m.Name == userName);
                    }
                    catch (Exception e)
                    {
                        var test = e.Message;
                    }
                }
            }
            if (exists)
            {
                status = LangResources.Utility.ResourceManager.GetLocalisedString("UserNameExists", "ErrorMessage");
                return true;
            }
            return false;
        }

        public static string GetUsername(int memberid)
        {
            using (var context = new SnitzMemberContext())
            {
                return context.UserProfiles.Single(m => m.UserId == memberid).UserName;
            }

        }

        public static int MemberCount()
        {
            using (var context = new SnitzMemberContext())
            {
                var members =
                   from profile in context.UserProfiles
                   where profile.Status == 1 && profile.UserLevel > 0
                   select profile;
                return members.Count();
            }
        }

        public static Member NewestMember()
        {
            using (var db = new SnitzDataContext())
            {
                return  db.First<Member>("SELECT M_NAME FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_STATUS=1 AND M_LEVEL>0 AND M_LASTHEREDATE IS NOT NULL ORDER BY M_DATE DESC");

            }

        }
        public static void CreateUser(string userName, string password, string email)
        {
            UserProfile newuser = new UserProfile();
            newuser.UserName = userName;
            newuser.Password = Common.SHA256Hash(password);
            newuser.Email = email;
            newuser.Created = DateTime.UtcNow.ToSnitzDate();

            using (var context = new SnitzMemberContext())
            {
                context.Configuration.ValidateOnSaveEnabled = false;
                context.UserProfiles.Add(newuser);
                context.SaveChanges();
            }
        }

        public static List<Member> GetAllMembers(bool administrator, FormCollection form, string sortCol, string sortOrder)
        {
            Sql sql = MemberManager.MemberSql(administrator);
            ApplySearchFilters(form, ref sql);
            if (administrator)
            {
                DateFilters(form, ref sql);
            }

            using (var db = new SnitzDataContext())
            {
                SortBy(sortOrder, sortCol, db.dbtype, ref sql);
                return db.Query<SnitzDataModel.Models.Member>(sql).ToList();

            }

        }

        public static void SaveAvatar(UserProfile profile, string newvalue, bool admin = false)
        {
            using (var context = new SnitzMemberContext())
            {
                try
                {
                    context.UserProfiles.Attach(profile);
                    profile.PhotoUrl = newvalue;

                    var entry = context.Entry(profile);
                    entry.State = EntityState.Unchanged;
                    entry.Property(e => e.PhotoUrl).IsModified = true;
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        }
        public static void SaveProfile(UserProfile profile, bool admin=false)
        {
            
            //need to trim the dob field
            if(profile.DateOfBirth != null)
            {
                if (profile.DateOfBirth.Length == 8)
                {
                    profile.DateOfBirth = profile.DateOfBirth.Replace("0000", "0101");
                }
                if (profile.DateOfBirth.Length > 8)
                    profile.DateOfBirth = profile.DateOfBirth.TrimEnd(new char[] { '0' });
            }

            using (var context = new SnitzMemberContext())
            {
                context.UserProfiles.Attach(profile);

                var entry = context.Entry(profile);
                    entry.State = EntityState.Modified;

                    entry.Property(e => e.Created).IsModified = false;
                    entry.Property(e => e.Password).IsModified = false;
                    entry.Property(e => e.UserName).IsModified = false;
                    entry.Property(e => e.IpAddress).IsModified = false;
                    entry.Property(e => e.LastPost).IsModified = false;
                    entry.Property(e => e.LastVisit).IsModified = false;
                    entry.Property(e => e.LastActivity).IsModified = false;
                    if (!admin)
                    {
                        entry.Property(e => e.Email).IsModified = false;
                        entry.Property(e => e.PostCount).IsModified = false;
                        entry.Property(e => e.Status).IsModified = false;
                        entry.Property(e => e.UserLevel).IsModified = false;
                        entry.Property(e => e.ForumTitle).IsModified = false;
                    }

                    entry.Property(e => e.PrivateMessageNotify).IsModified = false;
                    entry.Property(e => e.PrivateMessageReceive).IsModified = false;
                    entry.Property(e => e.PrivateMessageSentItems).IsModified = false;
                    context.SaveChanges();


            }
        }
        public static void SaveMergedProfile(UserProfile profile, string[] props)
        {

            using (var context = new SnitzMemberContext())
            {
                context.UserProfiles.Attach(profile);

                var entry = context.Entry(profile);
                    entry.State = EntityState.Modified;
                    foreach (string prop in props)
                    {
                        entry.Property(prop.Replace("Date","")).IsModified = true;
                    }

                    context.SaveChanges();


            }
        }
        public static void SaveApiProfile(UserProfile profile)
        {

            //need to trim the dob field
            if (profile.DateOfBirth != null)
                if (profile.DateOfBirth.Length > 8)
                    profile.DateOfBirth = profile.DateOfBirth.TrimEnd(new char[] { '0' });

            using (var context = new SnitzMemberContext())
            {
                context.UserProfiles.Attach(profile);

                var entry = context.Entry(profile);
                entry.State = EntityState.Modified;
                entry.Property(e => e.Password).IsModified = false;
                if (profile.Email == null)
                    entry.Property(e => e.Email).IsModified = false;
                if (profile.UserName == null)
                    entry.Property(e => e.UserName).IsModified = false;
                if (profile.DateOfBirth == null)
                    entry.Property(e => e.DateOfBirth).IsModified = false;

                entry.Property(e => e.Created).IsModified = false;
                entry.Property(e => e.IpAddress).IsModified = false;
                entry.Property(e => e.LastPost).IsModified = false;
                entry.Property(e => e.LastVisit).IsModified = false;
                entry.Property(e => e.LastActivity).IsModified = false;
                entry.Property(e => e.PostCount).IsModified = false;
                entry.Property(e => e.Status).IsModified = false;
                entry.Property(e => e.UserLevel).IsModified = false;
                entry.Property(e => e.ForumTitle).IsModified = false;
                entry.Property(e => e.PrivateMessageNotify).IsModified = false;
                entry.Property(e => e.PrivateMessageReceive).IsModified = false;
                entry.Property(e => e.PrivateMessageSentItems).IsModified = false;
                context.SaveChanges();


            }
        }

        public static bool ValidateUser(string membername, string password)
        {
            bool passhashed = false;
            using (var context = new SnitzMemberContext())
            {
                var res = context.UserProfiles.Single(m => m.UserName == membername);
                if (res != null)
                {
                    SnitzSecurity pass = context.Memberships.SingleOrDefault(m=> m.UserId == res.UserId);

                    //check password
                    if (pass != null) passhashed = Crypto.VerifyHashedPassword(pass.Password, password);
                    
                }
                return res != null && passhashed;
            }
        }

        public static bool IsAccountLockedOut(string membername)
        {
            using (var context = new SnitzMemberContext())
            {
                var res = context.UserProfiles.Single(m => m.UserName == membername);
                if (res == null)
                    return false;
                return res.Locked;
            }
        }

        /// <summary>
        /// Checks if new membership record is confirmed
        /// </summary>
        /// <param name="userid"></param>
        /// <returns>boolean</returns>
        public static bool IsConfirmed(int userid)
        {
            using (var context = new SnitzMemberContext())
            {
                var res = context.Memberships.SingleOrDefault(m => m.UserId == userid);
                var test = context.UserProfiles.SingleOrDefault(m => m.UserId == userid);
                if (res == null)
                {
                    return test != null;
                }
                return res.IsConfirmed;
            }
        }

        /// <summary>
        /// Get Members register date
        /// </summary>
        /// <param name="id">Username or MemberId</param>
        /// <returns>Registered DateTime</returns>
        public static DateTime GetCreateDate(int id)
        {
            using (var context = new SnitzMemberContext())
            {
                var res = context.Memberships.SingleOrDefault(m => m.UserId == id);
                if (res == null)
                    return DateTime.MinValue;
                DateTime created = res.CreateDate ?? DateTime.MinValue;
                return created;
            }
        }
        public static DateTime GetCreateDate(string id)
        {

            using (var context = new SnitzMemberContext())
            {
                var res = context.UserProfiles.Single(m => m.UserName == id);
                DateTime created = res.CreatedDate == null ? DateTime.MinValue : res.CreatedDate.Value;
                return created;
            }
        }

        
        public static bool ChangePassword(string userName, string newPassword)
        {
            
            using (var context = new SnitzMemberContext())
            {
                var user = context.UserProfiles.SingleOrDefault(m => m.UserName == userName);
                context.Configuration.ValidateOnSaveEnabled = false;
                user.Password = Common.SHA256Hash(newPassword);
                context.SaveChanges();
            }            
            return true;
        }
        public static bool ChangeUsername(string userName, string newUserName)
        {

            using (var context = new SnitzMemberContext())
            {
                var user = context.UserProfiles.SingleOrDefault(m => m.UserName == userName);
                context.Configuration.ValidateOnSaveEnabled = false;
                user.UserName = newUserName;
                context.SaveChanges();
            }
            return true;
        }
        public static Page<Member> GetMembers(bool administrator,int pagesize, int pagenum , string sortOrder, string sortCol , string initial="")
        {

            using (var db = new SnitzDataContext())
            {
                Sql sql = MemberSql(administrator);

                if (initial != "")
                {
                    
                    sql.Where("m.M_NAME LIKE @0", initial + "%");
                }
                SortBy(sortOrder, sortCol, db.dbtype, ref sql);

                return db.Page<SnitzDataModel.Models.Member>(pagenum, pagesize, sql);

            }
        }

        public static Page<Member> SearchMembers(bool administrator, int pagenum, string sortOrder, string sortCol, FormCollection form)
        {
            using (var db = new SnitzDataContext())
            {
                Sql sql = MemberSql(administrator);
                ApplySearchFilters(form, ref sql);

                if (administrator)
                {
                    DateFilters(form, ref sql);
                }

                SortBy(sortOrder, sortCol, db.dbtype, ref sql);

                return db.Page<SnitzDataModel.Models.Member>(pagenum, Config.MemberPageSize, sql);

            }
        }


        private static Sql MemberSql(bool administrator)
        {
            using (var db = new SnitzDataContext())
            {
                Sql sql = new Sql();
                sql.Select("m.MEMBER_ID,m.M_NAME,m.M_TITLE,m.M_EMAIL,m.M_DATE,m.M_LASTHEREDATE,m.M_LASTPOSTDATE,m.M_LAST_IP,m.M_STATUS,m.M_COUNTRY,m.M_POSTS,m.M_LEVEL,m.M_PHOTO_URL,ur.Roleid AS Disabled,um.IsConfirmed AS Confirmed ");
                sql.From(db.MemberTablePrefix + "MEMBERS m");
                sql.LeftJoin("webpages_UsersInRoles ur").On("m.MEMBER_ID=ur.UserId and ur.Roleid=3");
                sql.LeftJoin("webpages_Membership um").On("m.MEMBER_ID=um.UserId");
                //sql.LeftJoin("webpages_Roles r").On("ur.RoleId=r.RoleId");
                if (!administrator)
                {
                    sql.Where("m.M_LEVEL > 0");
                    sql.Where("m.M_STATUS > 0");
                }
                return sql;
            }

        }
        private static void ApplySearchFilters(FormCollection form, ref Sql sql)
        {
            if (!String.IsNullOrWhiteSpace(form["Initial"]))
            {
                sql.Where("m.M_NAME LIKE @0",  form["Initial"] + "%");
            }
            else if (!String.IsNullOrWhiteSpace(form["Username"]))
            {
                sql.Where("m.M_NAME LIKE @0", "%" + form["Username"] + "%");
            }
            else if (!String.IsNullOrWhiteSpace(form["Firstname"]))
            {
                sql.Where("m.M_FIRSTNAME LIKE @0", "%" + form["Firstname"] + "%");
            }
            else if (!String.IsNullOrWhiteSpace(form["Lastname"]))
            {
                sql.Where("m.M_LASTNAME LIKE @0", "%" + form["Lastname"] + "%");
            }
            else if (!String.IsNullOrWhiteSpace(form["Title"]))
            {
                sql.Where("m.M_TITLE LIKE @0", "%" + form["Title"] + "%");
            }
            else if (!String.IsNullOrWhiteSpace(form["Email"]))
            {
                sql.Where("m.M_EMAIL LIKE @0", "%" + form["Email"] + "%");
            }else if (!String.IsNullOrWhiteSpace(form["confirmed"]))
            {
                sql.Where("um.IsConfirmed = 0");
            }
        }
        private static void DateFilters(FormCollection form, ref Sql sql)
        {
            if (!String.IsNullOrWhiteSpace(form["LastPost"]))
            {
                //older than x years
                var years = Convert.ToInt32(form["LastPost"]);

                if (years == 0)
                {
                    sql.Where("m.M_LASTPOSTDATE IS NULL");
                }
                else
                {
                    var since = DateTime.UtcNow.AddYears(-years);
                    sql.Where("m.M_LASTPOSTDATE < @0", since.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));

                }
            }
            if (!String.IsNullOrWhiteSpace(form["LastVisit"]))
            {
                //older than x years
                var years = Convert.ToInt32(form["LastVisit"]);

                if (years == 0)
                {
                    sql.Where("m.M_LASTHEREDATE IS NULL");
                }
                else
                {
                    var since = DateTime.UtcNow.AddYears(-years);
                    sql.Where("m.M_LASTHEREDATE < @0", since.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));

                }
            }
            if (!String.IsNullOrWhiteSpace(form["Registered"]))
            {
                //older than x years
                var years = Convert.ToInt32(form["Registered"]);
                var since = DateTime.UtcNow.AddYears(-years);
                sql.Where("m.M_DATE < @0", since.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));
            }

        }
        private static void SortBy(string sortOrder, string sortCol, string dbType, ref Sql sql)
        {
            switch (sortCol)
            {
                case "user":
                    if (dbType == "mysql")
                        sql.OrderBy("m.M_NAME");//" COLLATE utf8mb4_danish_ci " + sortOrder);
                    else
                        sql.OrderBy("m.M_NAME COLLATE Danish_Norwegian_CI_AS " + sortOrder);
                    break;
                case "sincedate":
                    sql.OrderBy("m.M_DATE " + sortOrder);
                    break;
                case "lastvisit":
                    sql.OrderBy("m.M_LASTHEREDATE " + sortOrder);
                    break;
                case "posts":
                    sql.OrderBy("m.M_POSTS " + sortOrder);
                    break;
                case "lastpost":
                    sql.OrderBy("m.M_LASTPOSTDATE " + sortOrder);
                    break;
                case "country":
                    sql.OrderBy("m.M_COUNTRY " + sortOrder);
                    break;
                case "email":
                    sql.OrderBy("m.M_EMAIL " + sortOrder);
                    break;
                default:
                    sql.OrderBy("m.M_POSTS DESC");
                    sql.OrderBy("m.M_DATE DESC");

                    break;
            }
        }


        public static void ChangeOwnership(string tablename, string memberidfield, int primaryid, int secondaryid)
        {
            using (var db = new SnitzDataContext())
            {
                db.Execute("UPDATE " + tablename + " SET " + memberidfield + "=@0 WHERE " + memberidfield + "=@1",primaryid,secondaryid);
            }
        }
    }
}

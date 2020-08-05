using System;
using System.Linq;
using System.Web.Security;
using SnitzCore.Utility;
using SnitzMembership.Repositories;

namespace SnitzMembership.Models
{
    /// <summary>
    /// Class used to migrate a Snitz username & password to
    /// the SimpleMembership tables
    /// </summary>
    public class LegacySecurity
    {
        /// <summary>
        /// The user's profile record.
        /// </summary>
        private UserProfile userProfile;

        /// <summary>
        /// The users membership record.
        /// </summary>
        private SnitzSecurity membership;

        /// <summary>
        /// The clear text password.
        /// </summary>
        private string clearPassword;

        /// <summary>
        /// The password after it has been hashed using SHA256.
        /// </summary>
        private string sha256HashedPassword;

        /// <summary>
        /// The user's user name.
        /// </summary>
        private string userName;

        /// <summary>
        /// Inidcates if the authentication token in the cookie should be persisted beyond the current session.
        /// </summary>
        private bool persistCookie;

        /// <summary>
        /// Validates the user against legacy values.
        /// </summary>
        /// <param name="username">The user's UserName.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="persistcookie">Inidcates if the authentication token in the cookie should be persisted beyond the current session.</param>
        /// <returns>true if the user is validated and logged in, otherwise false.</returns>
        public bool Login(string username, string password, string passwordhashed , bool persistcookie = false)
        {
            this.userName = username;
            this.clearPassword = password;
            this.persistCookie = persistcookie;


            SetHashedPassword();

            if (this.sha256HashedPassword != passwordhashed)
            {
                return false;
            }
            if (!GetOriginalValues())
            {
                return false;
            }



            var res = SetPasswordAndLoginUser();
            return res;
        }

        /// <summary>
        /// Gets the original password values
        /// </summary>
        protected bool GetOriginalValues()
        {
            using (var context = new SnitzMembership.Repositories.SnitzMemberContext())
            {
                this.userProfile = context.UserProfiles.SingleOrDefault(x => x.UserName.ToLower() == userName.ToLower() && x.Password == this.sha256HashedPassword);

                if (this.userProfile == null)
                {
                    return false;
                }

                this.membership = context.Memberships.SingleOrDefault(x => x.UserId == this.userProfile.UserId);

                if (this.membership == null)
                {
                    //need to copy data here if it exists
                    return CopyExistingUser(this.userProfile.UserId);
                    //return false;
                }

                if (!this.membership.IsConfirmed)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetHashedPassword()
        {
            this.sha256HashedPassword = Common.SHA256Hash(clearPassword);
        }
        /// <summary>
        /// Sets the password using the new algorithm and perofrms a login.
        /// </summary>
        protected bool SetPasswordAndLoginUser()
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException(LangResources.Utility.ResourceManager.GetLocalisedString("Username_cannot_be_empty", "ErrorMessage"), "username");
            }
            var token = Guid.NewGuid().ToString();
            using (SnitzMemberContext db = new SnitzMemberContext())
            {
                var user = db.GetUser(userName);
                var member = db.GetMembership(user.UserId);

                if (member == null)
                {
                    member = new SnitzSecurity();
                    member.UserId = user.UserId;
                    member.CreateDate = user.CreatedDate;
                    member.Password = user.Password;
                    member.IsConfirmed = true;
                }

                if (member.PasswordVerificationTokenExpirationDate.HasValue &&
                    member.PasswordVerificationTokenExpirationDate.Value > DateTime.UtcNow)
                {
                    token = member.PasswordVerificationToken;
                }

                member.PasswordVerificationToken = token;
                member.PasswordVerificationTokenExpirationDate =
                    DateTime.UtcNow.AddMinutes((double)24 * 60);
                
                db.SaveChanges();

            }
            WebSecurity.ResetPassword(token, clearPassword);
            return WebSecurity.Login(userName, clearPassword, persistCookie);
        }

        protected bool CopyExistingUser(int userid)
        {
            var roles = Roles.Provider;

            using (SnitzMemberContext db = new SnitzMemberContext())
            {
                var forummember = db.UserProfiles.SingleOrDefault(m => m.UserId == userid);
                if (forummember != null)
                {
                    WebSecurity.CreateAccount(userName, clearPassword, false);
                    this.membership = db.GetMembership(userid);
                    
                    switch (forummember.UserLevel)
                    {
                        case 3 :
                            if (!roles.IsUserInRole(this.userProfile.UserName, "Administrator"))
                                roles.AddUsersToRoles(new[] {this.userProfile.UserName},new[] {"Administrator"});
                            break;
                        case 2:
                            if (!roles.IsUserInRole(this.userProfile.UserName, "Moderator"))
                                roles.AddUsersToRoles(new[] { this.userProfile.UserName }, new[] { "Moderator" });
                            break;

                    }                
                }
            }

            return true;
        }
    }
}
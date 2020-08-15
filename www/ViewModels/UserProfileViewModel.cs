using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Security;
using SnitzCore.Filters;
using SnitzMembership;
using SnitzMembership.Models;
using Member = SnitzDataModel.Models.Member;


namespace WWW.ViewModels
{
    public class UserProfileViewModel
    {
        public UserProfile Profile { get; set; }
        //public LocalPasswordModel PasswordModel { get; set; }
        //public ChangeEmailModel EmailModel { get; set; }
        //public ChangeUsernameModel UsernameModel { get; set; }

        [UIHint("YesNo")]
        [LocalisedDisplayName(ResourceType : "General", Name : "IsApproved")]
        public bool IsApproved
        {
            get
            {
                MembershipUser usr = Membership.GetUser(this.Profile.UserName);
                return usr != null && usr.IsApproved;
            }

        }
        [UIHint("YesNo")]
        public bool IsLockedout
        {
            get
            {
                if (WebSecurity.IsAccountLockedOut(this.Profile.UserName, 500, 100))
                {
                    return true;
                }
                return false;
            }
        }

        public string UserTime { get; set; }
    }

    public class ListUserViewModel
    {
        public List<Member> Users { get; set; }
        public string Initial { get; set; }

        public int? LastPost { get; set; }
        public int? LastVisit { get; set; }
        public int? Registered { get; set; }

        public string EmailMessage { get; set; }
        public string EmailSubject { get; set; }

        public UserSearchViewModel SearchForm { get; set; }
        public bool Confirmed { get; set; }
    }
    public class UserSearchViewModel
    {
        public string Username { get; set; }
        public string Firstname { get; set; }

        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }

    }
}
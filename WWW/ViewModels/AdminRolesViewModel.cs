using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Web;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;
using SnitzMembership.Models;
using DataType = System.ComponentModel.DataAnnotations.DataType;
using Forum = SnitzDataModel.Models.Forum;
using SpamFilter = SnitzDataModel.Models.SpamFilter;
using Subscriptions = SnitzDataModel.Models.Subscriptions;


namespace WWW.ViewModels
{
    public class AdminRolesViewModel
    {
        [RequiredIf("IsUsernameRequired", "true", "PropertyRequired")]
        public string Username { get; set; }
        [RequiredIf("IsUsernameRequired", "true", "PropertyRequired")]
        public string Rolename { get; set; }

        [RequiredIf("IsRolenameRequired", "true", "PropertyRequired")]
        public string NewRolename { get; set; }

        public List<UserProfile> Members { get; set; }

        public string[] RoleList { get; set; }

        public bool IsUsernameRequired { get; set; }
        public bool IsRolenameRequired { get; set; }
    }

    public class AdminGroupsViewModel
    {

        public int CurrentGroupId { get; set; }
        public List<Group> Groups { get; set; }
        public Dictionary<int, string> CurrentGroupForums { get; set; }

        public AdminGroupsViewModel(int id)
        {
            this.CurrentGroupId = id;
            if (id > 0)
            {
                this.CurrentGroupForums = new Dictionary<int, string>();
                foreach (KeyValuePair<int, string> forum in ForumGroup.List(id).ToDictionary(t => t.Key, t => t.Value))
                {
                    this.CurrentGroupForums.Add(forum.Key, forum.Value);
                }
            }
            else
            {
                this.CurrentGroupForums = null;
            }
            


        }
    }
    public class AdminSubscriptionsViewModel
    {
        public List<Subscriptions> Subscriptions { get; set; }

        public Enumerators.SubscriptionLevel SubscriptionLevel { get; set; }
        public Enumerators.SubscriptionLevel VisibleLevel { get; set; }
    }

    public class AdminEmailServer
    {
        [SnitzCore.Filters.Required]
        public string ContactEmail { get; set; }

        [SnitzCore.Filters.Required]
        public string Server { get; set; }
        public bool Auth { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        [SnitzCore.Filters.DataType(DataType.Password)]
        public string Password { get; set; }
        [SnitzCore.Filters.Required]
        public string From { get; set; }

        public SmtpDeliveryMethod DeliveryMethod { get; set; }
        public string PickUpFolder { get; set; }
        public bool DefaultCred { get; set; }

        public string UseSpamFilter { get; set; }
        public string EmailDomain { get; set; }
        public SpamFilter[] BannedDomains { get; set; }
        public string EmailMode { get; set; }
    }

    public class AdminFeaturesViewModel
    {
        public List<Enumerators.CaptchaOperator> CaptchaOperators { get { return SnitzConfig.Config.CaptchaOperators ?? new List<Enumerators.CaptchaOperator>() { Enumerators.CaptchaOperator.Plus }; } }
        public Dictionary<string, string> Config
        {
            get
            {
                return ClassicConfig.ConfigDictionary;
            }
        }
        public Enumerators.SubscriptionLevel SubscriptionLevel { get; set; }
        public Dictionary<int,string> AllowedForums { get; set; }

        public Dictionary<int, string> ForumList
        {
            get
            {
                List<int> allowed = new List<int>(ClassicConfig.GetValue("STRAPIFORUMS").StringToIntList());
                this.AllowedForums = new Dictionary<int, string>();

                var forumList = new Dictionary<int, string>();
                var forums = Forum.List(HttpContext.Current.User);
                foreach (KeyValuePair<int, string> forum in forums.ToDictionary(t => t.Key, t => t.Value))
                {
                    forumList.Add(forum.Key, forum.Value);
                    if (allowed.Contains(forum.Key))
                    {
                        this.AllowedForums.Add(forum.Key, forum.Value);
                    }
                }
                return forumList;
            }
        }

        public object ForumId { get; set; }

        public string GetValue(string key, string def="")
        {
            if (Config.ContainsKey(key))
            {
                return Config[key];
            }
            return def;
        }
    }

    public class AdminModeratorsViewModel
    {
        public int ForumId { get; set; }
        public int MemberId { get; set; }

        public Dictionary<int, string> ForumList { get; set; }
        public Dictionary<int, string> ModList { get; set; }
        public ICollection<int> ForumModerators { get; set; }

        public AdminModeratorsViewModel()
        {
            this.ForumModerators = new Collection<int>();
        }
        public AdminModeratorsViewModel(IPrincipal user)
        {

            this.ForumList = new Dictionary<int, string> { { -1, "--Select Forum--" } };
            foreach (KeyValuePair<int, string> forum in Forum.List(user).ToDictionary(t => t.Key, t => t.Value))
            {
                this.ForumList.Add(forum.Key, forum.Value);
            }
            this.ForumModerators = new Collection<int>();           
        }
    }
}
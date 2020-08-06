using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Filters;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;
using SnitzMembership.Models;
using Forum = SnitzDataModel.Models.Forum;
using Topic = SnitzDataModel.Models.Topic;


namespace WWW.ViewModels
{
    public class SearchViewModel
    {
        //dropdowns
        public Dictionary<int, string> ForumList { get; set; }
        public Dictionary<int, string> DaysList
        {
            get
            {
                return new Dictionary<int, string>
                       {
                           {0, "Any Date"},
                           {1, "Since Yesterday"}, 
                           {2, "Since 2 days ago"},
                           {5, "Since 5 days ago"},
                           {7, "Since a week ago"},
                           {14, "Since 2 weeks ago"},
                           {30, "In the last month"},
                           {60, "In the last 2 months"},
                           {120, "In the last 6 months"},
                           {365, "In the last year"}
                       };

            }
        }

        //search parameters
        public FullSearchModel SearchModel { get; set; }
        
        //Paging params
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public long TotalRecords { get; set; }
        public int Page { get; set; }

        //right column collections
        public List<Topic> RecentTopics { get; set; }

        public List<UserProfile> OnlineUsers { get; set; }

        public string OrderBy { get; set; }

        public string SortDir { get; set; }
        public SearchViewModel(IPrincipal user)
        {
            this.SearchModel = new FullSearchModel {PhraseType = Enumerators.SearchWordMatch.ExactPhrase};
            this.OrderBy = "t";
            this.SortDir = "ASC";
            this.ForumList = Config.AllowSearchAllForums && user.Identity.IsAuthenticated ? new Dictionary<int, string> {{0, LangResources.Utility.ResourceManager.GetLocalisedString("mnuForumAll", "Menu")}} : new Dictionary<int, string> { { -1, "Select Forum" } };
            foreach (KeyValuePair<int, string> forum in Forum.List(user).ToDictionary(t => t.Key, t => t.Value))
            {
                this.ForumList.Add(forum.Key, forum.Value);
            }
            
        }
    }

    public class PMSearchViewModel
    {
        [LocalisedDisplayName("Search_Term", "labels")]
        [RequiredIf("MemberName", "", "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100, MinimumLength = 3)]
        public string Term { get; set; }

        [SnitzCore.Filters.Required]
        public Enumerators.SearchWordMatch PhraseType { get; set; }

        [LocalisedDisplayName("Search_Date", "labels")]
        public Enumerators.SearchDays SearchByDays { get; set; }

        [LocalisedDisplayName("Search_Member", "labels")]
        [Remote("UserExists", "Account")]
        public string MemberName { get; set; }

        [SnitzCore.Filters.Required]
        [LocalisedDisplayName("Search_In", "labels")]
        public Enumerators.SearchIn SearchIn { get; set; }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Snitz.Base;
using SnitzCore.Filters;
using SnitzDataModel.Validation;

namespace SnitzDataModel.Models
{
    public class SearchModel
    {
        [SnitzCore.Filters.Required]
        public string Term { get; set; }

        public int? ForumId { get; set; }
        public int? TopicId { get; set; }
        public string MemberName { get; set; }

        public bool Grouping { get; set; }
        public bool SubjectOnly { get; set; }
        public int? Category { get; set; }
    }

    public class FullSearchModel
    {
        private bool archived = false;

        [LocalisedDisplayName("Search_Term","labels")]
        [RequiredIf("SearchModel_MemberName", "", "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100,MinimumLength = 3)]
        public string Term { get; set; }
        
        [LocalisedDisplayName("Search_Forum", "labels")]
        [SnitzCore.Filters.Range(0, int.MaxValue, ErrorMessage = "ErrSelectForum")]
        public int ForumId { get; set; }

        public string ForumIds { get; set; }

        [LocalisedDisplayName("Search_Member", "labels")]
        //[Remote("UserExists", "Account")]
        public string MemberName { get; set; }
        
        [LocalisedDisplayName("Search_Date", "labels")]
        public Enumerators.SearchDays SearchByDays { get; set; }

        [SnitzCore.Filters.Required]
        public Enumerators.SearchWordMatch PhraseType { get; set; }
        [SnitzCore.Filters.Required]
        public Enumerators.FullTextMatch FullTextType { get; set; }

        [SnitzCore.Filters.Required]
        [LocalisedDisplayName("Search_In", "labels")]
        public Enumerators.SearchIn SearchIn { get; set; }

        [LocalisedDisplayName("Search_Archive", "labels")]
        public bool Archived
        {
            get { return this.archived; }
            set { this.archived = value; }
        }
        [LocalisedDisplayName("Search_Terms", "labels")]
        public string[] Terms { get; set; }

        public int PageNo { get; set; }
        [LocalisedDisplayName("Search_Category", "labels")]
        public int? Category { get; set; }
        [LocalisedDisplayName("Search_CategoryList", "labels")]
        public IEnumerable<Category> CategoryList { get; set; }

        public string OrderBy { get; set; }
        public string SortDir { get; set; }
    }


    public class SearchResult
    {
        public Models.Forum Forum { get; set; }
        public List<Models.Topic> Topics { get; set; }
        public List<Models.Reply> Replies  { get; set; }
        public SearchModel Params { get; set; }
        public FullSearchModel FullParams { get; set; }

        public bool Archived { get; set; }

        //Paging params
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public long TotalRecords { get; set; }
        public int Page { get; set; }
        public Category Category { get; set; }
    }
}
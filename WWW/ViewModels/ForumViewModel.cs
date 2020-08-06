using System.Collections.Generic;
using Snitz.Base;
using SnitzDataModel.Models;
using Forum = SnitzDataModel.Models.Forum;
using Topic = SnitzDataModel.Models.Topic;


namespace WWW.ViewModels
{
    /// <summary>
    /// Home page ViewModel.
    /// contains collections required for the home page
    /// </summary>
    public class ForumViewModel
    {
        public int Id { get; set; }
        public Forum Forum { get; set; }
        public List<Topic> Topics { get; set; }
        public List<Topic> StickyTopics { get; set; } 

        //Paging params
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public long TotalRecords { get; set; }
        public int Page { get; set; }

        //right column collections
        public List<ActiveTopic> RecentTopics { get; set; }
        public Enumerators.ForumDays DefaultDays { get; set; }
        public Enumerators.ActiveTopicsSince ActiveSince { get; set; }
        public string OrderBy { get; set; }
        public string SortDir { get; set; }
    }

    public class ArchiveViewModel
    {
        public int ForumId { get; set; }
        public int MonthsOlder { get; set; }
    }
}
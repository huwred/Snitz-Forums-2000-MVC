using System;
using PetaPoco;
using Snitz.Base;
using System.Collections.Generic;
using SnitzDataModel.Models;
using Topic = SnitzDataModel.Models.Topic;


namespace WWW.ViewModels
{
    public class ActiveTopicsViewModel
    {
        public Enumerators.ActiveTopicsSince ActiveSince { get; set; }
        public Enumerators.ActiveRefresh Refresh { get; set; }
        public Page<Topic> RecentTopics { get; set; }
    }

    public class MyTopicsViewModel
    {
        public Enumerators.MyTopicsSince ActiveSince { get; set; }
        public Enumerators.ActiveRefresh Refresh { get; set; }
        public List<Topic> Topics { get; set; }
        public int PageCount { get; internal set; }
        public int Page { get; internal set; }
        public long TotalRecords { get; internal set; }
        public IEnumerable<MyViewTopic> AllTopics { get; set; }
        public Enumerators.ForumDays DefaultDays { get; set; }
    }


}
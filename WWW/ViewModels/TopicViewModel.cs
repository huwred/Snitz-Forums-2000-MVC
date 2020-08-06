using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using Snitz.Base;
using Forum = SnitzDataModel.Models.Forum;
using Reply = SnitzDataModel.Models.Reply;
using Topic = SnitzDataModel.Models.Topic;


namespace WWW.ViewModels
{
    /// <summary>
    /// View Model for Topic Display
    /// </summary>
    public class TopicViewModel
    {
        public int Id { get; set; }
        public Forum Forum { get; set; }
        public Topic Topic { get; set; }
        public IEnumerable<Reply> Replies { get; set; }

        //Paging params
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public long TotalRecords { get; set; }
        public int Page { get; set; }

        public Dictionary<int, string> ForumList { get; set; }
        public int ForumId { get; set; }
        public string Subject { get; set; }

        public TopicViewModel(){}
        public TopicViewModel(IPrincipal user)
        {
            this.ForumList = new Dictionary<int, string> { { -1, "Select Forum" } };
            foreach (KeyValuePair<int, string> forum in Forum.List(user).ToDictionary(t => t.Key, t => t.Value))
            {
                if(!this.ForumList.ContainsKey(forum.Key))
                    this.ForumList.Add(forum.Key, forum.Value);
            }           
        }
        public Enumerators.ActiveTopicsSince ActiveSince { get; set; }
    }

    /// <summary>
    /// View Model for Topic Navigator
    /// </summary>
    public class TopicNavViewModel
    {
        public int PreviousTopic { get; set; }
        public int NextTopic { get; set; }
    }

    /// <summary>
    /// View Model for Splitting Topics
    /// </summary>
    public class SplitTopicViewModel
    {
        public int Id { get; set; }
        public Topic Topic { get; set; }
        public IEnumerable<Reply> Replies { get; set; }

        public Dictionary<int, string> ForumList { get; set; }

        [Required]
        [Range(0, Int32.MaxValue,ErrorMessage = "You must select a Forum")]
        public int ForumId { get; set; }
        [Required]
        public string Subject { get; set; }

        public SplitTopicViewModel() { }
        public SplitTopicViewModel(IPrincipal user)
        {
            this.ForumList = new Dictionary<int, string> { { -1, "Select Forum" } };
            foreach (KeyValuePair<int, string> forum in Forum.List(user).ToDictionary(t => t.Key, t => t.Value))
            {
                if(!this.ForumList.ContainsKey(forum.Key))
                    this.ForumList.Add(forum.Key, forum.Value);
            }
        }

    }

    /// <summary>
    /// View Model for Post Moderation
    /// </summary>
    public class ApproveTopicViewModal
    {
        public string ApprovalMessage { get; set; }
        [Required(ErrorMessage = "Please select an Action")]
        public string PostStatus { get; set; }
        public int Id { get; set; }

        public bool EmailAuthor { get; set; }
    }
}
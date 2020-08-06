using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Filters;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;
using DataType = System.ComponentModel.DataAnnotations.DataType;
using Forum = SnitzDataModel.Models.Forum;


namespace WWW.ViewModels
{
    public enum PostType
    {
        Category,
        Forum,
        Topic,
        Reply
    }
    /// <summary>
    /// Object for use with post forms, Reply or Topic
    /// </summary>
    public class PostMessageViewModel 
    {
        public int CatId { get; set; }
        public int ForumId { get; set; }
        public int TopicId { get; set; }
        public int ReplyId { get; set; }
        public int? ReplyToId { get; set; }
        public PostType Type { get; set; }

        public bool AllowRating { get; set; }

        [AllowHtml]
        [RequiredIf("TopicId", 0, "PropertyRequired")] //only require a subject if it's a new topic
        [SnitzCore.Filters.StringLength(100)]
        public string Subject { get; set; }

        [AllowHtml]
        [SnitzCore.Filters.Required(ErrorMessageResourceName = "MessageRequired")]
        [SnitzCore.Filters.DataType(DataType.MultilineText)]
        [LocalisedDisplayName(Name: "ButtonFormatMode", ResourceType: "labels")]
        public string Message { get; set; }

        [LocalisedDisplayName(Name : "cbxShowSig", ResourceType : "labels")]
        public bool UseSignature { get; set; }
        [LocalisedDisplayName(Name : "cbxLockTopic", ResourceType : "labels")]
        public bool Lock { get; set; }
        [LocalisedDisplayName(Name : "cbxMakeSticky", ResourceType : "labels")]
        public bool Sticky { get; set; }
        [LocalisedDisplayName(Name : "cbxNoArchive", ResourceType : "labels")]
        public bool DoNotArchive { get; set; }
        [LocalisedDisplayName(Name: "cbxDraft", ResourceType: "labels")]
        public bool SaveDraft { get; set; }
        [LocalisedDisplayName(Name : "cbxAllowRating", ResourceType : "labels")]
        public bool AllowTopicRating { get; set; }

        public bool SubscribeTopic { get; set; }

        public bool IsAuthor { get; set; }
        public bool IsPoll { get; set; }
        public string Referrer { get; set; }
        [ScriptIgnore]
        public Dictionary<int, string> ForumList { get; set; }

        //Polls
        public int PollId { get; set; }
        [RequiredIf("IsPoll", true, "PropertyRequired")]
        public string PollQuestion { get; set; }
        public List<PollAnswer> PollAnswers { get; set; }
        public string PollRoles { get; set; }

        public decimal PostRating { get; set; }

        public PostMessageViewModel() { }

        public void SetForumList(IPrincipal user)
        {
            this.ForumList =  new Dictionary<int, string> ();
            var forums = Forum.List(user);

            if (ClassicConfig.GetValue("STRMOVETOPICMODE") == "1" && !user.IsAdministrator())
            {
                var modforumlist = user.ModeratedForums();
                foreach (KeyValuePair<int, string> forum in forums.ToDictionary(t => t.Key, t => t.Value))
                {
                    if (modforumlist.Contains(forum.Key))
                        this.ForumList.Add(forum.Key, forum.Value);
                } 
            }
            else
            {
                foreach (KeyValuePair<int, string> forum in forums.ToDictionary(t => t.Key, t => t.Value))
                {
                    this.ForumList.Add(forum.Key, forum.Value);
                }                
            }

        }

        [LocalisedDisplayName(Name : "ButtonFormatMode", ResourceType : "labels")]
        public Enumerators.PostButtonMode FormatMode { get; set; }
        public bool IsDraft { get; set; }
        public bool IsBlogPost { get; set; }
        public bool IsBugPost { get; set; }
        public bool Fixed { get; set; }

        public int pagenum { get; set; }
        public int Archived { get; set; }
    }

    public class PreviewPostViewModel
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool ShowSig { get; set; }
        public int ReplyId { get; set; }
        public int TopicId { get; set; }
        public string Signature { get; set; }
    }
}
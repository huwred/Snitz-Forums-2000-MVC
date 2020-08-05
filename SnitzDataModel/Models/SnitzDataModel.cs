
// /*
// ####################################################################################################################
// ##
// ## SnitzDataModel
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Validation;

namespace SnitzDataModel.Models
{

    [TableName("CATEGORY", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("CAT_ID")]
    [ExplicitColumns]
    public partial class Category : SnitzDataContext.Record<Category>
    {
        [Column("CAT_ID")]
        public int Id { get; set; }

        [Column("CAT_STATUS")]
        [LocalisedDisplayName(Name: "catStatus", ResourceType: "Admin")]
        public Enumerators.Status Status { get; set; }

        [Column("CAT_NAME")]
        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.StringLength(100)]
        [LocalisedDisplayName(Name: "catName", ResourceType: "Admin")]
        public string Title { get; set; }

        [Column("CAT_MODERATION")]
        [LocalisedDisplayName(Name: "catModeration", ResourceType: "Admin")]
        public Enumerators.ModerationLevel Moderation { get; set; }

        [Column("CAT_SUBSCRIPTION")]
        [LocalisedDisplayName(Name: "catSubscription", ResourceType: "Admin")]
        public Enumerators.CategorySubscription Subscription { get; set; }

        [Column("CAT_ORDER")]
        [LocalisedDisplayName(Name: "catOrder", ResourceType: "Admin")]
        public int? Order { get; set; }


    }

    [TableName("CONFIG_NEW", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("ID")]
    [ExplicitColumns]
    public class ConfigNew : SnitzDataContext.Record<ConfigNew>
    {
        [Column] public int ID { get; set; }

        [Column("C_VARIABLE")]
        public string Variable { get; set; }

        [Column("C_VALUE")]
        public string Value { get; set; }
    }

    [TableName("FORUM", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("FORUM_ID")]
    [ExplicitColumns]
    public partial class Forum : SnitzDataContext.Record<Forum>
    {
        [Column("CAT_ID")]
        [LocalisedDisplayName(Name: "forumCategory", ResourceType: "Admin")]
        public int CatId { get; set; }

        [Column("FORUM_ID")]
        public int Id { get; set; }

        [Column("F_STATUS")]
        [LocalisedDisplayName(Name: "forumStatus", ResourceType: "Admin")]
        public Enumerators.PostStatus Status { get; set; }

        [Column("F_MAIL")]
        public short? Mail { get; set; }

        [Column("F_SUBJECT")]
        [LocalisedDisplayName(Name: "forumSubject", ResourceType: "Admin")]
        [RequiredIf("Type", Enumerators.ForumType.Topics, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100)]
        public string Subject { get; set; }

        [Column("F_URL")]
        [RequiredIf("Type", Enumerators.ForumType.WebLink, "PropertyRequired")]
        [Url(ErrorMessage = "You must provide a valid url")]
        [SnitzCore.Filters.StringLength(255)]
        public string Url { get; set; }

        [Column("F_DESCRIPTION")]
        [LocalisedDisplayName(Name: "forumDescription", ResourceType: "Admin")]
        public string Description { get; set; }

        [Column("F_TOPICS")]
        public int TopicCount { get; set; }

        [Column("F_COUNT")]
        public int PostCount { get; set; }

        [Column("F_LAST_POST")]
        [SnitzCore.Filters.StringLength(14)]
        public String LastPost { get; set; }

        [Column("F_PASSWORD_NEW")]
        [Display(Name = "Password")]
        [SnitzCore.Filters.StringLength(255, MinimumLength = 3)]
        public string PasswordNew { get; set; }

        [Column("F_PRIVATEFORUMS")]
        [LocalisedDisplayName(Name: "forumAuthType", ResourceType: "Admin")]
        public Enumerators.ForumAuthType PrivateForums { get; set; }

        [Column("F_TYPE")]
        [LocalisedDisplayName(Name: "forumType", ResourceType: "Admin")]
        public Enumerators.ForumType Type { get; set; }

        [Column("F_IP")]
        public string IPAddress { get; set; }

        [Column("F_LAST_POST_AUTHOR")]
        public int? LastPostAuthorId { get; set; }

        [Column("F_LAST_POST_TOPIC_ID")]
        public int? LastPostTopicId { get; set; }

        [Column("F_LAST_POST_REPLY_ID")]
        public int? LastPostReplyId { get; set; }

        [Column("F_A_TOPICS")]
        public int? ArchivedTopicCount { get; set; }

        [Column("F_A_COUNT")]
        public int? ArchivedPostCount { get; set; }

        [Column("F_DEFAULTDAYS")]
        [LocalisedDisplayName(Name: "forumView", ResourceType: "Admin")]
        public Enumerators.ForumDays DefaultDays { get; set; }

        [Column("F_COUNT_M_POSTS")]
        [LocalisedDisplayName(Name: "forumPostCount", ResourceType: "Admin")]
        public short? IncrementPostCount { get; set; }

        [Column("F_MODERATION")]
        [LocalisedDisplayName(Name: "forumModeration", ResourceType: "Admin")]
        public Enumerators.Moderation Moderation { get; set; }

        [Column("F_SUBSCRIPTION")]
        [LocalisedDisplayName(Name: "forumSubscription", ResourceType: "Admin")]
        public Enumerators.Subscription Subscription { get; set; }

        [Column("F_ORDER")]
        [LocalisedDisplayName(Name: "forumOrder", ResourceType: "Admin")]
        public int Order { get; set; }

        [Column("F_L_ARCHIVE")]
        [SnitzCore.Filters.StringLength(14)]
        public string LastArchive { get; set; }

        [Column("F_ARCHIVE_SCHED")]
        public int ArchiveSchedule { get; set; }

        [Column("F_L_DELETE")]
        [SnitzCore.Filters.StringLength(14)]
        public string LastDeletion { get; set; }

        [Column("F_DELETE_SCHED")]
        public int DeleteSchedule { get; set; }
        [Column("F_POLLS")]
        [LocalisedDisplayName(Name: "forumPollsAuth", ResourceType: "Admin")]
        public Enumerators.PollAuth PollsAuth { get; set; }
        [Column("F_RATING")]
        [LocalisedDisplayName(Name: "forumRating", ResourceType: "Admin")]
        public short AllowRating { get; set; }
        [Column("F_POSTAUTH")]
        [LocalisedDisplayName(Name: "postAuthType", ResourceType: "Admin")]
        public Enumerators.PostAuthType PostAuth { get; set; }

        [Column("F_REPLYAUTH")]
        [LocalisedDisplayName(Name: "replyAuthType", ResourceType: "Admin")]
        public Enumerators.PostAuthType ReplyAuth { get; set; }

        public DateTime? LastPostDate
        {
            get { return LastPost.ToDateTime(); }
            set
            {
                LastPost = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        public DateTime? LastArchiveDate
        {
            get { return LastArchive.ToDateTime(); }
            set
            {
                LastArchive = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        public DateTime? LastDeletionDate
        {
            get { return LastDeletion.ToDateTime(); }
            set
            {
                LastDeletion = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }


    }

    [TableName("MEMBERS", prefixType = Extras.TablePrefixTypes.Member)]
    [PrimaryKey("MEMBER_ID")]
    [ExplicitColumns]
    public partial class Member : SnitzDataContext.Record<Member>
    {
        [Column("MEMBER_ID")]
        public int Id { get; set; }

        /// <summary>
        /// Not used for general functionality, replicated here for setup of Admin account
        /// </summary>
        [Column("M_STATUS")]
        public short IsValid { get; set; }
        [Column("M_NAME")]
        [SnitzCore.Filters.StringLength(75)]
        public string Username { get; set; }
        [Column("M_PASSWORD")]
        [SnitzCore.Filters.StringLength(65)]
        public string SnitzPassword { get; set; }
        [Column("M_LEVEL")]
        public short UserLevel { get; set; }
        [Column("M_COUNTRY")]
        public string Country { get; set; }
        [Column("M_HOMEPAGE")]
        public string Homepage { get; set; }
        [Column("M_SIG")]
        public string Signature { get; set; }

        [Column("M_AIM")]
        public string AIM { get; set; }
        [Column("M_ICQ")]
        public string ICQ { get; set; }
        [Column("M_MSN")]
        public string MSN { get; set; }
        [Column("M_YAHOO")]
        public string YAHOO { get; set; }

        [Column("M_POSTS")]
        public int PostCount { get; set; }
        [Column("M_TITLE")]
        public string ForumTitle { get; set; }
        [Column("M_PHOTO_URL")]
        public string PhotoUrl { get; set; }

        [Column("M_DATE")]
        [Display(Name = "MemberDate")]
        [SnitzCore.Filters.StringLength(14)]
        public String Created { get; set; }
        [Column("M_LASTHEREDATE")]
        [SnitzCore.Filters.StringLength(14)]
        public String LastVisit { get; set; }
        [Column("M_LASTPOSTDATE")]
        [SnitzCore.Filters.StringLength(14)]
        public String LastPost { get; set; }
        [Column("M_LASTACTIVITY")]
        [SnitzCore.Filters.StringLength(14)]
        public String LastActivity { get; set; }

        [Column("M_LAST_IP")]
        public string LastIP { get; set; }

        [Column("M_RECEIVE_EMAIL")]
        public short ReceiveEmails { get; set; }

        [Column("M_EMAIL")]
        public string Email { get; set; }
        [Column("M_NEWEMAIL")]
        public string NewEmail { get; set; }
        [Column("M_SIG_DEFAULT")]
        public short SigDefault { get; set; }

        [Column("M_PMEMAIL")]
        public int PrivateMessageNotify { get; set; }
        [Column("M_PMRECEIVE")]
        public int PrivateMessageReceive { get; set; }
        [Column("M_PMSAVESENT")]
        public short PrivateMessageSentItems { get; set; }
        [ResultColumn]
        public int? Disabled { get; set; }
        [ResultColumn]
        public bool? Confirmed { get; set; }

        [LocalisedDisplayName(Name: "ProfileDate", ResourceType: "General")]
        public DateTime? CreatedDate
        {
            get { return Created.ToDateTime(); }
            set
            {
                Created = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [LocalisedDisplayName(Name: "ProfileLastVisit", ResourceType: "General")]
        public DateTime? LastVisitDate
        {
            get { return LastVisit.ToDateTime(); }
            set
            {
                LastVisit = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [LocalisedDisplayName(Name: "ProfileLastPost", ResourceType: "General")]
        public DateTime? LastPostDate
        {
            get { return LastPost.ToDateTime(); }
            set
            {
                LastPost = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        public DateTime? LastActivityDate
        {
            get { return LastActivity.ToDateTime(); }
            set
            {
                LastActivity = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        public static void UpdateLastActivity(int id)
        {
            string ipString = "";
            if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
            {
                ipString = ",M_LAST_IP='" + Common.GetUserIP(System.Web.HttpContext.Current) + "'";
            }
            //member.Save();
            using (var context = new SnitzDataContext())
            {
                var sql = "UPDATE " + context.MemberTablePrefix + "MEMBERS SET M_LASTACTIVITY=@0" + ipString + " WHERE MEMBER_ID=" + id;
                context.Execute(sql, DateTime.UtcNow.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));
            }
        }
    }

    [TableName("PM", prefixType = Extras.TablePrefixTypes.Member)]
    [PrimaryKey("M_ID")]
    [ExplicitColumns]
    public partial class PrivateMessage : SnitzDataContext.Record<PrivateMessage>
    {
        [Column("M_ID")]
        public int Id { get; set; }

        [Column("M_SUBJECT")]
        public string Subject { get; set; }

        [Column("M_FROM")]
        public int FromMemberId { get; set; }

        [Column("M_TO")]
        public int ToMemberId { get; set; }

        [Column("M_SENT")]
        [SnitzCore.Filters.StringLength(14)]
        public String Sent { get; set; }

        [Column("M_MESSAGE")]
        public string Message { get; set; }

        [Column("M_PMCOUNT")]
        public string PmCount { get; set; }

        [Column("M_READ")]
        public int Read { get; set; }

        [Column("M_MAIL")]
        public string Email { get; set; }

        [Column("M_OUTBOX")]
        public short ShowOutBox { get; set; }

        [Column("PM_DEL_FROM")]
        public int DeleteFrom { get; set; }
        [Column("PM_DEL_TO")]
        public int DeleteTo { get; set; }

        [SnitzCore.Filters.StringLength(14)]
        public DateTime? SentDate
        {
            get { return Sent.ToDateTime(); }
            set
            {
                Sent = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [ResultColumn]
        public string ToUsername { get; set; }
        [ResultColumn]
        public string FromUsername { get; set; }

        [ResultColumn]
        public bool Selected { get; set; }

    }

    [TableName("PM_BLOCKLIST", prefixType = Extras.TablePrefixTypes.Member)]
    [PrimaryKey("BL_ID")]
    [ExplicitColumns]
    public partial class PrivateBlocklist : SnitzDataContext.Record<PrivateBlocklist>
    {
        [Column("BL_ID")]
        public int Id { get; set; }
        [Column("BL_MEMBER_ID")]
        public int MemberId { get; set; }
        [Column("BL_BLOCKED_ID")]
        public int BlockedMemberId { get; set; }
        [Column("BL_BLOCKED_NAME")]
        public string BlockedMemberName { get; set; }
    }

    [TableName("MODERATOR", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("MOD_ID")]
    [ExplicitColumns]
    public partial class ForumModerators : SnitzDataContext.Record<ForumModerators>
    {
        [Column("MOD_ID")]
        public int Id { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }

        [Column("MEMBER_ID")]
        public int MemberId { get; set; }

        [Column("MOD_TYPE")]
        public short ModType { get; set; }
    }

    [TableName("REPLY", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("REPLY_ID")]
    [ExplicitColumns]
    public partial class Reply : SnitzDataContext.Record<Reply>
    {

        [Column("CAT_ID")]
        public int CatId { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }

        [Column("TOPIC_ID")]
        public int TopicId { get; set; }

        [Column("REPLY_ID")]
        public int Id { get; set; }

        [Column("R_MAIL")]
        public short? Mail { get; set; }

        [Column("R_AUTHOR")]
        public int AuthorId { get; set; }

        [Column("R_MESSAGE")]
        public string Message { get; set; }

        [Column("R_DATE")]
        [SnitzCore.Filters.StringLength(14)]
        public String Created { get; set; }

        [Column("R_IP")]
        public string PosterIp { get; set; }

        [Column("R_STATUS")]
        public Enumerators.PostStatus PostStatus { get; set; }

        [Column("R_LAST_EDIT")]
        [SnitzCore.Filters.StringLength(14)]
        public DateTime? LastEditDate { get; set; }

        [Column("R_LAST_EDITBY")]
        public int? LastEditUserId { get; set; }

        [Column("R_SIG")]
        public short? ShowSig { get; set; }

        [Column("R_RATING")]
        public int Rating { get; set; }

        [SnitzCore.Filters.StringLength(14)]
        public DateTime? Date
        {
            get { return Created.ToDateTime(); }
            set
            {
                Created = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

    }

    [TableName("TOPICS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("TOPIC_ID")]
    [ExplicitColumns]
    public partial class Topic : SnitzDataContext.Record<Topic>
    {
        [Column("CAT_ID")]
        public int CatId { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }

        [Column("TOPIC_ID")]
        public int Id { get; set; }

        [Column("T_STATUS")]
        public Enumerators.PostStatus PostStatus { get; set; }

        [Column("T_MAIL")]
        public short? Mail { get; set; }

        [Column("T_SUBJECT")]
        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.StringLength(100)]
        public string Subject { get; set; }

        [Column("T_MESSAGE")]
        [SnitzCore.Filters.Required]
        public string Message { get; set; }

        [Column("T_AUTHOR")]
        public int AuthorId { get; set; }

        [Column("T_REPLIES")]
        public int ReplyCount { get; set; }

        [Column("T_UREPLIES")]
        public int? UnmoderatedReplyCount { get; set; }

        [Column("T_VIEW_COUNT")]
        public int? ViewCount { get; set; }

        [Column("T_LAST_POST")]
        [SnitzCore.Filters.StringLength(14)]
        public String LastPost { get; set; }

        [Column("T_DATE")]
        [SnitzCore.Filters.StringLength(14)]
        public String Created { get; set; }

        [Column("T_LAST_POSTER")]
        public int? LastPoster { get; set; }

        [Column("T_IP")]
        public string PosterIp { get; set; }

        [Column("T_LAST_POST_AUTHOR")]
        public int? LastPostAuthorId { get; set; }

        [Column("T_LAST_POST_REPLY_ID")]
        public int? LastPostReplyId { get; set; }

        [Column("T_ARCHIVE_FLAG")]
        public int? DoNotArchive { get; set; }

        [Column("T_LAST_EDIT")]
        [SnitzCore.Filters.StringLength(14)]
        public String Edited { get; set; }

        [Column("T_LAST_EDITBY")]
        public int? LastEditUserId { get; set; }

        [Column("T_STICKY")]
        public short? IsSticky { get; set; }

        [Column("T_SIG")]
        public short? ShowSig { get; set; }

        [Column("T_ISPOLL")]
        public short IsPoll { get; set; }
        [Column("T_POLLSTATUS")]
        public short PollActive { get; set; }

        [SnitzCore.Filters.StringLength(14)]
        public DateTime? LastPostDate
        {
            get { return LastPost.ToDateTime(); }
            set
            {
                LastPost = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [SnitzCore.Filters.StringLength(14)]
        public DateTime? Date
        {
            get { return Created.ToDateTime(); }
            set
            {
                Created = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [SnitzCore.Filters.StringLength(14)]
        public DateTime? LastEditDate
        {
            get { return Edited.ToDateTime(); }
            set
            {
                Edited = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

    }

    [TableName("TOTALS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("COUNT_ID", AutoIncrement = false)]
    [ExplicitColumns]
    public partial class ForumTotals : SnitzDataContext.Record<ForumTotals>
    {
        [Column("COUNT_ID")]
        public short Id { get; set; }

        [Column("P_COUNT")]
        public int? PostCount { get; set; }

        [Column("P_A_COUNT")]
        public int? ArchivedPostCount { get; set; }

        [Column("T_COUNT")]
        public int? TopicCount { get; set; }

        [Column("T_A_COUNT")]
        public int? ArchivedTopicCount { get; set; }

        [Column("U_COUNT")]
        public int? UserCount { get; set; }
    }

    [TableName("ALLOWED_MEMBERS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("MEMBER_ID,FORUM_ID", AutoIncrement = false)]
    [ExplicitColumns]
    public partial class AllowedMembers : SnitzDataContext.Record<AllowedMembers>
    {
        [Column("MEMBER_ID")]
        public int MemberId { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }
    }

    [TableName("SUBSCRIPTIONS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("SUBSCRIPTION_ID")]
    [ExplicitColumns]
    public partial class Subscriptions : SnitzDataContext.Record<Subscriptions>
    {
        [Column("SUBSCRIPTION_ID")]
        public int Id { get; set; }

        [Column("MEMBER_ID")]
        public int MemberId { get; set; }

        [Column("CAT_ID")]
        public int CatId { get; set; }

        [Column("TOPIC_ID")]
        public int TopicId { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }


    }

    [TableName("A_TOPICS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("TOPIC_ID", AutoIncrement = false)]
    [ExplicitColumns]
    public partial class ArchivedTopics : SnitzDataContext.Record<Topic>
    {
        [Column("CAT_ID")]
        public int CatId { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }

        [Column("TOPIC_ID")]
        public int Id { get; set; }

        [Column("T_STATUS")]
        public Enumerators.PostStatus PostStatus { get; set; }

        [Column("T_MAIL")]
        public short? Mail { get; set; }

        [Column("T_SUBJECT")]
        public string Subject { get; set; }

        [Column("T_MESSAGE")]
        public string Message { get; set; }

        [Column("T_AUTHOR")]
        public int? AuthorId { get; set; }

        [Column("T_REPLIES")]
        public int? ReplyCount { get; set; }

        [Column("T_UREPLIES")]
        public int? UnmoderatedReplyCount { get; set; }

        [Column("T_VIEW_COUNT")]
        public int? ViewCount { get; set; }

        [Column("T_LAST_POST")]
        [SnitzCore.Filters.StringLength(14)]
        public String LastPost { get; set; }

        [Column("T_DATE")]
        [SnitzCore.Filters.StringLength(14)]
        public String Created { get; set; }

        [Column("T_LAST_POSTER")]
        public int? LastPoster { get; set; }

        [Column("T_IP")]
        public string PosterIp { get; set; }

        [Column("T_LAST_POST_AUTHOR")]
        public int? LastPostAuthorId { get; set; }

        [Column("T_LAST_POST_REPLY_ID")]
        public int? LastPostReplyId { get; set; }

        //[PetaPoco.Column("T_ARCHIVE_FLAG")]
        //public int? ArchiveFlag { get; set; }

        [Column("T_LAST_EDIT")]
        [SnitzCore.Filters.StringLength(14)]
        public String Edited { get; set; }

        [Column("T_LAST_EDITBY")]
        public int? LastEditUserId { get; set; }

        [Column("T_STICKY")]
        public short? IsSticky { get; set; }

        [Column("T_SIG")]
        public short? ShowSig { get; set; }

        [ResultColumn]
        [SnitzCore.Filters.StringLength(14)]
        public DateTime? LastPostDate
        {
            get { return LastPost.ToDateTime(); }
            set
            {
                LastPost = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }
        [ResultColumn]
        [SnitzCore.Filters.StringLength(14)]
        public DateTime? Date
        {
            get { return Created.ToDateTime(); }
            set
            {
                Created = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }
        [ResultColumn]
        [SnitzCore.Filters.StringLength(14)]
        public DateTime? LastEditDate
        {
            get { return Edited.ToDateTime(); }
            set
            {
                Edited = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }
    }

    [TableName("A_REPLY", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("REPLY_ID", AutoIncrement = false)]
    [ExplicitColumns]
    public partial class ArchivedReply : SnitzDataContext.Record<Reply>
    {
        [Column("CAT_ID")]
        public int CatId { get; set; }

        [Column("FORUM_ID")]
        public int ForumId { get; set; }

        [Column("TOPIC_ID")]
        public int TopicId { get; set; }

        [Column("REPLY_ID")]
        public int Id { get; set; }

        [Column("R_MAIL")]
        public short? Mail { get; set; }

        [Column("R_AUTHOR")]
        public int? AuthorId { get; set; }

        [Column("R_MESSAGE")]
        public string Message { get; set; }

        [Column("R_DATE")]
        [SnitzCore.Filters.StringLength(14)]
        public String Created { get; set; }

        [Column("R_IP")]
        public string PosterIp { get; set; }

        [Column("R_STATUS")]
        public Enumerators.PostStatus PostStatus { get; set; }

        [Column("R_LAST_EDIT")]
        [SnitzCore.Filters.StringLength(14)]
        public String Edited { get; set; }

        [Column("R_LAST_EDITBY")]
        public int? LastEditUSerId { get; set; }

        [Column("R_SIG")]
        public short? ShowSig { get; set; }

        [Column("R_RATING")]
        public int Rating { get; set; }

        [ResultColumn]
        [SnitzCore.Filters.StringLength(14)]
        public DateTime? Date
        {
            get { return Created.ToDateTime(); }
            set
            {
                Created = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }
        [ResultColumn]
        [SnitzCore.Filters.StringLength(14)]
        public DateTime? LastEditDate
        {
            get { return Edited.ToDateTime(); }
            set
            {
                Edited = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }
    }

    [TableName("BADWORDS", prefixType = Extras.TablePrefixTypes.Filter)]
    [PrimaryKey("B_ID")]
    [ExplicitColumns]
    public partial class BadwordFilter : SnitzDataContext.Record<BadwordFilter>
    {
        [Column("B_ID")]
        public int Id { get; set; }

        [Column("B_BADWORD")]
        public string BadWord { get; set; }

        [Column("B_REPLACE")]
        public string ReplaceWord { get; set; }

    }

    [TableName("SPAM_MAIL", prefixType = Extras.TablePrefixTypes.Filter)]
    [PrimaryKey("SPAM_ID")]
    [ExplicitColumns]
    public partial class SpamFilter : SnitzDataContext.Record<SpamFilter>
    {
        [Column("SPAM_ID")]
        public int Id { get; set; }

        [Column("SPAM_SERVER")]
        public string SpamServer { get; set; }
    }

    [TableName("NAMEFILTER", prefixType = Extras.TablePrefixTypes.Filter)]
    [PrimaryKey("N_ID")]
    [ExplicitColumns]
    public partial class NameFilter : SnitzDataContext.Record<NameFilter>
    {
        [Column("N_ID")]
        public int Id { get; set; }

        [Column("N_NAME")]
        public string Name { get; set; }
    }

    [TableName("GROUP_NAMES", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("GROUP_ID")]
    [ExplicitColumns]
    public partial class Group : SnitzDataContext.Record<Group>
    {
        [Column("GROUP_ID")]
        public int Id { get; set; }

        [Column("GROUP_NAME")]
        [SnitzCore.Filters.Required]
        public string Name { get; set; }

        [Column("GROUP_DESCRIPTION")]
        [SnitzCore.Filters.Required]
        public string Description { get; set; }

        [Column("GROUP_ICON")]
        public string Icon { get; set; }

        [Column("GROUP_IMAGE")]
        public string Image { get; set; }

        public static List<Group> GetGroups()
        {
            var cacheService = new InMemoryCache() { DoNotExpire = true };
            return cacheService.GetOrSet("snitz.forumgroups", () => Fetch("SELECT GROUP_ID, GROUP_NAME FROM " + repo.ForumTablePrefix + "GROUP_NAMES"));

        }
    }

    [TableName("GROUPS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("GROUP_KEY")]
    [ExplicitColumns]
    public partial class ForumGroup : SnitzDataContext.Record<ForumGroup>
    {
        [Column("GROUP_KEY")]
        public int Key { get; set; }

        [Column("GROUP_ID")]
        public int Id { get; set; }

        [Column("GROUP_CATID")]
        public int CatId { get; set; }

        [ResultColumn]
        public string CatName { get; set; }

        public static IEnumerable<Pair<int, string>> List(int id)
        {
            var sql = new Sql();
            sql.Select("G.GROUP_CATID AS 'Key', C.CAT_NAME AS 'Value'");
            sql.From(repo.ForumTablePrefix + "GROUPS G");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("G.GROUP_CATID = C.CAT_ID");
            sql.Where("G.GROUP_ID=@0", id);

            return repo.Fetch<Pair<int, string>>(sql);

        }

    }

    [TableName("RANKING", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("RANK_ID", AutoIncrement = false)]
    [ExplicitColumns]
    public partial class Rankings : SnitzDataContext.Record<Rankings>
    {
        [Column("RANK_ID")]
        public int Id { get; set; }

        [Column("R_TITLE")]
        public string Title { get; set; }

        [Column("R_IMAGE")]
        public string Image { get; set; }

        [Column("R_POSTS")]
        public int Threshold { get; set; }

        [Column("R_IMG_REPEAT")]
        public int RepeatCount { get; set; }
    }

}


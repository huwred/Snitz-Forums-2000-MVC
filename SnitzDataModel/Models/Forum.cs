using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;

namespace SnitzDataModel.Models
{
    public partial class Forum
    {
        [ResultColumn]
        public bool IncreasePostCount
        {
            get { return this.IncrementPostCount == 1; }
            set {
                this.IncrementPostCount = (short) (value ? 1 : 0);
            }
        }
        [ResultColumn]
        public string LastPostAuthorName { get; set; }
        [ResultColumn]
        public string LastTopicSubject { get; set; }
        [ResultColumn]
        public bool AllowTopicRating
        {
            get { return this.AllowRating == 1; }
            set {
                this.AllowRating = (short) (value ? 1 : 0);
            }
        }
        public Category Category { get; set; }

        public Dictionary<int, string> ForumModerators
        {
            get
            {
                return Moderators(this.Id);
            }
        }

        public string[] AllowedMemberSelection { get; set; }

        public Dictionary<int, string> AllowedMembers
        {
            get
            {
                return Members(this.Id);
            }
            set {  }
        }

        public Forum()
        {
            this.ArchiveSchedule = 60;
            this.ArchivedPostCount = 0;
            this.ArchivedTopicCount = 0;
            this.DefaultDays = Enumerators.ForumDays.Last30Days;
            this.DeleteSchedule = 365;
            this.IncrementPostCount = 1;
            this.LastPostReplyId = 0;
            this.LastPostTopicId = 0;
            this.Mail = 0;
            this.Moderation = Enumerators.Moderation.UnModerated;
            this.Order = 99;
            this.PostCount = 0;
            this.Status = Enumerators.PostStatus.Open;
            this.Subscription = Enumerators.Subscription.None;
            this.TopicCount = 0;
            this.Type = (int)Enumerators.ForumType.Topics;

        }

        /// <summary>
        /// Fetches a list of forums to populate dropdowns
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static IEnumerable<Pair<int, string>> List(IPrincipal user)
        {
            //fetch categories plus forums that are not hidden
            string allowed = string.Join(",", user.AllowedForumIDs().Select(n => n.ToString()).ToArray());
            if (String.IsNullOrWhiteSpace(allowed))
                allowed = "-1";
            var forums = repo.Fetch<Pair<int, string>>(Sql.Builder
                .Select("f.FORUM_ID AS 'Key',f.F_SUBJECT AS 'Value'")
                .From(repo.ForumTablePrefix + "CATEGORY c")
                .LeftJoin(repo.ForumTablePrefix + "FORUM f").On("f.CAT_ID = c.CAT_ID")
                .Where("f.FORUM_ID IN (" + allowed + ")")
                .OrderBy("c.CAT_ORDER", "f.F_ORDER"));

            return forums;

        }

        public static Forum FetchForum(int forumid)
        {
            var sql = new Sql();
            sql.Select("F.*");
            sql.From(repo.ForumTablePrefix + "FORUM F");
            //sql.LeftJoin(SnitzDataContext.Record<Forum>.repo.ForumTablePrefix + "CATEGORY Category").On("Category.CAT_ID = F.CAT_ID");
            sql.Where("F.FORUM_ID = @0", forumid);
            sql.OrderBy("F.F_ORDER");

            var forum = repo.Query<Forum>(sql).SingleOrDefault();
            return forum;
        }
        public static Forum FetchForumWithCategory(int forumid)
        {
            var sql = new Sql();
            sql.Select("F.*,Category.*");
            sql.From(repo.ForumTablePrefix + "FORUM F");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY Category").On("Category.CAT_ID = F.CAT_ID");
            sql.Where("F.FORUM_ID = @0", forumid);
            sql.OrderBy("F.F_ORDER");

            var forum = repo.Query<Forum, Category>(sql).SingleOrDefault();
            return forum;
        }

        public void DeleteTopics()
        {
            try
            {
                repo.BeginTransaction();
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "REPLY WHERE FORUM_ID=@0", this.Id);
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "TOPICS WHERE FORUM_ID=@0", this.Id);
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "POLLS WHERE TOPIC_ID NOT IN (SELECT TOPIC_ID FROM " + repo.ForumTablePrefix + "TOPICS)");
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "POLL_VOTES WHERE TOPIC_ID NOT IN (SELECT TOPIC_ID FROM " + repo.ForumTablePrefix + "TOPICS)");
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "POLL_ANSWERS WHERE POLL_ID NOT IN (SELECT POLL_ID FROM " + repo.ForumTablePrefix + "POLLS)");
                repo.CompleteTransaction();
            }
            catch (Exception)
            {
                repo.AbortTransaction();
                throw;
            }
        }

        public List<Topic> StickyTopics(int page, int pagesize)
        {
            if (ClassicConfig.GetIntValue("STRSTICKYTOPIC") != 1)
            {
                return new List<Topic>();
            }
            var result = repo.Page<Topic>(page, pagesize, TopicQuery.Where("Forum.FORUM_ID=@0 AND t.T_STICKY=1", this.Id).OrderBy("t.T_LAST_POST DESC"));
            return result.Items;
        }

        public Page<Topic> Topics(int pagesize, int page, IPrincipal user, int currentUserId, int defaultdays = 0, string orderby="lpd", string sortdir="DESC")
        {
            Sql query;
            var modforumlist = user.ModeratedForums();
            string statuslist = user.IsAdministrator() ? "0,1,2,3,99" : "0,1";
            string OrderBy;

            switch (orderby)
            {
                case "pd" :
                    OrderBy = "t.T_DATE " + sortdir;
                    break;
                case "lpd" :
                    OrderBy = "t.T_LAST_POST " + sortdir;
                    break;
                case "a" :
                    OrderBy = "a.M_NAME " + sortdir;
                    break;
                case "lpa" :
                    OrderBy = "lpa.M_NAME " + sortdir;
                    break;
                case "r" :
                    OrderBy = "t.T_REPLIES " + sortdir;
                    break;
                case "v" :
                    OrderBy = "t.T_VIEW_COUNT " + sortdir;
                    break;
                case "rat" :
                    OrderBy = "(T_RATING_TOTAL/nullif(T_RATING_TOTAL_COUNT,0)) " + sortdir;
                    break;
                default:
                    OrderBy = "t.T_LAST_POST DESC";
                    break;
            }

            switch (defaultdays)
            {
                case -88 : //HotTopics
                    int hottopicount = 0;
                    if (ClassicConfig.GetIntValue("STRHOTTOPIC") == 1)
                    {
                        hottopicount = ClassicConfig.GetIntValue("INTHOTTOPICNUM");
                        query =
                            TopicQuery
                                .Append("WHERE t.FORUM_ID=@0", this.Id)
                                .Append("AND T_REPLIES > @0",hottopicount)
                                .OrderBy("t.T_DATE DESC");
                    }
                    else
                    {
                        query =
                            TopicQuery
                                .Append("WHERE t.FORUM_ID=@0 AND t.T_STICKY=0", this.Id)
                                .Append("AND t.T_LAST_POST > @0 ", DateTime.UtcNow.AddDays(-defaultdays).ToSnitzServerDateString(ClassicConfig.ForumServerOffset))
                                .Append("AND (t.T_AUTHOR=@0 OR t.T_STATUS IN (" + statuslist + ") OR t.FORUM_ID IN (" +
                                        string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId)
                                .OrderBy(OrderBy);
                    }

                    break;
                case -9999: //Draft Topics
                    query =
                    TopicQuery
                    .Append("WHERE t.FORUM_ID=@0", this.Id)
                    .Append("AND t.T_STATUS=99")
                    .Append("AND T_AUTHOR=@0",currentUserId)
                    .OrderBy("t.T_DATE DESC");
                    break;
                case -999: //Un Answered Topics
                    query =
                    TopicQuery
                    .Append("WHERE t.FORUM_ID=@0 AND t.T_STICKY=0", this.Id)
                    .Append("AND t.T_STATUS=1")
                    .Append("AND T_REPLIES=0")
                    .OrderBy(OrderBy);
                    break;
                case -99: //Archived Topics
                    query =
                        new Sql();
                        query.Select(" t.*,1 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,Forum.F_SUBJECT AS ForumSubject,Forum.F_STATUS AS ForumStatus, Forum.F_SUBSCRIPTION AS ForumSubscriptionLevel, Forum.F_RATING AS ForumAllowRating, Forum.F_POSTAUTH AS ForumPostAuth, Forum.F_REPLYAUTH AS ForumReplyAuth");
                        query.From(repo.ForumTablePrefix + "A_TOPICS t ");
                        query.LeftJoin(repo.ForumTablePrefix + "FORUM Forum ").On("t.FORUM_ID = Forum.FORUM_ID ");
                        query.LeftJoin(repo.MemberTablePrefix + "MEMBERS a ").On("a.MEMBER_ID = t.T_AUTHOR ");
                        query.LeftJoin(repo.MemberTablePrefix + "MEMBERS lpa ").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR ");
                        query.Where("t.FORUM_ID=@0 AND t.T_STICKY=0", this.Id);
                        query.Where("(t.T_AUTHOR=@0 OR t.T_STATUS IN (" + statuslist + ") OR t.FORUM_ID IN (" +
                                    string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId);
                        query.OrderBy(OrderBy);
                    break;
                case -1: //All open topics
                    query =
                    TopicQuery
                    .Append("WHERE t.FORUM_ID=@0 AND t.T_STICKY=0", this.Id)
                    .Append("AND t.T_STATUS=1")
                    .OrderBy(OrderBy);
                    break;
                case 0: //All topics
                    query =
                    TopicQuery
                    .Append("WHERE t.FORUM_ID=@0 AND t.T_STICKY=0", this.Id)
                    .Append("AND (t.T_AUTHOR=@0 OR t.T_STATUS IN (" + statuslist + ") OR t.FORUM_ID IN (" +
                        string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId)
                        .OrderBy(OrderBy);
                    break;
                default:
                    query =
                        TopicQuery
                            .Append("WHERE t.FORUM_ID=@0 AND t.T_STICKY=0", this.Id)
                            .Append("AND t.T_LAST_POST > @0 ", DateTime.UtcNow.AddDays(-defaultdays).ToSnitzServerDateString(ClassicConfig.ForumServerOffset))
                            .Append("AND (t.T_AUTHOR=@0 OR t.T_STATUS IN (" + statuslist + ") OR t.FORUM_ID IN (" +
                                string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId)
                            .OrderBy(OrderBy);
                    break;
            }

            Page<Topic> result = repo.Page<Topic>(page, pagesize, query);
            return result;
        }

        public List<Topic> Topics(int topiccount, IPrincipal user, int currentUserId, DateTime date, out long totalCount)
        {
            var modforumlist = user.ModeratedForums();
            string statuslist = user.IsAdministrator() ? "0,1,2,3" : "0,1";
            Sql query = TopicQuery
                .Where("t.FORUM_ID=@0", this.Id)
                .Where("t.T_STICKY=0")
                .Where("(t.T_AUTHOR=@0 OR t.T_STATUS IN (" + statuslist + ") OR t.FORUM_ID IN (" +
                       string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId)
                .Where("t.T_LAST_POST < @0",date.ToSnitzDate())
                .OrderBy("t.T_LAST_POST DESC");

            //List<Topic> result = SnitzDataContext.Record<Forum>.repo.Query<Topic>(query).Take(topiccount).ToList();
            var result = repo.Page<Topic>(1,topiccount,query);
            totalCount = result.TotalItems;
            return result.Items;

        }

        public static Sql TopicQuery
        {
            get
            {
                Sql sql = new Sql();
                sql.Select(" t.*,0 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,Category.CAT_ORDER AS CatOrder,Forum.F_ORDER AS ForumOrder,Forum.F_SUBJECT AS ForumSubject,Forum.F_STATUS AS ForumStatus, Forum.F_SUBSCRIPTION AS ForumSubscriptionLevel, Forum.F_RATING AS ForumAllowRating, Forum.F_POSTAUTH AS ForumPostAuth, Forum.F_REPLYAUTH AS ForumReplyAuth");
                sql.From(repo.ForumTablePrefix + "TOPICS t ");

                sql.LeftJoin(repo.ForumTablePrefix + "FORUM Forum ").On("t.FORUM_ID = Forum.FORUM_ID ");
                sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY Category ").On("Forum.CAT_ID = Category.CAT_ID ");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS a ").On("a.MEMBER_ID = t.T_AUTHOR ");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS lpa ").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR ");
                return sql;
            }
        }

        /// <summary>
        /// Search topics in a forum
        /// </summary>
        /// <param name="model">search parameters object</param>
        /// <param name="archived"></param>
        /// <returns></returns>
        public List<Topic> SearchTopics(SearchModel model, bool archived = false)
        {
            var sql = new Sql();
            if (archived)
            {
                sql.Select("t.*, 1 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,f.F_SUBJECT AS ForumSubject");
                sql.From(repo.ForumTablePrefix + "A_TOPICS t");
                sql.LeftJoin(repo.ForumTablePrefix + "A_REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }
            else
            {
                sql.Select("t.*, 0 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,f.F_SUBJECT AS ForumSubject");
                sql.From(repo.ForumTablePrefix + "TOPICS t");
                sql.LeftJoin(repo.ForumTablePrefix + "REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }

            sql.LeftJoin(repo.ForumTablePrefix + "FORUM f").On("t.FORUM_ID = f.FORUM_ID");

            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS a").On("a.MEMBER_ID = t.T_AUTHOR");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS lpa").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR");

            sql.Where("t.FORUM_ID=@0", this.Id);
            sql.Where("(t.T_MESSAGE LIKE @0 OR r.R_MESSAGE LIKE @1)", "%" + model.Term + "%", "%" + model.Term + "%");

            var res = repo.Query<Topic>(sql);

            return res.Distinct(new TopicComparer()).ToList();
        }

        public void UpdateLastPost(Topic topic, bool newtopic = false)
        {
            if (topic.PostStatus == Enumerators.PostStatus.Open)
            {
                this.LastPostDate = topic.Date;
                this.LastPostAuthorId = topic.AuthorId;
                this.LastPostTopicId = topic.Id;
                this.LastPostReplyId = 0;
                if (newtopic)
                {
                    this.TopicCount += 1;
                    this.PostCount += 1;
                }
                this.Update(new[]
                            {
                                "F_LAST_POST", "F_LAST_POST_AUTHOR", "F_LAST_POST_TOPIC_ID", "F_LAST_POST_REPLY_ID",
                                "F_TOPICS", "F_COUNT"
                            });

                var cacheService = new InMemoryCache();
                cacheService.Remove("category.forums");

            }
            else if (topic.PostStatus == Enumerators.PostStatus.UnModerated)
            {

            }
        }

        public void UpdateLastPost(Reply reply, bool newreply = false)
        {
            if (reply.PostStatus == Enumerators.PostStatus.Open)
            {
                this.LastPostDate = reply.Date;
                this.LastPostAuthorId = reply.AuthorId;
                this.LastPostTopicId = reply.TopicId;
                this.LastPostReplyId = reply.Id;
                if(newreply)
                    this.PostCount += 1;
                this.Update(new[]
                            {
                                "F_LAST_POST", "F_LAST_POST_AUTHOR", "F_LAST_POST_TOPIC_ID", "F_LAST_POST_REPLY_ID",
                                "F_TOPICS", "F_COUNT"
                            });
                var cacheService = new InMemoryCache();
                cacheService.Remove("category.forums");
            }
        }

        public void UpdateLastPost()
        {
            using (SnitzDataContext db = new SnitzDataContext())
            {
                var topstring = "TOP 1";
                var limitstring = "";
                if (db.dbtype == "mysql")
                {
                    topstring = "";
                    limitstring = "LIMIT 1";
                }

                var lasttopic = db.Query<Topic>("select " + topstring + " *,(SELECT COUNT(*) FROM " + repo.ForumTablePrefix + "TOPICS WHERE FORUM_ID=@0 AND T_STATUS<2) AS Topics,(SELECT COUNT(*) FROM " + repo.ForumTablePrefix + "REPLY WHERE FORUM_ID=@1 AND R_STATUS<2) AS Replies from " + repo.ForumTablePrefix + "TOPICS where forum_id=@2 AND T_STATUS<2 order by T_LAST_POST desc " + limitstring, this.Id, this.Id, this.Id).ToList();
                if (!lasttopic.Any())
                {
                    this.TopicCount = 0;
                    this.PostCount = 0;
                    this.LastPostTopicId = 0;
                    this.LastPostReplyId = 0;
                    this.LastPostDate = null;
                    this.LastPostAuthorId = 0;
                }
                else
                {
                    var topic = lasttopic.First();
                    this.PostCount = 0;
                    this.LastPostTopicId = topic.Id;
                    this.LastPostReplyId = topic.LastPostReplyId;
                    this.LastPostDate = topic.LastPostDate;
                    this.LastPostAuthorId = topic.LastPostAuthorId;
                    this.TopicCount = topic.Topics;
                    this.PostCount = topic.Topics + topic.Replies;
                }

                this.Update(new[]
                            {
                                "F_LAST_POST", "F_LAST_POST_AUTHOR", "F_LAST_POST_TOPIC_ID", "F_LAST_POST_REPLY_ID",
                                "F_TOPICS", "F_COUNT"
                            });
                var cacheService = new InMemoryCache();
                cacheService.Remove("category.forums");
            }            
        }

        public void SaveModerators( List<int> moderators)
        {
            //fetch the current moderators from the database
            Dictionary<int, string> modList = this.ForumModerators;
            List<int> keyList = new List<int>(modList.Keys);

            var removedMods = keyList.Except(moderators).ToList();
            var newmoderators = moderators.Except(keyList).ToList();

            this.RemoveModerators(removedMods);
            this.AddModerators(newmoderators);
            
        }

        private void RemoveModerators( List<int> removedMods)
        {
            if (removedMods.Any())
            {
                try
                {
                    repo.BeginTransaction();
                    repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "MODERATOR WHERE FORUM_ID=@Id AND MEMBER_ID IN (@removedMods)",
                        new { this.Id, removedMods });
                    repo.CompleteTransaction();
                }
                catch (Exception)
                {
                    repo.AbortTransaction();
                    throw;
                }
            }
        }

        private void AddModerators( List<int> newmoderators)
        {
            if (newmoderators.Any())
            {
                var sql = new Sql();
                foreach (int moderator in newmoderators)
                {
                    sql.Append("INSERT INTO " + repo.ForumTablePrefix + "MODERATOR (FORUM_ID,MEMBER_ID) VALUES(@0,@1)", this.Id, moderator);
                }
                repo.Execute(sql);
            }
        }


        public static Dictionary<int, string> Members(int forumid )
        {

                var sql = new Sql();
                sql.Select("DISTINCT M.MEMBER_ID AS 'Key', M.M_NAME AS 'Value'");
                sql.From(repo.ForumTablePrefix + "ALLOWED_MEMBERS FAM");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("FAM.MEMBER_ID = M.MEMBER_ID");
                sql.Where("FAM.FORUM_ID=@0", forumid);

                var res = repo.Fetch<Pair<int, string>>(sql);
                return res.ToDictionary(i => i.Key, i => i.Value);
        }
        
 
        public static Dictionary<int, string> Moderators(int forumid)
        {
            var sql = new Sql();
            sql.Select("M.MEMBER_ID AS 'Key', M.M_NAME AS 'Value'");
            sql.From(repo.ForumTablePrefix + "MODERATOR FM");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("FM.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("FM.MEMBER_ID = M.MEMBER_ID");
            sql.Where("FM.FORUM_ID=@0", forumid);

            var res = repo.Fetch<Pair<int, string>>(sql);
            return res.ToDictionary(i => i.Key, i => i.Value);
        }

        public int UnmoderatedPosts(int forumid)
        {
            var sql = new Sql();
            sql.Select("COUNT(*)");
            sql.From(repo.ForumTablePrefix + "TOPICS");
            sql.Where("FORUM_ID=@0", forumid);
            sql.Where("(T_UREPLIES > 0 OR (T_STATUS > 1 AND T_STATUS < 99))");
            var res = repo.ExecuteScalar<int>(sql);
            return res;
        }
        public void Archive()
        {
            var archiveDate = DateTime.UtcNow.AddDays(-this.ArchiveSchedule).ToSnitzDate();

            ArchiveTopics(this.Id,archiveDate);
        }

        public static void ArchiveTopics(int forumid, string date)
        {
            Sql sql = new Sql();
            sql.Select("TOPIC_ID ");
            sql.From(repo.ForumTablePrefix + "TOPICS ");
            sql.Where("FORUM_ID=@0 ", forumid);
            sql.Where("COALESCE(T_ARCHIVE_FLAG,1)=1 ");
            if (!String.IsNullOrWhiteSpace(date))
            {
                sql.Where("T_LAST_POST < @0 ", date);
            }
            List<int> topics = repo.Fetch<int>(sql);

            if (topics.Any())
            {
                Archive(topics, forumid);
            }
        }

        public static void Archive(List<int> topics, int forumid=0)
        {
            try
            {
                Sql sql = new Sql();

                sql.Append("INSERT INTO " + repo.ForumTablePrefix + "A_REPLY (CAT_ID,FORUM_ID,TOPIC_ID,REPLY_ID,R_MAIL,R_AUTHOR,R_MESSAGE,R_DATE,R_IP,R_STATUS,R_LAST_EDIT,R_LAST_EDITBY,R_SIG,R_RATING)");
                sql.Append("SELECT CAT_ID,FORUM_ID,TOPIC_ID,REPLY_ID,R_MAIL,R_AUTHOR,R_MESSAGE,R_DATE,R_IP,R_STATUS,R_LAST_EDIT,R_LAST_EDITBY,R_SIG,R_RATING FROM " + repo.ForumTablePrefix + "REPLY WHERE TOPIC_ID IN (@topics) ;", new { topics });

                sql.Append("INSERT INTO " + repo.ForumTablePrefix + "A_TOPICS (CAT_ID,FORUM_ID,TOPIC_ID,T_STATUS,T_MAIL,T_SUBJECT,T_MESSAGE,T_AUTHOR,T_REPLIES,T_UREPLIES,T_VIEW_COUNT,T_LAST_POST,T_DATE,T_LAST_POSTER,T_IP,T_LAST_POST_AUTHOR,T_LAST_POST_REPLY_ID,T_LAST_EDIT,T_LAST_EDITBY,T_STICKY,T_SIG)");
                sql.Append("SELECT CAT_ID,FORUM_ID,TOPIC_ID,T_STATUS,T_MAIL,T_SUBJECT,T_MESSAGE,T_AUTHOR,T_REPLIES,T_UREPLIES,T_VIEW_COUNT,T_LAST_POST,T_DATE,T_LAST_POSTER,T_IP,T_LAST_POST_AUTHOR,T_LAST_POST_REPLY_ID,T_LAST_EDIT,T_LAST_EDITBY,T_STICKY,T_SIG FROM " + repo.ForumTablePrefix + "TOPICS WHERE TOPIC_ID IN (@topics) ;", new { topics });

                sql.Append("DELETE FROM " + repo.ForumTablePrefix + "REPLY WHERE TOPIC_ID IN (@topics) ;", new { topics });
                sql.Append("DELETE FROM " + repo.ForumTablePrefix + "TOPICS WHERE TOPIC_ID IN (@topics) ;", new { topics });
                
                if(forumid>0)
                    sql.Append("UPDATE " + repo.ForumTablePrefix + "FORUM SET F_L_ARCHIVE=@0 WHERE FORUM_ID=@1", DateTime.UtcNow.ToSnitzServerDateString(ClassicConfig.ForumServerOffset), forumid);


                repo.BeginTransaction();
                repo.Execute(sql);
                repo.CompleteTransaction();

                repo.UpdatePostCount();
            }
            catch (Exception)
            {
                repo.AbortTransaction();
                throw;
            }            
        }

        //On save Event
        public void PostProcess()
        {
            this.OnForumSaved(new EventArgs());
        }
        public event EventHandler ForumSaved;

        protected virtual void OnForumSaved(EventArgs e)
        {
            if (ForumSaved != null)
                ForumSaved(this, e);
        }

        public static void UpdateCategoryId(int newCatId, int forumId)
        {
            repo.Update<Topic>("SET CAT_ID=@0 WHERE FORUM_ID=@1", newCatId, forumId);
            repo.Update<Reply>("SET CAT_ID=@0 WHERE FORUM_ID=@1", newCatId, forumId);
        }
        public void UpdateSubscriptions(Category cat)
        {
            var fId = this.Id;
            var cId = cat.Id;
            switch (cat.Subscription)
            {
                case Enumerators.CategorySubscription.CategorySubscription:
                case Enumerators.CategorySubscription.ForumSubscription:

                    repo.Execute("UPDATE " + repo.ForumTablePrefix +
                                 "SUBSCRIPTIONS SET CAT_ID=@0 WHERE FORUM_ID=@1", cId, fId );
                    break;
                case Enumerators.CategorySubscription.TopicSubscription:
                    //reset data (catid forumid)
                    //remove forum subs
                    Subscriptions.Delete("WHERE FORUM_ID=@0 AND TOPIC_ID=0", fId);
                    repo.Execute("UPDATE " + repo.ForumTablePrefix +
                                 "SUBSCRIPTIONS SET CAT_ID=@0 WHERE FORUM_ID=@1", cId, fId );
                    break;
                case Enumerators.CategorySubscription.None:
                    Subscriptions.Delete("WHERE FORUM_ID=@0", fId);
                    break;

            }

        }

        public void MoveTopics(int toForumId)
        {
            repo.Update<Topic>("SET CAT_ID=@0,FORUM_ID=@1 WHERE FORUM_ID=@2", this.CatId,toForumId, this.Id);
            repo.Update<Reply>("SET CAT_ID=@0,FORUM_ID=@1 WHERE FORUM_ID=@2", this.CatId, toForumId, this.Id);
        }

        public static List<string> GetTagStrings(int id)
        {
            Sql sql = new Sql();

            sql.Append("SELECT T_MESSAGE FROM " + repo.ForumTablePrefix + "TOPICS WHERE FORUM_ID=" + id);

            return repo.Fetch<string>(sql);

        }
        public static List<string> GetTagStrings(List<int> forums)
        {
            Sql sql = new Sql();

            sql.Append("SELECT T_MESSAGE FROM " + repo.ForumTablePrefix + "TOPICS WHERE FORUM_ID IN(" + string.Join(",",forums) + ")");

            return repo.Fetch<string>(sql);

        }


        // Slug generation taken from http://stackoverflow.com/questions/2920744/url-slugify-algorithm-in-c
        public string GenerateSlug()
        {
            return Id.ToString();
            string phrase = HttpUtility.UrlDecode(HttpUtility.HtmlDecode(Regex.Replace(Subject,@"\[[^\]]*\]","")));

            if (phrase != null)
            {
                string str = (phrase).ToLower();
                // invalid chars           
                str = Regex.Replace(str, @"[\\/?:;,.']", "");
                // convert multiple spaces into one space   
                str = Regex.Replace(str, @"\s+", " ").Trim();
                // cut and trim 
                str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
                str = Regex.Replace(str, @"\s", "-"); // hyphens   
                return $"{str}_{Id}";
            }

            return Subject;
        }

        private string RemoveAccent(string text)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(text);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
    }
}
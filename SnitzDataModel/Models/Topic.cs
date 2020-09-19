using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ActiveTopic : Topic
    {
        
    }
    public class MyViewTopic : Topic
    {

    }
    public partial class Topic
    {
        [ResultColumn]
        public string PostAuthorName { get; set; }
        [ResultColumn]
        public string LastPostAuthorName { get; set; }
        [ResultColumn]
        public string AuthorAvatar { get; set; }
        [ResultColumn]
        public string AuthorSignature { get; set; }
        [ResultColumn]
        public string CatTitle { get; set; }
        [ResultColumn]
        public string EditedBy { get; set; }
        [ResultColumn]
        public string Arch { get; set; }
        [ResultColumn]
        public string ForumSubject { get; set; }

        [ResultColumn]
        public Enumerators.PostStatus ForumStatus { get; set; }
        [ResultColumn]
        public Enumerators.Subscription ForumSubscriptionLevel { get; set; }
        [ResultColumn]
        public int ForumOrder { get; set; }
        [ResultColumn]
        public int CatOrder { get; set; }
        [ResultColumn]
        public int Topics { get; set; }
        [ResultColumn]
        public int Replies { get; set; }

        [ResultColumn]
        public int Archived { get; set; }
        [ResultColumn]
        public short ForumAllowRating { get; set; }
        [ResultColumn]
        public int ForumPostAuth { get; set; }
        [ResultColumn]
        public int ForumReplyAuth { get; set; }
        //, Forum.F_POSTAUTH AS ForumPostAuth, Forum.F_REPLYAUTH AS ForumReplyAuth
        public Member Author { get; set; }
        public Forum Forum { get; set; }

        public Poll TopicPoll
        {
            get
            {
                if (this.IsPoll==1)
                {
                    return new PollsRepository().GetTopicPoll(this.Id);
                }
                return null;
            }
        }



        public IEnumerable<Reply> PageReplies { get; set; } 

        public Topic()
        {
            this.Mail = 0;
            this.ReplyCount = 0;
            this.UnmoderatedReplyCount = 0;
            this.ViewCount = 0;
            this.IsSticky = 0;
            this.ShowSig = 0;
            this.DoNotArchive = 1;
            this.PostStatus = Enumerators.PostStatus.OnHold; //default to OnHold, will get reset in the post routines
        }

        public static Topic FetchTopic(int topicid, int getarchive = 0)
        {
            if (getarchive == 1)
            {
                return repo.FirstOrDefault<Topic>("SELECT * FROM " + repo.ForumTablePrefix + "A_TOPICS WHERE TOPIC_ID = @0", topicid);
            }
            return repo.FirstOrDefault<Topic>("WHERE TOPIC_ID = @0", topicid);
        }

        /// <summary>
        /// Get a Topic Object and it's Author Object
        /// </summary>
        /// <param name="id">Topic Id</param>
        /// <param name="archived"></param>
        /// <returns></returns>
        public static Topic WithAuthor(int id, bool archived = false)
        {
            Sql sql = new Sql();
            if (archived)
            {
                sql.Select("t.*, 1 AS Archived, e.M_NAME AS EditedBy,lpa.M_NAME AS LastPostAuthorName ,Author.*,Forum.*");
                sql.From(repo.ForumTablePrefix + "A_TOPICS t");
            }
            else
            {
                sql.Select("t.*, e.M_NAME AS EditedBy, lpa.M_NAME AS LastPostAuthorName ,0 AS Archived, Cat.CAT_NAME As CatTitle ,Author.*,Forum.*");
                sql.From(repo.ForumTablePrefix + "TOPICS t");
            }

            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS e").On("t.T_LAST_EDITBY = e.MEMBER_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS lpa").On("t.T_LAST_POST_AUTHOR = lpa.MEMBER_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS Author").On("Author.MEMBER_ID = t.T_AUTHOR");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM Forum").On("t.FORUM_ID = Forum.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY Cat").On("t.CAT_ID = Cat.CAT_ID");
            sql.Where("t.TOPIC_ID=@0", id);
            Topic topic;
            try
            {
                var temp = repo.Query<Topic, Member, Forum>(sql);
                topic = temp.SingleOrDefault();
                if(topic != null)
                    topic.ForumStatus = topic.Forum.Status;
            }
            catch (Exception)
            {

                return null;
            }
            
            return topic;
        }

        /// <summary>
        /// Search replies in a topic
        /// </summary>
        /// <param name="model">search parameters object</param>
        /// <param name="archived"></param>
        /// <returns></returns>
        public List<Reply> SearchReplies(SearchModel model, bool archived = false)
        {
            int topicid = this.Id;
            if (model.TopicId.HasValue)
                topicid = model.TopicId.Value;
            Sql replysql = archived ? ArchivedReplyWithAuthorQuery : ReplyWithAuthorQuery;

            var sql = replysql.Where("r.TOPIC_ID=@0", topicid)
                    .Where("r.R_MESSAGE LIKE @0", "%" + model.Term + "%")
                    .OrderBy("r.R_DATE DESC");
            var res = repo.Query<Reply, Member>(sql);

            return res.ToList();
        }

        private Sql ReplyWithAuthorQuery
        {
            get
            {
                Sql sql = new Sql();
                sql.Select("r.*,0 AS Archived, e.M_NAME AS EditedBy, Author.*");
                sql.From(repo.ForumTablePrefix + "REPLY r");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS e").On("r.R_LAST_EDITBY = e.MEMBER_ID");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS Author").On("Author.MEMBER_ID = r.R_AUTHOR");

                return sql;
            }
        }

        private Sql ArchivedReplyWithAuthorQuery
        {
            get
            {
                Sql sql = new Sql();
                sql.Select("r.*,1 AS Archived, e.M_NAME AS EditedBy, Author.*");
                sql.From(repo.ForumTablePrefix + "A_REPLY r");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS e").On("r.R_LAST_EDITBY = e.MEMBER_ID");
                sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS Author").On("Author.MEMBER_ID = r.R_AUTHOR");

                return sql;
            }
        }


        public IEnumerable<Reply> FetchReplies(int pagesize, int page, bool admin, int currentUserId, bool archive = false, string orderby="",string sortorder="ASC")
        {
            Sql replysql = archive ? ArchivedReplyWithAuthorQuery : ReplyWithAuthorQuery;

            var sql = admin ? replysql.Where("TOPIC_ID=@0", this.Id).OrderBy(orderby + " " + sortorder) : replysql.Where("TOPIC_ID=@0 AND ((R_AUTHOR=@1) OR R_STATUS IN (0,1))", this.Id, currentUserId).OrderBy(orderby + " " + sortorder);
            IEnumerable<Reply> replies;
            if (page == -1)
            {
                replies = repo.Query<Reply, Member>(sql) ?? new List<Reply>();
            }
            else
            {
                replies = repo.Query<Reply, Member>(sql).Skip(pagesize * (page - 1)).Take(pagesize);
            }
            return replies;
        }
        
        public void UpdateLastPost(Reply reply, bool newreply)
        {
            using (SnitzDataContext db = new SnitzDataContext())
            {
                //update the topic
                switch (reply.PostStatus)
                {
                    case Enumerators.PostStatus.Open:
                        this.LastPostAuthorId = reply.AuthorId;
                        this.LastPostDate = reply.Date;
                        this.LastPostReplyId = reply.Id;
                        if (newreply)
                            this.ReplyCount += 1;
                        this.Update(new string[]
                                    {"T_LAST_POST", "T_LAST_POST_AUTHOR", "T_LAST_POST_REPLY_ID", "T_REPLIES"});
                        //update the forum
                        var forum = db.GetById<Forum>(this.ForumId);
                        forum.UpdateLastPost(reply,newreply);

                        break;
                    case Enumerators.PostStatus.UnModerated:
                        this.UnmoderatedReplyCount += 1;
                        this.Update(new string[]
                                    {"T_UREPLIES"});
                        break;
                    default:
                        return;
                }

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

                var lastreply = db.Query<LastPostModel>("select " + topstring + " reply_id AS LastPostId,R_AUTHOR As LastPostAuthorId,R_DATE As LastPostDate,(SELECT COUNT(*) FROM " + repo.ForumTablePrefix + "REPLY WHERE TOPIC_ID=@0 AND R_STATUS<2 )  AS PostCount from " + repo.ForumTablePrefix + "REPLY where topic_id=@1 AND R_STATUS<2 order by r_date desc " + limitstring, this.Id, this.Id).ToList();
                if (!lastreply.Any())
                {
                    this.LastPostReplyId = 0;
                    this.LastPostDate = this.Date;
                    this.LastPostAuthorId = this.AuthorId;
                    this.ReplyCount = 0;
                }
                else
                {
                    var reply = lastreply.First();
                    this.LastPostReplyId = reply.LastPostId;
                    this.LastPostDate = reply.LastPostDate;
                    this.LastPostAuthorId = reply.LastPostAuthorId;
                    this.ReplyCount = reply.PostCount;
                }

                this.Update(new string[] { "T_LAST_POST", "T_LAST_POST_AUTHOR", "T_LAST_POST_REPLY_ID", "T_REPLIES" });
            }
        }

        public Enumerators.PostStatus UpdateStatus(Enumerators.PostStatus status)
        {
            var prevstatus = this.PostStatus;
            this.PostStatus = status;
            this.Update(new string[] { "T_STATUS" });
            return prevstatus;
        }

        public void MoveReplies()
        {
            repo.Update<Reply>("SET FORUM_ID=@0,CAT_ID=@1,R_STATUS=@2 WHERE TOPIC_ID=@3", this.ForumId, this.CatId, (int)this.PostStatus, this.Id);
        }

        public void MoveSubscriptions(int topicid, int forumid, int catid)
        {
            repo.Update<Subscriptions>("SET TOPIC_ID=@0,FORUM_ID=@1,CAT_ID=@2 WHERE TOPIC_ID=@3", topicid, forumid,catid, this.Id);
        }

        public void MoveReplies(Topic newTopic)
        {
            repo.Update<Reply>("SET FORUM_ID=@0,CAT_ID=@1,TOPIC_ID=@2 WHERE TOPIC_ID=@3", newTopic.ForumId, newTopic.CatId, newTopic.Id, this.Id);
        }

        /// <summary>
        /// Set the status flag for a topic
        /// </summary>
        /// <param name="topicid"></param>
        /// <param name="status"></param>
        public static void SetStatus(int topicid, Enumerators.PostStatus status)
        {
            var topic = FetchTopic(topicid);
            var old = topic.UpdateStatus(status);
            //if we are approving a moderated post then update lastpost info and member post count
            if (status == Enumerators.PostStatus.Open && old.In(Enumerators.PostStatus.UnModerated, Enumerators.PostStatus.OnHold))
            {
                Member.UpdatePostCount(topic.AuthorId, true);
                var forum = Forum.FetchForum(topic.ForumId);
                forum.UpdateLastPost();

            }
        }
        /// <summary>
        /// Lock/Unlock a topic
        /// </summary>
        /// <returns></returns>
        public Enumerators.PostStatus ToggleLock()
        {
            //var topic = Topic.FetchTopic(topicid);
            this.PostStatus = this.PostStatus != Enumerators.PostStatus.Closed ? Enumerators.PostStatus.Closed : Enumerators.PostStatus.Open;
            this.Save();
            //update the reply statuses
            repo.Update<Reply>("SET R_STATUS=@0 WHERE TOPIC_ID=@1", (int)this.PostStatus, this.Id);
            return this.PostStatus;
        }

        public decimal Rating()
        {
            var ratings = repo.Query<Reply>("WHERE TOPIC_ID=@0 AND R_STATUS<2", this.Id).ToList();
            decimal rating = 0;
            if (ratings.Any())
            {
                decimal ratingSum = Decimal.Divide(ratings.Sum(d => d.Rating),10);
                var ratingCount = ratings.Count;
                rating = (ratingSum / ratingCount);
            }
            return decimal.Parse(rating.ToString());
        }
        public decimal GetTopicRating()
        {
            var ratings = repo.First<Topic>("WHERE TOPIC_ID=@0", this.Id);
            decimal rating = 0;
            if (ratings.RatingTotal > 0)
            {
                decimal ratingSum = Decimal.Divide(ratings.RatingTotal,10);
                var ratingCount = ratings.RatingCount;
                rating = (ratingSum / ratingCount);
            }
            return decimal.Parse(rating.ToString());
        }
        public void UpdateSubscriptions(Forum forum)
        {
            var fId = forum.Id;
            var cId = forum.CatId;
            switch (forum.Subscription)
            {
                case Enumerators.Subscription.ForumSubscription:
                case Enumerators.Subscription.TopicSubscription :
                    //reset data (catid forumid)
                    repo.Execute("UPDATE " + repo.ForumTablePrefix +
                                 "SUBSCRIPTIONS SET CAT_ID=@0,FORUM_ID=@1 WHERE TOPIC_ID=@2",cId,fId,this.Id);
                    break;
                case Enumerators.Subscription.None:
                    Subscriptions.Delete("WHERE TOPIC_ID=@0",this.Id);
                    break;

            }
            
        }

        public static int PreviousTopic(int id, DateTime lastPostDate)
        {
            Sql sql = new Sql();
            sql.Select("TOPIC_ID");
            sql.From(repo.ForumTablePrefix + "TOPICS");
            sql.Where("T_LAST_POST < @0 ", lastPostDate.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));
            sql.Where("FORUM_ID=@0",id);
            sql.Where("T_STATUS < 2");
            sql.OrderBy("T_LAST_POST DESC");

            try
            {
                return repo.ExecuteScalar<int>(sql);
            }
            catch
            {
                return 0;
            }
        }

        public static int NextTopic(int id, DateTime lastPostDate)
        {
            Sql sql = new Sql();
            sql.Select("TOPIC_ID");
            sql.From(repo.ForumTablePrefix + "TOPICS");
            sql.Where("T_LAST_POST > @0 ", lastPostDate.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));
            sql.Where("FORUM_ID=@0",id);
            sql.Where("T_STATUS < 2");
            sql.OrderBy("T_LAST_POST");

            try
            {
                return repo.ExecuteScalar<int>(sql);
            }
            catch
            {
                return 0;
            }
        }

        public void Touch()
        {
            this.LastEditDate = DateTime.UtcNow;
            this.LastPostDate = DateTime.UtcNow;
            this.LastPostReplyId = 0;
            this.LastEditUserId = Member.GetByName(ClassicConfig.GetValue("STRADMINUSER")).Id;
            this.Update(new string[] { "T_LAST_POST", "T_LAST_POST_REPLY_ID", "T_LAST_EDIT", "T_LAST_EDITBY" });
            
        }

        // Slug generation taken from http://stackoverflow.com/questions/2920744/url-slugify-algorithm-in-c
        public string GenerateSlug()
        {
            return Id.ToString();
            string phrase = HttpUtility.UrlDecode(HttpUtility.HtmlDecode(Regex.Replace(Subject,@"\[[^\]]*\]","")));
            if (phrase != null)
            {
                string str = phrase.ToLower();
                // invalid chars           
                str = Regex.Replace(str, @"[\\/?:;,.']", "");
                // convert multiple spaces into one space   
                str = Regex.Replace(str, @"\s+", " ").Trim();
                // cut and trim 
                str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
                str = Regex.Replace(str, @"\s", "-"); // hyphens   
                //str = HttpUtility.HtmlDecode(str); 
                return $"{str}_{Id}";
            }

            return Subject;
        }

        public static List<string> GetTagStrings(int id)
        {
            Sql sql = new Sql();
            sql.Append("SELECT T_MESSAGE FROM " + repo.ForumTablePrefix + "TOPICS WHERE TOPIC_ID=" + id);
            List<string> tmessage = repo.Fetch<string>(sql);
            sql = new Sql();
            sql.Append("SELECT R_MESSAGE FROM " + repo.ForumTablePrefix + "REPLY WHERE TOPIC_ID=" + id);
            List<string> rmessage = repo.Fetch<string>(sql);

            tmessage.AddRange(rmessage);

            return tmessage;
        }

        public static void ChangeOwner(int primaryId, int secondaryId)
        {
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "TOPICS SET T_AUTHOR=@0  WHERE T_AUTHOR=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "TOPICS SET T_LAST_POST_AUTHOR=@0  WHERE T_LAST_POST_AUTHOR=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "TOPICS SET T_LAST_EDITBY=@0  WHERE T_LAST_EDITBY=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "A_TOPICS SET T_AUTHOR=@0  WHERE T_AUTHOR=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "A_TOPICS SET T_LAST_POST_AUTHOR=@0  WHERE T_LAST_POST_AUTHOR=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "A_TOPICS SET T_LAST_EDITBY=@0  WHERE T_LAST_EDITBY=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "REPLY SET R_AUTHOR=@0  WHERE R_AUTHOR=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "REPLY SET R_LAST_EDITBY=@0  WHERE R_LAST_EDITBY=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "A_REPLY SET R_AUTHOR=@0  WHERE R_AUTHOR=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.ForumTablePrefix + "A_REPLY SET R_LAST_EDITBY=@0  WHERE R_LAST_EDITBY=@1",primaryId,secondaryId);
        }
    }
}
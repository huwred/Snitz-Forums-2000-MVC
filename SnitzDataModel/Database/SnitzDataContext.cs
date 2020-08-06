// /*
// ####################################################################################################################
// ##
// ## SnitzDataContext
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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using LangResources.Models;
using MySql.Data;
using MySql.Data.MySqlClient;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;

namespace SnitzDataModel.Database
{
    public partial class SnitzDataContext : PetaPoco.Database
    {
        public SnitzDataContext()
            : base("SnitzConnectionString")
        {
            CommonConstruct();

            dbtype = "mssql";
            if (ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ProviderName.StartsWith("MySql", StringComparison.Ordinal))
            {
                dbtype = "mysql";
            }
            DBTypeName = dbtype;
            Config.DbType = DBTypeName;
        }

        public SnitzDataContext(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
            CommonConstruct();

        }

        partial void CommonConstruct();

        public interface IFactory
        {
            SnitzDataContext Instance { get; }
        }

        public static IFactory Factory { get; set; }

        public static SnitzDataContext GetInstance()
        {
            if (_instance != null)
                return _instance;

            if (Factory != null)
                return Factory.Instance;
            else
                return new SnitzDataContext();
        }


        [ThreadStatic] static SnitzDataContext _instance;

        public override void OnBeginTransaction()
        {
            if (_instance == null)
                _instance = this;
        }

        public override void OnEndTransaction()
        {
            if (_instance == this)
                _instance = null;
        }

        public class Record<T> where T : new()
        {
            public static SnitzDataContext repo
            {
                get { return SnitzDataContext.GetInstance(); }
            }

            public bool IsNew()
            {
                return repo.IsNew(this);
            }

            public object Insert()
            {
                return repo.Insert(this);
            }

            public void Save()
            {
                repo.Save(this);
            }

            public int Update()
            {
                return repo.Update(this);
            }

            public int Update(IEnumerable<string> columns)
            {
                return repo.Update(this, columns);
            }

            public static int Update(string sql, params object[] args)
            {
                return repo.Update<T>(sql, args);
            }

            public static int Update(Sql sql)
            {
                return repo.Update<T>(sql);
            }

            public int Delete()
            {
                return repo.Delete(this);
            }

            public static int Delete(string sql, params object[] args)
            {
                return repo.Delete<T>(sql, args);
            }

            public static int Delete(Sql sql)
            {
                return repo.Delete<T>(sql);
            }

            public static int Delete(object primaryKey)
            {
                return repo.Delete<T>(primaryKey);
            }

            public static bool Exists(object primaryKey)
            {
                return repo.Exists<T>(primaryKey);
            }

            public static T SingleOrDefaultById(object primaryKey)
            {
                return repo.SingleOrDefault<T>(primaryKey);
            }

            public static T SingleOrDefault(string sql, params object[] args)
            {
                return repo.SingleOrDefault<T>(sql, args);
            }

            public static T SingleOrDefault(Sql sql)
            {
                return repo.SingleOrDefault<T>(sql);
            }

            public static T FirstOrDefault(string sql, params object[] args)
            {
                return repo.FirstOrDefault<T>(sql, args);
            }

            public static T FirstOrDefault(Sql sql)
            {
                return repo.FirstOrDefault<T>(sql);
            }

            public static T SingleById(object primaryKey)
            {
                return repo.Single<T>(primaryKey);
            }

            public static T Single(string sql, params object[] args)
            {
                return repo.Single<T>(sql, args);
            }

            public static T Single(Sql sql)
            {
                return repo.Single<T>(sql);
            }

            public static T First(string sql, params object[] args)
            {
                return repo.First<T>(sql, args);
            }

            public static T First(Sql sql)
            {
                return repo.First<T>(sql);
            }

            public static List<T> Fetch(string sql, params object[] args)
            {
                return repo.Fetch<T>(sql, args);
            }

            public static List<T> Fetch(Sql sql)
            {
                return repo.Fetch<T>(sql);
            }

            public static List<T> Fetch(long page, long itemsPerPage, string sql, params object[] args)
            {
                return repo.Fetch<T>(page, itemsPerPage, sql, args);
            }

            public static List<T> Fetch(long page, long itemsPerPage, Sql sql)
            {
                return repo.Fetch<T>(page, itemsPerPage, sql);
            }

            public static List<T> SkipTake(long skip, long take, string sql, params object[] args)
            {
                return repo.SkipTake<T>(skip, take, sql, args);
            }

            public static List<T> SkipTake(long skip, long take, Sql sql)
            {
                return repo.SkipTake<T>(skip, take, sql);
            }

            public static Page<T> Page(long page, long itemsPerPage, string sql, params object[] args)
            {
                return repo.Page<T>(page, itemsPerPage, sql, args);
            }

            public static Page<T> Page(long page, long itemsPerPage, Sql sql)
            {
                return repo.Page<T>(page, itemsPerPage, sql);
            }

            public static IEnumerable<T> Query(string sql, params object[] args)
            {
                return repo.Query<T>(sql, args);
            }

            public static IEnumerable<T> Query(Sql sql)
            {
                return repo.Query<T>(sql);
            }
        }


        public string dbtype { get; set; }

        /// <summary>
        /// Fetches a page of topics containing posts since a specific date
        /// </summary>
        /// <param name="page"></param>
        /// <param name="activesince"></param>
        /// <param name="lastVisitCookie"></param>
        /// <param name="user"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        public Page<Topic> FetchActiveTopics(int pagesize, int page, Enumerators.ActiveTopicsSince activesince,
            string lastVisitCookie,
            IPrincipal user, int currentUserId, int GroupId, bool grouped = false)
        {
            bool admin = user.IsAdministrator();

            var publicview = new List<int> { 0, 2 };
            if (user.Identity.IsAuthenticated && !admin)
            {
                publicview = user.AllowedForumIDs();
            }
            var srvOffset = ClassicConfig.ForumServerOffset;
            string lastvisitdate = null;
            switch (activesince)
            {
                case Enumerators.ActiveTopicsSince.LastVisit:
                    var dateTime = lastVisitCookie.ToDateTime();
                    if (dateTime.HasValue)
                        lastvisitdate = dateTime.Value.ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.LastFifteen:
                    lastvisitdate = DateTime.UtcNow.AddMinutes(-15).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.LastThirty:
                    lastvisitdate = DateTime.UtcNow.AddMinutes(-30).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.LastHour:
                    lastvisitdate = DateTime.UtcNow.AddHours(-1).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.Last2Hours:
                    lastvisitdate = DateTime.UtcNow.AddHours(-2).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.Last6Hours:
                    lastvisitdate = DateTime.UtcNow.AddHours(-6).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.Last12Hours:
                    lastvisitdate = DateTime.UtcNow.AddHours(-12).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.LastDay:
                    lastvisitdate = DateTime.UtcNow.AddDays(-1).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.Last2Days:
                    lastvisitdate = DateTime.UtcNow.AddDays(-2).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.LastWeek:
                    lastvisitdate = DateTime.UtcNow.AddDays(-7).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.Last2Weeks:
                    lastvisitdate = DateTime.UtcNow.AddDays(-14).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.LastMonth:
                    lastvisitdate = DateTime.UtcNow.AddMonths(-1).ToSnitzServerDateString(srvOffset);
                    break;
                case Enumerators.ActiveTopicsSince.Last2Months:
                    lastvisitdate = DateTime.UtcNow.AddMonths(-2).ToSnitzServerDateString(srvOffset);
                    break;
                default:
                    lastvisitdate = DateTime.UtcNow.AddDays(-7).ToSnitzServerDateString(srvOffset);
                    break;
            }

            Sql query = Models.Forum.TopicQuery;

            if (!admin)
            {
                if (!user.Identity.IsAuthenticated)
                {
                    query.Where(" Forum.F_PRIVATEFORUMS IN (" +
                                string.Join(",", publicview.Select(n => n.ToString()).ToArray()) + ") ");
                    query.Where(" T_STATUS IN (0,1)");
                    query.Where(" t.T_LAST_POST>'" + lastvisitdate + "'");
                }
                else
                {
                    var modforumlist = user.ModeratedForums();
                    query.Where(" Forum.FORUM_ID IN (" +
                                string.Join(",", publicview.Select(n => n.ToString()).ToArray()) +
                                ") ");
                    query.Where(
                        " (T_AUTHOR=@0 OR T_STATUS IN (0,1) OR t.FORUM_ID IN (" +
                        string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId);
                    query.Where(" t.T_LAST_POST>'" + lastvisitdate + "'");
                }

            }
            else
            {
                query.Where(" T_STATUS < 99 OR T_AUTHOR=@0", currentUserId);
                query.Where(" t.T_LAST_POST > '" + lastvisitdate + "' OR T_UREPLIES > 0");
            }

            if (GroupId > 1)
            {
                var cats = Query<int>("SELECT GROUP_CATID FROM " + ForumTablePrefix + "GROUPS WHERE GROUP_ID=@0", GroupId);


                query.Where(" t.CAT_ID IN(" + string.Join(",", cats.Select(n => n.ToString()).ToArray()) + ")");

            }
            if (grouped)
                query.OrderBy("Category.CAT_ORDER, Category.CAT_NAME, Forum.F_ORDER, Forum.F_SUBJECT");
            query.OrderBy(" T_LAST_POST DESC");
            Page<Models.Topic> result = Page<Models.Topic>(page, pagesize, query);
            return result;
        }
        /// <summary>
        /// Fetch topics from subscribed forums
        /// </summary>
        /// <param name="pagesize">items per page</param>
        /// <param name="page">pagenumber</param>
        /// <param name="forums">List of forum id's</param>
        /// <returns></returns>
        public Page<Topic> FetchMyForumTopics(int pagesize, int page, List<int> forums)
        {

            Sql sql = new Sql();
            sql.Select(" t.*,0 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,Category.CAT_ORDER AS CatOrder,Forum.F_ORDER AS ForumOrder,Forum.F_SUBJECT AS ForumSubject,Forum.F_STATUS AS ForumStatus, Forum.F_SUBSCRIPTION AS ForumSubscriptionLevel, Forum.F_RATING AS ForumAllowRating, Forum.F_POSTAUTH AS ForumPostAuth, Forum.F_REPLYAUTH AS ForumReplyAuth");
            sql.From(this.ForumTablePrefix + "TOPICS t ");

            sql.LeftJoin(ForumTablePrefix + "FORUM Forum ").On("t.FORUM_ID = Forum.FORUM_ID ");
            sql.LeftJoin(ForumTablePrefix + "CATEGORY Category ").On("Forum.CAT_ID = Category.CAT_ID ");
            sql.LeftJoin(MemberTablePrefix + "MEMBERS a ").On("a.MEMBER_ID = t.T_AUTHOR ");
            sql.LeftJoin(MemberTablePrefix + "MEMBERS lpa ").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR ");
            //sql.Where("Forum.FORUM_ID=" + forum);
            sql.Where("Forum.FORUM_ID IN (" + string.Join(",", forums.Select(n => n.ToString()).ToArray()) + ")");
            sql.Where(" T_STATUS IN (0,1)");
            //sql.Where(" t.T_LAST_POST>'" + lastvisitdate + "'");
            sql.OrderBy(" T_LAST_POST DESC");

            Page<Models.Topic> result = Page<Models.Topic>(page, pagesize, sql);
            return result;
        }
        public IEnumerable<MyViewTopic> FetchAllMyForumTopics(List<int> forums)
        {
            Sql sql = new Sql();
            sql.Select(" t.TOPIC_ID, t.T_SUBJECT, t.T_DATE , t.T_LAST_POST");
            sql.From(this.ForumTablePrefix + "TOPICS t ");
            sql.LeftJoin(ForumTablePrefix + "FORUM Forum ").On("t.FORUM_ID = Forum.FORUM_ID ");
            sql.Where("Forum.FORUM_ID IN (" + string.Join(",", forums.Select(n => n.ToString()).ToArray()) + ")");
            sql.Where(" t.T_STATUS IN (0,1)");
            sql.OrderBy(" t.T_LAST_POST DESC");

            var result = Query<MyViewTopic>(sql);
            return result;
        }

        /// <summary>
        /// Fetches the topics with most recent posts
        /// </summary>
        /// <param name="count">number of topics to fetch</param>
        /// <param name="user"></param>
        /// <param name="currentUserId"></param>
        /// <param name="catid"></param>
        /// <param name="forumid"></param>
        /// <returns></returns>
        public List<ActiveTopic> FetchRecentTopicList(IPrincipal user, int currentUserId, int count, int catid = 0, int forumid = 0)
        {
            bool admin = user.IsAdministrator();

            var publicview = new List<int> { 0, 2 };
            if (user.Identity.IsAuthenticated && !admin)
            {
                publicview = user.AllowedForumIDs();
            }

            var sql = new Sql();
            var topstring = "TOP " + count;
            if (dbtype == "mysql")
                topstring = "";
            sql.Select(topstring +
                       " t.*,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName, Cat.CAT_NAME AS CatTitle, Forum.*");
            sql.From(this.ForumTablePrefix + "TOPICS t");
            sql.LeftJoin(this.ForumTablePrefix + "FORUM Forum").On("t.FORUM_ID = Forum.FORUM_ID");
            sql.LeftJoin(this.ForumTablePrefix + "CATEGORY Cat").On("t.CAT_ID = Cat.CAT_ID");
            sql.LeftJoin(this.MemberTablePrefix + "MEMBERS a").On("a.MEMBER_ID = t.T_AUTHOR");
            sql.LeftJoin(this.MemberTablePrefix + "MEMBERS lpa").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR");
            if (!admin)
            {
                if (!user.Identity.IsAuthenticated)
                {
                    sql.Where("Forum.F_PRIVATEFORUMS IN (" +
                              string.Join(",", publicview.Select(n => n.ToString()).ToArray()) + ") ");
                    sql.Where("T_STATUS IN (0,1)");
                }
                else
                {
                    var modforumlist = user.ModeratedForums();
                    sql.Where("Forum.FORUM_ID IN (" + string.Join(",", publicview.Select(n => n.ToString()).ToArray()) +
                              ") ");
                    sql.Where(
                        "(T_AUTHOR=@0 OR T_STATUS IN (0,1) OR t.FORUM_ID IN (" +
                        string.Join(",", modforumlist.Select(n => n.ToString()).ToArray()) + "))", currentUserId);

                }
                if (catid > 0)
                {
                    sql.Where("t.CAT_ID=@0", catid);
                }
                else if (forumid > 0)
                {
                    sql.Where("t.FORUM_ID=@0", forumid);
                }
            }
            else
            {
                sql.Where("T_STATUS < 99 OR T_AUTHOR=@0 OR T_UREPLIES>0", currentUserId);
                if (catid > 0)
                {
                    sql.Where("t.CAT_ID=@0", catid);
                }
                else if (forumid > 0)
                {
                    sql.Where("t.FORUM_ID=@0", forumid);
                }

            }
            sql.OrderBy("T_LAST_POST DESC");
            if (dbtype == "mysql")
            {
                sql.Append("LIMIT " + count);
            }
            return Query<Models.ActiveTopic, Models.Forum>(sql).ToList();

        }

        /// <summary>
        /// Fetches a list of Categories and Forums
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public IEnumerable<Category> FetchCategoryForumList(IPrincipal user)
        {
            var sql = new Sql();
            sql.Select("C.*,F.*, M.M_NAME AS LastPostAuthorName,T.T_SUBJECT AS LastTopicSubject");
            sql.From(this.ForumTablePrefix + "CATEGORY C");
            sql.LeftJoin(this.ForumTablePrefix + "FORUM F").On("F.CAT_ID = C.CAT_ID");
            sql.LeftJoin(this.MemberTablePrefix + "MEMBERS M").On("M.MEMBER_ID = F.F_LAST_POST_AUTHOR");
            sql.LeftJoin(this.ForumTablePrefix + "TOPICS T").On("T.TOPIC_ID = F.F_LAST_POST_TOPIC_ID");
            sql.OrderBy("C.CAT_ORDER", "C.CAT_NAME", "F.F_ORDER", "F.F_SUBJECT");

            var service = new InMemoryCache(60);
            return service.GetOrSet("category.forums",
                () => Fetch<Category, Forum, Category>(
                    new CategoryForumRelator().MapIt, sql));
        }

        /// <summary>
        /// Fetches a list of Categories and Forums
        /// </summary>
        /// <returns></returns>
        public List<Category> FetchJumptoList()
        {

            var sql = new Sql();
            sql.Select("C.CAT_ID,C.CAT_NAME,F.FORUM_ID,F.F_SUBJECT,F.F_TYPE,F.F_A_COUNT,F.F_PRIVATEFORUMS");
            sql.From(this.ForumTablePrefix + "CATEGORY C");
            sql.LeftJoin(this.ForumTablePrefix + "FORUM F").On("F.CAT_ID = C.CAT_ID");
            sql.OrderBy("C.CAT_ORDER", "C.CAT_NAME", "F.F_ORDER", "F.F_SUBJECT");
            var cacheService = new InMemoryCache(600);

            return cacheService.GetOrSet("category.forumlist",
                () => Fetch<Models.Category, Models.Forum, Models.Category>(
                    new CategoryForumRelator().MapIt, sql));

        }

        /// <summary>
        /// Delete a reply
        /// </summary>
        /// <param name="reply"></param>
        public void DeleteReply(Reply reply)
        {
            int tId = reply.TopicId;
            int fId = reply.ForumId;
            var topic = GetById<Models.Topic>(tId);
            if (reply.PostStatus == Enumerators.PostStatus.UnModerated)
            {
                topic.UnmoderatedReplyCount -= 1;
                topic.Save();
            }
            Delete(reply);

            topic.UpdateLastPost();
            var forum = GetById<Models.Forum>(fId);
            forum.UpdateLastPost();
        }

        public void DeleteReply(int topicid, List<int> selectedreplies)
        {
            try
            {
                BeginTransaction();
                //delete related replies
                Execute("DELETE FROM " + this.ForumTablePrefix + "REPLY WHERE REPLY_ID IN (@replies)",
                    new { replies = selectedreplies });
                var ucount =
                    ExecuteScalar<int>(
                        "SELECT COUNT(REPLY_ID) FROM " + this.ForumTablePrefix +
                        "REPLY WHERE TOPIC_ID=@0 and R_STATUS=2", topicid);
                Execute("UPDATE " + this.ForumTablePrefix + "TOPICS SET T_UREPLIES=@0", ucount);
                CompleteTransaction();
                UpdatePostCount();

            }
            catch (Exception)
            {
                AbortTransaction();
                throw;
            }
        }

        /// <summary>
        /// Delete a topic
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public void DeleteTopic(Topic topic)
        {
            int id = topic.Id;
            int fId = topic.ForumId;

            try
            {
                BeginTransaction();
                //delete related poll
                if (topic.IsPoll == 1)
                {
                    var poll = new PollsRepository().DeletePoll(id);

                }
                //delete related replies
                Execute("DELETE FROM " + this.ForumTablePrefix + "REPLY WHERE TOPIC_ID = @0", id);
                Delete(topic);
                CompleteTransaction();
                var forum = GetById<Models.Forum>(fId);
                forum.UpdateLastPost();
            }
            catch (Exception)
            {
                AbortTransaction();
                throw;
            }

        }

        public void DeleteTopic(IEnumerable<int> selectedtopics, int forumid)
        {
            try
            {
                BeginTransaction();
                var topics = selectedtopics as IList<int> ?? selectedtopics.ToList();
                Execute(
                    "DELETE FROM " + this.ForumTablePrefix + "REPLY WHERE TOPIC_ID IN (@topics) AND FORUM_ID=" + forumid,
                    new { topics });
                Execute(
                    "DELETE FROM " + this.ForumTablePrefix + "TOPICS WHERE TOPIC_ID IN (@topics) AND FORUM_ID=" +
                    forumid, new { topics });
                Execute("DELETE FROM " + this.ForumTablePrefix + "POLLS WHERE TOPIC_ID IN (@topics)", new { topics });
                Execute("DELETE FROM " + this.ForumTablePrefix + "POLL_VOTES WHERE TOPIC_ID IN (@topics)", new { topics });
                Execute("DELETE FROM " + this.ForumTablePrefix +
                        "POLL_ANSWERS WHERE POLL_ID NOT IN (SELECT POLL_ID FROM " + this.ForumTablePrefix + "POLLS)");
                CompleteTransaction();
                var forum = GetById<Models.Forum>(forumid);
                forum.UpdateLastPost();
            }
            catch (Exception)
            {
                AbortTransaction();
                throw;
            }
        }

        public void DeleteArchiveTopic(IEnumerable<int> selectedtopics, int forumid)
        {
            try
            {
                BeginTransaction();
                var topics = selectedtopics as IList<int> ?? selectedtopics.ToList();
                Execute(
                    "DELETE FROM " + this.ForumTablePrefix + "A_REPLY WHERE TOPIC_ID IN (@topics) AND FORUM_ID=" +
                    forumid, new { topics });
                Execute(
                    "DELETE FROM " + this.ForumTablePrefix + "A_TOPICS WHERE TOPIC_ID IN (@topics) AND FORUM_ID=" +
                    forumid, new { topics });
                CompleteTransaction();
                var forum = GetById<Models.Forum>(forumid);
                forum.UpdateLastPost();
            }
            catch (Exception)
            {
                AbortTransaction();
                throw;
            }
        }

        /// <summary>
        /// Performs a normal search query on the database.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public List<Topic> SearchForum(FullSearchModel model)
        {
            var sql = new Sql();
            sql.Select("t.*, " + (model.Archived ? "1" : "0") +
                       " AS Archived, a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,f.F_SUBJECT AS ForumSubject");
            if (model.Archived)
            {
                sql.From(this.ForumTablePrefix + "A_TOPICS t");
                sql.LeftJoin(this.ForumTablePrefix + "A_REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }
            else
            {
                sql.From(this.ForumTablePrefix + "TOPICS t");
                sql.LeftJoin(this.ForumTablePrefix + "REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }

            sql.LeftJoin(this.ForumTablePrefix + "FORUM f").On("t.FORUM_ID = f.FORUM_ID");
            sql.LeftJoin(this.MemberTablePrefix + "MEMBERS a").On("a.MEMBER_ID = t.T_AUTHOR");
            sql.LeftJoin(this.MemberTablePrefix + "MEMBERS lpa").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR");

            string[] terms = model.Term.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (model.ForumId > 0)
            {
                sql.Where("t.FORUM_ID=@0", model.ForumId);
            }
            if (!String.IsNullOrWhiteSpace(model.MemberName))
            {
                var author = Models.Member.GetByName(model.MemberName);
                sql.Where("(t.T_AUTHOR=@0 OR r.R_AUTHOR=@1)", author.Id, author.Id);
            }
            switch (model.PhraseType)
            {
                case Enumerators.SearchWordMatch.Any:
                    string or;
                    string sqlwhere;

                    switch (model.SearchIn)
                    {
                        case Enumerators.SearchIn.Subject:
                            or = " ";
                            sqlwhere = "";
                            var args1 = new List<object>();
                            foreach (string term in terms)
                            {
                                sqlwhere += or;
                                sqlwhere += "t.T_SUBJECT LIKE @0";
                                args1.Add("%" + term.Replace("'", "''") + "%");
                                or = " OR ";
                            }
                            sql.Where("(" + sqlwhere + ")", args1.ToArray());
                            break;
                        case Enumerators.SearchIn.Message:
                            or = " ";
                            sqlwhere = "";
                            var args2 = new List<object>();
                            foreach (string term in terms)
                            {
                                sqlwhere += or;
                                sqlwhere += "(t.T_MESSAGE LIKE @0 OR r.R_MESSAGE LIKE @1)";
                                args2.Add("%" + term.Replace("'", "''") + "%");
                                args2.Add("%" + term.Replace("'", "''") + "%");
                                or = " OR ";
                            }
                            sql.Where("(" + sqlwhere + ")", args2.ToArray());
                            break;
                    }
                    break;
                case Enumerators.SearchWordMatch.All:
                    foreach (string term in terms)
                    {
                        switch (model.SearchIn)
                        {
                            case Enumerators.SearchIn.Subject:
                                sql.Where("(t.T_SUBJECT LIKE @0)", "%" + term + "%");
                                break;
                            case Enumerators.SearchIn.Message:
                                sql.Where("(t.T_MESSAGE LIKE @0 OR r.R_MESSAGE LIKE @1)", "%" + term + "%",
                                    "%" + term + "%");
                                break;
                        }
                    }
                    break;
                case Enumerators.SearchWordMatch.ExactPhrase:
                    switch (model.SearchIn)
                    {
                        case Enumerators.SearchIn.Subject:
                            sql.Where("t.T_SUBJECT LIKE @0", "%" + model.Term + "%");
                            break;
                        case Enumerators.SearchIn.Message:
                            sql.Where("(t.T_SUBJECT LIKE @0 OR t.T_MESSAGE LIKE @1 OR r.R_MESSAGE LIKE @2)",
                                "%" + model.Term + "%", "%" + model.Term + "%", "%" + model.Term + "%");
                            break;
                    }
                    break;
            }
            string datestr = "";
            switch (model.SearchByDays)
            {
                case Enumerators.SearchDays.Any:
                    break;
                case Enumerators.SearchDays.Since30Days:
                    datestr = DateTime.UtcNow.AddMonths(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.Since60Days:
                    datestr = DateTime.UtcNow.AddMonths(-2).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.Since120Days:
                    datestr = DateTime.UtcNow.AddMonths(-6).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.SinceYear:
                    datestr = DateTime.UtcNow.AddYears(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                default:
                    datestr = DateTime.UtcNow.AddDays(-(int)model.SearchByDays).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;

            }
            if (!String.IsNullOrWhiteSpace(datestr))
            {
                switch (model.SearchIn)
                {
                    case Enumerators.SearchIn.Subject:
                        sql.Where("t.T_DATE > @0", datestr);
                        break;
                    case Enumerators.SearchIn.Message:
                        sql.Where("(t.T_DATE > @0 OR r.R_DATE > @1)", datestr, datestr);
                        break;
                }
            }

            var res = Query<Models.Topic>(sql);

            return res.Distinct(new TopicComparer()).ToList();
        }

        /// <summary>
        /// Performs a Fulltext search query on the database.
        /// Fulltext indexing needs to enabled and configured in the database
        /// </summary>
        /// <param name="model">search parameters object</param>
        /// <param name="user"></param>
        /// <param name="currentuserid"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public Page<Topic> FullTextSearch(FullSearchModel model, IPrincipal user, int currentuserid, int pagesize, int page = 1,
            string searchdate = null)
        {
            bool isAdministrator = user.IsAdministrator();
            string topicTable = this.ForumTablePrefix + "TOPICS";
            string replyTable = this.ForumTablePrefix + "REPLY";
            string strAndOr = "";
            string searchPhrase = "";
            string searchSuffix;
            string[] keywords = { };

            string allAllowedForums = string.Join(",", user.AllowedForumIDs().Select(n => n.ToString()).ToArray());
            if (String.IsNullOrWhiteSpace(allAllowedForums))
                allAllowedForums = "-1";
            List<object> args = new List<object> { currentuserid };
            if (model.Archived)
            {
                topicTable = this.ForumTablePrefix + "A_TOPICS";
                replyTable = this.ForumTablePrefix + "A_REPLY";
            }
            string fullTextSelect = "SELECT A.*," + (model.Archived ? "1" : "0") +
                                    " AS Archived FROM (SELECT DISTINCT T.FORUM_ID,T.TOPIC_ID, T.CAT_ID, T.T_AUTHOR, T.T_SUBJECT,T.T_MESSAGE, T.T_DATE, T.T_STATUS, T.T_LAST_POST, T.T_LAST_POST_AUTHOR, T.T_LAST_POST_REPLY_ID, T.T_REPLIES, T.T_UREPLIES, T.T_VIEW_COUNT,T.T_STICKY,M.M_PHOTO_URL AS AuthorAvatar, M.M_NAME AS PostAuthorName, M1.M_NAME AS LastPostAuthorName, F.F_SUBJECT AS ForumSubject,F.F_STATUS AS ForumStatus, C.CAT_ORDER, F.F_ORDER ";
            string fullTextFrom = "FROM ((((" + this.ForumTablePrefix + "FORUM F INNER JOIN " + topicTable +
                                  " T ON F.FORUM_ID = T.FORUM_ID) LEFT JOIN " + replyTable +
                                  " R ON T.TOPIC_ID = R.TOPIC_ID) INNER JOIN " + this.MemberTablePrefix +
                                  "MEMBERS M ON T.T_AUTHOR = M.MEMBER_ID) INNER JOIN " + this.ForumTablePrefix +
                                  "CATEGORY C ON T.CAT_ID = C.CAT_ID) LEFT JOIN " + this.MemberTablePrefix +
                                  "MEMBERS M1 ON T.T_LAST_POST_AUTHOR = M1.MEMBER_ID LEFT JOIN (SELECT TOPIC_ID, R_AUTHOR FROM " +
                                  replyTable +
                                  " GROUP BY TOPIC_ID, R_AUTHOR HAVING (R_AUTHOR = @0)) AS RD ON T.TOPIC_ID = RD.TOPIC_ID WHERE T.TOPIC_ID IN (";
            string fullTextTopic = "SELECT T.TOPIC_ID FROM " + topicTable + " T ";
            string fullTextReply = "UNION SELECT T.TOPIC_ID FROM " + topicTable + " T INNER JOIN " + replyTable +
                                   " R ON R.TOPIC_ID=T.TOPIC_ID ";
            string fullTextOrder = ")) AS A ORDER BY A.T_LAST_POST ";
            if (!String.IsNullOrWhiteSpace(model.OrderBy))
            {
                switch (model.OrderBy)
                {
                    case "a": //Author
                        fullTextOrder = ")) AS A ORDER BY A.PostAuthorName ";
                        break;
                    case "r": //Author
                        fullTextOrder = ")) AS A ORDER BY A.LastPostAuthorName ";
                        break;
                    case "t": //default
                        fullTextOrder = ")) AS A ORDER BY A.T_LAST_POST ";
                        break;
                    case "f": //forum
                        fullTextOrder = ")) AS A ORDER BY A.F_ORDER ";
                        break;
                    case "i":
                        fullTextOrder = ")) AS A ORDER BY A.T_SUBJECT ";
                        break;
                }
            }

            fullTextOrder += model.SortDir;

            switch (model.PhraseType)
            {
                case Enumerators.SearchWordMatch.All:
                    strAndOr = " AND ";
                    break;
                case Enumerators.SearchWordMatch.Any:
                    strAndOr = " OR ";
                    break;
            }

            switch (model.FullTextType)
            {
                case Enumerators.FullTextMatch.Loose:
                    searchSuffix = "*\"";
                    break;
                case Enumerators.FullTextMatch.Exact:
                    searchSuffix = "\"";
                    break;
                default:
                    searchSuffix = "\"";
                    break;
            }
            if (!String.IsNullOrWhiteSpace(model.Term))
            {
                searchPhrase = model.Term.Replace("'", "''").Replace("@", "@@");
                keywords = searchPhrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //remove noise words
                keywords = keywords.Where(val => !QueryStrings.NoiseWords.Contains(val)).ToArray();
            }

            if (keywords.Length > 0 || !String.IsNullOrWhiteSpace(model.MemberName))
            {
                string topicWhere;
                string replyWhere;
                if (!String.IsNullOrWhiteSpace(searchPhrase) && keywords.Length > 0)
                {
                    topicWhere = " WHERE ("; //31
                    replyWhere = " WHERE (";

                    switch (model.SearchIn)
                    {
                        case Enumerators.SearchIn.Subject:
                            if (strAndOr == "")
                            {
                                topicWhere += " Contains(T.T_SUBJECT,'\"" + searchPhrase + "\"') "; //" T.T_SUBJECT LIKE '%" + searchPhrase + "%' "
                            }
                            else
                            {
                                topicWhere += " Contains(T.T_SUBJECT, ' ";
                                int cnt = 1;
                                foreach (string keyword in keywords)
                                {

                                    topicWhere = topicWhere + "\"" + keyword + searchSuffix;
                                    if (cnt < keywords.Length)
                                        topicWhere += strAndOr;
                                    cnt++;
                                }
                                topicWhere += "') ";
                            }
                            break;
                        case Enumerators.SearchIn.Message:
                            if (strAndOr == "")
                            {
                                topicWhere += "( Contains((T.T_SUBJECT,T.T_MESSAGE),'\"" + searchPhrase + "\"') ) "; //"(T.T_SUBJECT LIKE '%" + searchPhrase + "%' "
                                //topicWhere += " OR Contains(T.T_MESSAGE,'\"" + searchPhrase + "\"') ) "; //T.T_MESSAGE LIKE '%" + searchPhrase + "%') "
                                replyWhere += " (Contains(R.R_MESSAGE, '\"" + searchPhrase + "\"')) ";
                            }
                            else
                            {
                                topicWhere += " (Contains((T.T_SUBJECT,T.T_MESSAGE), ' ";
                                //var tmpSql = " OR Contains(T.T_MESSAGE, ' ";
                                replyWhere += " (Contains(R.R_MESSAGE, ' ";
                                int cnt = 1;
                                foreach (string keyword in keywords)
                                {
                                    topicWhere += "\"" + keyword + searchSuffix;
                                    //tmpSql += "\"" + keyword + searchSuffix;
                                    replyWhere += "\"" + keyword + searchSuffix;
                                    if (cnt < keywords.Length)
                                    {
                                        topicWhere += strAndOr;
                                        //tmpSql += strAndOr;
                                        replyWhere += strAndOr;
                                    }
                                    cnt++;
                                }
                                topicWhere = topicWhere + " ') )"; // + tmpSql + " ')) ";
                                replyWhere = replyWhere + " ')) ";
                            }
                            break;
                    }
                }
                else
                {
                    topicWhere = " WHERE ((0 = 0)";
                    replyWhere = " WHERE ((0 = 0)";
                }
                if (!isAdministrator)
                {
                    topicWhere = topicWhere + " AND ((T.T_AUTHOR <> " + currentuserid;
                    topicWhere = topicWhere + " AND T.T_STATUS < 2)"; // Ignore unapproved/held posts
                    topicWhere = topicWhere + " OR T.T_AUTHOR = " + currentuserid + ")";
                    replyWhere = replyWhere + " AND ((T.T_AUTHOR <> " + currentuserid;
                    replyWhere = replyWhere + " AND T.T_STATUS < 2)"; // Ignore unapproved/held posts
                    replyWhere = replyWhere + " OR T.T_AUTHOR = " + currentuserid + ")";
                }
                //date
                if (model.SearchByDays != 0)
                {
                    string datestr = "";
                    switch (model.SearchByDays)
                    {
                        case Enumerators.SearchDays.Any:
                            break;
                        case Enumerators.SearchDays.Since30Days:
                            datestr = DateTime.UtcNow.AddMonths(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                            break;
                        case Enumerators.SearchDays.Since60Days:
                            datestr = DateTime.UtcNow.AddMonths(-2).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                            break;
                        case Enumerators.SearchDays.Since120Days:
                            datestr = DateTime.UtcNow.AddMonths(-6).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                            break;
                        case Enumerators.SearchDays.SinceYear:
                            datestr = DateTime.UtcNow.AddYears(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                            break;
                        default:
                            datestr = DateTime.UtcNow.AddDays(-(int)model.SearchByDays).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                            break;

                    }
                    topicWhere += " AND (T.T_LAST_POST > '" + datestr + "')";
                    replyWhere += " AND (T.T_LAST_POST > '" + datestr + "')";
                }
                else if (searchdate != null)
                {
                    var dt = searchdate.ToDateTime().Value.ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    topicWhere += " AND (T.T_LAST_POST > '" + dt + "')";
                    replyWhere += " AND (T.T_LAST_POST > '" + dt + "')";
                }
                //Member
                if (!String.IsNullOrWhiteSpace(model.MemberName))
                {
                    bool wildcard = false;
                    if (model.MemberName.Contains("*"))
                    {
                        wildcard = true;
                        model.MemberName = model.MemberName.Replace("*", "%");
                    }
                    //var author = Models.Member.GetByName(model.MemberName);
                    if (wildcard)
                    {
                        topicWhere = topicWhere + " AND M.M_NAME LIKE '" + model.MemberName + "' ";
                        replyWhere = replyWhere + " AND M.M_NAME LIKE '" + model.MemberName + "' ";

                        fullTextTopic = fullTextTopic + "INNER JOIN " + this.MemberTablePrefix +
                                        "MEMBERS M ON T.T_AUTHOR=M.MEMBER_ID ";
                        fullTextReply = fullTextReply + "INNER JOIN " + this.MemberTablePrefix +
                                        "MEMBERS M ON R.R_AUTHOR=M.MEMBER_ID ";
                    }
                    else
                    {

                        topicWhere = topicWhere + " AND M.M_NAME = '" + model.MemberName + "' ";
                        replyWhere = replyWhere + " AND M.M_NAME = '" + model.MemberName + "' ";

                        fullTextTopic = fullTextTopic + "INNER JOIN " + this.MemberTablePrefix +
                                        "MEMBERS M ON T.T_AUTHOR=M.MEMBER_ID ";
                        fullTextReply = fullTextReply + "INNER JOIN " + this.MemberTablePrefix +
                                        "MEMBERS M ON R.R_AUTHOR=M.MEMBER_ID ";
                    }

                }
                //Category
                if (model.Category.HasValue && model.Category > 0)
                {
                    if (!isAdministrator)
                    {
                        topicWhere = topicWhere + " AND T.CAT_ID = " + model.Category + " AND T.FORUM_ID IN (" +
                                     allAllowedForums + ")";
                        replyWhere = replyWhere + " AND R.CAT_ID = " + model.Category + " AND R.FORUM_ID IN (" +
                                     allAllowedForums + ")";
                    }
                    else
                    {
                        topicWhere = topicWhere + " AND T.CAT_ID = " + model.Category + " ";
                        replyWhere = replyWhere + " AND R.CAT_ID = " + model.Category + " ";
                    }
                }
                else if (model.ForumId > 0) //Forum
                {
                    if (!isAdministrator)
                    {
                        topicWhere = topicWhere + " AND F.FORUM_ID = " + model.ForumId + " AND F.FORUM_ID IN (" +
                                     allAllowedForums + ")";
                        replyWhere = replyWhere + " AND F.FORUM_ID = " + model.ForumId + " AND F.FORUM_ID IN (" +
                                     allAllowedForums + ")";
                    }
                    else
                    {
                        topicWhere = topicWhere + " AND F.FORUM_ID = " + model.ForumId + " ";
                        replyWhere = replyWhere + " AND F.FORUM_ID = " + model.ForumId + " ";
                    }

                    fullTextTopic = fullTextTopic + "INNER JOIN " + this.ForumTablePrefix +
                                    "FORUM F ON T.FORUM_ID=F.FORUM_ID ";
                    fullTextReply = fullTextReply + "INNER JOIN " + this.ForumTablePrefix +
                                    "FORUM F ON T.FORUM_ID=F.FORUM_ID ";
                }
                else
                {
                    if (!isAdministrator)
                    {
                        topicWhere += " AND F.FORUM_ID IN (" + allAllowedForums + ")";
                        replyWhere += " AND F.FORUM_ID IN (" + allAllowedForums + ")";

                        fullTextTopic += "INNER JOIN " + this.ForumTablePrefix + "FORUM F ON T.FORUM_ID=F.FORUM_ID ";
                        fullTextReply += "INNER JOIN " + this.ForumTablePrefix + "FORUM F ON T.FORUM_ID=F.FORUM_ID ";
                    }
                }
                topicWhere += " AND F.F_TYPE <> 1)";
                replyWhere += " AND F.F_TYPE <> 1)";

                var strFinalSql = fullTextSelect + fullTextFrom + fullTextTopic + topicWhere;

                if (model.SearchIn == Enumerators.SearchIn.Message)
                {
                    strFinalSql += fullTextReply + replyWhere;
                }
                strFinalSql += fullTextOrder;

                return Page<Models.Topic>(page, pagesize, strFinalSql, args.ToArray());


            }
            //Invalid search params so return null
            return null;
        }

        public Page<Topic> SearchForum(FullSearchModel model, IPrincipal user, int currentuserid, int pagesize, int page = 1,
            string searchdate = null)
        {
            var sql1 = new Sql();
            var sql2 = new Sql();
            sql1.Select("t.*, " + (model.Archived ? "1" : "0") +
                        " AS Archived, a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,f.F_SUBJECT AS ForumSubject,f.F_STATUS AS ForumStatus,f.F_ORDER AS ForumOrder");
            if (model.Archived)
            {
                sql1.From(this.ForumTablePrefix + "A_TOPICS t");
                //sql1.LeftJoin(this.ForumTablePrefix + "A_REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }
            else
            {
                sql1.From(this.ForumTablePrefix + "TOPICS t");
                //sql1.LeftJoin(this.ForumTablePrefix + "REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }

            sql1.LeftJoin(this.ForumTablePrefix + "FORUM f").On("t.FORUM_ID = f.FORUM_ID");
            sql1.LeftJoin(this.MemberTablePrefix + "MEMBERS a").On("a.MEMBER_ID = t.T_AUTHOR");
            sql1.LeftJoin(this.MemberTablePrefix + "MEMBERS lpa").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR");

            string[] terms = model.Term.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            sql2.Select("DISTINCT t.TOPIC_ID");
            if (model.Archived)
            {
                sql2.From(this.ForumTablePrefix + "A_TOPICS t");
                sql2.LeftJoin(this.ForumTablePrefix + "A_REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
                sql2.LeftJoin(this.MemberTablePrefix + "MEMBERS ra").On("ra.MEMBER_ID = r.R_AUTHOR");
                sql2.LeftJoin(this.MemberTablePrefix + "MEMBERS ta").On("ta.MEMBER_ID = t.T_AUTHOR");
            }
            else
            {
                sql2.From(this.ForumTablePrefix + "TOPICS t");
                sql2.LeftJoin(this.ForumTablePrefix + "REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
                sql2.LeftJoin(this.MemberTablePrefix + "MEMBERS ra").On("ra.MEMBER_ID = r.R_AUTHOR");
                sql2.LeftJoin(this.MemberTablePrefix + "MEMBERS ta").On("ta.MEMBER_ID = t.T_AUTHOR");
            }

            if (model.Category.HasValue && model.Category > 0)
            {
                sql2.Where(" t.CAT_ID =" + model.Category + " ");
            }
            else if (!String.IsNullOrWhiteSpace(model.ForumIds))
            {
                List<int> tagIds = model.ForumIds.Split(',').Select(int.Parse).ToList();
                if (!tagIds.Contains(0))
                {
                    sql2.Where(" t.FORUM_ID IN (" + model.ForumIds + ") ");
                }

            }
            else if (model.ForumId > 0)
            {
                sql2.Where(" t.FORUM_ID=@0", model.ForumId);
            }
            if (!String.IsNullOrWhiteSpace(model.MemberName))
            {
                bool wildcard = false;
                if (model.MemberName.Contains("*"))
                {
                    wildcard = true;
                    model.MemberName = model.MemberName.Replace("*", "%");
                }
                //var author = Models.Member.GetByName(model.MemberName);
                if (wildcard)
                {
                    if (model.SearchIn == Enumerators.SearchIn.Subject)
                    {
                        sql2.Where("(ta.M_NAME LIKE @0 )", model.MemberName);
                    }
                    else
                    {
                        sql2.Where("(ta.M_NAME LIKE @0 OR ra.M_NAME LIKE @1)", model.MemberName, model.MemberName);
                    }
                }
                else
                {
                    if (model.SearchIn == Enumerators.SearchIn.Subject)
                    {
                        sql2.Where("(a.M_NAME=@0 )", model.MemberName);
                    }
                    else
                    {
                        sql2.Where("(a.M_NAME=@0 OR ra.M_NAME=@1)", model.MemberName, model.MemberName);
                    }
                }

                //if (model.SearchIn == Enumerators.SearchIn.Subject)
                //{
                //    sql2.Where("(t.T_AUTHOR=@0 )", author.Id);
                //}
                //else
                //{
                //    sql2.Where("(t.T_AUTHOR=@0 OR r.R_AUTHOR=@1)", author.Id, author.Id);
                //}

            }

            #region search terms
            switch (model.PhraseType)
            {
                case Enumerators.SearchWordMatch.Any:
                    string or;
                    string sqlwhere;

                    switch (model.SearchIn)
                    {
                        case Enumerators.SearchIn.Subject:
                            or = " ";
                            sqlwhere = "";
                            var args1 = new List<object>();
                            if (terms.Length > 0)
                            {
                                //must include
                                if (terms.Any(t => t.StartsWith("+", StringComparison.Ordinal)))
                                {
                                    foreach (string term in terms.Where(t => t.StartsWith("+", StringComparison.Ordinal)))
                                    {
                                        string eTag = "% ", sTag = " %";
                                        if (term.Substring(1).StartsWith("*", StringComparison.Ordinal))
                                        {
                                            sTag = "%";
                                        }

                                        if (term.EndsWith("*"))
                                        {
                                            eTag = "%";
                                        }
                                        sqlwhere += or;
                                        sqlwhere += "t.T_SUBJECT LIKE @0";
                                        args1.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        or = " AND ";
                                    }
                                    sqlwhere += or;
                                }

                                //mustexclude
                                if (terms.Any(t => t.StartsWith("-")))
                                {

                                    sqlwhere += "(";
                                    foreach (string term in terms.Where(t => t.StartsWith("-")))
                                    {
                                        string eTag = "% ", sTag = " %";
                                        if (term.Substring(1).StartsWith("*"))
                                        {
                                            sTag = "%";
                                        }

                                        if (term.EndsWith("*"))
                                        {
                                            eTag = "%";
                                        }
                                        sqlwhere += or;
                                        sqlwhere += "t.T_SUBJECT NOT LIKE @0";
                                        args1.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        or = " AND ";
                                    }
                                    sqlwhere += ")";
                                    sqlwhere += or;

                                }

                                if (terms.Any(t => !t.StartsWith("+") && !t.StartsWith("-")))
                                {
                                    sqlwhere += "(";
                                    foreach (string term in terms.Where(t => !t.StartsWith("+") && !t.StartsWith("-")))
                                    {
                                        string eTag = "% ", sTag = " %";
                                        if (term.Substring(1).StartsWith("*"))
                                        {
                                            sTag = "%";
                                        }

                                        if (term.EndsWith("*"))
                                        {
                                            eTag = "%";
                                        }
                                        sqlwhere += or;
                                        sqlwhere += "t.T_SUBJECT LIKE @0";
                                        args1.Add(sTag + term.Replace("'", "''").Trim('*') + eTag);
                                        or = " OR ";
                                    }
                                    sqlwhere += ")";
                                }

                                sql2.Where("(" + sqlwhere + ")", args1.ToArray());
                            }
                            break;
                        case Enumerators.SearchIn.Message:
                            or = " ";
                            sqlwhere = "";
                            var args2 = new List<object>();
                            bool exclude = false;
                            bool include = false;
                            if (terms.Length > 0)
                            {
                                //must include
                                if (terms.Any(t => t.StartsWith("+")))
                                {
                                    include = true;
                                    foreach (string term in terms.Where(t => t.StartsWith("+")))
                                    {
                                        string eTag = "% ", sTag = " %";
                                        if (term.Substring(1).StartsWith("*"))
                                        {
                                            sTag = "%";
                                        }

                                        if (term.EndsWith("*"))
                                        {
                                            eTag = "%";
                                        }
                                        sqlwhere += or;
                                        sqlwhere += "(t.T_SUBJECT LIKE @0 OR t.T_MESSAGE LIKE @1 OR r.R_MESSAGE LIKE @2)";
                                        args2.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        args2.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        args2.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        or = " AND ";
                                    }

                                }

                                //mustexclude
                                if (terms.Any(t => t.StartsWith("-")))
                                {
                                    exclude = true;
                                    if (include)
                                    {
                                        sqlwhere += or;
                                        or = " ";
                                    }
                                    sqlwhere += "(";
                                    foreach (string term in terms.Where(t => t.StartsWith("-")))
                                    {
                                        string eTag = "% ", sTag = " %";
                                        if (term.Substring(1).StartsWith("*"))
                                        {
                                            sTag = "%";
                                        }

                                        if (term.EndsWith("*"))
                                        {
                                            eTag = "%";
                                        }
                                        sqlwhere += or;
                                        sqlwhere += "(t.T_SUBJECT NOT LIKE @0 AND t.T_MESSAGE NOT LIKE @0 AND r.R_MESSAGE NOT LIKE @1)";
                                        args2.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        args2.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        args2.Add(sTag + term.Replace("'", "''").Substring(1).Trim('*') + eTag);
                                        or = " AND ";
                                    }
                                    sqlwhere += ")";

                                }

                                if (terms.Any(t => !t.StartsWith("+") && !t.StartsWith("-")))
                                {
                                    if (include || exclude)
                                    {
                                        sqlwhere += or;
                                        sqlwhere += "(";
                                        or = " ";
                                    }

                                    foreach (string term in terms.Where(t => !t.StartsWith("+") && !t.StartsWith("-")))
                                    {
                                        string eTag = "% ", sTag = " %";
                                        if (term.Substring(1).StartsWith("*"))
                                        {
                                            sTag = "%";
                                        }

                                        if (term.EndsWith("*"))
                                        {
                                            eTag = "%";
                                        }
                                        sqlwhere += or;
                                        sqlwhere += "(t.T_SUBJECT LIKE @0 OR t.T_MESSAGE LIKE @0 OR r.R_MESSAGE LIKE @1)";
                                        args2.Add(sTag + term.Replace("'", "''").Trim('*') + eTag);
                                        args2.Add(sTag + term.Replace("'", "''").Trim('*') + eTag);
                                        args2.Add(sTag + term.Replace("'", "''").Trim('*') + eTag);
                                        or = " OR ";
                                    }
                                    if (include || exclude)
                                    {
                                        sqlwhere += ")";
                                    }
                                }

                                sql2.Where("(" + sqlwhere + ")", args2.ToArray());
                            }
                            break;
                    }
                    break;
                case Enumerators.SearchWordMatch.All:
                    foreach (string term in terms)
                    {
                        string eTag = "% ", sTag = " %";
                        if (term.Substring(1).StartsWith("*"))
                        {
                            sTag = "%";
                        }

                        if (term.EndsWith("*"))
                        {
                            eTag = "%";
                        }
                        switch (model.SearchIn)
                        {
                            case Enumerators.SearchIn.Subject:
                                if (term.StartsWith("-"))
                                {
                                    sql2.Where("(t.T_SUBJECT NOT LIKE @0)", sTag + term.Substring(1).Trim('*') + eTag);
                                }
                                else if (term.StartsWith("+"))
                                {
                                    sql2.Where("(t.T_SUBJECT LIKE @0)", sTag + term.Substring(1).Trim('*') + eTag);
                                }
                                else
                                {
                                    sql2.Where("(t.T_SUBJECT LIKE @0)", sTag + term.Trim('*') + eTag);
                                }

                                break;
                            case Enumerators.SearchIn.Message:
                                if (term.StartsWith("-"))
                                {
                                    sql2.Where("(t.T_SUBJECT NOT LIKE @0 AND t.T_MESSAGE NOT LIKE @1 AND r.R_MESSAGE NOT LIKE @2)", sTag + term.Substring(1).Trim('*') + eTag,
                                        sTag + term.Substring(1).Trim('*') + eTag, sTag + term.Substring(1).Trim('*') + eTag);
                                }
                                else if (term.StartsWith("+"))
                                {
                                    sql2.Where("(t.T_SUBJECT LIKE @0 OR t.T_MESSAGE LIKE @1 OR r.R_MESSAGE LIKE @2)", sTag + term.Substring(1).Trim('*') + eTag,
                                        sTag + term.Substring(1).Trim('*') + eTag, sTag + term.Substring(1).Trim('*') + eTag);
                                }
                                else
                                {
                                    sql2.Where("(t.T_SUBJECT LIKE @0 OR t.T_MESSAGE LIKE @1 OR r.R_MESSAGE LIKE @2)", sTag + term.Trim('*') + eTag,
                                        sTag + term.Trim('*') + eTag, sTag + term.Trim('*') + eTag);
                                }

                                break;
                        }
                    }
                    break;
                case Enumerators.SearchWordMatch.ExactPhrase:
                    switch (model.SearchIn)
                    {
                        case Enumerators.SearchIn.Subject:
                            if (!String.IsNullOrWhiteSpace(model.Term))
                                sql2.Where("t.T_SUBJECT LIKE @0", "%" + model.Term + "%");
                            break;
                        case Enumerators.SearchIn.Message:
                            if (!String.IsNullOrWhiteSpace(model.Term))
                                sql2.Where("(t.T_SUBJECT LIKE @0 OR t.T_MESSAGE LIKE @1 OR r.R_MESSAGE LIKE @2)",
                                    "%" + model.Term + "%", "%" + model.Term + "%", "%" + model.Term + "%");
                            break;
                    }
                    break;
            }
            #endregion

            #region datefilter
            string datestr = "";
            switch (model.SearchByDays)
            {
                case Enumerators.SearchDays.Any:
                    break;
                case Enumerators.SearchDays.Since30Days:
                    datestr = DateTime.UtcNow.AddMonths(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.Since60Days:
                    datestr = DateTime.UtcNow.AddMonths(-2).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.Since120Days:
                    datestr = DateTime.UtcNow.AddMonths(-6).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.SinceYear:
                    datestr = DateTime.UtcNow.AddYears(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                default:
                    datestr = DateTime.UtcNow.AddDays(-(int)model.SearchByDays).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;

            }
            if (!String.IsNullOrEmpty(searchdate))
            {
                datestr = searchdate.ToDateTime().Value.ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
            }
            if (!String.IsNullOrWhiteSpace(datestr))
            {
                switch (model.SearchIn)
                {
                    case Enumerators.SearchIn.Subject:
                        sql2.Where("t.T_DATE > @0", datestr);
                        break;
                    case Enumerators.SearchIn.Message:
                        sql2.Where("(t.T_DATE > @0 OR r.R_DATE > @1)", datestr, datestr);
                        break;
                }
            }
            #endregion

            sql1.Where("(t.TOPIC_ID IN(" + sql2.SQL + "))", sql2.Arguments);
            //sql1.OrderBy("t.T_LAST_POST DESC");
            switch (model.OrderBy)
            {
                case "a": //Author
                    sql1.OrderBy("a.M_NAME " + model.SortDir);
                    break;
                case "r": //Author
                    sql1.OrderBy("lpa.M_NAME " + model.SortDir);
                    break;
                case "t": //default
                    sql1.OrderBy("t.T_LAST_POST " + model.SortDir);
                    break;
                case "f": //forum
                    sql1.OrderBy("f.F_ORDER " + model.SortDir);

                    break;
                case "i":
                    sql1.OrderBy("t.T_SUBJECT " + model.SortDir);
                    break;
                default:
                    sql1.OrderBy("t.T_LAST_POST DESC");
                    break;
            }
            var res = Page<Models.Topic>(page, pagesize, sql1);

            return res; //.Distinct(new TopicComparer()).ToList();
        }

        /// <summary>
        /// Fetch the ranking info from the database
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, Ranking> GetRankings()
        {
            Dictionary<int, Ranking> rankings = new Dictionary<int, Ranking>();
            var service = new InMemoryCache() { DoNotExpire = true };
            using (SnitzDataContext db = new SnitzDataContext())
            {
                foreach (Rankings rank in db.Query<Rankings>("Select * FROM " + db.ForumTablePrefix + "RANKING"))
                {
                    Ranking ranking = new Ranking
                    {
                        Title = rank.Title,
                        Image = rank.Image,
                        RankLevel = rank.Threshold,
                        Repeat = rank.RepeatCount
                    };
                    if (!rankings.ContainsKey(rank.Id))
                        rankings.Add(rank.Id, ranking);
                }
                //return rankings;
            }
            return service.GetOrSet("Snitz.Rankings", () => rankings);

        }

        /// <summary>
        /// File download counter
        /// </summary>
        /// <param name="filename"></param>
        public void UpdateFileCounter(string filename)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("IF EXISTS(SELECT FC_ID FROM FORUM_FILECOUNT WHERE FileName = @0)");
            sql.AppendLine("    UPDATE FORUM_FILECOUNT SET LinkHits = LinkHits + 1 WHERE FileName = @1");

            using (SnitzDataContext db = new SnitzDataContext())
            {
                db.Execute(sql.ToString(), filename, filename, filename);
            }
        }

        public void AddFileCounter(string filename)
        {
            DownloadFile file = new DownloadFile();
            file.FileName = filename;
            file.Name = filename;
            file.UploadedDate = DateTime.UtcNow;

            using (SnitzDataContext db = new SnitzDataContext())
            {
                db.Save(file);
            }
        }

        public static Dictionary<string, int> GetFileCounts(string filename)
        {
            var e = new Dictionary<string, int>();

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT FileName, LinkHits FROM FORUM_FILECOUNT");
            if (filename != null)
                sql.AppendLine("WHERE FileName = @0");

            using (SnitzDataContext db = new SnitzDataContext())
            {
                try
                {
                    var d =
                        db.Fetch<Pair<string, int>>("SELECT FileName AS 'Key', LinkHits AS 'Value' FROM FORUM_FILECOUNT");
                    foreach (var item in d)
                    {
                        if (!e.ContainsKey(item.Key))
                            e.Add(item.Key, item.Value);
                    }
                }
                catch (SqlException)
                {

                }


            }
            return e;
        }

        public static IEnumerable<DownloadFile> GetDownloadFiles()
        {

            using (SnitzDataContext db = new SnitzDataContext())
            {
                try
                {
                    var test = db.Query<DownloadFile>("SELECT * FROM FORUM_FILECOUNT");
                    return test;
                }
                catch (SqlException)
                {
                    return null;
                }


            }
        }

        public void UpdatePostCount()
        {
            //call stored proc to update postcount
            Execute(dbtype == "mysql" ? "call  snitz_updatecounts;" : ";exec snitz_updatecounts");
        }

        public void SetStickyTopic(List<int> topics, int sticky)
        {
            var donotarchive = 1;
            if (sticky == 1)
            {
                donotarchive = 1;
            }
            try
            {
                BeginTransaction();
                Execute(
                    "UPDATE " + this.ForumTablePrefix +
                    "TOPICS SET T_STICKY=@sticky , T_ARCHIVE_FLAG=@donotarchive WHERE TOPIC_ID IN (@topics)",
                    new { sticky, donotarchive, topics });
                CompleteTransaction();
            }
            catch (Exception)
            {
                AbortTransaction();
                throw;
            }
        }

        public static dynamic FetchSubscribers(Reply reply)
        {
            using (SnitzDataContext db = new SnitzDataContext())
            {
                var res = db.Fetch<Subscriptions>(Sql.Builder
                        .Select(
                            "S.*, C.CAT_NAME AS Category,C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F.F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_EMAIL AS UserEmail")
                        .From(db.ForumTablePrefix + "SUBSCRIPTIONS S")
                        .LeftJoin(db.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID")
                        .LeftJoin(db.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID")
                        .LeftJoin(db.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID")
                        .LeftJoin(db.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID")
                        .Where(
                            "S.FORUM_ID=@0 AND (S.TOPIC_ID=0 OR S.TOPIC_ID=@1) AND S.MEMBER_ID <> @2 AND M.M_STATUS=1",
                            reply.ForumId, reply.TopicId, reply.AuthorId)
                );
                return res;
            }
        }

        public static dynamic FetchTopicSubscribers(Topic topic)
        {
            using (SnitzDataContext db = new SnitzDataContext())
            {
                return db.Fetch<Subscriptions>(Sql.Builder
                        .Select(
                            "S.*, C.CAT_NAME AS Category,C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F.F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_EMAIL AS UserEmail")
                        .From(db.ForumTablePrefix + "SUBSCRIPTIONS S")
                        .LeftJoin(db.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID")
                        .LeftJoin(db.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID")
                        .LeftJoin(db.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID")
                        .LeftJoin(db.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID")
                        .Where("S.FORUM_ID=@0 AND S.TOPIC_ID=0 AND S.MEMBER_ID <> @1 AND M.M_STATUS=1", topic.ForumId,
                            topic.AuthorId)
                );
            }
        }

        public static void ImportLangResCSV(string path, bool updateExisting)
        {
            var csv = new CSVFiles(path, new[]
            {
                new DataColumn("pk", typeof(string)),
                new DataColumn("ResourceId", typeof(string)),
                new DataColumn("Value", typeof(string)),
                new DataColumn("Culture", typeof(string)),
                new DataColumn("Type", typeof(string)),
                new DataColumn("ResourceSet", typeof(string))
            });

            DataTable dt = csv.Table;

            string langCon = ConfigurationManager.AppSettings["LanguageConnectionString"];
            using (var db = new SnitzDataContext())
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (String.IsNullOrWhiteSpace(row.Field<string>("ResourceId")))
                        continue;
                    ResourceEntry res = new ResourceEntry
                    {
                        Name = row.Field<string>("ResourceId"),
                        ResourceSet = row.Field<string>("ResourceSet"),
                        Value = row.Field<string>("Value"),
                        Culture = row.Field<string>("Culture")
                    };
                    const string sql = "select pk,Culture, ResourceId, Value, ResourceSet from LANGUAGE_RES where culture = @0 and ResourceId = @1;";
                    var existing = db.SingleOrDefault<ResourceEntry>(sql, row["Culture"], row["ResourceId"]);

                    if (existing != null)
                    {
                        if (updateExisting)
                        {
                            existing.Value = res.Value;
                            existing.ResourceSet = res.ResourceSet;
                            db.Update(existing, new string[] { "Value", "ResourceSet" });
                        }
                    }
                    else
                    {
                        db.Insert(res);
                    }
                }
            }

        }

        public static void ImportSpamDomainsCSV(string path)
        {
            DataTable dt = new DataTable();

            dt.Columns.AddRange(new[]
            {
                new DataColumn("SPAM_ID", typeof(int)),
                new DataColumn("SPAM_SERVER", typeof(string))
            });

            string csvData = System.IO.File.ReadAllText(path);

            foreach (string row in csvData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    dt.Rows.Add();
                    dt.Rows[dt.Rows.Count - 1][1] = row;
                }
            }

            using (var db = new SnitzDataContext())
            {
                foreach (DataRow row in dt.Rows)
                {
                    SpamFilter res = new SpamFilter
                    {
                        SpamServer = row.Field<string>("SPAM_SERVER")
                    };
                    const string sql = "select SPAM_ID from FORUM_SPAM_MAIL where SPAM_SERVER = @0;";
                    var existing = db.SingleOrDefault<ResourceEntry>(sql, row["SPAM_SERVER"]);
                    if (existing == null)
                    {
                        db.Insert(res);
                    }
                }
            }

        }


        /// <summary>
        /// This function checks for any sleeping connections beyond a reasonable time and kills them.
        /// Since .NET appears to have a bug with how pooling MySQL connections are handled and leaves
        /// too many sleeping connections without closing them, we will kill them here.
        /// </summary>
        /// iMinSecondsToExpire - all connections sleeping more than this amount in seconds will be killed.
        /// <returns>integer - number of connections killed</returns>
        public static int KillSleepingConnections(int iMinSecondsToExpire)
        {
            if (DBTypeName != "mysql")
                return 0;
            string strSql = "show processlist";
            System.Collections.ArrayList m_ProcessesToKill = new ArrayList();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString))
                {
                    conn.Open();
                    // Get a list of processes to kill.
                    using (MySqlCommand cmd = new MySqlCommand(strSql))
                    {
                        cmd.Connection = conn;

                        var myReader = cmd.ExecuteReader();
                        while (myReader.Read())
                        {
                            // Find all processes sleeping with a timeout value higher than our threshold.
                            int iPID = Convert.ToInt32(myReader["Id"].ToString());
                            string strState = myReader["Command"].ToString();
                            int iTime = Convert.ToInt32(myReader["Time"].ToString());

                            if (strState == "Sleep" && iTime >= iMinSecondsToExpire && iPID > 0)
                            {
                                // This connection is sitting around doing nothing. Kill it.
                                m_ProcessesToKill.Add(iPID);
                            }
                        }

                        myReader.Close();

                        foreach (int aPID in m_ProcessesToKill)
                        {
                            strSql = "kill " + aPID;
                            cmd.CommandText = strSql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception)
            {
            }


            return m_ProcessesToKill.Count;
        }

        internal T GetById<T>(int id)
        {
            var repo = GetInstance();

            return repo.Single<T>(id);
        }

        internal T GetById<T>(string id)
        {
            var repo = GetInstance();

            return repo.SingleOrDefault<T>(id);
        }

        internal class test : SnitzDataContext.Record<test>
        {
            public int parentLevel { get; set; }
            public int threadId { get; set; }
            public int replyTo { get; set; }
            public int sortOrder { get; set; }

        }
    }

}

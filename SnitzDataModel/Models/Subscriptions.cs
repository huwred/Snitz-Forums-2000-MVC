using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using PetaPoco;
using Snitz.Base;
using SnitzCore.Utility;

namespace SnitzDataModel.Models
{
    public partial class Subscriptions
    {
        [ResultColumn]
        public string Category { get; set; }
        [ResultColumn]
        public Enumerators.CategorySubscription CategoryLevel { get; set; }
        [ResultColumn]
        public string Forum { get; set; }
        [ResultColumn]
        public Enumerators.Subscription ForumLevel { get; set; }
        [ResultColumn]
        public string Topic { get; set; }
        [ResultColumn]
        public string TopicMessage { get; set; }
        [ResultColumn]
        public string Username { get; set; }
        [ResultColumn]
        public string UserEmail { get; set; }
        [ResultColumn]
        public int UserStatus { get; set; }

        [NotMapped]
        public bool Selected { get; set; }


        public static void RemoveMember(int memberid)
        {
            repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "SUBSCRIPTIONS WHERE MEMBER_ID=@0", memberid);
        }

        /// <summary>
        /// Fetch a list of subscriptions for a user
        /// </summary>
        /// <param name="memberid"></param>
        /// <returns></returns>
        public static IEnumerable<Models.Subscriptions> Member(int memberid)
        {
            var sql = new Sql();
            sql.Select("S.*, C.CAT_NAME AS Category, F.F_SUBJECT AS Forum, T.T_SUBJECT AS Topic, T.T_MESSAGE AS TopicMessage");
            sql.From(repo.ForumTablePrefix + "SUBSCRIPTIONS S");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID");
            sql.Where("S.MEMBER_ID = @0", memberid);
            sql.OrderBy("S.FORUM_ID", "S.TOPIC_ID");

            return repo.Query<Models.Subscriptions>(sql);
        }

        public static IEnumerable<Models.Subscriptions> All()
        {
            var sql = new Sql();
            sql.Select("S.*, C.CAT_NAME AS Category, C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_STATUS As UserStatus");
            sql.From(repo.ForumTablePrefix + "SUBSCRIPTIONS S");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID");
            sql.OrderBy("S.CAT_ID", "S.FORUM_ID", "S.TOPIC_ID");

            return repo.Query<Models.Subscriptions>(sql);
        }
        public static IEnumerable<Models.Subscriptions> BoardSubs()
        {
            var sql = new Sql();
            sql.Select("S.*, C.CAT_NAME AS Category, C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_STATUS As UserStatus");
            sql.From(repo.ForumTablePrefix + "SUBSCRIPTIONS S");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID");
            sql.Where("S.CAT_ID=0");
            sql.OrderBy("S.CAT_ID");

            return repo.Query<Models.Subscriptions>(sql);
        }
        public static IEnumerable<Models.Subscriptions> CatSubs()
        {
            var sql = new Sql();
            sql.Select("S.*, C.CAT_NAME AS Category, C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_STATUS As UserStatus");
            sql.From(repo.ForumTablePrefix + "SUBSCRIPTIONS S");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID");
            sql.Where("S.FORUM_ID=0");
            sql.OrderBy("S.CAT_ID");

            return repo.Query<Models.Subscriptions>(sql);
        }
        public static IEnumerable<Models.Subscriptions> ForumSubs()
        {
            var sql = new Sql();
            sql.Select("S.*, C.CAT_NAME AS Category, C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_STATUS As UserStatus");
            sql.From(repo.ForumTablePrefix + "SUBSCRIPTIONS S");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID");
            sql.Where("S.TOPIC_ID=0");
            sql.OrderBy("S.CAT_ID", "S.FORUM_ID");

            return repo.Query<Models.Subscriptions>(sql);
        }
        public static Page<Models.Subscriptions> TopicSubs(int page)
        {
            var sql = new Sql();
            sql.Select("S.*, C.CAT_NAME AS Category, C.CAT_SUBSCRIPTION AS CategoryLevel, F.F_SUBJECT AS Forum,F_SUBSCRIPTION AS ForumLevel, T.T_SUBJECT AS Topic, M.M_NAME AS Username, M.M_STATUS As UserStatus");
            sql.From(repo.ForumTablePrefix + "SUBSCRIPTIONS S");
            sql.LeftJoin(repo.ForumTablePrefix + "CATEGORY C").On("S.CAT_ID = C.CAT_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "FORUM F").On("S.FORUM_ID = F.FORUM_ID");
            sql.LeftJoin(repo.ForumTablePrefix + "TOPICS T").On("S.TOPIC_ID = T.TOPIC_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M").On("S.MEMBER_ID = M.MEMBER_ID");
            sql.Where("S.TOPIC_ID<>0");
            sql.OrderBy("S.CAT_ID", "S.FORUM_ID", "S.TOPIC_ID");

            return repo.Page<Models.Subscriptions>(page,100,sql);
        }

        /// <summary>
        /// Add a subscription record for a user
        /// </summary>
        /// <param name="catid"></param>
        /// <param name="forumid">0 if Category subscription</param>
        /// <param name="topicid">0 if Forum or Category subscription</param>
        /// <param name="userid">UserId</param>
        public static Models.Subscriptions Subscribe(int catid, int forumid, int topicid, int userid)
        {
            //lets make sure values are valid
            if (topicid > 0)
            {
                var topic = Models.Topic.FetchTopic(topicid);
                if (topic == null)
                {
                    throw new Exception("No Topic found with that ID");
                }
                var forum = Models.Forum.FetchForum(topic.ForumId);
                if (forum.Id != forumid)
                {
                    throw new Exception("ID's do not match");
                }
                var cat = Models.Category.Fetch(topic.CatId);
                if (cat.Id != catid)
                {
                    throw new Exception("ID's do not match");
                }
                SessionData.Clear("TopicSubs");
            }else if (forumid > 0)
            {
                var forum = Models.Forum.FetchForum(forumid);
                if (forum == null)
                {
                    throw new Exception(LangResources.Utility.ResourceManager.GetLocalisedString("ForumNotfound", "ErrorMessage"));
                }
                var cat = Models.Category.Fetch(forum.CatId);
                if (cat.Id != catid)
                {
                    throw new Exception("ID's do not match");
                }
                SessionData.Clear("ForumSubs");
            }
            else if (catid > 0)
            {
                var cat = Models.Category.Fetch(catid);
                if (cat == null)
                {
                    throw new Exception("No Category found with that ID");
                }
                SessionData.Clear("CatSubs");
            }
            Models.Subscriptions sub = new Models.Subscriptions
            {
                CatId = catid,
                ForumId = forumid,
                TopicId = topicid,
                MemberId = userid
            };

            sub.Insert();
            sub.Save();
            return sub;

        }
        /// <summary>
        /// Remove a subscription record
        /// </summary>
        /// <param name="catid"></param>
        /// <param name="forumid"></param>
        /// <param name="topicid"></param>
        /// <param name="userid"></param>
        public static void UnSubscribe(int catid, int forumid, int topicid, int userid)
        {
            if (topicid > 0)
            {
                SessionData.Clear("TopicSubs");
            }else if (forumid > 0)
            {
                SessionData.Clear("ForumSubs");
            }
            else if (catid > 0)
            {
                SessionData.Clear("CatSubs");
            }

            repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "SUBSCRIPTIONS WHERE MEMBER_ID=@0 AND CAT_ID=@1 AND FORUM_ID=@2 AND TOPIC_ID = @3 ", userid, catid, forumid, topicid);
        }

        public static void Remove(int subid)
        {
            repo.Delete<Models.Subscriptions>("WHERE SUBSCRIPTION_ID=@0", subid);
        }
    }


    public class MemberSub : Subscriptions
    {
        public string category { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Extensions;

namespace SnitzDataModel.Models
{
    public partial class Member
    {
        public object this[string propertyName]
        {
            get
            {

                PropertyInfo myPropInfo = GetType().GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {

                PropertyInfo myPropInfo = GetType().GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);

            }

        }
        public void DeleteTopics(bool delreplies)
        {
            try
            {
                repo.BeginTransaction();
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "TOPICS WHERE T_AUTHOR=@0", this.Id);
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "A_TOPICS WHERE T_AUTHOR=@0", this.Id);
                //clean up redundant replies
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "REPLY WHERE TOPIC_ID NOT IN (SELECT TOPIC_ID FROM " + repo.ForumTablePrefix + "TOPICS)");
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "A_REPLY WHERE TOPIC_ID NOT IN (SELECT TOPIC_ID FROM " + repo.ForumTablePrefix + "A_TOPICS)");
                //clean up polls
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "POLLS WHERE TOPIC_ID NOT IN (SELECT TOPIC_ID FROM " + repo.ForumTablePrefix + "TOPICS)");
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "POLL_VOTES WHERE TOPIC_ID NOT IN (SELECT TOPIC_ID FROM " + repo.ForumTablePrefix + "TOPICS)");
                repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "POLL_ANSWERS WHERE POLL_ID NOT IN (SELECT POLL_ID FROM " + repo.ForumTablePrefix + "POLLS)");
                if (delreplies)
                {
                    repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "REPLY WHERE R_AUTHOR=@0", this.Id);
                    repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "A_REPLY WHERE R_AUTHOR=@0", this.Id);
                }
                repo.CompleteTransaction();
                repo.UpdatePostCount();

            }
            catch (Exception)
            {
                repo.AbortTransaction();
                throw;
            }
        }

        public static IEnumerable<Topic> MyTopics(int memberid, int topiccount, DateTime? refDate, out long totalCount)
        {
            totalCount = topiccount;
            Sql query = Models.Forum.TopicQuery
                .Where("(t.T_AUTHOR=@0) ", memberid);
            if (refDate.HasValue)
            {query.Where("t.T_LAST_POST < @0 ", refDate.Value.ToSnitzDate());}
            query.OrderBy("t.T_LAST_POST DESC");

            var result = repo.Page<Models.Topic>(1, topiccount, query);
            totalCount = result.TotalItems;
            return result.Items;            
        }
        /// <summary>
        /// Fetches a record from the Member table
        /// </summary>
        /// <param name="memberid"></param>
        /// <returns>Member Object</returns>
        public static Models.Member GetById(int memberid)
        {
            return repo.GetById<Models.Member>(memberid);
        }
        /// <summary>
        /// Fetches a record from the Member table
        /// </summary>
        /// <param name="membername"></param>
        /// <returns>Member Object</returns>
        public static Models.Member GetByName(string membername)
        {
            return repo.SingleOrDefault<Models.Member>("SELECT * FROM " + repo.MemberTablePrefix + "MEMBERS WHERE M_NAME=@0", membername);
        }

        /// <summary>
        /// Fetches a record from the Member table
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Member Object</returns>
        public static Models.Member GetByEmail(string email)
        {
            return repo.SingleOrDefault<Models.Member>("SELECT * FROM " + repo.MemberTablePrefix + "MEMBERS WHERE M_EMAIL=@0", email);
        }

        public static void UpdatePostCount(int memberid, bool incposts = false)
        {
            var sql = new Sql();
            sql.Append("UPDATE " + repo.MemberTablePrefix + "MEMBERS SET");
            if (incposts)
                sql.Append("M_POSTS = M_POSTS+1,");
            sql.Append("M_LASTPOSTDATE = @0,", DateTime.UtcNow.ToSnitzServerDateString(ClassicConfig.ForumServerOffset));
            sql.Append("M_LAST_IP=@0 ", Common.GetUserIP(System.Web.HttpContext.Current));
            sql.Append("WHERE MEMBER_ID=@0", memberid);
            repo.Execute(sql);

        }

        public static string RemoveAvatar(int memberid, string path)
        {
            var user = GetById(memberid);
            var avatar = user.PhotoUrl;
            try
            {
                if (!avatar.Contains("http"))
                    File.Delete(Path.Combine(path, user.PhotoUrl));
                user.PhotoUrl = "";
                user.Update(new[] { "M_PHOTO_URL" });
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "Success";
        }

        public static bool SetPassword(int memberid, string oldPassword, string newPassword)
        {
            Models.Member member = GetById(memberid);
            if (member.SnitzPassword == Common.SHA256Hash(oldPassword))
            {
                member.SnitzPassword = Common.SHA256Hash(newPassword);
                member.Update(new[] { "M_PASSWORD" });
                return true;
            }
            return false;
        }

        public static bool HasActivePosts(int memberid)
        {
            int postcount = 0;
            Sql sql = new Sql();
            sql.Append("SELECT SUM(POSTCOUNT) totalPosts ");
            sql.Append("FROM ( ");
            sql.Append("SELECT COUNT(R_AUTHOR) AS POSTCOUNT FROM " + repo.ForumTablePrefix + "REPLY WHERE R_AUTHOR=@0 ", memberid);
            sql.Append("UNION ALL ");
            sql.Append("SELECT COUNT(T_AUTHOR) AS POSTCOUNT FROM " + repo.ForumTablePrefix + "TOPICS WHERE T_AUTHOR=@0 ", memberid);
            sql.Append("UNION ALL ");
            sql.Append("SELECT COUNT(R_AUTHOR) AS POSTCOUNT FROM " + repo.ForumTablePrefix + "A_REPLY WHERE R_AUTHOR=@0 ", memberid);
            sql.Append("UNION ALL ");
            sql.Append("SELECT COUNT(T_AUTHOR) AS POSTCOUNT FROM " + repo.ForumTablePrefix + "A_TOPICS WHERE T_AUTHOR=@0 ", memberid);
            sql.Append(")s");

            postcount = repo.ExecuteScalar<int>(sql);
            return postcount > 0;
        }

        public static List<String> UserNames()
        {
            return repo.Fetch<String>("SELECT M_NAME FROM " + repo.MemberTablePrefix + "MEMBERS");
        }

        public static void TogglePrivacy(int id)
        {
            var sql = "UPDATE " + repo.MemberTablePrefix + "MEMBERS SET M_PRIVATEPROFILE = ABS(1-M_PRIVATEPROFILE) WHERE MEMBER_ID=" + id;
            repo.Execute(sql);
        }

        public static IEnumerable<Pair<int, string>> CachedUsers(string term)
        {
            return //cacheService.GetOrSet("lookup.paypal", () =>
                repo.Fetch<Pair<int, string>>("SELECT MEMBER_ID AS [Key], M_NAME + ' | ' + M_EMAIL AS [Value] FROM " + repo.MemberTablePrefix + "MEMBERS WHERE M_STATUS = 1 AND (M_EMAIL LIKE '" + term + "%' OR M_NAME LIKE '" + term + "%' OR M_FIRSTNAME LIKE '" + term + "%'  OR M_LASTNAME LIKE '" + term + "%')");

        }

    }
}
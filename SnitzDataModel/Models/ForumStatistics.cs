using System;
using System.Linq;
using System.Security.Principal;
using Snitz.Base;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;

namespace SnitzDataModel.Models
{
    public class ForumStatistics
    {
        public Topic LatestTopic { get; set; }
        public Member NewestMember { get; set; }
        public int ForumCount { get; set; }
        public int MemberCount { get; set; }
        public int ActiveMemberCount { get; set; }
        public int TopicCount { get; set; }
        public int ActiveTopicCount { get; set; }
        public int ArchivedReplyCount { get; set; }
        public int ArchivedTopicCount { get; set; }
        public int TotalPostCount { get; set; }
        public DateTime LastVisit { get; set; }

        public ForumStatistics(string lastVisitDate, IPrincipal user)
        {
            var db = new SnitzDataContext();

            var forums = user.Forums();

            ForumCount = forums.Count;

            TopicCount = forums.Sum(f => f.TopicCount);
            TotalPostCount = forums.Sum(f => f.PostCount);
            LastVisit = lastVisitDate.ToSnitzDateTime();

            string sql = "SELECT  TOPIC_ID FROM " + db.ForumTablePrefix + "TOPICS WHERE T_STATUS<=1 ORDER BY T_LAST_POST DESC";

            Topic topic = db.First<Topic>(sql);


            LatestTopic = topic != null ? Topic.WithAuthor(topic.Id) : null;

            NewestMember = db.First<Member>("SELECT M_NAME FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_STATUS=1 AND M_LEVEL>0 AND M_LASTHEREDATE IS NOT NULL ORDER BY M_DATE DESC");
            
            var totals = db.First<ForumTotals>("ORDER BY COUNT_ID");

            MemberCount = db.ExecuteScalar<int>("SELECT COUNT(MEMBER_ID) FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_STATUS=1 AND M_LEVEL > 0");
            ActiveMemberCount = db.ExecuteScalar<int>("SELECT COUNT(MEMBER_ID) FROM " + db.MemberTablePrefix + "MEMBERS WHERE M_STATUS=1 AND M_POSTS > 0;");
            
            ActiveTopicCount = db.ExecuteScalar<int>("SELECT COUNT(TOPIC_ID) FROM " + db.ForumTablePrefix + "TOPICS WHERE T_STATUS=1 AND T_LAST_POST > @0; ",lastVisitDate);

            ArchivedReplyCount = totals.ArchivedPostCount.HasValue ? totals.ArchivedPostCount.Value : 0;
            ArchivedTopicCount = totals.ArchivedTopicCount.HasValue ? totals.ArchivedTopicCount.Value : 0;


        }
    }
}
using System.Linq;
using PetaPoco;
using Snitz.Base;
using SnitzCore.Extensions;

namespace SnitzDataModel.Models
{
    public partial class Reply
    {
        public Models.Member Author { get; set; }
        public Models.Topic topic;

        [ResultColumn]
        public string PostAuthorName { get; set; }
        [ResultColumn]
        public string LastPostAuthorName { get; set; }
        [ResultColumn]
        public string AuthorAvatar { get; set; }
        [ResultColumn]
        public string AuthorSignature { get; set; }
        [ResultColumn]
        public string EditedBy { get; set; }
        [ResultColumn]
        public Enumerators.PostStatus ForumStatus { get; set; }
        [ResultColumn]
        public int Archived { get; set; }

        public Reply()
        {
            this.Mail = 0;
            this.ShowSig = 0;
            this.PostStatus = Enumerators.PostStatus.OnHold; //default to OnHold, will get reset in the post routines           
        }

        public Enumerators.PostStatus UpdateStatus(Enumerators.PostStatus status)
        {
            var prevstatus = this.PostStatus;
            this.PostStatus = status;
            this.Update(new string[] {"R_STATUS"});

            return prevstatus;

        }


        public static Models.Reply FetchReply(int replyid, int getarchive = 0)
        {
            if (getarchive == 1)
            {
                return repo.FirstOrDefault<Models.Reply>("SELECT * FROM " + repo.ForumTablePrefix + "A_REPLY WHERE REPLY_ID = @0", replyid);
            }
            return repo.FirstOrDefault<Models.Reply>("WHERE REPLY_ID = @0", replyid);

        }

        /// <summary>
        /// Get a Reply and it's Author
        /// </summary>
        /// <param name="replyid"></param>
        /// <returns>Reply + Author Object</returns>
        public static Models.Reply WithAuthor(int replyid)
        {
            Sql sql = new Sql();
            sql.Select("r.*,0 AS Archived, e.M_NAME AS EditedBy, Author.*");
            sql.From(repo.ForumTablePrefix + "REPLY r");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS e").On("r.R_LAST_EDITBY = e.MEMBER_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS Author").On("Author.MEMBER_ID = r.R_AUTHOR");
            sql.Where("r.REPLY_ID=@0", replyid);

            var reply = repo.Query<Models.Reply, Models.Member>(sql).FirstOrDefault();

            return reply;

        }

        /// <summary>
        /// Set the status flag for a reply
        /// </summary>
        /// <param name="replyid"></param>
        /// <param name="status"></param>
        public static void SetStatus(int replyid, Enumerators.PostStatus status)
        {
            var reply = FetchReply(replyid);

            var old = reply.UpdateStatus(status);
            //if we are approving a moderated post then update lastpost info and member post count
            if (status == Enumerators.PostStatus.Open && old.In(Enumerators.PostStatus.UnModerated, Enumerators.PostStatus.OnHold))
            {
                Models.Member.UpdatePostCount(reply.AuthorId, true);
                var topic = Models.Topic.FetchTopic(reply.TopicId);
                if (old == Enumerators.PostStatus.UnModerated)
                {
                    topic.ReplyCount += 1;
                    if (topic.UnmoderatedReplyCount > 0)
                        topic.UnmoderatedReplyCount -= 1;
                    else
                        topic.UnmoderatedReplyCount = 0;
                }
                topic.Save();
                topic.UpdateLastPost();
                var forum = Models.Forum.FetchForum(reply.ForumId);
                forum.UpdateLastPost();
            }
        }
    }
}

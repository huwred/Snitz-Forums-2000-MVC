// /*
// ####################################################################################################################
// ##
// ## PollsRepository
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
using System.Configuration;
using System.Linq;
using PetaPoco;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Models;

namespace SnitzDataModel.Database
{
    public class PollsRepository : IDisposable
    {
        private List<Poll> _data;

        public PollsRepository()
        {

            Initialize();
        }
        private void Initialize()
        {
            string tablePrefix = ConfigurationManager.AppSettings["forumTablePrefix"];


            _data = new List<Poll>();
            Sql sql = new Sql();

            sql.Select("P.*, T.T_REPLIES AS CommentCount,T.T_STATUS AS Active,T.TOPIC_ID AS Topic ,A.*");
            sql.From(tablePrefix + "POLLS P");
            sql.LeftJoin(tablePrefix + "TOPICS T").On("T.TOPIC_ID=P.TOPIC_ID");
            sql.LeftJoin(tablePrefix + "POLL_ANSWERS A").On("A.POLL_ID=P.POLL_ID");
            sql.OrderBy(" P.POLL_ID");
            //sql.LeftJoin(tablePrefix + "POLL_VOTES ").On("V.POLL_ID=P.POLL_ID");

            using (var context = new SnitzDataContext())
            {
                _data = context.Fetch<Poll, PollAnswer, Poll>(new PollQuestionAnswerRelator().MapIt, sql);

            }

        }
        public Poll GetTopicPoll(int topicid)
        {
            return _data.FirstOrDefault(c => c.TopicId == topicid);
        }
        public List<PollVotes> GetPollVotes(int pollid)
        {
            string tablePrefix = ConfigurationManager.AppSettings["forumTablePrefix"];
            Sql sql = new Sql();

            sql.Select("*");
            sql.From(tablePrefix + "POLL_VOTES");
            sql.Where("POLL_ID=@0", pollid);

            using (var context = new SnitzDataContext())
            {
                return context.Fetch<PollVotes>(sql);
            }
        }
        public List<Poll> GetAllEntries()
        {
            return _data.OrderByDescending(c => c.Id).ToList();
        }
        public bool AddPoll(int topicId)
        {
            var poll = new Poll();

            poll.TopicId = topicId;

            using (var context = new SnitzDataContext())
            {
                return context.Insert(ConfigurationManager.AppSettings["forumTablePrefix"] + "POLLS", "POLL_ID", poll) != null;
            }
        }
        public bool DeletePoll(int id)
        {
            if (ClassicConfig.GetIntValue("INTFEATUREDPOLLID") == id)
            {
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTFEATUREDPOLLID", "0");
            }
            ClassicConfig.Update(new[]
                                 {
                                         "INTFEATUREDPOLLID"
                                     });
            using (var context = new SnitzDataContext())
            {
                context.Delete<PollVotes>("POLL_ID=@0", id);
                context.Delete<PollAnswer>("POLL_ID=@0", id);
                return context.Delete<Poll>(id) > 0;
            }

        }

        public static bool IsPoll(int topicid)
        {
            using (var context = new SnitzDataContext())
            {
                return context.Exists<Poll>("TOPIC_ID=@0", topicid);
            }
        }

        public void Dispose()
        {
            _data.Clear();
            _data = null;
        }

        public bool Vote(int userId, Poll poll, int answerid)
        {
            using (var db = new SnitzDataContext())
            {
                db.Save(poll);
                Sql sql = new Sql();
                sql.Select("*");
                sql.From("FORUM_POLL_VOTES");
                sql.Where("POLL_ID=@0", poll.Id);
                sql.Where("MEMBER_ID=@0", userId);

                //Poll Votes
                var v = db.SingleOrDefault<PollVotes>(sql);
                if (v != null)
                {
                    return false;
                }
                else
                {
                    v = new PollVotes
                    {
                        PollId = poll.Id,
                        CatId = poll.CatId,
                        ForumId = poll.ForumId,
                        TopicId = poll.TopicId,
                        MemberId = userId,
                        GuestVote = (userId == -1) ? 1 : 0
                    };

                    db.Save(v);

                    //Poll answers
                    var a = db.Single<PollAnswer>("SELECT * FROM FORUM_POLL_ANSWERS WHERE POLLANSWER_ID=@0", answerid);
                    a.Votes += 1;
                    db.Update("FORUM_POLL_ANSWERS", "POLLANSWER_ID", a);

                    return true;
                }
            }
        }

        public PollAnswer GetPollAnswer(int answerid)
        {
            string tablePrefix = ConfigurationManager.AppSettings["forumTablePrefix"];
            using (var context = new SnitzDataContext())
            {
                return context.SingleOrDefault<PollAnswer>("POLLANSWER_ID=@0", answerid);
            }
        }

        public Poll GetPoll(int id)
        {
            string tablePrefix = ConfigurationManager.AppSettings["forumTablePrefix"];
            Sql sql = new Sql();

            sql.Select("P.*, T.T_REPLIES AS CommentCount,T.T_STATUS AS Active,T.TOPIC_ID AS Topic ,A.*");
            sql.From(tablePrefix + "POLLS P");
            sql.LeftJoin(tablePrefix + "TOPICS T").On("T.TOPIC_ID=P.TOPIC_ID");
            sql.LeftJoin(tablePrefix + "POLL_ANSWERS A").On("A.POLL_ID=P.POLL_ID");

            var polls = new List<Poll>();
            using (var context = new SnitzDataContext())
            {
                polls = context.Fetch<Poll, PollAnswer, Poll>(new PollQuestionAnswerRelator().MapIt, sql);

            }
            return polls.FirstOrDefault(c => c.Id == id);
        }

        public bool HasVoted(int pollid, int userId)
        {
            if (userId < 0)
                return false;
            string tablePrefix = ConfigurationManager.AppSettings["forumTablePrefix"];
            Sql sql = new Sql();

            sql.Select("*");
            sql.From(tablePrefix + "POLL_VOTES");
            sql.Where("POLL_ID=@0", pollid);
            sql.Where("MEMBER_ID=@0", userId);


            using (var context = new SnitzDataContext())
            {
                return context.Fetch<PollVotes>(sql).Any();
            }
        }

        public void UpdatePollQuestion(int pollId, string pollQuestion, string roles)
        {
            var poll = GetPoll(pollId);
            poll.Question = pollQuestion;
            poll.AllowedRoles = roles;
            using (var context = new SnitzDataContext())
            {
                context.Save(poll);
            }
        }

        public void SaveAnswer(PollAnswer ans)
        {
            using (var context = new SnitzDataContext())
            {
                context.Save(ans);
            }
        }

        public void AddPoll(Models.Topic topic, string pollQuestion, string roles, IEnumerable<PollAnswer> answers)
        {
            var poll = new Poll();

            poll.TopicId = topic.Id;
            poll.CatId = topic.CatId;
            poll.ForumId = topic.ForumId;
            poll.Question = pollQuestion;
            poll.AllowedRoles = roles;

            using (var context = new SnitzDataContext())
            {
                context.Insert(poll);
                foreach (PollAnswer answer in answers)
                {
                    if (!String.IsNullOrWhiteSpace(answer.Answer))
                    {
                        answer.PollId = poll.Id;
                        context.Insert(answer);
                    }
                }
            }

        }

        public static void Merge(int pId, int sId)
        {
            using (var db = new SnitzDataContext())
            {
                var primary = db.Query<int>("SELECT POLL_ID FROM FORUM_POLL_VOTES WHERE MEMBER_ID=@0", pId).ToList();
                var secondary = db.Query<int>("SELECT POLL_ID FROM FORUM_POLL_VOTES WHERE MEMBER_ID=@0", sId).ToList();
                var result = secondary.Except(primary);

                foreach (int id in result)
                {
                    db.Execute("UPDATE FORUM_POLL_VOTES SET MEMBER_ID=@0 WHERE POLL_ID=@1", pId, id);
                }

                db.Execute("DELETE FORUM_POLL_VOTES WHERE MEMBER_ID=@0", sId);
            }
        }
    }

}

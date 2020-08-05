using System;
using BbCodeFormatter;
using Postal;
using Snitz.Base;
using SnitzConfig;
using SnitzDataModel.Database;
using SnitzDataModel.Models;
using Member = SnitzDataModel.Models.Member;
using Reply = SnitzDataModel.Models.Reply;
using Topic = SnitzDataModel.Models.Topic;


namespace SnitzMembership
{
    public static class ProcessSubscriptions
    {
        /// <summary>
        /// Process subscriptions for a new topic
        /// </summary>
        /// <param name="topicid"></param>
        public static void Topic(int topicid)
        {

            Topic topic = SnitzDataModel.Models.Topic.FetchTopic(topicid);
            if (topic==null || topic.Id == 0)
            {
                return;
            }
            topic.PostAuthorName = Member.GetById(topic.AuthorId).Username;

            foreach (Subscriptions a in SnitzDataContext.FetchTopicSubscribers(topic))
            {
                //create an email
                dynamic email = new TopicSubscriptionEmail();
                email.To = a.UserEmail;
                email.Sender = ClassicConfig.ForumEmail;
                email.UserName = a.Username;
                email.Subject = Config.ForumTitle;
                email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
                email.Unsubscribe = String.Format("{0}Topic/UnSubscribe/{1}?forumid={2}&catid={3}&userid={4}", Config.ForumUrl, a.TopicId, a.ForumId, a.CatId, a.MemberId);
                email.Topiclink = String.Format("{0}Topic/Posts/{1}?pagenum=-1", Config.ForumUrl, topic.Id);

                email.Author = topic.PostAuthorName;

                //string body;
                if (a.ForumId > 0 && a.ForumLevel == Enumerators.Subscription.ForumSubscription)
                {
                    email.PostedIn = "forum";
                    email.PostedInName = a.Forum;
                }
                else if (a.CategoryLevel == Enumerators.CategorySubscription.CategorySubscription)
                {
                    email.PostedIn = "category";
                    email.PostedInName = a.Category;
                }
                //email.ViewName = "TopicSubscription";
                try
                {
                    email.Send();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Process subscriptions for new reply
        /// </summary>
        /// <param name="replyid"></param>
        public static void Reply(int replyid)
        {

            Reply reply = SnitzDataModel.Models.Reply.FetchReply(replyid);
            if (reply == null || reply.Id == 0)
            {
                return;
            }

            reply.PostAuthorName = Member.GetById(reply.AuthorId).Username;
            foreach (Subscriptions a in SnitzDataContext.FetchSubscribers(reply))
            {
                try
                {
                    //send an email
                    dynamic email = new SubscriptionEmail();// new Email("Subscription");
                    email.To = a.UserEmail;
                    email.Sender = ClassicConfig.ForumEmail;
                    email.UserName = a.Username;
                    email.Subject = Config.ForumTitle ;

                    email.Unsubscribe = String.Format("{0}Topic/UnSubscribe/{1}?forumid={2}&catid={3}&userid={4}", Config.ForumUrl, a.TopicId, a.ForumId, a.CatId, a.MemberId);
                    email.Topiclink = String.Format("{0}Topic/Posts/{1}?pagenum=-1#{2}", Config.ForumUrl, reply.TopicId, reply.Id);
                    email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
                    email.Author = reply.PostAuthorName;

                    if (a.TopicId > 0)
                    {
                        email.PostedIn = "";
                        email.PostedInName = BbCodeProcessor.Format(Config.ForumTitle, false, false);
                    }
                    else if (a.ForumId > 0 && a.ForumLevel == Enumerators.Subscription.ForumSubscription)
                    {
                        email.PostedIn = "forum";
                        email.PostedInName = a.Forum;
                    }
                    else if (a.CategoryLevel == Enumerators.CategorySubscription.CategorySubscription)
                    {
                        email.PostedIn = "category";
                        email.PostedInName = a.Category;
                    }

                    email.Send();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    public class SubscriptionEmail : Email
    {
        public string To { get; set; }
        public string Sender { get; set; }
        public string UserName { get; set; }
        public string Subject { get; set; }

        public string Unsubscribe { get; set; }
        public string Topiclink { get; set; }
        public string ForumTitle { get; set; }
        public string Author { get; set; }

        public string PostedIn { get; set; }
        public string PostedInName { get; set; }


    }
    public class TopicSubscriptionEmail : Email
    {
        public string To { get; set; }
        public string Sender { get; set; }
        public string UserName { get; set; }
        public string Subject { get; set; }

        public string Unsubscribe { get; set; }
        public string Topiclink { get; set; }
        public string ForumTitle { get; set; }
        public string Author { get; set; }

        public string PostedIn { get; set; }
        public string PostedInName { get; set; }

    }
}
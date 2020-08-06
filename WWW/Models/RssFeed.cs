using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Routing;
using BbCodeFormatter;
using HtmlAgilityPack;
using SnitzConfig;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using Forum = SnitzDataModel.Models.Forum;
using Reply = SnitzDataModel.Models.Reply;
using Topic = SnitzDataModel.Models.Topic;

namespace WWW.Models
{
    public class RssFeed
    {
        private static readonly string STitle = Config.ForumTitle;
        private static readonly string SSiteUrl = Config.ForumUrl;
        private static readonly string SCopyright = ClassicConfig.Copyright;
        private static readonly string SDescription = Config.ForumDescription;
        private static readonly string AppUrl = Config.ForumUrl; 

        public static SyndicationFeed ActiveFeed(HttpRequestBase request)
        {

            List<ActiveTopic> topics;

            #region -- Load database records --
            using (var db = new SnitzDataContext())
            {
                topics = db.FetchRecentTopicList(request.RequestContext.HttpContext.User, WebSecurity.CurrentUserId,10);

            }
            #endregion

            #region -- Create Feed --
            var feed = SyndicationFeed(AppUrl, STitle, SDescription, DateTime.UtcNow);

            // Add the URL that will link to your published feed when it's done
            SyndicationLink link = new SyndicationLink(new Uri(AppUrl + "/RssFeed/Active"))
                                   {
                                       RelationshipType = "self",
                                       MediaType = "text/html",
                                       Title =
                                           SSiteUrl +
                                           " Active Topics"
                                   };
            feed.Links.Add(link);

            // Add your site link
            link = new SyndicationLink(new Uri(AppUrl)) {MediaType = "text/html", Title = STitle};
            feed.Links.Add(link);
            #endregion

            #region -- Loop over active topics to add feed items --
            List<SyndicationItem> items = new List<SyndicationItem>();

            foreach (ActiveTopic t in topics)
            {
                // create new entry for feed
                SyndicationItem item = new SyndicationItem();
                feed.Categories.Add(new SyndicationCategory(t.CatTitle));
                // set the entry id to the URL for the item
                string url = string.Format(Config.ForumUrl + "Topic/Posts/{0}/?pagenum=-1#{1}", t.Id,t.LastPostReplyId);

                item.Id = url;
                
                // Add the URL for the item as a link
                link = new SyndicationLink(new Uri(AppUrl + url));
                item.Links.Add(link);
                // Fill some properties for the item
                
                item.Title = new TextSyndicationContent(t.Subject);
                if (t.LastPostDate != null) item.LastUpdatedTime = t.LastPostDate.Value;
                if (t.Date != null) item.PublishDate = t.Date.Value;
                item.Authors.Add(new SyndicationPerson(t.PostAuthorName));
                item.Contributors.Add(new SyndicationPerson(t.LastPostAuthorName));
                // Fill the item content            
                string html = BbCodeProcessor.Format(t.Message,false,false,true); 
                TextSyndicationContent content
                    = new TextSyndicationContent(html, TextSyndicationContentKind.Html);
                item.Content = content;
                item.Summary = GetSummary(html);
                item.Categories.Add(new SyndicationCategory(t.Forum.Subject));
                // Finally add this item to the item collection
                items.Add(item);

            }

            feed.Items = items;

            #endregion

            return feed;

        }

        private static TextSyndicationContent GetSummary(string html)
        {
            string summaryHtml = string.Empty;

            // load our html document
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            int wordCount = 0;


            foreach (var element in htmlDoc.DocumentNode.ChildNodes)
            {
                // inner text will strip out all html, and give us plain text
                string elementText = element.InnerText;

                // we split by space to get all the words in this element
                string[] elementWords = elementText.Split(new char[] { ' ' });

                // and if we haven't used too many words ...
                if (wordCount <= 100)
                {
                    // add the *outer* HTML (which will have proper 
                    // html formatting for this fragment) to the summary
                    summaryHtml += element.OuterHtml;

                    wordCount += elementWords.Count() + 1;
                }
                else
                {
                    break;
                }
            }

            return new TextSyndicationContent(summaryHtml, TextSyndicationContentKind.Html);
        }

        public static SyndicationFeed TopicFeed(int id, RequestContext request)
        {
            
            var topic = Topic.WithAuthor(id);
            if (topic == null || !request.HttpContext.User.AllowedForumIDs().Contains(topic.ForumId))
            {
                return null;
            }
            #region -- Create Feed --
            var feed = SyndicationFeed(AppUrl + "/RssFeed/Topic/" + id, STitle + " : " +
                                            topic.Forum.Subject + " - " +
                                            topic.Subject, BbCodeProcessor.Format(topic.Message), topic.LastPostDate);

            // Add the URL that will link to your published feed when it's done
            SyndicationLink link = new SyndicationLink(new Uri(AppUrl + "/RssFeed/Topic/" + id))
                                   {
                                       RelationshipType =
                                           "self",
                                       MediaType =
                                           "text/html",
                                       Title =
                                           topic.Subject
                                   };
            feed.Links.Add(link);

            // Add your site link
            link = new SyndicationLink(new Uri(AppUrl)) {MediaType = "text/html", Title = STitle};
            feed.Links.Add(link);
            feed.Categories.Add(new SyndicationCategory(topic.CatTitle));
            #endregion
            List<SyndicationItem> items = new List<SyndicationItem>();
            #region Topic
            SyndicationItem topicItem = new SyndicationItem();
            string topicurl = string.Format("Topic/Posts/{0}/?pagenum=-1#{1}", topic.Id, topic.LastPostReplyId);
            topicItem.Id = topicurl;

            // Add the URL for the item as a link
            link = new SyndicationLink(new Uri(AppUrl + topicurl));
            topicItem.Links.Add(link);
            // Fill some properties for the item

            topicItem.Title = new TextSyndicationContent(topic.Subject);
            if (topic.LastPostDate != null) topicItem.LastUpdatedTime = topic.LastPostDate.Value;
            if (topic.Date != null) topicItem.PublishDate = topic.Date.Value;
            topicItem.Authors.Add(new SyndicationPerson(topic.PostAuthorName));
            topicItem.Contributors.Add(new SyndicationPerson(topic.LastPostAuthorName));
            // Fill the item content            
            string topichtml = BbCodeProcessor.Format(topic.Message, false, false, true);
            TextSyndicationContent topiccontent
                = new TextSyndicationContent(topichtml, TextSyndicationContentKind.Html);
            topicItem.Content = topiccontent;
            topicItem.Summary = GetSummary(topichtml);
            topicItem.Categories.Add(new SyndicationCategory(topic.Forum.Subject));
            // Finally add this item to the item collection
            items.Add(topicItem);
            #endregion
            #region -- Loop over replies to add feed items --
            

            foreach (Reply r in topic.FetchReplies(Config.TopicPageSize, 1, false, WebSecurity.CurrentUserId,false,"R_DATE","DESC"))
            {
                // create new entry for feed
                SyndicationItem item = new SyndicationItem();

                // set the entry id to the URL for the item
                string url = string.Format("Topic/Posts/{0}/?pagenum=-1#{1}", topic.Id,r.Id);

                item.Id = url;

                // Add the URL for the item as a link
                link = new SyndicationLink(new Uri(AppUrl + url));
                item.Links.Add(link);
                // Fill some properties for the item
                item.Title = new TextSyndicationContent("Reply by " + r.Author.Username);
                if (r.LastEditDate.HasValue)
                {
                    item.LastUpdatedTime = r.LastEditDate.Value;
                }else if (r.Date.HasValue)
                {
                    item.LastUpdatedTime = r.Date.Value;
                }
                if (r.Date != null) item.PublishDate = r.Date.Value;
                item.Authors.Add(new SyndicationPerson(r.Author.Username));
                item.Contributors.Add(new SyndicationPerson(r.Author.Email));
                // Fill the item content            
                string html = BbCodeProcessor.Format(r.Message);
                TextSyndicationContent content
                    = new TextSyndicationContent(html, TextSyndicationContentKind.Html);
                item.Content = content;
                item.Summary = GetSummary(html);
                // Finally add this item to the item collection
                items.Add(item);

            }

            feed.Items = items;

            #endregion

            return feed;
        }

        public static SyndicationFeed ForumFeed(int id, RequestContext request)
        {
            if (!request.HttpContext.User.AllowedForumIDs().Contains(id))
            {
                return null;
            }
            var forum = Forum.FetchForumWithCategory(id);

            #region -- Create Feed --
            var feed = SyndicationFeed(AppUrl + "/RssFeed/Forum/" + id, STitle + " - " + forum.Subject, forum.Description, forum.LastPostDate);

            // Add the URL that will link to your published feed when it's done
            SyndicationLink link = new SyndicationLink(new Uri(AppUrl + "/RssFeed/Forum/" + id))
                                   {
                                       RelationshipType =
                                           "self",
                                       MediaType =
                                           "text/html",
                                       Title =
                                           forum.Subject +
                                           " Active Topics"
                                   };
            feed.Links.Add(link);
            feed.Categories.Add(new SyndicationCategory(forum.Category.Title));

            // Add your site link
            link = new SyndicationLink(new Uri(AppUrl)) {MediaType = "text/html", Title = STitle};
            feed.Links.Add(link);
            #endregion

            #region -- Loop over topics to add feed items --
            List<SyndicationItem> items = new List<SyndicationItem>();

            foreach (Topic t in forum.Topics(Config.TopicPageSize,1, request.HttpContext.User, WebSecurity.CurrentUserId).Items)
            {
                // create new entry for feed
                SyndicationItem item = new SyndicationItem();

                // set the entry id to the URL for the item
                string url = string.Format("Topic/Posts/{0}/?pagenum=1", t.Id);

                item.Id = url;

                // Add the URL for the item as a link
                link = new SyndicationLink(new Uri(AppUrl + url));
                item.Links.Add(link);
                // Fill some properties for the item
                item.Title = new TextSyndicationContent(t.Subject);
                if (t.LastPostDate != null) item.LastUpdatedTime = t.LastPostDate.Value;
                if (t.Date != null) item.PublishDate = t.Date.Value;
                item.Authors.Add(new SyndicationPerson(t.PostAuthorName));
                item.Contributors.Add(new SyndicationPerson(t.LastPostAuthorName));
                // Fill the item content            
                string html = BbCodeProcessor.Format(t.Message, false, false, true);
                TextSyndicationContent content
                    = new TextSyndicationContent(html, TextSyndicationContentKind.Html);
                item.Content = content;
                item.Summary = GetSummary(html);
                // Finally add this item to the item collection
                items.Add(item);

            }

            feed.Items = items;

            #endregion

            return feed;

        }

        private static SyndicationFeed SyndicationFeed(string id, string subject, string description, DateTime? lastUpdated)
        {
            SyndicationFeed feed = new SyndicationFeed
            {
                Id = id,
                Title = new TextSyndicationContent(subject),
                Description = new TextSyndicationContent(description),
                Copyright = new TextSyndicationContent(SCopyright),
                LastUpdatedTime = DateTimeOffset.UtcNow, // new DateTimeOffset(lastUpdated.GetValueOrDefault()),
                Generator = "Snitz Forums Mvc RSS Generator v 1.0",
                ImageUrl = new Uri(AppUrl + "/Content/images/logo.png")
            };
            return feed;
        }

        public static SyndicationFeed ForumFeed(List<int> id, RequestContext request)
        {
            if (id.Count == 1)
            {
                return ForumFeed(id.First(), request);
            }

            if (!request.HttpContext.User.AllowedForumIDs().Intersect(id).Any())
            {
                return null;
            }
            

            #region -- Create Feed --
            SyndicationFeed feed = new SyndicationFeed
            {
                Id = AppUrl + "/RssFeed/Forum/" + id,
                Title = new TextSyndicationContent(STitle),
                Description = new TextSyndicationContent(SDescription),
                Copyright = new TextSyndicationContent(SCopyright),
                
                Generator = "Snitz Forums Mvc RSS Generator v 1.0",
                ImageUrl = new Uri(AppUrl + "/Content/images/logo.png")
            };

            // Add the URL that will link to your published feed when it's done
            SyndicationLink link = new SyndicationLink(new Uri(AppUrl + "/RssFeed/Forum/" + id))
            {
                RelationshipType ="self",
                MediaType ="text/html",
                Title = " Active Topics"
            };
            feed.Links.Add(link);
            

            // Add your site link
            link = new SyndicationLink(new Uri(AppUrl)) { MediaType = "text/html", Title = STitle };
            feed.Links.Add(link);
            #endregion

            #region -- Loop over topics to add feed items --
            List<SyndicationItem> items = new List<SyndicationItem>();

            //var idlink = "?";

            foreach (var i in request.HttpContext.User.AllowedForumIDs().Intersect(id))
            {
                //idlink += "id=" + i;

                var forum = Forum.FetchForumWithCategory(i);
                feed.Categories.Add(new SyndicationCategory(forum.Category.Title));
                foreach (Topic t in forum.Topics(10, 1, request.HttpContext.User, WebSecurity.CurrentUserId).Items)
                {
                    // create new entry for feed
                    SyndicationItem item = new SyndicationItem();

                    // set the entry id to the URL for the item
                    string url = string.Format("Topic/Posts/{0}/?pagenum=1", t.Id);

                    item.Id = url;

                    // Add the URL for the item as a link
                    link = new SyndicationLink(new Uri(AppUrl + url));
                    item.Links.Add(link);
                    // Fill some properties for the item
                    item.Title = new TextSyndicationContent(t.Subject);
                    if (t.LastPostDate != null) item.LastUpdatedTime = t.LastPostDate.Value;
                    if (t.Date != null) item.PublishDate = t.Date.Value;
                    item.Authors.Add(new SyndicationPerson(t.PostAuthorName));
                    item.Contributors.Add(new SyndicationPerson(t.LastPostAuthorName));
                    // Fill the item content            
                    string html = BbCodeProcessor.Format(t.Message, false, false, true);
                    TextSyndicationContent content
                        = new TextSyndicationContent(html, TextSyndicationContentKind.Html);
                    item.Summary = GetSummary(html);
                    item.Content = content;

                    // Finally add this item to the item collection
                    items.Add(item);

                }
                //idlink += "&";
            }

            feed.Items = items.OrderByDescending(t=>t.LastUpdatedTime).Take(Config.TopicPageSize);
            feed.LastUpdatedTime = feed.Items.Last().LastUpdatedTime;
            #endregion

            return feed;
        }
    }
}
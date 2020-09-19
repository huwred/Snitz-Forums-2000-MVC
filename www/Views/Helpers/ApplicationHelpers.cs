using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using BbCodeFormatter;
using SnitzConfig;
using SnitzDataModel.Models;
using LangResources.Utility;
using SnitzMembership;
using WWW.ViewModels;
using Snitz.Base;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using Category = SnitzDataModel.Models.Category;
using Forum = SnitzDataModel.Models.Forum;
using Topic = SnitzDataModel.Models.Topic;
using System.Text.RegularExpressions;
using Sparc.TagCloud;
using WWW.Controllers;

namespace WWW.Views.Helpers
{
    public static partial class HtmlHelpers
    {
        /// <summary>
        /// Replaces a datetime object as an abbr tag for freindly display
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static MvcHtmlString Timeago(this HtmlHelper helper, DateTime? date)
        {
            UrlHelper urlHelper = new UrlHelper(helper.ViewContext.RequestContext);
            CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
            string clang = ci.TwoLetterISOLanguageName;

            var tag = new TagBuilder("time");
            if (clang == "fa")
            {
                tag.Attributes.Add("dir", "rtl");
            }
            tag.AddCssClass("timeago");
            //tag.AddCssClass("numbers");
            tag.Attributes.Add("data-toggle", "tooltip");
            if (date != null)
            {
                tag.Attributes.Add("datetime", date.Value.ToString("o") + "Z");
                tag.SetInnerText(String.Format(ResourceManager.GetLocalisedString("lblPostedOn","labels"),date.ToClientTime().ToFormattedString()));
            }

            return MvcHtmlString.Create(tag.ToString());
        }

        /// <summary>
        /// Renders Topic page numbers
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="replycount">Total No. of replies in topic</param>
        /// <param name="topicid">Unique Id of the Topic</param>
        /// <returns></returns>
        public static MvcHtmlString TopicPaging(this HtmlHelper helper, int replycount, int topicid)
        {
            int pagesize = Config.TopicPageSize;
            SnitzCookie.GetCookieValue("topic-pagesize");
            if (HttpContext.Current.Request.Cookies.AllKeys.Contains("topic-pagesize") && ClassicConfig.GetValue("STRTOPICPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
            {
                var pagesizeCookie = HttpContext.Current.Request.Cookies["topic-pagesize"];
                if (pagesizeCookie != null)
                    pagesize = Convert.ToInt32(pagesizeCookie.Value);
            }
            int pagesperrow = Convert.ToInt32(ClassicConfig.GetValue("STRPAGENUMBERSIZE"));
            int pageNum = replycount/ pagesize;
            int rem = replycount% pagesize;
            if (rem > 0) pageNum += 1;
            string pages = "";
            int pagecount = 0;
            UrlHelper urlHelper = new UrlHelper(helper.ViewContext.RequestContext);

            for (int i = 2; i < pageNum + 1; i++)
            {
                var tag = new TagBuilder("a");
                tag.AddCssClass("quick-page");
                tag.Attributes.Add("href", urlHelper.Action("Posts", "Topic", new {id = topicid, pagenum = i}));
                tag.Attributes.Add("title",
                    String.Format(ResourceManager.GetLocalisedString("tipGotoPage", "Tooltip"), i));
                tag.Attributes.Add("data-toggle", "tooltip");
                tag.SetInnerText(i.ToString());
                pages += tag.ToString();
                pagecount++;
                if (pagecount%pagesperrow == 0)
                {
                    pages += "<br />";
                }
            }


            return MvcHtmlString.Create(pages);
        }

        /// <summary>
        /// Helper to display Rank images for config page
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="selectedImage"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static MvcHtmlString ShowRankImages(this HtmlHelper helper, string selectedImage, int key)
        {
            var physicalPath = HttpContext.Current.Server.MapPath("~/Content/rankimages");
            var files = Directory.GetFiles(physicalPath, "*.gif");
            string images = "";
            foreach (string file in files)
            {
                var imagefile = Path.GetFileName(file);
                var test = VirtualPathUtility.ToAbsolute(string.Format(@"~/Content/rankimages/{0}", imagefile));
                var tag = new TagBuilder("img");
                tag.Attributes.Add("src", test);
                tag.AddCssClass("rank");
                tag.Attributes.Add("title", Path.GetFileName(file));
                tag.Attributes.Add("name", key.ToString());
                if (!String.IsNullOrWhiteSpace(selectedImage) && file.EndsWith(selectedImage))
                    tag.AddCssClass("selected");
                images += tag.ToString();
            }
            return MvcHtmlString.Create(images);
        }

        /// <summary>
        /// Helper for displaying post counts
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="topics">Topic Count</param>
        /// <param name="posts">Post Count</param>
        /// <returns></returns>
        public static MvcHtmlString DisplayCounts(this HtmlHelper helper, int topics, int posts)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("<span class='numbers'>{0}</span> Topic{1}", topics, topics == 1 ? "" : "s");
            str.Append("<br/>");
            str.AppendFormat("<span class='numbers'>{0}</span> Post{1}", posts, posts == 1 ? "" : "s");
            return new MvcHtmlString(str.ToString());
        }

        public static MvcHtmlString DisplayCount(this HtmlHelper helper, int count, string label)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("<span class='numbers'>{0}</span> {1}{2}", count, label, count == 1 ? "" : "s");

            return new MvcHtmlString(str.ToString());
        }

        /// <summary>
        /// Renders the Subscribe/Unsubscribe Icon for Topics
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="catId">Unique Id of Category</param>
        /// <param name="forumId">Unique Id of Forum</param>
        /// <param name="id">Unique Id of post to Subscribe</param>
        /// <param name="user">IPrincipal for current user</param>
        /// <returns>Topic Subscribe Link + Icon</returns>
        public static MvcHtmlString SubscriptionLink(this HtmlHelper helper, int catId, int forumId, int id, IPrincipal user)
        {
            bool isSubscribed = false;
            if (user.CategorySubscriptions().Count > 0)
            {
                if (user.CategorySubscriptions().Contains(catId))
                    return CategorySubscriptionLink(helper,catId,user);
            }
            if (user.ForumSubscriptions().Count > 0)
            {
                if (user.ForumSubscriptions().Contains(forumId))
                    return ForumSubscriptionLink(helper,catId,forumId,user);
            }
            var forum = Forum.FetchForum(forumId);
            if (
                forum.Subscription.In(new[]
                    {Enumerators.Subscription.TopicSubscription, Enumerators.Subscription.ForumSubscription}))
            {

                if (user.TopicSubscriptions().Count > 0)
                {
                    isSubscribed = user.TopicSubscriptions().Contains(id);
                }
                if (isSubscribed)
                {
                    return
                        helper.ActionLinkConfirm(
                            ResourceManager.GetLocalisedString("cnfUnsubscribeTopic", "labels"),
                            "UnSubscribe", "Topic", new {id, forumid = forumId, catid = catId},
                            "fa fa-share-square fa-1_5x");
                }
                return
                    helper.ActionLinkConfirm(
                        ResourceManager.GetLocalisedString("cnfSubscribeTopic", "labels"),
                        "Subscribe", "Topic",
                        new {id, forumid = forumId, catid = catId}, "fa fa-share-square-o fa-1_5x");
            }
            return new MvcHtmlString("");
        }

        /// <summary>
        /// Renders the Subscribe/Unsubscribe Icon for Forums
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="catId">Unique Id of Category</param>
        /// <param name="forumId">Unique Id of Forum</param>
        /// <param name="user">IPrincipal for current user</param>
        /// <param name="iconsize">Fontawesome css class to size the icon</param>
        /// <returns></returns>
        public static MvcHtmlString ForumSubscriptionLink(this HtmlHelper helper, int catId, int forumId, IPrincipal user, string iconsize="fa-1_5x")
        {
            var forum = Forum.FetchForum(forumId);

            if (forum.Subscription != Enumerators.Subscription.None)
            {
                bool isSubscribed = false;
                if (user.CategorySubscriptions().Count > 0)
                {
                    if (user.CategorySubscriptions().Contains(catId))
                        return new MvcHtmlString("");
                }
                if (user.ForumSubscriptions().Count > 0)
                {
                    isSubscribed = user.ForumSubscriptions().Contains(forumId);
                }

                if (forum.Subscription == Enumerators.Subscription.ForumSubscription)
                {
                    if (isSubscribed)
                    {
                        return
                            helper.ActionLinkConfirm(
                                ResourceManager.GetLocalisedString("cnfUnsubscribeForum", "labels"),
                                "UnSubscribe", "Forum",
                                new {id = 0, forumid = forumId, catid = catId}, "fa fa-share-square " + iconsize);
                    }
                    return
                        helper.ActionLinkConfirm(
                            ResourceManager.GetLocalisedString("cnfSubscribeForum", "labels"),
                            "Subscribe", "Forum",
                            new {id = 0, forumid = forumId, catid = catId}, "fa fa-share-square-o " + iconsize);
                }
            }
            return new MvcHtmlString("");
        }

        /// <summary>
        /// Renders the Subscribe/Unsubscribe Icon for Categories
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="catId">Unique Id of Category</param>
        /// <param name="user">IPrincipal for current user</param>
        /// <returns></returns>
        public static MvcHtmlString CategorySubscriptionLink(this HtmlHelper helper, int catId, IPrincipal user)
        {
            bool isSubscribed = false;
            var category = Category.Fetch(catId);

            if (category.Subscription == Enumerators.CategorySubscription.CategorySubscription)
            {
                if (user.CategorySubscriptions().Count > 0)
                {
                    isSubscribed = user.CategorySubscriptions().Contains(catId);
                }
                if (isSubscribed)
                {
                    return
                        helper.ActionLinkConfirm(
                            ResourceManager.GetLocalisedString("cnfUnsubscribeCategory", "labels"),
                            "UnSubscribe", "Category",
                            new {id = catId}, "fa fa-share-square fa-1_5x");
                }
                return
                    helper.ActionLinkConfirm(
                        ResourceManager.GetLocalisedString("cnfSubscribeCategory", "labels"),
                        "Subscribe", "Category",
                        new {id = catId}, "fa fa-share-square-o fa-1_5x");
            }
            return new MvcHtmlString("");
        }

        /// <summary>
        /// Helper to display post and view counts
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="postcount"></param>
        /// <param name="viewcount"></param>
        /// <param name="viewsonly"></param>
        /// <returns></returns>
        public static MvcHtmlString DisplayViewCounts(this HtmlHelper helper, int postcount, int viewcount, bool viewsonly = false)
        {
            string posts = postcount == 1
                ? ResourceManager.GetLocalisedString("lblReply", "labels")
                : ResourceManager.GetLocalisedString("lblReplies", "labels");
            string views = viewcount == 1
                ? ResourceManager.GetLocalisedString("lblView", "labels")
                : ResourceManager.GetLocalisedString("lblViews", "labels");

            StringBuilder str = new StringBuilder();
            if (!viewsonly)
            {
                str.AppendFormat("<span class='numbers'>{0}</span> {1}", postcount, posts);
                str.Append("<br/>");
            }
            str.AppendFormat("<span class='numbers'>{0}</span> {1}", viewcount, views);
            return new MvcHtmlString(str.ToString());
        }

        public static MvcHtmlString DisplayMyViewCounts(this HtmlHelper helper, int postcount, int viewcount, bool viewsonly = false)
        {
            string posts = postcount == 1
                ? ResourceManager.GetLocalisedString("lblReply", "labels")
                : ResourceManager.GetLocalisedString("lblReplies", "labels");
            string views = viewcount == 1
                ? ResourceManager.GetLocalisedString("lblView", "labels")
                : ResourceManager.GetLocalisedString("lblViews", "labels");

            StringBuilder str = new StringBuilder();
            if (!viewsonly)
            {
                str.AppendFormat("<li><i class=\"fa fa-comment\"></i> <span class='numbers'>{0}</span> {1}</li>", postcount, posts);
                str.Append("<li>|</li>");
            }
            str.AppendFormat("<li><i class=\"fa fa-eye\"></i> <span class='numbers'>{0}</span> {1}</li>", viewcount, views);
            return new MvcHtmlString(str.ToString());
        }

        /// <summary>
        /// Helpers for parsing BB code tags
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="text">Text to Parse</param>
        /// <param name="urls">Flag for parsing [url] tags</param>
        /// <returns></returns>
        public static MvcHtmlString FormatBbCode(this HtmlHelper helper, string text, bool urls = true)
        {
            if (text == null)
            {
                return new MvcHtmlString(null);
            }
            var postedvia = String.Format(ResourceManager.GetLocalisedString("strPostedFrom", "Api"), "$2");
            text = Regex.Replace(text, @"(\[APIPOST=)(.+(?=]))]", postedvia);

            return new MvcHtmlString(BbCodeProcessor.Format(text, urls));
        }
        public static MvcHtmlString FormatBbCodeTitle(this HtmlHelper helper, string text, bool urls = true)
        {
            return new MvcHtmlString(BbCodeProcessor.Format(text, urls).Replace("&#39;", "'"));
        }
        public static MvcHtmlString FormatBbCodeTooltip(this HtmlHelper helper, string text, bool urls = true)
        {
            return new MvcHtmlString("<div class=\"message clearfix\">" + BbCodeProcessor.Format(text, urls, true) + "</div>");
        }

        /// <summary>
        /// Helper for preparing BB code tags for editing
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MvcHtmlString CleanBbCode(this HtmlHelper helper, string text)
        {
            return new MvcHtmlString(HttpUtility.HtmlDecode(BbCodeProcessor.CleanCode(text)));
        }

        /// <summary>
        /// Generates breadcrumb for pages
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static MvcHtmlString BuildBreadcrumbNavigation(this HtmlHelper helper)
        {
            Category category = null;
            Forum forum = null;
            Topic topic = null;
            int forumid = 0;
            string archive = helper.ViewContext.HttpContext.Request.QueryString.Get("archived");

            string controller = helper.ViewContext.RouteData.Values["controller"].ToString().ToLower();
            string action = helper.ViewContext.RouteData.Values["action"].ToString().ToLower();

            int topicid;
            if (action == "postmessage")
            {
                //todo: need to check this
                if (Convert.ToInt32(helper.ViewContext.RouteData.Values["id"]) < 0)
                {
                    topicid = ((PostMessageViewModel) ((helper.ViewData).Model)).TopicId;

                    topic = Topic.FetchTopic(topicid);
                }
                if (topic != null)
                {
                    forumid = topic.ForumId;
                    forum = Forum.FetchForumWithCategory(forumid);
                    category = forum.Category; // Category.FetchCategory(topic.CatId);
                }
                else
                {
                    forumid = ((PostMessageViewModel) ((helper.ViewData).Model)).ForumId;
                    forum = Forum.FetchForumWithCategory(forumid);
                    category = forum.Category; // Category.FetchCategory(topic.CatId);
                }
            }
            switch (controller)
            {
                case "category":
                    if (action == "index")
                    {
                        int catid = Convert.ToInt32(helper.ViewContext.RouteData.Values["id"]);
                        category = Category.FetchSimple(catid);
                        forum = null;
                        topic = null;
                    }
                    break;
                case "topic":
                    if (action == "posts")
                    {
                        if (topic == null)
                        {
                            topicid = Convert.ToInt32(helper.ViewContext.RouteData.Values["id"]);
                            int getarchive = 0;
                            if (!String.IsNullOrEmpty(archive))
                            {
                                getarchive = Convert.ToInt32(archive);
                            }
                            topic = Topic.FetchTopic(topicid, getarchive);
                        }
                        if (topic != null)
                        {
                            forumid = topic.ForumId;
                            forum = Forum.FetchForumWithCategory(forumid);
                            category = forum.Category; // Category.FetchCategory(topic.CatId);
                        }
                    }
                    if (action == "search")
                    {
                        if (((SearchResult) (helper.ViewData.Model)).Forum != null)
                        {
                            forumid = ((SearchResult) (helper.ViewData.Model)).Forum.Id;
                            forum = Forum.FetchForumWithCategory(forumid);
                            category = forum.Category;
                        }
                        else
                        {
                           topic = ((SearchResult) (helper.ViewData.Model)).Topics[0];
                            if (topic != null)
                            {
                                forumid = topic.ForumId;
                                forum = Forum.FetchForumWithCategory(forumid);
                                category = forum.Category; // Category.FetchCategory(topic.CatId);
                            }
                        }
 
                    }
                    break;
                case "forum":
                    if (action == "posts")
                    {
                        forumid = Convert.ToInt32(helper.ViewContext.RouteData.Values["id"]);
                        forum = Forum.FetchForumWithCategory(forumid);
                        category = forum.Category; // Category.FetchCategory(topic.CatId);
                        topic = null;
                    }
                    break;
                case "help" :
                    break;
                case "reply":
                    topicid = ((PostMessageViewModel) ((helper.ViewData).Model)).TopicId;
                    topic = Topic.FetchTopic(topicid);
                    if (topic != null)
                    {
                        forumid = topic.ForumId;
                        forum = Forum.FetchForumWithCategory(forumid);
                        category = forum.Category; // Category.FetchCategory(topic.CatId);
                    }
                    break;
                case "account":
                    break;
                default: // default condition: I didn't wanted it to show on home and account controller
                    return new MvcHtmlString(string.Empty);
            }



            StringBuilder breadcrumb =
                new StringBuilder("<ul><li>").Append(
                    helper.ActionLink(ResourceManager.GetLocalisedString("mnuForumHome", "Menu"), "Index", "Home")
                        .ToHtmlString()).Append("</li>");
            if (category != null)
            {
                breadcrumb.Append("<li>");
                if (forum != null)
                {
                    breadcrumb.Append(helper.ActionLink(WebUtility.HtmlDecode(category.Title),
                        "Index", "Category",
                        new {id = category.GenerateSlug(), pagenum = 1}, null));
                    breadcrumb.Append("</li>");
                }
                else
                {
                    breadcrumb.Append("<b>");
                    breadcrumb.Append(WebUtility.HtmlDecode(category.Title));
                    breadcrumb.Append("</b></li>");
                }
            }
            if (forum != null) // && topic != null)
            {
                var tmpUrl = helper.ActionLink(WebUtility.HtmlDecode(forum.Subject),
                    "Posts", "Forum",
                    new {id = forum.Id, pagenum = 1}, null);
                breadcrumb.Append("<li>");
                breadcrumb.Append(tmpUrl);
                breadcrumb.Append("</li>");
            }

            if (action != "index" && topic != null)
            {
                var title = BbCodeProcessor.Format(topic.Subject, false);
                if (helper.ViewContext.RouteData.Values["action"].ToString().ToLower() == "postmessage")
                {
                    breadcrumb.Append("<li>");

                    breadcrumb.Append(helper.ActionLink(WebUtility.HtmlDecode(title), "Posts", "Topic",
                        new {id = topic.GenerateSlug(), pagenum = -1}, null));
                    breadcrumb.Append("</li>");

                }
                else
                {
                    breadcrumb.Append("<li><b>");
                    if (!String.IsNullOrEmpty(archive))
                    {
                        if (archive == "1")
                        {
                            breadcrumb.Append(WebUtility.HtmlDecode(title) + " (Archived)");
                        }
                        else
                        {
                            breadcrumb.Append(WebUtility.HtmlDecode(title));
                        }
                    }
                    else
                    {
                        breadcrumb.Append(WebUtility.HtmlDecode(title));
                    }

                    breadcrumb.Append("</b></li>");

                }
            }
            if (action == "active")
            {
                breadcrumb.Append("<li>");
                breadcrumb.Append(ResourceManager.GetLocalisedString("mnuForumActive", "labels"));
                breadcrumb.Append("</li>");
            }
            if (controller == "account" && action=="index")
            {
                breadcrumb.Append("<li>");
                breadcrumb.Append(ResourceManager.GetLocalisedString("mnuForumMembers", "labels"));
                breadcrumb.Append("</li>");
            }
            if (action == "search")
            {
                try
                {
                    topic = ((SearchResult)(helper.ViewData.Model)).Topics[0];
                }
                catch (Exception)
                {
                    // ignored
                }


                breadcrumb.Append("<li>");
                if (topic != null)
                {
                    breadcrumb.Append(helper.ActionLink(ResourceManager.GetLocalisedString("mnuForumSearch", "labels"),"Search", "Forum") );
                    breadcrumb.Append("<li>");
                    breadcrumb.Append(ResourceManager.GetLocalisedString("SearchResults", "Title"));
                    breadcrumb.Append("</li>");
                }
                else
                {
                    breadcrumb.Append(ResourceManager.GetLocalisedString("mnuForumSearch", "labels"));
                }
                
                breadcrumb.Append("</li>");

            }
            if (action == "myview")
            {
                breadcrumb.Append("<li>").Append(
                    helper.ActionLink(ResourceManager.GetLocalisedString("mnuMyView", "Menu"), "MyView", "Forum")
                        .ToHtmlString()).Append("</li>");;

            }


            return new MvcHtmlString(breadcrumb.Append("</ul>").ToString());
        }

        /// <summary>
        /// Renders the users Avatar
        /// </summary>
        /// <param name="htmlhelper"></param>
        /// <param name="authorname">Username of member</param>
        /// <param name="avatar">Filename of Avatar</param>
        /// <param name="imgclass">CSS class to style Avatar</param>
        /// <returns></returns>
        public static MvcHtmlString Avatar(this HtmlHelper htmlhelper, string authorname, string avatar, string imgclass)
        {
            bool isOnline = OnlineUsersInstance.OnlineUsers.IsOnline(authorname);
            
            if (String.IsNullOrWhiteSpace(avatar))
                avatar = null;

            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("alt", authorname + " avatar");
            if (avatar != null && avatar.StartsWith("http"))
            {
                bool exist = false;
                try
                {
                    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(avatar);
                    using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                    {
                        exist = response.StatusCode == HttpStatusCode.OK;
                    }
                }
                catch
                {
                    // ignored
                }
                if (exist)
                {
                    builder.Attributes.Add("src", avatar);
                }
                else
                {
                    builder.Attributes.Add("src", Common.RootFolder + "/" + Config.ContentFolder + "/Avatar/notfound.gif");
                }
            }
            else if (avatar != null)
            {
                if (File.Exists(HttpContext.Current.Server.MapPath("~/" + Config.ContentFolder + "/Avatar/" + avatar)))
                    builder.Attributes.Add("data-src", Common.RootFolder + "/" + Config.ContentFolder + "/Avatar/" + (avatar));
                else
                    builder.Attributes.Add("data-src", Common.RootFolder + "/" + Config.ContentFolder + "/Avatar/notfound.gif");
            }
            else
            {
                builder.Attributes.Add("data-src", Common.RootFolder + "/" + Config.ContentFolder + "/Avatar/default.gif");
            }

            if (isOnline)
            {
                
                var lastactive = OnlineUsersInstance.OnlineUsers.GetLastActivity(authorname);
                builder.Attributes.Add("class", imgclass + " lazyload online");
                builder.Attributes.Add("title", authorname + " " + ResourceManager.GetLocalisedString("tipOnline", "Tooltip") + "<br/>" + ResourceManager.GetLocalisedString("tipLastActive", "Tooltip") + " <time class='tiemago' datetime='" + lastactive.ToString("o") + "Z'>" + lastactive.ToClientTime().ToFormattedString(true) + "</time>");
            }
            else
            {
                builder.Attributes.Add("class", imgclass + " lazyload");
                builder.Attributes.Add("title", authorname + " " + ResourceManager.GetLocalisedString("tipOffline", "Tooltip"));
            }
            builder.Attributes.Add("data-toggle","tooltip");
            builder.Attributes.Add("data-html", "true");
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }

        /// <summary>
        /// Renders the Members Rank titles and stars for display in the forum
        /// </summary>
        /// <param name="htmlhelper"></param>
        /// <param name="author"></param>
        /// <param name="ranking"></param>
        /// <returns></returns>
        public static MvcHtmlString MemberRankTitleStars(this HtmlHelper htmlhelper, Member author, Dictionary<int, Ranking> ranking)
        {
            
            StringBuilder rankhtml = new StringBuilder();

            string mTitle = author.ForumTitle;
            //if(WebSecurity.IsUserInRole(author.Username, "Disabled")  || author.Username == "n/a")
            if (author.Disabled != null || author.Username == "n/a")
            {
                mTitle = ResourceManager.GetLocalisedString("tipMemberLocked", "Tooltip");// "Member Locked";
            }
            if (author.Username == "zapped" )
            {
                mTitle = ResourceManager.GetLocalisedString("tipZapped", "Tooltip");// "Zapped Member";
            }
            RankInfoHelper rank = new RankInfoHelper(author, ref mTitle, author.PostCount, ranking);
            TagBuilder title = new TagBuilder("span");
            title.AddCssClass("rank-label");
            title.InnerHtml = mTitle;
            TagBuilder stars = new TagBuilder("span");
            stars.AddCssClass("rank-label");
            stars.InnerHtml = rank.Stars;

            if(Config.ShowRankTitle)
                rankhtml.Append(title);
            if(Config.ShowRankStars)
                rankhtml.Append(stars);
            return new MvcHtmlString(rankhtml.ToString());
        }

        /// <summary>
        /// Renders the Members Rank Title when viewing/editing profile
        /// </summary>
        /// <param name="htmlhelper"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        public static MvcHtmlString MemberRankingTitle(this HtmlHelper htmlhelper, Member author)
        {
            var ranking = SnitzDataContext.GetRankings();

            string mTitle = author.ForumTitle;
            //if(WebSecurity.IsUserInRole(author.Username, "Disabled")  || author.Username == "n/a")
            if (author.Disabled != null || author.Username == "n/a")
            {
                mTitle = ResourceManager.GetLocalisedString("tipMemberLocked", "Tooltip");// "Member Locked";
            }
            if (author.Username == "zapped")
            {
                mTitle = ResourceManager.GetLocalisedString("tipZapped", "Tooltip");// "Zapped Member";
            }
            StringBuilder rankhtml = new StringBuilder();

            var rank = new RankInfoHelper(author, ref mTitle, author.PostCount, ranking);
            TagBuilder title = new TagBuilder("label") {InnerHtml = mTitle};
            rankhtml.Append(title);
            return new MvcHtmlString(rankhtml.ToString());
        }
        public static MvcHtmlString MemberRankingTitle(this HtmlHelper htmlhelper, SnitzMembership.Models.UserProfile author)
        {
            var ranking = SnitzDataContext.GetRankings();

            string mTitle = author.ForumTitle;
            if (WebSecurity.IsUserInRole(author.UserName, "Disabled") || author.UserName == "n/a")
            {
                mTitle = ResourceManager.GetLocalisedString("tipMemberLocked", "Tooltip");// "Member Locked";
            }
            if (author.UserName == "zapped")
            {
                mTitle = ResourceManager.GetLocalisedString("tipZapped", "Tooltip");//"Zapped Member";
            }
            StringBuilder rankhtml = new StringBuilder();

            var rank = new RankInfoHelper(author, ref mTitle, author.PostCount, ranking);
            TagBuilder title = new TagBuilder("label") { InnerHtml = mTitle };
            rankhtml.Append(title);
            return new MvcHtmlString(rankhtml.ToString());
        }

        /// <summary>
        /// Renders the Members Rank stars when viewing/editing profile
        /// </summary>
        /// <param name="htmlhelper"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        public static MvcHtmlString MemberRankingStars(this HtmlHelper htmlhelper, Member author)
        {
            var ranking = SnitzDataContext.GetRankings();

            string mTitle = author.ForumTitle;
            //if(WebSecurity.IsUserInRole(author.Username, "Disabled")  || author.Username == "n/a")
            if (author.Disabled != null || author.Username == "n/a")
            {
                mTitle = ResourceManager.GetLocalisedString("tipMemberLocked", "Tooltip");// "Member Locked";
            }
            if (author.Username == "zapped")
            {
                mTitle = ResourceManager.GetLocalisedString("tipZapped", "Tooltip");// "Zapped Member";
            }
            StringBuilder rankhtml = new StringBuilder();
            RankInfoHelper rank = new RankInfoHelper(author, ref mTitle, author.PostCount, ranking);
            rankhtml.Append(rank.Stars);
            return new MvcHtmlString(rankhtml.ToString());
        }
        public static MvcHtmlString MemberRankingStars(this HtmlHelper htmlhelper, SnitzMembership.Models.UserProfile author)
        {
            var ranking = SnitzDataContext.GetRankings();

            string mTitle = author.ForumTitle;
            if (WebSecurity.IsUserInRole(author.UserName, "Disabled") || author.UserName == "n/a")
            {
                mTitle = "Member Locked";
            }
            if (author.UserName == "zapped")
            {
                mTitle = "Zapped Member";
            }
            StringBuilder rankhtml = new StringBuilder();
            RankInfoHelper rank = new RankInfoHelper(author, ref mTitle, author.PostCount, ranking);
            rankhtml.Append(rank.Stars);
            return new MvcHtmlString(rankhtml.ToString());
        }

        public static string ToDescription(this Enum value)
        {
            var attributes = (DescriptionAttribute[])value.GetType().GetField(
              value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        /// <summary>
        /// Renders a set of radio buttons from a SelectList
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="helper"></param>
        /// <param name="expression">Property Expression of Selected Item</param>
        /// <param name="listOfValues">SelectList of available values</param>
        /// <param name="position">Layout position (Position.Horizontal/Position.Vertical</param>
        /// <returns></returns>
        public static MvcHtmlString RadioButtonForSelectList<TModel, TProperty>(
            this HtmlHelper<TModel> helper,
            Expression<Func<TModel, TProperty>> expression,
            SelectList listOfValues, Enumerators.Position position = Enumerators.Position.Horizontal)
        {
            //var metaData = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            string fullName = ExpressionHelper.GetExpressionText(expression);
            var sb = new StringBuilder();

            if (listOfValues != null)
            {

                // Create a radio button for each item in the list 
                foreach (SelectListItem item in listOfValues)
                {

                    // Generate an id to be given to the radio button field 
                    var id = string.Format("rb_{0}_{1}",
                      fullName.Replace("[", "").Replace(
                      "]", "").Replace(".", "_"), item.Value);

                    // Create and populate a radio button using the existing html helpers 
                    var label = helper.Label(id, item.Text);
                    //var radio = htmlHelper.RadioButtonFor(expression, item.Value, new { id = id }).ToHtmlString();
                    var radio = helper.RadioButton(fullName, item.Value, item.Selected, new {id }).ToHtmlString();

                    // Create the html string that will be returned to the client 
                    // e.g. <input data-val="true" data-val-required=
                    //   "You must select an option" id="TestRadio_1" 
                    //    name="TestRadio" type="radio"
                    //   value="1" /><label for="TestRadio_1">Line1</label> 
                    sb.AppendFormat("<{2} class=\"radio\" >{0} {1}</{2}>",
                       radio, label, (position == Enumerators.Position.Horizontal ? "span" : "div"), fullName);
                }
            }

            return MvcHtmlString.Create(sb.ToString());
        }

        /// <summary>
        /// Renders a CheckBox list from a Dictionary of objects
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="helper"></param>
        /// <param name="expression">Property Expression of Selected Item</param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString CheckboxListForEnum<TModel, TEnum>(this HtmlHelper<TModel> helper,
                   Expression<Func<TModel, TEnum>> expression,
                    IDictionary<string, object> htmlAttributes = null)
        {
            //if (!typeof(TProperty).IsEnum)
            //    throw new ArgumentException("TProperty must be an enumerated type");
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, helper.ViewData);
            Type enumType = typeof(Enumerators.CaptchaOperator); // GetNonNullableModelType(metadata);

            var values = Enum.GetValues(enumType).Cast<Enumerators.CaptchaOperator>();

            IEnumerable<SelectListItem> items = from value in values
                                                select new SelectListItem
                                                {
                                                    Text = ResourceManager.GetLocalisedString(value.GetType().Name + "_" + value),
                                                    Value = value.ToString(),
                                                    Selected = ((List<Enumerators.CaptchaOperator>)metadata.Model).Contains(value)
                                                };

            var name = ExpressionHelper.GetExpressionText(expression);

            var sb = new StringBuilder();
            var ul = new TagBuilder("ul");
            ul.AddCssClass("checkbox");
            ul.MergeAttributes(htmlAttributes);

            foreach (var item in items)
            {
                var id = string.Format("{0}{1}", name, item.Value);

                var li = new TagBuilder("li");

                var checkBox = new TagBuilder("input");
                checkBox.Attributes.Add("id", id);
                checkBox.Attributes.Add("value", item.Value);
                checkBox.Attributes.Add("name", id);
                checkBox.Attributes.Add("type", "checkbox");
                if (item.Selected)
                    checkBox.Attributes.Add("checked", "checked");

                var label = new TagBuilder("label");
                label.Attributes.Add("for", id);

                label.SetInnerText(item.Text);

                li.InnerHtml = checkBox.ToString(TagRenderMode.SelfClosing) + "\r\n" +
                               label.ToString(TagRenderMode.Normal);

                sb.AppendLine(li.ToString(TagRenderMode.Normal));
            }

            ul.InnerHtml = sb.ToString();

            return new MvcHtmlString(ul.ToString(TagRenderMode.Normal));
        }

        /// <summary>
        /// Display Date with optional Time
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="helper"></param>
        /// <param name="expression">Property Expression of DateTime field</param>
        /// <param name="showtime">Time display On/Off</param>
        /// <returns></returns>
        public static MvcHtmlString DisplayDateFor<TModel, TResult>(this HtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression, bool showtime = false)
        {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, helper.ViewData);
            ExpressionType type = expression.Body.NodeType;
            if (type == ExpressionType.MemberAccess)
            {
                //MemberExpression memberExpression = (MemberExpression)expression.Body;
                //PropertyInfo pi = memberExpression.Member as PropertyInfo;
                var date = metadata.SimpleDisplayText;
                if (date.ToDateTime().HasValue)
                {
                    return MvcHtmlString.Create(date.ToDateTime().Value.ToClientTime().ToFormattedString(showtime));
                }

            }

            return MvcHtmlString.Create("");

        }

        /// <summary>
        /// Renders a help link for a CONFIG_VARIABLE
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="key">CONFIG_VARIABLE</param>
        /// <param name="extracss">Additional css classes</param>
        /// <returns></returns>
        public static MvcHtmlString HelpLink(this HtmlHelper helper, string key, string extracss="")
        {
            var helpString = ResourceManager.GetKey(key);
            TagBuilder col = new TagBuilder("div");
            if (helpString != null)
            {
                TagBuilder help = new TagBuilder("i");
                help.AddCssClass("fa fa-question-circle fa-1_5x " + extracss);
                help.Attributes["data-toggle"] = "tooltip";
                help.Attributes["data-html"] = "true";
                help.Attributes["style"] = "vertical-align:bottom;";
                help.Attributes["title"] = BbCodeProcessor.Format(helpString,true,true);
                col.InnerHtml += help;

            }


            return MvcHtmlString.Create(col.InnerHtml);
        }

        /// <summary>
        /// Render a Text input for a CONFIG_VARIABLE
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="labeltext">Text for the associated label</param>
        /// <param name="key">CONFIG_VARIABLE</param>
        /// <param name="placeholder">TExt to display as placeholder</param>
        /// <param name="disabled">set to true to disable the control</param>
        /// <param name="labelCss">non default label css</param>
        /// <param name="controlCss">non default control css</param>
        /// <returns></returns>
        public static MvcHtmlString ConfigString(this HtmlHelper helper, string labeltext, string key, bool disabled = false, string placeholder = "", string labelCss = "control-label col-xs-5 col-sm-3", string controlCss = "col-xs-6")
        {
            TagBuilder controlGroup = new TagBuilder("div");
            controlGroup.AddCssClass("form-group");

            TagBuilder label = new TagBuilder("label");
            label.AddCssClass("control-label");
            label.AddCssClass(labelCss);
            label.InnerHtml = labeltext;

            TagBuilder controls = new TagBuilder("div");
            controls.AddCssClass(controlCss);

            TagBuilder input = new TagBuilder("input");
            
            if (disabled)
            {
                input.Attributes.Add("disabled", "");
                input.AddCssClass("form-control disabled");
            }
            else
            {
                input.AddCssClass("form-control");
            }

            if (!String.IsNullOrEmpty(placeholder))
            {
                input.Attributes["placeholder"] = placeholder;
            }
            input.Attributes["type"] = "text";
            input.Attributes["id"] = key.ToLower();
            input.Attributes["name"] = key.ToLower();
            input.Attributes["value"] = ClassicConfig.GetValue(key.ToUpper());
            controls.InnerHtml += input.ToString(TagRenderMode.SelfClosing);
            controlGroup.InnerHtml += label;
            controlGroup.InnerHtml += controls;

            return MvcHtmlString.Create(controlGroup.ToString());
        }

        /// <summary>
        /// Renders a CONFIG VARIABLE (key) as number input
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="labeltext">Text for the associated label</param>
        /// <param name="key">CONFIG_VARIABLE</param>
        /// <param name="min">Minumum no allowed</param>
        /// <param name="max">Maximum no allowed</param>
        /// <param name="step">Increment step for number buttons</param>
        /// <param name="disabled">set to true to disable the control</param>
        /// <param name="labelCss">non default label css</param>
        /// <param name="controlCss">non default control css</param>
        /// <returns></returns>
        public static MvcHtmlString ConfigInt(this HtmlHelper helper, string labeltext, string key,int min, int max, string step, bool disabled = false, string labelCss = "control-label col-xs-5 col-sm-3", string controlCss = "col-xs-2")
        {
            TagBuilder controlGroup = new TagBuilder("div");
            controlGroup.AddCssClass("form-group");

            TagBuilder label = new TagBuilder("label");
            label.AddCssClass("control-label");
            label.AddCssClass(labelCss);
            label.InnerHtml = labeltext;

            TagBuilder controls = new TagBuilder("div");
            controls.AddCssClass(controlCss);

            TagBuilder input = new TagBuilder("input");

            if (disabled)
            {
                input.AddCssClass("form-control disabled");
                input.Attributes.Add("disabled", "");
            }
            else
            {
                input.AddCssClass("form-control");
            }
            input.AddCssClass("inline");
            input.Attributes["type"] = "number";
            input.Attributes["id"] = key.ToLower();
            input.Attributes["name"] = key.ToLower();
            input.Attributes["value"] = ClassicConfig.GetIntValue(key.ToUpper()).ToString();
            
            input.Attributes["min"] = min.ToString();
            input.Attributes["max"] = max.ToString();
            input.Attributes["step"] = step;
            input.Attributes["data-size"] = "sm";

            controls.InnerHtml += input.ToString(TagRenderMode.SelfClosing);
            controlGroup.InnerHtml += label;
            var helpString = BbCodeProcessor.Format(ResourceManager.GetKey(key),true,true);
            if (helpString != null)
            {
                //TagBuilder col = new TagBuilder("div");
                //col.AddCssClass("col-xs-1");
                TagBuilder help = new TagBuilder("i");
                help.AddCssClass("fa fa-question-circle fa-1_5x pull-right flip");
                help.Attributes["data-toggle"] = "tooltip";
                help.Attributes["style"] = "margin-top:7px";
                help.Attributes["title"] = helpString;
                controls.InnerHtml += help;
            }
            controlGroup.InnerHtml += controls;
            return MvcHtmlString.Create(controlGroup.ToString());
        }

        /// <summary>
        /// Renders a CONFIG VARIABLE (key) as yes-no toggle switch and optionally a help icon if a langresource is defined for the key
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="labeltext">Text shown on switch</param>
        /// <param name="key">CONFIG_VARIABLE</param>
        /// <param name="disabled">set to "disabled" to disable the control</param>
        /// <param name="labelCss">non default label css</param>
        /// <param name="controlCss">non default control css</param>
        /// <returns></returns>
        public static MvcHtmlString ConfigToggle(this HtmlHelper helper, string labeltext, string key, string disabled = "", string labelCss = "control-label col-xs-8 col-sm-3", string controlCss = "col-xs-4 col-sm-1", bool hiddeninput = true)
        {
            string value = ClassicConfig.GetValue(key.ToUpper());

            TagBuilder controlGroup = new TagBuilder("div");
            //controlGroup.AddCssClass("form-group");

            TagBuilder label = new TagBuilder("label");
            label.AddCssClass("control-label");
            label.AddCssClass(labelCss);
            label.InnerHtml = labeltext;

            TagBuilder controls = new TagBuilder("div");
            controls.AddCssClass(controlCss);

            TagBuilder input = new TagBuilder("input");
            input.AddCssClass("yesno-checkbox " + disabled);
            if (!String.IsNullOrEmpty(disabled))
            {
                input.Attributes.Add("disabled", "");
            }
            input.Attributes["data-size"] = "mini";
            input.Attributes["type"] = "checkbox";
            input.Attributes["id"] = key.ToLower();
            input.Attributes["name"] = key.ToLower();
            if (value == "1")
            {
                input.Attributes["checked"] = "true";
            }

            input.Attributes["value"] = "1";

            TagBuilder hidden = new TagBuilder("input");
            hidden.Attributes["type"] = "hidden";
            hidden.Attributes["id"] = "hdn" + key.ToLower();
            hidden.Attributes["name"] = key.ToLower();
            hidden.Attributes["value"] = "0";

            controls.InnerHtml += input.ToString(TagRenderMode.SelfClosing);
            if (hiddeninput)
                controls.InnerHtml += hidden;
            controlGroup.InnerHtml += label;
            controlGroup.InnerHtml += controls;
            var helpString = ResourceManager.GetKey(key);
            if (helpString != null)
            {
                TagBuilder col = new TagBuilder("div");
                //col.AddCssClass("col-xs-1");
                TagBuilder help = new TagBuilder("i");
                help.AddCssClass("fa fa-question-circle fa-1_5x pull-left");
                help.Attributes["data-toggle"] = "tooltip";
                help.Attributes["data-html"] = "true";
                help.Attributes["style"] = "margin-top:7px";
                help.Attributes["title"] = BbCodeProcessor.Format(helpString,true,true);
                col.InnerHtml += help;
                controlGroup.InnerHtml += col.InnerHtml;
            }


            return MvcHtmlString.Create(controlGroup.InnerHtml);
        }

        public static MvcHtmlString TopicKeyWords(this HtmlHelper htmlhelper, int id)
        {
            TagCloudSetting setting = new TagCloudSetting();
            setting.NumCategories = 20;
            setting.MaxCloudSize = 50;

            setting.StopWords = TagCloudController.LoadStopWords();
            List<string> phrases;
            phrases = Topic.GetTagStrings(id);
            var tagfree = new List<string>();
            foreach (var phrase in phrases)
            {
                string newphrase = BbCodeFormatter.BbCodeProcessor.CleanCode(phrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.StripCodeContents(newphrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.StripTags(newphrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.RemoveHtmlTags(newphrase);
                tagfree.Add(newphrase);

            }
            var model = new TagCloudAnalyzer(setting)
                .ComputeTagCloud(tagfree)
                .Shuffle();
            var tagString = string.Join(",",model.Select(t => t.Text));

            return new MvcHtmlString(tagString);
        }
        public static MvcHtmlString ForumKeyWords(this HtmlHelper htmlhelper, IPrincipal user, int id=-1)
        {
            TagCloudSetting setting = new TagCloudSetting
            {
                NumCategories = 20, MaxCloudSize = 50, StopWords = TagCloudController.LoadStopWords()
            };

            List<string> phrases;
            phrases = id == -1 ? Forum.GetTagStrings(user.AllowedForumIDs()) : Forum.GetTagStrings(id);
            var tagfree = new List<string>();
            foreach (var phrase in phrases)
            {
                string newphrase = BbCodeFormatter.BbCodeProcessor.CleanCode(phrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.StripCodeContents(newphrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.StripTags(newphrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.RemoveHtmlTags(newphrase);
                tagfree.Add(newphrase);

            }
            var model = new TagCloudAnalyzer(setting)
                .ComputeTagCloud(tagfree)
                .Shuffle();
            var tagString = string.Join(",",model.Select(t => t.Text));

            return new MvcHtmlString(tagString);
        }

    }
}
using BbCodeFormatter;
using Hangfire;
using SnitzConfig;
using SnitzCore.Filters;
using SnitzMembership;
using SnitzMembership.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using LangResources.Utility;
using log4net;
using PetaPoco;
using Snitz.Base;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership.Models;
using WWW.ViewModels;
using BadwordFilter = SnitzDataModel.Models.BadwordFilter;
using Category = SnitzDataModel.Models.Category;
using Forum = SnitzDataModel.Models.Forum;
using Member = SnitzDataModel.Models.Member;
using PrivateMessage = SnitzDataModel.Models.PrivateMessage;
using Reply = SnitzDataModel.Models.Reply;
using Subscriptions = SnitzDataModel.Models.Subscriptions;
using Topic = SnitzDataModel.Models.Topic;
using WWW.Models;

namespace WWW.Controllers
{
    public class TopicController : CommonController
    {
        public TopicController()
        {
            Dbcontext = new SnitzDataContext();

        }

        [HttpPost]
        public ActionResult Search(SearchModel model)
        {
            ViewBag.IsAdministrator = User.IsAdministrator();
            ViewBag.IsForumModerator = false;

            var terms = model.Term.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (ClassicConfig.GetValue("STRBADWORDFILTER") == "1")
            {
                foreach (var term in terms)
                {
                    if (BadwordFilter.IsBadWord(term))
                    {
                        ViewBag.Error = ResourceManager.GetLocalisedString("BannedWords", "ErrorMessage");
                        return View("Error");
                    }
                }
            }

            if (model.Category.HasValue)
            {
                SearchResult sr = new SearchResult
                {
                    Forum = null,
                    Params = model,
                    Replies = null,
                    Category = Category.Fetch(model.Category.Value)
                };
                
                sr.Topics = sr.Category.SearchTopics(model,User);
                return View("SearchResult", sr);
            }

            if (model.ForumId.HasValue)
            {
                SearchResult sr = new SearchResult
                {
                    Forum = Forum.FetchForum(model.ForumId.Value),
                    Params = model,
                    Replies = null,
                    Category = null
                };
                sr.Topics = sr.Forum.SearchTopics(model);
                ViewBag.IsForumModerator = User.IsForumModerator(model.ForumId.Value);
                return View("SearchResult", sr);

            }
            if (model.TopicId.HasValue)
            {
                Topic topic = Topic.WithAuthor(model.TopicId.Value);
                SearchResult sr = new SearchResult
                {
                    Forum = null,
                    Replies = topic.SearchReplies(model),
                    Params = model,
                    Topics = new List<Topic>(),
                    Category = null
                };
                sr.Topics.Add(topic);
                return View("SearchResult", sr);
            }

            ViewBag.Error = "Error performing search";
            return View("Error");
        }

        [Authorize]
        public JsonResult Delete(int id)
        {
            var topic = Topic.FetchTopic(id);
            if (topic == null)
            {
                //ViewBag.Error = ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage");
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage"));
                //return View("Error");
            }
            if (topic.ReplyCount > 0 && !(User.IsAdministrator() ||
                                          User.IsForumModerator(topic.ForumId)))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ResourceManager.GetLocalisedString("DeleteTopicNotEmpty", "ErrorMessage"));
                //ViewBag.Error = ResourceManager.GetLocalisedString("DeleteTopicNotEmpty", "ErrorMessage");
                //return View("Error");
            }
            int forumid = topic.ForumId;
            if (User.IsAdministrator() ||
                User.IsForumModerator(topic.ForumId) ||
                SessionData.MyUserId == topic.AuthorId)
            {

                Dbcontext.DeleteTopic(topic);
            }
            TempData["successMessage"] = ResourceManager.GetLocalisedString("lblTopic", "labels")  + " " + ResourceManager.GetLocalisedString("lblDeleted", "labels");

            string redirectUrl = Url.Action("Posts", "Forum", new { id = forumid, pagenum = 1 });

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Posts", "Forum", new {id = forumid, pagenum = 1});
        }

        public ActionResult Subject(int id=0)
        {
            var topic = Topic.FetchTopic(id);
            var Message = BbCodeProcessor.Format(topic.Message);
            return Json(new { Message }, JsonRequestBehavior.AllowGet);
        }

        [RefreshDetectFilter]
        public ActionResult Posts(int id, int pagenum = 1, List<Reply> replies = null, int archived = 0,
            string terms = null,bool threaded=false, string sortdir="ASC")
        {
            string templateView = "";

            int pagesize = Config.TopicPageSize;
            if (HttpContext.Request.Cookies.AllKeys.Contains("topic-pagesize") && ClassicConfig.GetValue("STRTOPICPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
            {
                var pagesizeCookie = HttpContext.Request.Cookies["topic-pagesize"];
                if (pagesizeCookie != null)
                    pagesize = Convert.ToInt32(pagesizeCookie.Value);
            }
            TopicViewModel vm = new TopicViewModel();
            ViewBag.RequireAuth = false;
            var topic = Topic.WithAuthor(id, archived == 1);

            if (topic == null) //no topic found
            {
                //just in case lets check the archive
                if (archived == 0 && ArchivedTopics.Exists(id))
                {
                    topic = Topic.WithAuthor(id, true);
                }

                if (topic == null)
                {
                    ViewBag.Error = ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage");
                    return RedirectToAction("NotFound", "Error", new HandleErrorInfo(new HttpException(404,LangResources.Utility.ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage")), "Topic", "Posts"));
                }
            }
            if (topic.Forum.Type == Enumerators.ForumType.BlogPosts)
            {
                templateView = "Blog/";
            }

            if (threaded)
            {
                templateView = "Threaded/";
            }

            if (!User.AllowedAccess(topic.Forum, null))
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("PostMessageForumPermission",
                    "ErrorMessage");
                if (!User.Identity.IsAuthenticated)
                    throw new HttpException(403,
                        ResourceManager.GetLocalisedString("PostMessageForumAuth", "ErrorMessage"));
                throw new HttpException(403,
                    ResourceManager.GetLocalisedString("PostMessageForumPermission",
                        "ErrorMessage"));
            }
            if (!User.IsAdministrator() && topic.Forum.PrivateForums.In(Enumerators.ForumAuthType.AllowedMemberPassword,
                    Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.PasswordProtected))
            {
                var auth = Session["Forum_" + topic.ForumId] == null ? "" : Session["Forum_" + topic.ForumId].ToString();
                if (auth != topic.Forum.PasswordNew)
                {
                    ViewBag.RequireAuth = true;
                }
            }
            ViewBag.SearchTerms = null;
            if (terms != null)
            {
                ViewBag.SearchTerms = terms.Split('+');
            }

            ViewBag.Subscription = topic.Forum.Subscription;
            bool moderator = User.IsForumModerator(topic.ForumId);
            bool admin = User.IsAdministrator();

            if (topic.Subject != null)
            {

                if (topic.PostStatus != Enumerators.PostStatus.Draft && !(bool)RouteData.Values["IsRefreshed"])
                {
                    if (archived == 1)
                    {
                        ArchivedTopics t = new ArchivedTopics();
                        topic.CopyProperties(t, new string[] {});
                        t.ViewCount += 1;
                        t.Update(new[] {"T_VIEW_COUNT"});
                    }
                    else
                    {
                        //Update Topic Tracking
                        if (WebSecurity.CurrentUserId > 0)
                        {
                            SnitzCookie.UpdateTopicTrack(topic.Id.ToString());
                        }
                        topic.ViewCount += 1;
                        topic.Update(new[] { "T_VIEW_COUNT" });
                    }
                }
                vm.Topic = topic;
                
                vm.Forum = topic.Forum;
                vm.Id = topic.Id;
                vm.PageSize = pagesize;
                //first use total count
                int replycount = topic.ReplyCount;
                if (topic.UnmoderatedReplyCount.HasValue) 
                {
                    replycount += topic.UnmoderatedReplyCount.Value;
                }
                int pagecount = replycount / pagesize;
                if (replycount % pagesize > 0) pagecount += 1;


                vm.Page = pagenum == -1 ? pagecount : pagenum;

                //pagenum = vm.Page;
                if (replies != null)
                {
                    vm.Replies = replies;
                    vm.PageSize = pagesize; //replies.Count;
                    vm.PageCount = 1;
                    vm.Page = 1;
                }
                else
                {
                    var orderby = "R_DATE";

                    if (threaded)
                    {
                        orderby = "R_SORTORDER";
                    }
                    replies = topic.FetchReplies(pagesize, vm.Page, (admin || moderator),WebSecurity.CurrentUserId, archived == 1, orderby,sortdir).ToList();
                    vm.Replies = replies;
                    //replycount = replies.Count;
                    pagecount = replycount / pagesize;
                    if (replycount % pagesize > 0) pagecount += 1;
                    vm.Page = pagenum == -1 ? pagecount : pagenum;

                    vm.PageCount = pagecount;

                }

            }
            else
            {
                return View("Error");
            }
            ViewBag.Ranking = SnitzDataContext.GetRankings();


            var viewdata = new PostMessageViewModel()
            {
                IsAuthor = false,
                CatId = topic.CatId,
                ForumId = topic.ForumId,
                TopicId = topic.Id,
                Sticky = topic.IsSticky == 1,
                ReplyId = 0,
                IsBlogPost = (topic.Forum.Type == Enumerators.ForumType.BlogPosts),
                IsBugPost = (topic.Forum.Type == Enumerators.ForumType.BugReports),
                Fixed = (topic.Forum.Type == Enumerators.ForumType.BugReports) && topic.Subject.Contains("[Fixed]"),
                AllowRating = topic.Forum.AllowTopicRating && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1,
                AllowTopicRating = topic.AllowRating==1 && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1

            };
            if (WebSecurity.IsAuthenticated)
            {
                var author = Member.GetById(WebSecurity.CurrentUserId);
                viewdata.UseSignature = author.SigDefault == 1;
            }
            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"].ToString();
            }
            ViewData["quickreply"] = viewdata;
            ViewBag.IsForumModerator = moderator;
            ViewBag.IsAdministrator = admin;
            ViewBag.SortDir = sortdir;
            return View(templateView + "Posts", vm);
        }

        /// <summary>
        /// Show Form to Edit/Post new Topic
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forumid"></param>
        /// <param name="catid"></param>
        /// <param name="ispoll"></param>
        /// <param name="pagenum"></param>
        /// <param name="archived"></param>
        /// <param name="isevent"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult PostMessage(int id, int forumid, int catid, bool ispoll = false, int pagenum = -1,
            int archived = 0, bool isevent = false)
        {
            if (isevent)
            {
                TempData["Event"] = true;
            }
            else
            {
                TempData["Event"] = null;
            }
            if (Config.DisablePosting)
            {
                throw new HttpException(405,
                    ResourceManager.GetLocalisedString("PostingDisabled", "ErrorMessage"));
            }


            var forum = Forum.FetchForum(forumid);
            if (forum == null)
            {
                throw new HttpException(404,
                    ResourceManager.GetLocalisedString("ForumNotfound", "ErrorMessage"));
            }

            if (!User.AllowedAccess(forum, null))
            {
                throw new HttpException(403,
                    ResourceManager.GetLocalisedString("PostMessageForumPermission",
                        "ErrorMessage"));
            }

            if (id > 0) //topic edit or reply
            {

            }
            else //new topic
            {
                if (forum.PostAuth != Enumerators.PostAuthType.Anyone &&
                     !(User.IsForumModerator(forum.Id) || User.IsAdministrator()))
                {
                    throw new HttpException(403,
                        ResourceManager.GetLocalisedString("PostMessageForumPermission",
                            "ErrorMessage"));
                }
            }

            if (!User.IsAdministrator() && forum.PrivateForums.In(Enumerators.ForumAuthType.AllowedMemberPassword,
                    Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.PasswordProtected))
            {
                var auth = Session["Forum_" + forumid] == null ? "" : Session["Forum_" + forumid].ToString();
                if (auth != forum.PasswordNew)
                {
                    ViewBag.RequireAuth = true;
                }
            }
            if (forum.CatId != catid)
            {
                throw new HttpException(404,
                    ResourceManager.GetLocalisedString("ForumCategoryIDMatch", "ErrorMessage"));
            }
            if (forum.Status == Enumerators.PostStatus.Closed &&
                !(User.IsAdministrator() || User.IsForumModerator(forumid)))
            {
                throw new HttpException(400,
                    ResourceManager.GetLocalisedString("LockedForumNoPost", "ErrorMessage"));
            }
            ViewBag.Title = ResourceManager.GetLocalisedString("btnNewTopic", "labels");
            if (Request.UrlReferrer != null)
            {
                var viewModel = new PostMessageViewModel()
                {
                    Referrer = Request.UrlReferrer.PathAndQuery,
                    IsAuthor = true,
                    IsDraft = false,
                    IsBlogPost = forum.Type == Enumerators.ForumType.BlogPosts,
                    IsBugPost = forum.Type == Enumerators.ForumType.BugReports,
                    CatId = catid,
                    ForumId = forumid,
                    TopicId = id,
                    Type = PostType.Topic,
                    IsPoll = ispoll,
                    pagenum = pagenum,
                    Fixed = false,
                    Archived = archived,
                    AllowRating = forum.AllowTopicRating && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1,
                    AllowTopicRating = false
                };
                if (User.IsForumModerator(forumid) || User.IsAdministrator())
                {
                    viewModel.SetForumList(User);
                }


                var author = Member.GetById(WebSecurity.CurrentUserId);
                viewModel.UseSignature = author.SigDefault == 1;
                if (id > 0)
                {
                    ViewBag.Title = ResourceManager.GetLocalisedString("tipEditTopic", "Tooltip");

                    var topic = Topic.FetchTopic(id, archived);
                    viewModel.Fixed = topic.Subject.Contains("[Fixed]");
                    viewModel.AllowTopicRating = topic.AllowRating == 1 && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1;
                    if (topic.AuthorId != WebSecurity.CurrentUserId)
                        viewModel.IsAuthor = false;
                    //are we allowed to edit the topic
                    if (WebSecurity.CurrentUserId != topic.AuthorId &&
                        !(User.IsAdministrator() || User.IsForumModerator(topic.ForumId)))
                    {
                        throw new HttpException(403,
                            ResourceManager.GetLocalisedString("EditTopicPermission",
                                "ErrorMessage"));
                    }
                    //has the forum/cat been changed in the url
                    if (topic.ForumId != forumid && topic.CatId != catid)
                    {
                        throw new HttpException(400, "Invalid post URL");
                    }
                    if (topic.PostStatus == Enumerators.PostStatus.Draft && viewModel.IsAuthor)
                    {

                        viewModel.IsDraft = true;
                    }
                    viewModel.Subject = topic.Subject;
                    viewModel.Message = topic.Message;
                    viewModel.UseSignature = topic.ShowSig == 1;
                    viewModel.Lock = topic.PostStatus == 0;
                    viewModel.Sticky = topic.IsSticky == 1;
                    viewModel.DoNotArchive = topic.DoNotArchive == 0;
                    if (topic.IsPoll == 1)
                    {
                        viewModel.IsPoll = true;
                        var poll = new PollsRepository().GetTopicPoll(topic.Id);
                        viewModel.PollQuestion = poll.Question;
                        viewModel.PollAnswers = poll.Answers;
                        viewModel.PollId = poll.Id;
                    }
                    if (viewModel.IsBugPost)
                    {
                        viewModel.Fixed = topic.Subject.Contains("[Fixed]");
                    }
                }
                else if (ispoll)
                {
                    viewModel.PollAnswers = new List<PollAnswer>();
                    for (int i = 0; i < Convert.ToInt32(ClassicConfig.GetValue("INTMAXVOTES")); i++)
                    {
                        viewModel.PollAnswers.Add(new PollAnswer {Order = i});
                    }
                }
                else
                {
                    viewModel.IsDraft = true;
                }
                if (forum.Type == Enumerators.ForumType.BugReports && id <= 0)
                {
                    return View("PostBugReport", viewModel);
                }
                return View(viewModel);
            }
            return View("Error", new Exception("something went wrong"));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult PostMessage(PostMessageViewModel viewModel)
        {
            if (Config.DisablePosting)
            {
                throw new HttpException(400,
                    ResourceManager.GetLocalisedString("PostingDisabled", "ErrorMessage"));
            }

            bool isAdmin = User.IsAdministrator();
            var forum = Forum.FetchForum(viewModel.ForumId);
            if (!User.AllowedAccess(forum, null))
            {
                //ViewBag.Error = LangResources.Utility.ResourceManager.GetLocalisedString("PostMessageForumPermission", "ErrorMessage");
                throw new HttpException(403,
                    ResourceManager.GetLocalisedString("PostMessageForumPermission",
                        "ErrorMessage"));
            }
            if (forum.Status == Enumerators.PostStatus.Closed && !isAdmin)
            {
                //ViewBag.Error = LangResources.Utility.ResourceManager.GetLocalisedString("LockedForumNoPost", "ErrorMessage");
                throw new HttpException(403,
                    ResourceManager.GetLocalisedString("LockedForumNoPost", "ErrorMessage"));
            }
            Member author;

            if (viewModel.TopicId <= 0 && ClassicConfig.GetValue("STRFLOODCHECK") == "1" && !isAdmin)
            {
                author = Member.GetById(WebSecurity.CurrentUserId);
                //Check flood time
                int seconds = Convert.ToInt32(ClassicConfig.GetValue("STRFLOODCHECKTIME"));
                if (author.LastPostDate != null)
                {
                    var timeleft = DateTime.UtcNow.AddSeconds(-Math.Abs(seconds)) - author.LastPostDate.Value;

                    if (author.LastPostDate > DateTime.UtcNow.AddSeconds(-Math.Abs(seconds)))
                    {
                        Response.StatusCode = (int) HttpStatusCode.BadRequest;
                        return
                            Json(
                                String.Format(
                                    ResourceManager.GetLocalisedString("FloodcheckErr",
                                        "ErrorMessage"), Math.Abs(timeleft.Seconds)));

                    }
                }
            }

            Forum prevForum = null;
            int authorid = WebSecurity.CurrentUserId;
            bool isForumModerator = User.IsForumModerator(viewModel.ForumId);
            bool topicMoved = false;

            if (ModelState.IsValid)
            {
                Topic topic;
                bool newtopic = false;
                if (viewModel.TopicId > 0)
                {
                    //edit topic
                    topic = Topic.FetchTopic(viewModel.TopicId, viewModel.Archived);
                    author = Member.GetById(topic.AuthorId);
                    if (topic.ForumId != forum.Id)
                    {
                        topicMoved = true;
                        prevForum = Forum.FetchForum(topic.ForumId);
                    }

                    bool changedpost = topic.Subject != BbCodeProcessor.Subject(viewModel.Subject).Replace("&#39;", "'") || topic.Message != BbCodeProcessor.Post(viewModel.Message);
                    topic.Subject = BbCodeProcessor.Subject(viewModel.Subject).Replace("&#39;", "'");
                    topic.Message = BbCodeProcessor.Post(viewModel.Message);
                    topic.ShowSig = Convert.ToInt16(viewModel.UseSignature ? 1 : 0);
                    
                    if (!isAdmin && ClassicConfig.ShowEditedBy && topic.PostStatus != Enumerators.PostStatus.Draft && changedpost)
                    {
                        topic.LastEditDate = DateTime.UtcNow;
                        topic.LastEditUserId = authorid;
                    }
                    if (isAdmin && topicMoved)
                    {
                        topic.ForumId = forum.Id;
                        topic.CatId = forum.CatId;

                    }
                    if (isAdmin || isForumModerator)
                    {
                        if (!topic.Subject.Contains("[Fixed]") && viewModel.Fixed)
                        {
                            topic.Subject += " [Fixed]";
                        }
                        else if (!viewModel.Fixed)
                        {
                            topic.Subject = topic.Subject.Replace("[Fixed]", "");
                        }
                    }
                    if (topic.IsPoll == 1 && viewModel.PollQuestion != null)
                    {
                        using (var db = new PollsRepository())
                        {
                            db.UpdatePollQuestion(viewModel.PollId, viewModel.PollQuestion, viewModel.PollRoles);
                            foreach (var ans in viewModel.PollAnswers)
                            {
                                db.SaveAnswer(ans);
                            }
                        }
                    }


                }
                else //new topic
                {
                    author = Member.GetById(authorid);
                    topic = new Topic
                    {
                        CatId = viewModel.CatId,
                        ForumId = viewModel.ForumId,
                        Subject = BbCodeProcessor.Subject(viewModel.Subject),
                        Message = BbCodeProcessor.Post(viewModel.Message),
                        Date = DateTime.UtcNow,
                        AuthorId = authorid,
                        IsPoll = (short) (viewModel.IsPoll ? 1 : 0),
                        ShowSig = Convert.ToInt16(viewModel.UseSignature ? 1 : 0),
                        LastPostAuthorId = authorid,
                        LastPostReplyId = 0,
                        PostStatus =
                            forum.Moderation.In(Enumerators.Moderation.AllPosts, Enumerators.Moderation.Topics) &&
                            !(isAdmin || isForumModerator)
                                ? Enumerators.PostStatus.UnModerated
                                : Enumerators.PostStatus.Open,
                        LastPostDate = DateTime.UtcNow
                    };
                    if (viewModel.AllowRating && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1)
                    {
                        topic.AllowRating = viewModel.AllowTopicRating ? 1 : 0;
                    }
                    if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
                    {
                        topic.PosterIp = Common.GetUserIP(System.Web.HttpContext.Current);

                        author.LastIP = topic.PosterIp;
                        author.Update(new List<String> {"M_LAST_IP"});
                    }
                    newtopic = true;

                }
                if (viewModel.AllowTopicRating && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1)
                {
                    topic.AllowRating = viewModel.AllowTopicRating ? 1 : 0;
                }
                if (isAdmin || isForumModerator)
                {
                    if (viewModel.Lock)
                    {
                        topic.PostStatus = Enumerators.PostStatus.Closed;
                    }
                    else
                    {
                        if (topic.PostStatus == Enumerators.PostStatus.Closed)
                            topic.PostStatus = Enumerators.PostStatus.Open;
                    }
                    topic.IsSticky = Convert.ToInt16(viewModel.Sticky ? 1 : 0);
                    topic.DoNotArchive = Convert.ToInt16(viewModel.DoNotArchive ? 0 : 1);
                }
                if (ClassicConfig.GetValue("STRSIGNATURES") == "1" && ClassicConfig.GetValue("STRDSIGNATURES") == "0")
                {
                    if (viewModel.UseSignature)
                    {
                        author = Member.GetById(authorid);
                        if (!String.IsNullOrWhiteSpace(author.Signature))
                        {
                            topic.Message += "[br][br][hr]";
                            topic.Message += author.Signature;
                        }
                    }
                }
                if (newtopic && topic.IsPoll == 1 && viewModel.PollQuestion != null)
                {
                    topic.PollActive = 1;
                }
                if (viewModel.SaveDraft)
                {
                    topic.PostStatus = Enumerators.PostStatus.Draft;
                }
                else if (topic.PostStatus == Enumerators.PostStatus.Draft)
                {
                    if (!viewModel.SaveDraft)
                    {
                        topic.PostStatus =
                            forum.Moderation.In(Enumerators.Moderation.AllPosts, Enumerators.Moderation.Topics) &&
                            !(isAdmin || isForumModerator)
                                ? Enumerators.PostStatus.UnModerated
                                : Enumerators.PostStatus.Open;

                        topic.Date = DateTime.UtcNow;
                        topic.LastPostDate = DateTime.UtcNow;
                        newtopic = true;
                    }
                }
                if (viewModel.Archived == 1)
                {
                    ArchivedTopics t = new ArchivedTopics();
                    topic.CopyProperties(t, new string[] {});
                    t.Update(new[] {"T_MESSAGE", "T_SUBJECT"});
                    var returnUrl = Url.Action("Posts", new {id = topic.Id, viewModel.pagenum, archived = 1});
                    return Json(new {success = true, responseText = topic.Id + "|" + returnUrl});
                }
                try
                {
                    if (forum.Status == Enumerators.PostStatus.Closed)
                    {
                        topic.PostStatus = Enumerators.PostStatus.Closed;
                    }
                    topic.Save();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errors = ex.Message }, JsonRequestBehavior.AllowGet);

                }


                //if user on monitor list send admin a pm
                if (User.IsUserInRole("Monitored"))
                {
                    try
                    {
                        var adminId = WebSecurity.GetUserId(ClassicConfig.GetValue("STRADMINUSER"));
                        PrivateMessage.SendPrivateMessage(authorid, adminId, "Monitored User Activity", String.Format("{0} just posted/edited a topic [url]{1}Topic/Posts/{2}[/url]", author.Username, Config.ForumUrl, topic.Id));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                if (newtopic && topic.IsPoll == 1 && viewModel.PollQuestion != null)
                {
                    using (var db = new PollsRepository())
                    {
                        db.AddPoll(topic, viewModel.PollQuestion, viewModel.PollRoles, viewModel.PollAnswers);
                    }
                }
                //update forum last post info

                if (topic.PostStatus == Enumerators.PostStatus.Open || topic.PostStatus == Enumerators.PostStatus.Closed)
                {
                    if (newtopic)
                    {
                        forum.UpdateLastPost(topic, true);
                        Member.UpdatePostCount(authorid, forum.IncrementPostCount == 1);
                        if (ClassicConfig.GetValue("STRPMSTATUS") == "1")
                        {
                            MatchCollection matches = Regex.Matches(topic.Message, @"(?:(?<=\s|^)@""(?:[^""]+))|(?:(?<=\s|^)@(?:[^\s]+))");
                            foreach (Match match in matches)
                            {
                                var user = MemberManager.GetUser(match.Value.Replace("@", ""));
                                if (user != null && user.UserId != WebSecurity.CurrentUserId)
                                {
                                    //user mentioned in post, send them a PM
                                    PrivateMessage msg = new PrivateMessage
                                    {
                                        ToUsername = user.UserName,
                                        ToMemberId = user.UserId,
                                        FromMemberId = WebSecurity.CurrentUserId,
                                        SentDate = DateTime.UtcNow,
                                        Read = 0,
                                        ShowOutBox = 0
                                    };
                                    msg.Subject = ResourceManager.GetLocalisedString("MentionedInPostSubject","PrivateMessage");// "You were mentioned in a Post",
                                    msg.Message =
                                        String.Format(
                                            ResourceManager.GetLocalisedString("MentionedMessage", "PrivateMessage"),
                                            WebSecurity.CurrentUserName, Config.ForumUrl.Trim(), topic.Id,"top");
                                    msg.Save();
                                    if (user.PrivateMessageNotify == 1 && ClassicConfig.AllowEmail && !user.HasSubscription(topic))
                                    {
                                        EmailController.SendTaggedUserNotification(ControllerContext, Member.GetById(user.UserId), author,null,topic);
                                    }
                                }
                            }
                        }
                    }
                    else if (!(isAdmin || isForumModerator) && topic.PostStatus == Enumerators.PostStatus.Open)
                    {
                        forum.UpdateLastPost(topic);
                    }
                    //topic moved
                    if (topicMoved)
                    {
                        topic.UpdateSubscriptions(forum);
                        topic.MoveReplies();
                        prevForum.UpdateLastPost();
                        forum.UpdateLastPost();
                        if (ClassicConfig.GetValue("STRMOVENOTIFY") == "1")
                        {
                            EmailController.TopicMoveEmail(ControllerContext, topic);
                        }
                    }
                    if (newtopic || topicMoved)
                    {
                        if (!ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.None, Enumerators.SubscriptionLevel.Topic))
                        {
                            if (forum.Subscription == Enumerators.Subscription.ForumSubscription)
                            {
                                BackgroundJob.Enqueue(() => ProcessSubscriptions.Topic(topic.Id));
                            }
                        }                        
                    }

                }
                if (viewModel.SubscribeTopic)
                {
                    Subscriptions.Subscribe(topic.CatId, topic.ForumId, topic.Id, authorid);
                }
                var retUrl = Url.Action("Posts", new {id = topic.Id, viewModel.pagenum });
                return Json(new { success = true, responseText = topic.Id + "|" + retUrl }, JsonRequestBehavior.AllowGet);
                //return Redirect(viewModel.Referrer);
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList() }, JsonRequestBehavior.AllowGet);
            //return View();
        }

        public ActionResult Active(int pagenum=1, Enumerators.ActiveTopicsSince activesince = Enumerators.ActiveTopicsSince.LastVisit, Enumerators.ActiveRefresh? refresh = null,bool readcookie = true, int groupId=0, bool? groupresults=null)
        {
            if (!groupresults.HasValue)
            {
                var grouped = SnitzCookie.GetCookieValue("GROUPRESULTS");
                groupresults = grouped != null && Convert.ToBoolean(grouped);
            }
            else
            {
                SnitzCookie.SetCookie("GROUPRESULTS", groupresults.ToString().ToLower(),DateTime.UtcNow.AddYears(1));
            }
            ViewBag.IsAdministrator = User.IsAdministrator();
            ViewBag.IsForumModerator = false;
            int pagesize = Config.ActiveTopicPageSize;
            if (HttpContext.Request.Cookies.AllKeys.Contains("active-pagesize") && ClassicConfig.GetValue("STRACTIVEPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
            {
                var pagesizeCookie = HttpContext.Request.Cookies["active-pagesize"];
                if (pagesizeCookie != null)
                    pagesize = Convert.ToInt32(pagesizeCookie.Value);
            }
            if (ClassicConfig.GetIntValue("STRGROUPCATEGORIES") ==1)
            {
                if (groupId == 0)
                {
                    var groupcookie = SnitzCookie.GetCookieValue("GROUP");
                    if (groupcookie != null)
                    {
                        groupId = Convert.ToInt32(groupcookie);
                    }
                }

                SnitzCookie.SetCookie("GROUP", groupId.ToString());                
            }
            ViewBag.PageSize = pagesize;


            ViewBag.GroupId = groupId;

            ViewBag.Page = pagenum;
            ViewBag.Subscription = Enumerators.Subscription.None;
            DateTime lastvisit = (DateTime)ViewData["LastVisitDateTime"];
            var lastvisit2 = SnitzCookie.GetLastVisitDate();
            var refreshcookie = SnitzCookie.GetActiveRefresh();
            if (refreshcookie != null && refresh == null)
            {
                refresh = (Enumerators.ActiveRefresh)(Convert.ToInt32(refreshcookie));
            }
            if (readcookie)
            {
                var activesinceCookie = SnitzCookie.GetTopicSince();
                if (activesinceCookie != null)
                {
                    activesince = (Enumerators.ActiveTopicsSince)(Convert.ToInt32(activesinceCookie));
                }
                
            }
            var logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);  
            //logger.Debug(User.Identity.Name + "-SnitzCookie.GetLastVisitDate=" + lastvisit2 + "\nViewData['LastVisitDateTime']=" + lastvisit);
            //if (refresh == null) refresh = 0;
            ActiveTopicsViewModel vm = new ActiveTopicsViewModel
                                       {
                                           RecentTopics =
                                               Dbcontext.FetchActiveTopics(pagesize, pagenum, activesince, lastvisit.ToSnitzDate(),User,WebSecurity.CurrentUserId,groupId,groupresults.Value),
                                           ActiveSince = activesince,
                                           Refresh = refresh ?? 0
                                       };

            ViewBag.PageCount = vm.RecentTopics.TotalPages;
            ViewBag.LastVisit = lastvisit.ToFormattedString();

            ViewBag.RefreshSeconds = (int)vm.Refresh * 1000;
            
            
            ViewBag.GroupResults = groupresults.Value;
            SnitzCookie.SetActiveRefresh(((int)vm.Refresh).ToString());
            SnitzCookie.SetTopicSince(((int)activesince).ToString());
            return View(vm);
        }

        [Authorize]
        public ActionResult SendTo(int id)
        {
            EmailModel em = new EmailModel();
            var archived = Request.QueryString["archived"] == "1";
            var topic = Topic.FetchTopic(id, archived ? 1 : 0);
            if (topic != null)
            {
                UserProfile from;
                using (SnitzMemberContext udb = new SnitzMemberContext())
                {

                    from = udb.UserProfiles.SingleOrDefault(u => u.UserId == WebSecurity.CurrentUserId);
                    if (from != null)
                    {
                        em.FromEmail = from.Email;
                        em.FromName = from.UserName;
                    }
                    else
                    {
                        ViewBag.Sent = true;
                        ViewBag.Error = "Error loading data";
                        return View("Error");
                    }
                    em.ReturnUrl = topic.Id.ToString();
                }
                
                em.Subject = String.Format(ResourceManager.GetLocalisedString("sendtoSubject", "General"), from.UserName);
                
                em.Message =
                    String.Format(
                        ResourceManager.GetLocalisedString("sendtoMessage", "General"),
                        Config.ForumTitle ?? ClassicConfig.ForumTitle, Config.ForumUrl ?? ClassicConfig.ForumUrl,
                        topic.Id, Request.QueryString["archived"], topic.Subject);
                ViewBag.TopicTitle = topic.Subject;
                
                ViewBag.Sent = false;
                return PartialView("popSendTo", em);
            }

            ViewBag.Error = ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage"); 
            return PartialView("_Error");
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SendTo(EmailModel model)
        {
            
            EmailController.SendToFreind(ControllerContext, model);


            TempData["Success"] = "Email sent successfully";
            return RedirectToAction("Posts", "Topic", new { id=model.ReturnUrl, pagenum = -1 });
            //return PartialView("popSendTo",model);
        }

        public ActionResult Print(int id)
        {
            bool moderator;
            bool admin;
            string templateView = "";
            TopicViewModel vm = new TopicViewModel();
            var archived = Request.QueryString["archived"] == "1";
            var topic = Topic.WithAuthor(id,archived);
            if (topic.Forum.Type == Enumerators.ForumType.BlogPosts)
            {
                templateView = "Blog/";
            }
            if (topic != null)
            {
                moderator = User.IsForumModerator(topic.ForumId);
                admin = User.IsAdministrator();
                if (archived)
                {
                    ArchivedTopics t = new ArchivedTopics();
                    topic.CopyProperties(t, new string[] { });
                    t.ViewCount += 1;
                    t.Update(new[] { "T_VIEW_COUNT" });
                }
                else
                {
                    topic.ViewCount += 1;
                    topic.Update(new[] { "T_VIEW_COUNT" });
                }

                vm.Topic = topic;
                vm.Id = topic.Id;
                vm.PageSize = topic.ReplyCount;
                vm.Replies = topic.FetchReplies(topic.ReplyCount, 1, (admin || moderator), WebSecurity.CurrentUserId, archived, "R_DATE ", "DESC");

            }
            else
            {
                ViewBag.Error = "No Topic Found with that ID";
                return View("Error");
            }
            ViewBag.Ranking = SnitzDataContext.GetRankings();
            ViewData["quickreply"] = new PostMessageViewModel()
            {
                IsAuthor = false,
                CatId = topic.CatId,
                ForumId = topic.ForumId,
                TopicId = topic.Id,
                ReplyId = 0,
                AllowRating = topic.Forum.AllowTopicRating && topic.AllowRating==1 && ClassicConfig.GetIntValue("INTTOPICRATING",0)==1
            };
            ViewBag.IsForumModerator = moderator;
            ViewBag.IsAdministrator = admin;
            return View(templateView + "Print", vm);
        }

        [Authorize]
        public JsonResult Subscribe(int id, int forumid, int catid, int? userid = null)
        {
            string returnUrl = "";

            if (Request.UrlReferrer != null)
                returnUrl = Request.UrlReferrer.PathAndQuery;

            if (userid == null)
                userid = WebSecurity.CurrentUserId;
            try
            {
                Subscriptions.Subscribe(catid, forumid, id, userid.Value);

                string redirectUrl = Url.Action("Posts", new { id, pagenum = 1 });
                if (returnUrl != "")
                    redirectUrl = returnUrl;

                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
                //return View("Error");
            }
        }

        [Authorize]
        public JsonResult UnSubscribe(int id, int forumid, int catid,int? userid = null)
        {
            if (userid == null)
                userid = WebSecurity.CurrentUserId;
            string redirectUrl = "";

            if (Request.UrlReferrer != null)
                redirectUrl = Request.UrlReferrer.AbsoluteUri;

            Subscriptions.UnSubscribe(catid, forumid, id, userid.Value);
            if (id > 0)
            {
                var topic = Topic.FetchTopic(id);

                TempData["successMessage"] = String.Format(ResourceManager.GetLocalisedString("TopicSubscriptionRemove", "General"), topic.Subject);
            }else if (forumid > 0)
            {
                var forum = Forum.FetchForum(forumid);
                TempData["successMessage"] = String.Format(ResourceManager.GetLocalisedString("ForumSubscriptionRemove", "General"), forum.Subject);
            }
            else
            {
                var category = Category.Fetch(catid);
                TempData["successMessage"] = String.Format(ResourceManager.GetLocalisedString("ForumSubscriptionRemove", "General"), category.Title);
            }
            if (redirectUrl == "")
                redirectUrl = Url.Action("Posts", new { id, pagenum = 1 });

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Posts", new {id, pagenum = 1 });

        }

        /// <summary>
        /// Render UserProfile for Post
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult MessageProfile(object model)
        {
            return PartialView("_MessageUserProfile");
        }

        /// <summary>
        /// Render Topic Navigation Controls
        /// </summary>
        /// <param name="id">Current Topic Id</param>
        /// <param name="lastPostDate"></param>
        /// <returns></returns>
        public PartialViewResult TopicNav(int id, DateTime lastPostDate)
        {
            var vm = new TopicNavViewModel
            {
                PreviousTopic = Topic.PreviousTopic(id, lastPostDate),
                NextTopic = Topic.NextTopic(id, lastPostDate)
            };


            return PartialView("TopicNav",vm);
        }

        /// <summary>
        /// Prompt dialog for editor buttons
        /// </summary>
        /// <param name="data"></param>
        /// <param name="selected"></param>
        /// <returns></returns>
        public PartialViewResult PromptDialog(string data, string selected)
        {
            var sizes = new[] {"xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large"};
            ViewBag.Title = data.ToUpper();
            ViewBag.Url = false;
            ViewBag.Textarea = true;
            ViewBag.Process = "";
            ViewBag.Style = "";
            switch (data)
            {
                case "bold" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_bold", "btnPrompt");
                    ViewBag.Style = "font-weight:bold";
                    break;
                case "italic" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_italic", "btnPrompt");
                    ViewBag.Style = "font-style:italic";
                    break;
                case "underline" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_underline", "btnPrompt");
                    ViewBag.Style = "text-decoration:underline";
                    break;
                case "strike" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_strike", "btnPrompt");
                    ViewBag.Style = "text-decoration:line-through";
                    break;
                case "left" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_left", "btnPrompt");
                    ViewBag.Style = "text-align:left";
                    break;
                case "center":
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_center", "btnPrompt");
                    ViewBag.Style = "text-align:center";
                    break;
                case "right":
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_right", "btnPrompt");
                    ViewBag.Style = "text-align:right";
                    break;
                case "url" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_url", "btnPrompt");
                    ViewBag.UrlPrompt = ResourceManager.GetLocalisedString("prompt_url_url", "btnPrompt");
                    ViewBag.Placeholder = "http://";
                    ViewBag.Textarea = false;
                    ViewBag.Url = true;
                    ViewBag.Process = "link";
                    break;
                case "email" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_email", "btnPrompt");
                    ViewBag.UrlPrompt = ResourceManager.GetLocalisedString("prompt_email_url", "btnPrompt");
                    ViewBag.Placeholder = "mailto:";
                    ViewBag.Textarea = false;
                    ViewBag.Url = true;
                    ViewBag.Process = "link";
                    break;
                case "image" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_image", "btnPrompt");
                    ViewBag.Textarea = false;
                    break;
                case "code" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_code", "btnPrompt");
                    ViewBag.Style = "white-space: pre-wrap";
                    break;
                case "quote" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_quote", "btnPrompt");
                    break;
                case "numbered" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_numbered", "btnPrompt");
                    ViewBag.Process = "list";
                    break;
                case "alpha" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_alpha", "btnPrompt");
                    ViewBag.Process = "list";
                    break;
                case "unordered" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_unordered", "btnPrompt");
                    ViewBag.Process = "list";
                    break;
                case "listitem" :
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_listitem", "btnPrompt");
                    ViewBag.Textarea = false;
                    break;
                case "font" :
                    ViewBag.Prompt = String.Format(ResourceManager.GetLocalisedString("prompt_font", "btnPrompt") ,selected);
                    ViewBag.Style = "font-family:" + selected;
                    break;
                case "size" :
                    ViewBag.Prompt = String.Format(ResourceManager.GetLocalisedString("prompt_size", "btnPrompt"), sizes[Convert.ToInt32(selected)]);
                    ViewBag.Style = "font-size:" + sizes[Convert.ToInt32(selected)];
                    break;
                case "colour" :
                    ViewBag.Style = String.Format(ResourceManager.GetLocalisedString("prompt_colour", "btnPrompt"), selected);
                    ViewBag.Prompt = "Text to be " + selected;
                    break;
                case "sub":
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_sub", "btnPrompt"); 
                    ViewBag.Style = "vertical-align:sub";
                    break;
                case "sup":
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_sup", "btnPrompt"); 
                    ViewBag.Style = "vertical-align:sub";
                    break;
                case "rtl":
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_rtl", "btnPrompt"); 
                    ViewBag.Style = "direction:rtl;float:right;";
                    break;
                case "ltr":
                    ViewBag.Prompt = ResourceManager.GetLocalisedString("prompt_ltr", "btnPrompt");
                    ViewBag.Style = "direction:ltr;float:left;";
                    break;
            }
            return PartialView("popPromptDialog");
        }

        public PartialViewResult BlogList(int id)
        {
            ForumViewModel vm = new ForumViewModel();
            var forum = Forum.FetchForumWithCategory(id);
            Page<Topic> result = forum.Topics(50, 1, User, WebSecurity.CurrentUserId, 120);

            vm.Id = forum.Id;
            vm.Forum = forum;
            vm.PageSize = 50;
            vm.Topics = result.Items;
            vm.StickyTopics = null;
            vm.TotalRecords = result.TotalItems;
            int pagecount = Convert.ToInt32(result.TotalPages);
            vm.PageCount = pagecount;
            vm.Page = 1;
            return PartialView("_BlogList", vm);
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult Touch(int id)
        {
            var topic = Topic.FetchTopic(id);
            var forum = Forum.FetchForum(topic.ForumId);
            topic.Touch();
            forum.UpdateLastPost(topic);
            //Send approved email
            return RedirectToAction("Posts", "Topic", new { id, pagenum = -1 });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult Approve(int id)
        {
            Topic.SetStatus(id, Enumerators.PostStatus.Open);
            //Send approved email
            return RedirectToAction("Posts", "Topic", new {id, pagenum = -1 });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult OnHold(int id)
        {
            Topic.SetStatus(id, Enumerators.PostStatus.OnHold);
            //Send on hold
            return RedirectToAction("Posts", "Topic", new {id, pagenum = -1 });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult SplitTopic(int id, int replyid)
        {
            SplitTopicViewModel vm = new SplitTopicViewModel(User);

            var topic = Topic.WithAuthor(id);
            if (topic.Subject != null)
            {
                vm.Topic = topic;
                vm.Id = topic.Id;
                vm.Replies = topic.FetchReplies(Config.TopicPageSize, -1, true, WebSecurity.CurrentUserId,false,"R_DATE ","DESC");
                ViewBag.ReplyId = replyid;
            }
            else
            {
                return View("Error");
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        [ValidateAntiForgeryToken]
        public ActionResult SplitTopic(SplitTopicViewModel vm)
        {

            string[] ids = Request.Form.GetValues("check");
            if (ids==null || !ids.Any())
            {
                ModelState.AddModelError("Reply", ResourceManager.GetLocalisedString("TopicController_select_at_least_one_reply", "ErrorMessage"));
            }
            bool first = true;
            Topic topic = null;
            Forum forum = Forum.FetchForum(vm.ForumId);
            if (ModelState.IsValid)
            {
                if (ids != null)
                    foreach (string id in ids.OrderBy(s => s))
                    {
                        //fetch the reply
                        var reply = Reply.FetchReply(Convert.ToInt32(id));

                        if (first)
                        {
                            //first reply so create the Topic
                            topic = new Topic
                                    {
                                        CatId = forum.CatId,
                                        ForumId = forum.Id,
                                        Subject = BbCodeProcessor.Subject(vm.Subject),
                                        Message = reply.Message,
                                        Date = reply.Date,
                                        AuthorId = reply.AuthorId,
                                        ShowSig = reply.ShowSig,
                                        LastPostAuthorId = reply.AuthorId,
                                        LastPostReplyId = 0,
                                        PostStatus =
                                            forum.Moderation.In(Enumerators.Moderation.AllPosts,
                                                Enumerators.Moderation.Topics)
                                                ? Enumerators.PostStatus.UnModerated
                                                : Enumerators.PostStatus.Open,
                                        LastPostDate = reply.Date
                                    };
                            if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
                            {
                                topic.PosterIp = Common.GetUserIP(System.Web.HttpContext.Current);
                                Member member = Member.GetById(reply.AuthorId);
                                member.LastIP = topic.PosterIp;
                                member.Update(new List<String> { "M_LAST_IP" });
                            }
                            topic.Save();
                            Dbcontext.DeleteReply(reply);
                            first = false;
                        }
                        else
                        {
                            reply.TopicId = topic.Id;
                            reply.ForumId = forum.Id;
                            reply.CatId = forum.CatId;
                            reply.PostStatus = topic.PostStatus;
                            reply.Save();
                        }
                    }
                //topic.UpdateLastPost();
                //forum.UpdateLastPost();
                Dbcontext.UpdatePostCount();
                EmailController.TopicSplitEmail(ControllerContext, topic);
            }
            if (topic != null) return RedirectToAction("Posts", new {id = topic.Id, pagenum = 1});
            return View("Error");
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult MergeTopic(int id)
        {
            List<Topic> topics = new List<Topic>();
            if (SessionData.Contains("TopicList"))
            {
                List<int> selectedtopics = SessionData.Get<List<int>>("TopicList");
                if (!selectedtopics.Contains(id))
                {
                    selectedtopics.Add(id);
                }
                if (selectedtopics.Count < 2)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json("You must select at least two topics before merging");
                }
                foreach (int topicid in selectedtopics)
                {
                    topics.Add(Topic.FetchTopic(topicid));
                }
                Topic mainTopic = topics.OrderBy(t => t.Date).First();
                Forum forum = Forum.FetchForum(mainTopic.ForumId);
                Forum oldforum = null;
                foreach (Topic topic in topics)
                {
                    if (topic != mainTopic)
                    {
                        //creat a reply from the topic
                        if (topic.ForumId != mainTopic.ForumId)
                        {
                            oldforum = Forum.FetchForum(topic.ForumId);
                            if (oldforum == null)
                            {
                                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return Json("Source FORUM_ID is invalid");

                            }
                        }
                        var reply = new Reply
                        {
                            CatId = mainTopic.CatId,
                            ForumId = mainTopic.ForumId,
                            TopicId = mainTopic.Id,
                            Date = topic.Date,
                            AuthorId = topic.AuthorId,
                            ShowSig = topic.ShowSig,
                            Message = topic.Message,
                            LastEditDate = topic.LastEditDate,
                            PostStatus = topic.PostStatus
                        };
                        if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
                        {
                            reply.PosterIp = topic.PosterIp;
                        }
                        reply.Save();
                        if (topic.ForumId != mainTopic.ForumId)
                        {
                            topic.ForumId = mainTopic.ForumId;
                            topic.CatId = mainTopic.CatId;
                        }
                        //move all the replies and subscriptions;
                        topic.MoveSubscriptions(mainTopic.Id,mainTopic.ForumId,mainTopic.CatId);
                        topic.MoveReplies(mainTopic);
                        //send move notify
                        if (ClassicConfig.GetValue("STRMOVENOTIFY") == "1")
                            EmailController.TopicMergeEmail(ControllerContext, topic, mainTopic);
                        topic.Delete();

                    }
                }
                //update counts
                mainTopic.UpdateLastPost();
                if (oldforum != null)
                {
                    oldforum.UpdateLastPost();
                }
                forum.UpdateLastPost();

                //clear the session
                SessionData.Clear("TopicList");
                if (ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.Topic, Enumerators.SubscriptionLevel.Forum, Enumerators.SubscriptionLevel.Category))
                {
                    switch (forum.Subscription)
                    {
                        case Enumerators.Subscription.ForumSubscription:
                        case Enumerators.Subscription.TopicSubscription:
                            BackgroundJob.Enqueue(() => ProcessSubscriptions.Topic( mainTopic.Id));
                            break;
                    }
                }
                string redirectUrl = Url.Action("Posts", new { id = mainTopic.Id, pagenum = 1 });

                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
                //return RedirectToAction("Posts",new {id=mainTopic.Id,pagenum=1});
            }
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return Json("You must select at least two topics before merging");

            
        }
        
        /// <summary>
        /// Process Topic Moderation
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult ModeratePost(ApproveTopicViewModal vm)
        {

            if (ModelState.IsValid)
            {
                var topic = Topic.WithAuthor(vm.Id);
                var author = Member.GetById(topic.AuthorId);
                var forum = Forum.FetchForum(topic.ForumId);
                var subject = "";
                var message = "";
                

                switch (vm.PostStatus)
                {
                    case "Approve" :
                        Topic.SetStatus(vm.Id, Enumerators.PostStatus.Open);
                        //Send email
                            subject = ClassicConfig.ForumTitle + ": Post Approved";
                            message = "Has been approved. You can view it at " + Environment.NewLine +
                                            Config.ForumUrl + "Topic/Posts/" + topic.Id + "?pagenum=-1" +
                                            Environment.NewLine +
                                            vm.ApprovalMessage;
                        if (!ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.None, Enumerators.SubscriptionLevel.Topic))
                        {
                            switch (forum.Subscription)
                            {
                                case Enumerators.Subscription.ForumSubscription:
                                    BackgroundJob.Enqueue(() => ProcessSubscriptions.Topic(vm.Id));
                                    break;
                            }
                        }
                        break;
                    case "Reject":
                        
                        Dbcontext.DeleteTopic(topic);
                        //Send email
                            subject = ClassicConfig.ForumTitle + ": Post rejected";
                            message = "Has been rejected. " + Environment.NewLine +
                                            vm.ApprovalMessage;
                        break;
                    case "Hold":
                        Topic.SetStatus(vm.Id, Enumerators.PostStatus.OnHold);
                        //Send email
                            subject = ClassicConfig.ForumTitle + ": Post placed on Hold";
                            message = "Has been placed on Hold. " + Environment.NewLine +
                                            vm.ApprovalMessage;
                        break;
                }
                if (vm.EmailAuthor)
                {
                    EmailController.ModerationEmail(ControllerContext, author, subject, message, forum, topic);
                }
                
                return RedirectToAction("Posts", "Forum", new { id=topic.ForumId});
            }

            return PartialView("popModerate",vm);
        }

        /// <summary>
        /// Open moderation Popup window
        /// </summary>
        /// <param name="id">Id of Unmoderated <see cref="Topic"/></param>
        /// <returns>PopUp Window</returns>
        [Authorize(Roles = "Administrator,Moderator")]
        public PartialViewResult Moderate(int id)
        {
            ApproveTopicViewModal vm = new ApproveTopicViewModal {Id = id};
            return PartialView("popModerate",vm);
        }

        /// <summary>
        /// Tracks Checked Topic list
        /// </summary>
        /// <param name="topicid">Id of checked <see cref="Topic"/></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrator,Moderator")]
        public EmptyResult UpdateTopicList(int topicid)
        {
            if (SessionData.Contains("TopicList"))
            {
                List<int> selectedtopics = SessionData.Get<List<int>>("TopicList");
                if (!selectedtopics.Contains(topicid))
                {
                    selectedtopics.Add(topicid);
                }
                else
                {
                    selectedtopics.Remove(topicid);
                }
                SessionData.Set("TopicList", selectedtopics);
            }
            else
            {
                List<int> topics = new List<int> {topicid};
                SessionData.Set("TopicList", topics);
            }

            return new EmptyResult();
        }

        public ActionResult SaveRating(FormCollection form)
        {
            if (form.AllKeys.Contains("PostRating"))
            {
                var topic = Topic.FetchTopic(Convert.ToInt32(Request.Form["TopicId"]));
                if (Request.Form["PostRating"] != "0")
                {
                    topic.RatingTotal += (int) (decimal.Parse(Request.Form["PostRating"])*10);
                }

                topic.RatingCount += 1;
                topic.Save();

                using (var db = new SnitzDataContext())
                {
                    TopicRating tr = new TopicRating
                    {
                        MemberId = Convert.ToInt32(Request.Form["MemberId"]),
                        TopicId = Convert.ToInt32(Request.Form["TopicId"])
                    };                    
                    db.Save(tr);
                }
                return Json(new {success = true, responseText = topic.GetTopicRating()});
            }
            return Json(new {success = false, responseText = "PostRating not found"});
            
        }
    }


}
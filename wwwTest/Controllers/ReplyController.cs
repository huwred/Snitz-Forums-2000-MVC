using BbCodeFormatter;
using Hangfire;
using SnitzConfig;
using SnitzMembership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Snitz.Base;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using WWW.ViewModels;
using Forum = SnitzDataModel.Models.Forum;
using Member = SnitzDataModel.Models.Member;
using PrivateMessage = SnitzDataModel.Models.PrivateMessage;
using Reply = SnitzDataModel.Models.Reply;
using Subscriptions = SnitzDataModel.Models.Subscriptions;
using Topic = SnitzDataModel.Models.Topic;
using LangResources.Utility;

namespace WWW.Controllers
{
    public class ReplyController : CommonController
    {
        public ReplyController()
        {
            Dbcontext = new SnitzDataContext();

        }
        public JsonResult LastPost(int id)
        {
            var reply = Reply.FetchReply(id);
            var Message = BbCodeProcessor.Format(reply.Message);
            return Json(new { Message }, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Delete Reply
        /// </summary>
        /// <param name="id">Id of <see cref="Reply"/> to Delete</param>
        /// <returns></returns>
        [Authorize]
        public JsonResult Delete(int id)
        {
            var reply = Reply.FetchReply(id);
            var topic = Topic.FetchTopic(reply.TopicId);

            //if this is not the last reply then do not allow it to be deleted unless we are an
            //Admin or Forum Moderator or it is a draft
            if (reply.PostStatus != Enumerators.PostStatus.Draft)
            {
                if (topic.LastPostReplyId != id &&
                    !(User.IsAdministrator() || User.IsForumModerator(reply.ForumId)))
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(ResourceManager.GetLocalisedString("DeleteReplyNotLast", "ErrorMessage"));
                    //ViewBag.Error = LangResources.Utility.ResourceManager.GetLocalisedString("DeleteReplyNotLast",
                    //    "ErrorMessage");
                    //return View("Error");
                }
            }
            if (Session["ReplyList"] != null)
            {
                List<int> selectedreplies = (List<int>) Session["ReplyList"];
                if (!selectedreplies.Contains(id))
                {
                    selectedreplies.Add(id);
                }
                Dbcontext.DeleteReply(topic.Id, selectedreplies);
                Session["ReplyList"] = null;
            }
            else
            {
                Dbcontext.DeleteReply(reply);
            }
            TempData["Success"] = "Reply deleted";
            string redirectUrl = Url.Action("Posts", "Topic", new { id = topic.Id, pagenum = -1 });

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Posts", "Topic", new {id = topic.Id, pagenum = -1});

        }

        //GET: fetch post message form
        /// <summary>
        /// Displays Form to post a <see cref="Reply"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="topicid">Id of Topic</param>
        /// <param name="forumid">Id of Forum</param>
        /// <param name="catid">Id of Category</param>
        /// <param name="quote"> The <see cref="PostType"/> of reply (reply or quote)</param>
        /// <param name="pagenum">Current Page No.</param>
        /// <param name="archived"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public ActionResult PostMessage(int id, int? topicid = null, int forumid = 0, int catid = 0,
            PostType quote = PostType.Reply, int pagenum = -1, int archived=0)
        {
            if (Config.DisablePosting)
            {
                throw new HttpException(400, LangResources.Utility.ResourceManager.GetLocalisedString("PostingDisabled", "ErrorMessage"));
            }

            var forum = Forum.FetchForum(forumid);
            if (forum == null)
            {

                throw new HttpException(404, LangResources.Utility.ResourceManager.GetLocalisedString("ForumNotFound", "ErrorMessage"));
            }
            if (!User.AllowedAccess(forum, null))
            {
                throw new HttpException(403, LangResources.Utility.ResourceManager.GetLocalisedString("PostMessageForumPermission", "ErrorMessage"));
            }
            if (forum.ReplyAuth != Enumerators.PostAuthType.Anyone &&
                !(User.IsForumModerator(forum.Id) || User.IsAdministrator()))
            {
                throw new HttpException(403,
                    ResourceManager.GetLocalisedString("PostMessageForumPermission",
                        "ErrorMessage"));
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
                throw new HttpException(403, LangResources.Utility.ResourceManager.GetLocalisedString("ForumCategoryIDMatch", "ErrorMessage"));
            }

            var author = Member.GetById(WebSecurity.CurrentUserId);
            var viewModel = new PostMessageViewModel
            {
                IsAuthor = false,
                IsDraft = false,
                IsBlogPost = forum.Type == Enumerators.ForumType.BlogPosts,
                pagenum = pagenum,
                Type = PostType.Reply,
                UseSignature = author.SigDefault == 1,
                IsBugPost = forum.Type == Enumerators.ForumType.BugReports
            };
            if (id > 0 && !topicid.HasValue) //edit the reply
            {
                
                ViewBag.Title = LangResources.Utility.ResourceManager.GetLocalisedString("tipEditReply", "Tooltip");
                var reply = Reply.FetchReply(id,archived);
                var topic = Topic.FetchTopic(reply.TopicId,archived);
                
                    //check to make sure we can edit this
                    if (topic.LastPostReplyId != id &&
                    !(User.IsAdministrator() || User.IsForumModerator(reply.ForumId)))
                {
                    if (reply.PostStatus != Enumerators.PostStatus.Draft)
                    {
                        ViewBag.Error = LangResources.Utility.ResourceManager.GetLocalisedString("DeleteReplyNotLast", "ErrorMessage");
                        return View("Error");
                    }
                }
                if (WebSecurity.CurrentUserId != reply.AuthorId &&
                    !(User.IsAdministrator() || User.IsForumModerator(reply.ForumId)))
                {
                    throw new HttpException(403, LangResources.Utility.ResourceManager.GetLocalisedString("EditReplyNoPermission", "ErrorMessage"));
                }
                viewModel.CatId = reply.CatId;
                viewModel.ReplyId = reply.Id;
                viewModel.ForumId = reply.ForumId;
                viewModel.TopicId = reply.TopicId;
                viewModel.Message = reply.Message;
                viewModel.Sticky = topic.IsSticky == 1;
                viewModel.UseSignature = reply.ShowSig == 1;
                viewModel.Fixed = topic.Subject.Contains("[Fixed]");
                viewModel.Archived = archived;
                if (reply.AuthorId == WebSecurity.CurrentUserId)
                    viewModel.IsAuthor = true;
                if (reply.PostStatus == Enumerators.PostStatus.Draft && viewModel.IsAuthor)
                {
                    //update the date if we are editing a draft
                    viewModel.IsDraft = true;
                }
            }
            else if (quote == PostType.Topic && topicid.HasValue) //quote topic
            {
                ViewBag.Title = LangResources.Utility.ResourceManager.GetLocalisedString("tipQuoteTopic", "Tooltip");
                var topic = Topic.WithAuthor(topicid.Value);
                if (topic.ReplyCount > 0)
                    viewModel.ReplyToId = -1;
                else
                    viewModel.ReplyToId = 0;
                viewModel.CatId = catid;
                viewModel.ForumId = forumid;
                viewModel.TopicId = topicid.Value;
                viewModel.Sticky = topic.IsSticky == 1;
                viewModel.Fixed = topic.Subject.Contains("[Fixed]");
                viewModel.Message = "[quote]" + topic.Message + "[/quote=" + LangResources.Utility.ResourceManager.GetLocalisedString("msgQuote", "General") + " " + topic.Author.Username + "]";
            }
            else if (id > 0 && topicid.HasValue) //quoted reply
            {
                ViewBag.Title = LangResources.Utility.ResourceManager.GetLocalisedString("tipQuoteReply", "Tooltip");
                var reply = Reply.WithAuthor(id);
                viewModel.CatId = reply.CatId;
                viewModel.ForumId = reply.ForumId;
                viewModel.TopicId = reply.TopicId;
                Topic topic = Topic.WithAuthor(reply.TopicId);
                viewModel.Sticky = topic.IsSticky == 1;
                viewModel.Fixed = topic.Subject.Contains("[Fixed]");
                viewModel.Message = "[quote]" + reply.Message
                                     + "[/quote=" + LangResources.Utility.ResourceManager.GetLocalisedString("msgQuote", "General") + " " + reply.Author.Username + "]";
                viewModel.ReplyToId = id;
            }
            else //new reply
            {
                Topic topic = null;
                viewModel.IsDraft = true;
                viewModel.CatId = catid;
                viewModel.ForumId = forumid;
                viewModel.ReplyToId = 0;
                if (topicid.HasValue)
                {
                    viewModel.TopicId = topicid.Value;
                    topic = Topic.WithAuthor(topicid.Value);
                    viewModel.Sticky = topic.IsSticky == 1;
                    viewModel.Fixed = topic.Subject.Contains("[Fixed]");
                    if (topic.ReplyCount > 0)
                        viewModel.ReplyToId = -1;
                }
                ViewBag.Title = LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicReply", "Tooltip") +
                                " : " + (topic == null ? "" : BbCodeProcessor.Format(topic.Subject, false));
                
                
            }

            return View(viewModel);
        }

        /// <summary>
        /// Process <see cref="Reply"/> 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost] //POST:
        [Authorize]
        public ActionResult PostMessage(PostMessageViewModel viewModel)
        {

            if (Config.DisablePosting)
            {
                throw new HttpException(400, LangResources.Utility.ResourceManager.GetLocalisedString("PostingDisabled", "ErrorMessage"));
            }
            bool isAdmin = User.IsAdministrator();
            var author = Member.GetById(WebSecurity.CurrentUserId);

            if ((viewModel.ReplyId <= 0) && ClassicConfig.GetValue("STRFLOODCHECK") == "1" &&
                !isAdmin)
            {
                //Check flood time
                DateTime lastpost = author.LastPostDate ?? DateTime.MinValue;
                int seconds = Convert.ToInt32(ClassicConfig.GetValue("STRFLOODCHECKTIME"));
                var timeleft = DateTime.UtcNow.AddSeconds(-Math.Abs(seconds)) - lastpost;

                if (lastpost > DateTime.UtcNow.AddSeconds(-Math.Abs(seconds)).ToSnitzServerDateString(ClassicConfig.ForumServerOffset).ToDateTime())
                {
                    Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return
                        Json(
                            String.Format(
                                LangResources.Utility.ResourceManager.GetLocalisedString("FloodcheckErr", "ErrorMessage"),
                                Math.Abs(timeleft.Seconds)));

                }

            }

            if (ModelState.IsValid)
            {
                //fetch the topic we are replying to
                var topic = Topic.FetchTopic(viewModel.TopicId,viewModel.Archived);
                var forum = Forum.FetchForum(topic.ForumId);
                if (!User.AllowedAccess(forum, null))
                {
                    forum = null;
                    throw new HttpException(403, LangResources.Utility.ResourceManager.GetLocalisedString("PostMessageForumPermission", "ErrorMessage"));
                }
                bool isForumModerator = User.IsForumModerator(viewModel.ForumId);

                if (topic.PostStatus != Enumerators.PostStatus.Open && !(isAdmin || isForumModerator))
                {
                    throw new HttpException(403, LangResources.Utility.ResourceManager.GetLocalisedString("LockedTopicReply", "ErrorMessage"));
                }
                int authorid = WebSecurity.CurrentUserId;
                if (viewModel.ReplyToId == null)
                {
                    if (topic.ReplyCount > 0)
                        viewModel.ReplyToId = -1;
                    else
                        viewModel.ReplyToId = 0;
                }

                Reply reply = null;
                if (viewModel.ReplyId > 0)
                    reply = Reply.FetchReply(viewModel.ReplyId,viewModel.Archived);
                bool newreply;
                if (reply == null)
                {
                    //new reply
                    reply = new Reply
                    {
                        CatId = topic.CatId,
                        ForumId = topic.ForumId,
                        TopicId = topic.Id,
                        Date = DateTime.UtcNow,
                        AuthorId = authorid,
                        ShowSig = Convert.ToInt16(viewModel.UseSignature ? 1 : 0),
                        Message = BbCodeProcessor.Post(viewModel.Message),
                        LastEditDate = null,
                        PostStatus =
                            forum.Moderation.In(Enumerators.Moderation.AllPosts, Enumerators.Moderation.Replies) &&
                            !(isForumModerator || isAdmin)
                                ? Enumerators.PostStatus.UnModerated
                                : Enumerators.PostStatus.Open
                    };
                    if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
                    {
                        reply.PosterIp = Common.GetUserIP(System.Web.HttpContext.Current);
                        Member member = Member.GetById(authorid);
                        member.LastIP = reply.PosterIp;
                        member.Update(new List<String> {"M_LAST_IP"});
                    }
                    newreply = true;
                    if (isAdmin || isForumModerator)
                    {
                        reply.PostStatus = Enumerators.PostStatus.Open;
                    }
                    if (viewModel.SaveDraft)
                    {
                        reply.PostStatus = Enumerators.PostStatus.Draft;
                    }
                    if (Request.Form.AllKeys.Contains("PostRating"))
                    {
                        if (Request.Form["PostRating"] != "0")
                        {
                            reply.Rating = (int) (decimal.Parse(Request.Form["PostRating"])*10);
                        }
                    }
                }
                else
                {
                    //editing reply
                    newreply = false;
                    if (reply.PostStatus == Enumerators.PostStatus.Draft)
                    {
                        reply.Date = DateTime.UtcNow;
                        if (!viewModel.SaveDraft)
                        {
                            reply.PostStatus =
                                forum.Moderation.In(Enumerators.Moderation.AllPosts, Enumerators.Moderation.Replies) &&
                                !(isForumModerator || isAdmin)
                                    ? Enumerators.PostStatus.UnModerated
                                    : Enumerators.PostStatus.Open;
                        }
                    }
                    reply.Message = BbCodeProcessor.Post(viewModel.Message);
                    reply.ShowSig = Convert.ToInt16(viewModel.UseSignature ? 1 : 0);

                    if (!isAdmin && ClassicConfig.ShowEditedBy && reply.PostStatus != Enumerators.PostStatus.Draft)
                    {
                        reply.LastEditDate = DateTime.UtcNow;
                        reply.LastEditUserId = authorid;
                    }
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
                    if (!topic.Subject.Contains("[Fixed]") && viewModel.Fixed)
                    {
                        topic.Subject += " [Fixed]";
                    }
                    else if (!viewModel.Fixed)
                    {
                        topic.Subject = topic.Subject.Replace("[Fixed]", "");
                    }
                    topic.IsSticky = Convert.ToInt16(viewModel.Sticky ? 1 : 0);
                    topic.DoNotArchive = Convert.ToInt16(viewModel.DoNotArchive ? 0 : 1);
                }
                if (ClassicConfig.GetValue("STRSIGNATURES") == "1" && ClassicConfig.GetValue("STRDSIGNATURES") == "0")
                {
                    if (viewModel.UseSignature)
                    {
                        if (!String.IsNullOrWhiteSpace(author.Signature))
                        {
                            reply.Message += "[br][br][hr]";
                            reply.Message += author.Signature;
                        }

                    }
                }
                if (ClassicConfig.GetValue("STRARCHIVESTATE") == "1")
                {
                    if (viewModel.DoNotArchive)
                        topic.DoNotArchive = 0;
                }
                if (viewModel.Archived == 1)
                {
                    ArchivedReply r = new ArchivedReply();
                    reply.CopyProperties(r, new string[] { });
                    r.Update(new[] { "R_MESSAGE" });
                    var returnUrl = Url.RouteUrl(new { controller = "Topic", action = "Posts", id = reply.TopicId, viewModel.pagenum,archived=1 });
                    return Json(new { success = true, responseText = reply.TopicId + "|" + returnUrl + "|#" + reply.Id }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    reply.Save();
                    topic.Save();
                    //Dbcontext.UpdateThreadOrder(viewModel.ReplyToId.Value,reply.TopicId,reply.Id);
                }
                //if user on monitor list send admin a pm
                if (User.IsUserInRole("Monitored"))
                {
                    try
                    {
                        var adminId = WebSecurity.GetUserId(ClassicConfig.GetValue("STRADMINUSER"));
                        PrivateMessage.SendPrivateMessage(authorid, adminId, "Monitored User Activity",
                            String.Format("{0} just posted/edited a reply {1}Topic/Posts/{2}?pagenum=-1#{3}",
                                author.Username, Config.ForumUrl, topic.Id, reply.Id));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                //update last post info

                if (newreply || !(isAdmin || isForumModerator))
                {
                    topic.UpdateLastPost(reply, newreply);
                }
                if (reply.PostStatus == Enumerators.PostStatus.Open || topic.PostStatus == Enumerators.PostStatus.Closed)
                {
                    if (newreply)
                    {
                        Member.UpdatePostCount(authorid, forum.IncrementPostCount == 1);
                        var debugMsg = new StringBuilder();

                        if (ClassicConfig.SubscriptionLevel != Enumerators.SubscriptionLevel.None)
                        {
                            switch (forum.Subscription)
                            {
                                case Enumerators.Subscription.ForumSubscription:
                                case Enumerators.Subscription.TopicSubscription:
                                    try
                                    {
                                        BackgroundJob.Enqueue(() => ProcessSubscriptions.Reply(reply.Id));
                                    }
                                    catch (Exception ex)
                                    {
                                        debugMsg.AppendLine(ex.Message);
                                        debugMsg.Append(ex.StackTrace);
                                    }
                                    
                                    break;
                            }

                        }
                        if (ClassicConfig.GetValue("STRPMSTATUS") == "1")
                        {
                            MatchCollection matches = Regex.Matches(reply.Message, @"(?:(?<=\s|^)@""(?:[^""]+))|(?:(?<=\s|^)@(?:[^\s]+))");
                            foreach (Match match in matches)
                            {
                                var user = MemberManager.GetUser(WebUtility.HtmlDecode(match.Value.Replace("@", "")));
                                if (user != null && user.UserId != WebSecurity.CurrentUserId)
                                {
                                    //user mentioned in post, send them a PM
                                    PrivateMessage msg = new PrivateMessage
                                    {
                                        ToUsername = user.UserName,
                                        ToMemberId = user.UserId,
                                        FromMemberId = WebSecurity.CurrentUserId,
                                        Subject = LangResources.Utility.ResourceManager.GetLocalisedString("MentionedInPostSubject", "PrivateMessage"),// "You were mentioned in a Post",
                                        Message = String.Format(LangResources.Utility.ResourceManager.GetLocalisedString("MentionedMessage", "PrivateMessage"), WebSecurity.CurrentUserName, Config.ForumUrl.Trim(), reply.TopicId, reply.Id),
                                        SentDate = DateTime.UtcNow,
                                        Read = 0,
                                        ShowOutBox = 0
                                    };
                                    msg.Save();
                                    if (user.PrivateMessageNotify == 1 && ClassicConfig.AllowEmail && !user.HasSubscription(reply))
                                    {
                                        EmailController.SendTaggedUserNotification(ControllerContext, Member.GetById(user.UserId), author,reply);
                                    }
                                }
                            }
                        }
                    }
                }
                if (viewModel.SubscribeTopic)
                {
                    Subscriptions.Subscribe(reply.CatId, reply.ForumId, reply.TopicId, authorid);
                }

                return Json(new { success = true, responseText = topic.Id + "|" + Url.RouteUrl(new { controller = "Topic", action = "Posts", id = topic.Id, viewModel.pagenum }) + "|#" + reply.Id }, JsonRequestBehavior.AllowGet);

            }
            return View();
        }

        /// <summary>
        /// Approve moderated post
        /// </summary>
        /// <param name="id">Id of Unmoderated <see cref="Reply"/></param>
        /// <param name="topicid">Id of Topic</param>
        /// <returns></returns>
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult Approve(int id,int topicid)
        {
            var topic = Topic.FetchTopic(topicid);
            var forum = Forum.FetchForum(topic.ForumId);
            Reply.SetStatus(id, Enumerators.PostStatus.Open);
            if (ClassicConfig.SubscriptionLevel != Enumerators.SubscriptionLevel.None)
            {
                switch (forum.Subscription)
                {
                    case Enumerators.Subscription.ForumSubscription:
                    case Enumerators.Subscription.TopicSubscription:

                        BackgroundJob.Enqueue(() => ProcessSubscriptions.Reply(id));
                        break;
                }

            }
            return RedirectToAction("Posts", "Topic", new { id = topicid, pagenum = -1 });
        }

        /// <summary>
        /// Place post on hold
        /// </summary>
        /// <param name="id">Id of Unmoderated <see cref="Reply"/></param>
        /// <param name="topicid">Id of Topic</param>
        /// <returns></returns>
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult OnHold(int id, int topicid)
        {
            Reply.SetStatus(id, Enumerators.PostStatus.OnHold);
            return RedirectToAction("Posts", "Topic", new { id = topicid, pagenum = -1 });
        }

        /// <summary>
        /// Tracks checked Replies
        /// </summary>
        /// <param name="replyid">Id of reply checkbox</param>
        /// <returns></returns>
        [Authorize(Roles = "Administrator,Moderator")]
        public EmptyResult UpdateReplyList(int replyid)
        {
            if (Session["ReplyList"] != null)
            {
                List<int> selectedreplies = (List<int>)Session["ReplyList"];
                if (!selectedreplies.Contains(replyid))
                {
                    selectedreplies.Add(replyid);
                }
                else
                {
                    selectedreplies.Remove(replyid);
                }

                Session["ReplyList"] = selectedreplies;
            }
            else
            {
                List<int> replies = new List<int> {replyid};
                Session["ReplyList"] = replies;
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Open moderation Popup window
        /// </summary>
        /// <param name="id">Id of Unmoderated <see cref="Reply"/></param>
        /// <returns>PopUp Window</returns>
        [Authorize(Roles = "Administrator,Moderator")]
        public PartialViewResult Moderate(int id)
        {
            ApproveTopicViewModal vm = new ApproveTopicViewModal {Id = id};
            return PartialView("popModerate", vm);
        }

        /// <summary>
        /// Process Moderation Popup Form
        /// </summary>
        /// <param name="vm"><see cref="ApproveTopicViewModal"/></param>
        /// <returns>OnSuccess Redirects to Post</returns>
        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult ModeratePost(ApproveTopicViewModal vm)
        {

            if (ModelState.IsValid)
            {
                var reply = Reply.WithAuthor(vm.Id);
                var author = Member.GetById(reply.AuthorId);
                var forum = Forum.FetchForum(reply.ForumId);
                var topic = Topic.FetchTopic(reply.TopicId);
                var subject = "";
                var message = "";


                switch (vm.PostStatus)
                {
                    case "Approve":
                        Reply.SetStatus(vm.Id, Enumerators.PostStatus.Open);
                        //Send email
                        subject = ClassicConfig.ForumTitle + ": Post Approved";
                        message = "Has been approved. You can view it at " + Environment.NewLine +
                                        Config.ForumUrl + "Topic/Posts/" + reply.TopicId + "?pagenum=-1#" + reply.Id +
                                        Environment.NewLine +
                                        vm.ApprovalMessage;
                        if (!ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.None, Enumerators.SubscriptionLevel.Topic))
                        {
                            switch (forum.Subscription)
                            {
                                case Enumerators.Subscription.ForumSubscription:
                                    BackgroundJob.Enqueue(() => ProcessSubscriptions.Reply(vm.Id));
                                    break;
                            }
                        }
                        break;
                    case "Reject":

                        Dbcontext.DeleteReply(reply);
                        //Send email
                        subject = ClassicConfig.ForumTitle + ": Post rejected";
                        message = "Has been rejected. " + Environment.NewLine +
                                        vm.ApprovalMessage;
                        break;
                    case "Hold":
                        Reply.SetStatus(vm.Id, Enumerators.PostStatus.OnHold);
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

                return RedirectToAction("Posts", "Forum", new { id = topic.ForumId });
            }

            return PartialView("popModerate", vm);
        }


    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.WebPages;
using Hangfire;
using LangResources.Utility;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using SnitzMembership.Models;
using SnitzMembership.Repositories;
using WWW.Helpers;
using WWW.ViewModels;
using AllowedMembers = SnitzDataModel.Models.AllowedMembers;
using BadwordFilter = SnitzDataModel.Models.BadwordFilter;
using Category = SnitzDataModel.Models.Category;
using Forum = SnitzDataModel.Models.Forum;
using ForumModerators = SnitzDataModel.Models.ForumModerators;
using Reply = SnitzDataModel.Models.Reply;
using Subscriptions = SnitzDataModel.Models.Subscriptions;
using Topic = SnitzDataModel.Models.Topic;

namespace WWW.Controllers
{
    public class ForumController : CommonController
    {
        public ForumController()
        {
            Dbcontext = new SnitzDataContext();

        }

        [Route("title/{*forumid}")]
        public ActionResult Title(string forumid, int pagenum=1)
        {
            return RedirectToAction("Posts", new { id = forumid, pagenum });
        }

        /// <summary>
        /// display a page of forum topics
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pagenum"></param>
        /// <param name="topics"></param>
        /// <param name="archived"></param>
        /// <returns></returns>
        public ActionResult Posts(int id, int pagenum = 1, List<Topic> topics = null, int archived = 0)
        {

            string templateView = "";
            int pagesize = Config.ForumPageSize;
            if (HttpContext.Request.Cookies.AllKeys.Contains("forum-pagesize") && ClassicConfig.GetValue("STRFORUMPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
            {
                var pagesizeCookie = HttpContext.Request.Cookies["forum-pagesize"];
                if (pagesizeCookie != null)
                    pagesize = Convert.ToInt32(pagesizeCookie.Value);
            }
            var forum = Forum.FetchForumWithCategory(id);
            //check if allowed access to forum
            if (!User.AllowedAccess(forum, null))
            {
                throw new HttpException(403, LangResources.Utility.ResourceManager.GetLocalisedString("PostMessageForumPermission", "ErrorMessage"));
            }
            if (forum.Type == Enumerators.ForumType.BlogPosts)
            {
                templateView = "Blog/";
            }
            ViewBag.RequireAuth = false;
            ViewBag.Subscription = forum.Subscription;
            Enumerators.ForumDays defaultdays = (Enumerators.ForumDays) (Session["DefaultDays" + id] ?? forum.DefaultDays);
            if (archived == 1)
            {
                defaultdays = Enumerators.ForumDays.Archived;
            }

            var sortby = "lpd";
            var sortdir = "DESC";
            if (Request.Form.HasKeys())
            {
                defaultdays = (Enumerators.ForumDays)Enum.Parse(typeof(Enumerators.ForumDays), Request.Form["DefaultDays"]);
                Session["DefaultDays" + id] = defaultdays;
                sortby = Request.Form["OrderBy"];
                sortdir = Request.Form["SortDir"];
            }
            

            if (!User.IsAdministrator() && forum.PrivateForums.In(Enumerators.ForumAuthType.AllowedMemberPassword,
                Enumerators.ForumAuthType.MembersPassword, Enumerators.ForumAuthType.PasswordProtected))
            {
                var auth = Session["Forum_" + id] == null ? "" : Session["Forum_" + id].ToString();
                if (auth != forum.PasswordNew)
                {
                    ViewBag.RequireAuth = true;
                }
            }
            ForumViewModel vm = new ForumViewModel {DefaultDays = defaultdays, OrderBy=sortby,SortDir=sortdir};

            Page<Topic> result = forum.Topics(pagesize, pagenum, User, WebSecurity.CurrentUserId, (int)defaultdays,sortby,sortdir);

            vm.Id = forum.Id;
            vm.Forum = forum;
            if (topics == null)
            {
                vm.PageSize = pagesize;
                vm.Topics = result.Items;
                vm.StickyTopics = forum.StickyTopics(pagenum, 20);
                vm.TotalRecords = result.TotalItems;
            }
            else
            {
                vm.Topics = topics;
                vm.TotalRecords = topics.Count;
                vm.PageSize = topics.Count;

            }
            int pagecount = Convert.ToInt32(result.TotalPages);
            vm.PageCount = pagecount;
            vm.Page = pagenum;

            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                UserProfile userProfile = udb.UserProfiles.SingleOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.LastVisit = userProfile != null ? userProfile.LastVisitDate : DateTime.UtcNow.AddDays(-30);
            }

            ViewBag.IsAdministrator = User.IsAdministrator();
            ViewBag.IsForumModerator = User.IsForumModerator(forum.Id);
            ViewBag.hasForumSubscription = User.ForumSubscriptions().Contains(forum.Id);

            return ClassicConfig.GetIntValue("INTNEWLAYOUT",0) == 1 ? View(templateView + "PostsNew",vm) : View(templateView + "Posts", vm);
            //return View(templateView + "Posts", vm);
        }

        public ActionResult MyView(int pagenum=1, Enumerators.MyTopicsSince activesince = Enumerators.MyTopicsSince.Last12Months)
        {
            var forums = User.ForumSubscriptions();
            Page<Topic> result = Dbcontext.FetchMyForumTopics(5, pagenum, forums);
            MyTopicsViewModel vm = new MyTopicsViewModel
            {
                Topics = result.Items,
                AllTopics = Dbcontext.FetchAllMyForumTopics(forums),
                ActiveSince = activesince
            };

            return View("MyView/Posts", vm);
        }
        public ActionResult MyViewNext(int nextpage, string refresh = "NO" )
        {
            Page<Topic> result = Dbcontext.FetchMyForumTopics(5, nextpage, User.ForumSubscriptions());

            return PartialView("MyView/MyView", result.Items);
        }
        public ActionResult PostDefaultDays(int id, int pagenum = 1, List<Topic> topics = null, int archived = 0)
        {
            return RedirectToAction("Posts","Forum",new{id,pagenum,topics,archived});
        }

        [Authorize]
        public JsonResult DeleteTopic(int id, int archived = 0)
        {
            var topic = Topic.FetchTopic(id);
            int forumid = topic.ForumId;
            if (topic.ReplyCount > 0 && !(User.IsAdministrator() || User.IsForumModerator(forumid)))
            {
                //ViewBag.Error = ResourceManager.GetLocalisedString("delTopicErr", "Admin");
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ResourceManager.GetLocalisedString("delTopicErr", "Admin"));
                //return View("Error");
            }
            if (SessionData.Contains("TopicList") && (User.IsAdministrator() ||
                    User.IsForumModerator(topic.ForumId)))
            {
                List<int> selectedtopics = SessionData.Get<List<int>>("TopicList");
                if (!selectedtopics.Contains(id))
                {
                    selectedtopics.Add(id);
                }
                if (archived == 1)
                {
                    Dbcontext.DeleteArchiveTopic(selectedtopics, forumid);
                }
                else
                {
                    Dbcontext.DeleteTopic(selectedtopics,forumid);
                }
                
                TempData["successMessage"] = selectedtopics.Count + ResourceManager.GetLocalisedString("lblDeleted", "labels");
                SessionData.Clear("TopicList");
            }
            else
            {
                if (User.IsAdministrator() ||
                    User.IsForumModerator(topic.ForumId) ||
                    SessionData.MyUserId == topic.AuthorId)
                {
                    Dbcontext.DeleteTopic(topic);
                    TempData["successMessage"] = ResourceManager.GetLocalisedString("lblTopic", "labels") + " " + ResourceManager.GetLocalisedString("lblDeleted", "labels"); ;
                }
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = fromPage.AbsoluteUri;

                if (fromPage.AbsolutePath == "/")
                {
                    redirectUrl = Url.Action("Index", "Home");
                }
                if (fromPage.AbsolutePath.EndsWith("Active"))
                {
                    redirectUrl = Url.Action("Active", "Topic");
                }

            }
            else
            {
                redirectUrl = Url.Action("Posts", new { id = forumid, pagenum = 1 });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Posts", new { id = forumid, pagenum = 1 });
        }

        //[HttpPost]
        public ActionResult ProcessPreview(PreviewPostViewModel msg)
        {
            if (msg.ReplyId >0 )
            {
                var reply = Reply.FetchReply(msg.ReplyId);
                var author = MemberManager.GetUser(reply.AuthorId);
                msg.Signature = author.Signature;
            }else if (msg.TopicId > 0 && !String.IsNullOrEmpty(msg.Subject))
            {
                var topic = Topic.FetchTopic(msg.TopicId);
                //msg.Subject = topic.Subject;
                var author = MemberManager.GetUser(topic.AuthorId);
                msg.Signature = author.Signature;
            }
            else
            {
                var author = MemberManager.GetUser(WebSecurity.CurrentUserId);
                msg.Signature = author.Signature;
            }
            return View("popPreviewPost","_LayoutNoMenu",msg);

        }        

        public ActionResult Search(int id=0, string phrase="")
        {
            TempData["SideBox"] = false;
            SearchViewModel vm = new SearchViewModel(User)
            {
                OrderBy = "t", 
                SortDir = "DESC", 
                SearchModel = {CategoryList = Category.FetchAll()}
            };

            ViewBag.ForumId = id;
            vm.SearchModel.ForumId = id;
            if (!String.IsNullOrWhiteSpace(phrase))
            {
                vm.SearchModel.Term = phrase;
            }
           
            if (ClassicConfig.GetIntValue("INTNEWLAYOUT",0) == 1)
            {
                return View("SearchNew", vm);
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Search(FormCollection form, string command)
        {
            int pagenum = 1;
            if (command != null)
            {
                pagenum = Convert.ToInt32(command);
            }
            FullSearchModel model = new FullSearchModel
            {
                ForumId = Convert.ToInt32(form["SearchModel.ForumId"] ?? form["FullParams.ForumId"]),
                Category = Convert.ToInt32(form["SearchModel.Category"] ?? form["FullParams.Category"]),
                Term = form["SearchModel.Term"] ?? form["FullParams.Term"],
                PhraseType =
                    (form["SearchModel.PhraseType"] ??
                     form["FullParams.PhraseType"])
                        .ToEnum<Enumerators.SearchWordMatch>(),
                SearchByDays =
                    (form["SearchModel.SearchByDays"] ??
                                    form["FullParams.SearchByDays"])
                                    .ToEnum<Enumerators.SearchDays>(),
                SearchIn =
                    (form["SearchModel.SearchIn"] ??
                     form["FullParams.SearchIn"])
                        .ToEnum<Enumerators.SearchIn>(),
                MemberName =
                    form["SearchModel.MemberName"] ??
                    form["FullParams.MemberName"],
                OrderBy = form["OrderBy"],
                SortDir = form["SortDir"]
                
            };
            if (form.AllKeys.Contains("SearchModel.Archived") || form.AllKeys.Contains("FullParams.Archived"))
            {
                var archived = form["SearchModel.Archived"] ?? form["FullParams.Archived"];
                model.Archived = Convert.ToBoolean(archived);
            }
            model.Terms = model.Term.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (ClassicConfig.GetValue("STRBADWORDFILTER") == "1")
            {
                foreach (var term in model.Terms)
                {
                    if (BadwordFilter.IsBadWord(term))
                    {
                        ModelState.AddModelError("Search Term",
                            LangResources.Utility.ResourceManager.GetLocalisedString("srchBanned", "Error")); //"Unable to search for banned words");
                    }
                }                
            }

            if (!ModelState.IsValid)
            {
                SearchViewModel vm = new SearchViewModel(User);
                return View(vm);
            }

            SearchResult sr = new SearchResult
            {
                Params = new SearchModel { MemberName = model.MemberName,Grouping = true, SubjectOnly = model.SearchIn==Enumerators.SearchIn.Subject},
                FullParams = model
            };
            if (!String.IsNullOrWhiteSpace(model.Term))
            {
                //LangResources.Utility.ResourceManager.GetLocalisedString(model.SearchIn.GetType().Name + "_" + model.SearchIn, "EnumDescriptions")
                sr.Params.Term = "'" + model.Term + "' " + LangResources.Utility.ResourceManager.GetLocalisedString("lblin", "labels") + " " + LangResources.Utility.ResourceManager.GetLocalisedString(model.SearchIn.GetType().Name + "_" + model.SearchIn, "EnumDescriptions");
            }
            else
            {
                //looks like it is a member search, so turn of grouping
                sr.Params.Grouping = false;
            }
            int pagesize = Config.SearchPageSize;
            if (HttpContext.Request.Cookies.AllKeys.Contains("search-pagesize") && ClassicConfig.GetValue("STRSEARCHPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
            {
                var pagesizeCookie = HttpContext.Request.Cookies["search-pagesize"];
                if (pagesizeCookie != null)
                    pagesize = Convert.ToInt32(pagesizeCookie.Value);
            }
            if (!model.Terms.Any() && String.IsNullOrWhiteSpace(model.Term) && String.IsNullOrWhiteSpace(model.MemberName))
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("srchIgnored", "ErrorMessage");// "Your search terms contained only ignored words and no member was specified";
                return View("Error");
            }
            if (Config.FullTextSearch)
            {

                model.FullTextType = (form["SearchModel.FullTextType"] ?? form["FullParams.FullTextType"]).ToEnum<Enumerators.FullTextMatch>();

                var result = Dbcontext.FullTextSearch(model, User, WebSecurity.CurrentUserId,pagesize,pagenum);
                sr.Topics = result.Items;
                sr.PageSize = Config.SearchPageSize;
                sr.TotalRecords = result.TotalItems;
                sr.PageCount = Convert.ToInt32(result.TotalPages);
                sr.Page = pagenum;
                if (sr.Topics == null)
                {
                    ViewBag.Error =
                        ResourceManager.GetLocalisedString("srchIgnored", "ErrorMessage");// "Your search terms contained only ignored words and no member was specified";
                    return View("Error");
                }
            }
            else
            {
                var result = Dbcontext.SearchForum(model, User, WebSecurity.CurrentUserId, pagesize, pagenum);
                sr.Topics = result.Items;
                sr.PageSize = Config.SearchPageSize;
                sr.TotalRecords = result.TotalItems;
                sr.PageCount = Convert.ToInt32(result.TotalPages);
                sr.Page = pagenum;
            }
 
            
            sr.Archived = model.Archived;
            ViewBag.IsAdministrator = User.IsAdministrator();
            ViewBag.IsForumModerator = User.IsForumModerator(model.ForumId);
            bool sidebox = false;
            if (TempData["SideBox"] != null)
            {
                sidebox = (bool) TempData["SideBox"];

            }
            if (sidebox)
            {
                sidebox = sidebox && !Request.IsAjaxRequest();
            }

            if (((System.Web.HttpRequestWrapper) Request).UrlReferrer.PathAndQuery.Contains("Account"))
            {
                sidebox = true;
            }
            if (ClassicConfig.GetIntValue("INTNEWLAYOUT",0) == 1 && !sidebox)
            {
                return PartialView("_SearchResult", sr);
            }
            return View("SearchResult", sr);
        }
        
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Search(FormCollection form)
        //{

        //    FullSearchModel model = new FullSearchModel
        //    {
        //        ForumId = Convert.ToInt32(form["SearchModel.ForumId"] ?? form["FullParams.ForumId"]),
        //        Category = Convert.ToInt32(form["SearchModel.Category"] ?? form["FullParams.Category"]),
        //        Term = form["SearchModel.Term"] ?? form["FullParams.Term"],
        //        PhraseType =
        //            (form["SearchModel.PhraseType"] ??
        //             form["FullParams.PhraseType"])
        //                .ToEnum<Enumerators.SearchWordMatch>(),
        //        SearchByDays =
        //            (form["SearchModel.SearchByDays"] ??
        //                            form["FullParams.SearchByDays"])
        //                            .ToEnum<Enumerators.SearchDays>(),
        //        SearchIn =
        //            (form["SearchModel.SearchIn"] ??
        //             form["FullParams.SearchIn"])
        //                .ToEnum<Enumerators.SearchIn>(),
        //        MemberName =
        //            form["SearchModel.MemberName"] ??
        //            form["FullParams.MemberName"],
        //        OrderBy = form["OrderBy"],
        //        SortDir = form["SortDir"]
                
        //    };
        //    if (form.AllKeys.Contains("SearchModel.Archived") || form.AllKeys.Contains("FullParams.Archived"))
        //    {
        //        var archived = form["SearchModel.Archived"] ?? form["FullParams.Archived"];
        //        model.Archived = Convert.ToBoolean(archived);
        //    }
        //    model.Terms = model.Term.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //    if (ClassicConfig.GetValue("STRBADWORDFILTER") == "1")
        //    {
        //        foreach (var term in model.Terms)
        //        {
        //            if (BadwordFilter.IsBadWord(term))
        //            {
        //                ModelState.AddModelError("Search Term",
        //                    LangResources.Utility.ResourceManager.GetLocalisedString("srchBanned", "Error")); //"Unable to search for banned words");
        //            }
        //        }                
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        SearchViewModel vm = new SearchViewModel(User);
        //        return View(vm);
        //    }

        //    SearchResult sr = new SearchResult
        //    {
        //        Params = new SearchModel { MemberName = model.MemberName,Grouping = true, SubjectOnly = model.SearchIn==Enumerators.SearchIn.Subject},
        //        FullParams = model
        //    };
        //    if (!String.IsNullOrWhiteSpace(model.Term))
        //    {
        //        //LangResources.Utility.ResourceManager.GetLocalisedString(model.SearchIn.GetType().Name + "_" + model.SearchIn, "EnumDescriptions")
        //        sr.Params.Term = "'" + model.Term + "' " + LangResources.Utility.ResourceManager.GetLocalisedString("lblin", "labels") + " " + LangResources.Utility.ResourceManager.GetLocalisedString(model.SearchIn.GetType().Name + "_" + model.SearchIn, "EnumDescriptions");
        //    }
        //    else
        //    {
        //        //looks like it is a member search, so turn of grouping
        //        sr.Params.Grouping = false;
        //    }
        //    int pagesize = Config.SearchPageSize;
        //    if (HttpContext.Request.Cookies.AllKeys.Contains("search-pagesize") && ClassicConfig.GetValue("STRSEARCHPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
        //    {
        //        var pagesizeCookie = HttpContext.Request.Cookies["search-pagesize"];
        //        if (pagesizeCookie != null)
        //            pagesize = Convert.ToInt32(pagesizeCookie.Value);
        //    }
        //    if (!model.Terms.Any() && String.IsNullOrWhiteSpace(model.Term) && String.IsNullOrWhiteSpace(model.MemberName))
        //    {
        //        ViewBag.Error = ResourceManager.GetLocalisedString("srchIgnored", "ErrorMessage");// "Your search terms contained only ignored words and no member was specified";
        //        return View("Error");
        //    }
        //    if (Config.FullTextSearch)
        //    {

        //        model.FullTextType = (form["SearchModel.FullTextType"] ?? form["FullParams.FullTextType"]).ToEnum<Enumerators.FullTextMatch>();

        //        var result = Dbcontext.FullTextSearch(model, User, WebSecurity.CurrentUserId,pagesize,1);
        //        sr.Topics = result.Items;
        //        sr.PageSize = Config.SearchPageSize;
        //        sr.TotalRecords = result.TotalItems;
        //        sr.PageCount = Convert.ToInt32(result.TotalPages);
        //        sr.Page = 1;
        //        if (sr.Topics == null)
        //        {
        //            ViewBag.Error =
        //                ResourceManager.GetLocalisedString("srchIgnored", "ErrorMessage");// "Your search terms contained only ignored words and no member was specified";
        //            return View("Error");
        //        }
        //    }
        //    else
        //    {
        //        var result = Dbcontext.SearchForum(model, User, WebSecurity.CurrentUserId, pagesize, 1);
        //        sr.Topics = result.Items;
        //        sr.PageSize = Config.SearchPageSize;
        //        sr.TotalRecords = result.TotalItems;
        //        sr.PageCount = Convert.ToInt32(result.TotalPages);
        //        sr.Page = 1;
        //    }
 
            
        //    sr.Archived = model.Archived;
        //    ViewBag.IsAdministrator = User.IsAdministrator();
        //    ViewBag.IsForumModerator = User.IsForumModerator(model.ForumId);
        //    bool sidebox = false;
        //    if (TempData["SideBox"] != null)
        //    {
        //        sidebox = (bool)TempData["SideBox"];
        //    }
            
        //    if (ClassicConfig.GetIntValue("INTNEWLAYOUT",0) == 1 && !sidebox)
        //    {
        //        return PartialView("_SearchResult", sr);
        //    }
        //    return View("SearchResult", sr);
        //}

        [Authorize]
        public JsonResult Subscribe(int id, int forumid, int catid)
        {

            try
            {
                Subscriptions.Subscribe(catid, forumid, 0, WebSecurity.CurrentUserId);
                var redirectUrl = Url.Action("Posts", new { id = forumid, pagenum = 1 });
                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ex.Message);
            }
        }

        public JsonResult PasswordCheck(string pwd,string forumid,string topicid)
        {
            var forum = Forum.FetchForum(Convert.ToInt32(forumid));
            if (forum.PasswordNew == pwd)
            {
                Session["Forum_" + forumid] = pwd;
                return Json(true, JsonRequestBehavior.AllowGet);
            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult ArchiveTopic(int id)
        {
            var topic = Topic.FetchTopic(id);
            int forumid = topic.ForumId;

            if (SessionData.Contains("TopicList"))
            {
                List<int> selectedtopics = SessionData.Get<List<int>>("TopicList");
                if (!selectedtopics.Contains(id))
                {
                    selectedtopics.Add(id);
                }
                Forum.Archive(selectedtopics);
                SessionData.Clear("TopicList");
            }
            else
            {
                Forum.Archive(new List<int> { id });
            }


            return RedirectToAction("Posts", new { id = forumid, pagenum = 1 });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult ArchiveForum(int id,int scheddays=60)
        {
            var archiveDate = DateTime.UtcNow.AddDays(-scheddays).ToSnitzDate();
            BackgroundJob.Enqueue(() => Forum.ArchiveTopics(id, archiveDate));

            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;
            }
            else
            {
                redirectUrl = Url.Action("Posts", new { id, pagenum = 1 });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult Details(int id)
        {
            var forum = Forum.FetchForumWithCategory(id);
            ViewBag.AllModerators = ForumModerators.All();
            ViewBag.CategoryList = Category.List();
            return View(forum);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult CreateEdit(int id, int catid = 0)
        {
            string returnUrl = "";

            if (Request.UrlReferrer != null)
                returnUrl = Server.UrlEncode(Request.UrlReferrer.PathAndQuery);

            Forum forum;
            
            if (id == 0)
            {
                ViewBag.Title = "New Forum";
                forum = new Forum
                        {
                            CatId = catid,
                            Id = 0,
                            Subscription = Enumerators.Subscription.None,
                            Category = Category.Fetch(catid)
                        };
            }
            else
            {
                forum = Forum.FetchForumWithCategory(id);
                ViewBag.Title = ResourceManager.GetLocalisedString("forumProps", "Title") + ": " + forum.Subject ;

            }
            ViewBag.AllModerators = ForumModerators.All();
            ViewBag.CategoryList = Category.List();
            ViewBag.ReturnUrl = returnUrl;
            return View("Details",forum);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEdit(Forum forum)
        {

            ModelState.Remove("AllowedMembers");
            ModelState.Remove("ForumModerators");
            if (ModelState.IsValid)
            {
                if (ClassicConfig.GetValue("STRIPLOGGING") == "1")
                {
                    forum.IPAddress = Common.GetUserIP(System.Web.HttpContext.Current);
                }
                Forum originalForum = Forum.SingleOrDefaultById(forum.Id);

                forum.LastArchiveDate = DateTime.MinValue;
                forum.LastDeletionDate = DateTime.MinValue;
                forum.Save();
                new InMemoryCache().Remove("forumauth_" + forum.Id);
                //if subscription changes, remove any invalid subs
                if (originalForum != null && originalForum.Subscription != forum.Subscription)
                {
                    switch (forum.Subscription)
                    {
                        case Enumerators.Subscription.TopicSubscription:
                            Subscriptions.Delete("WHERE FORUM_ID=@0 AND TOPIC_ID=0", forum.Id);
                            break;

                    }
                }
                if (originalForum != null && forum.CatId != originalForum.CatId)
                {
                    Forum.UpdateCategoryId(forum.CatId, forum.Id);
                    forum.UpdateSubscriptions(Category.Fetch(forum.CatId));
                    var cacheService = new InMemoryCache();
                    cacheService.Remove("category.forums");
                    cacheService.Remove("category.forumlist");
                    SessionData.Clear("AllowedForums");
                }
                if (originalForum == null)
                {
                    var cacheService = new InMemoryCache();
                    cacheService.Remove("category.forums");
                    cacheService.Remove("category.forumlist");
                    SessionData.Clear("AllowedForums");
                }

                List<int> moderators = new List<int>();
                if (Request.Form.AllKeys.Contains("ForumModerators"))
                {
                    
                    if (Request.Form["ForumModerators"] != null)
                        moderators = Request.Form["ForumModerators"].Split(',').Select(int.Parse).ToList();
                }
                forum.SaveModerators(moderators.ToList());
                if (forum.PrivateForums.In(Enumerators.ForumAuthType.AllowedMembers, Enumerators.ForumAuthType.AllowedMembersHidden, Enumerators.ForumAuthType.AllowedMemberPassword) )
                {
                    //Do we have a Role for this, if not create one
                    if (!Roles.RoleExists("Forum_" + forum.Id))
                    {
                        Roles.CreateRole("Forum_" + forum.Id);
                    }
                    if (Request.Form.AllKeys.Contains("AllowedMembers"))
                    {
                        List<int> allowedmembers = new List<int>();
                        if (Request.Form["AllowedMembers"] != null)
                        {
                            string[] items = Request.Form["AllowedMembers"].Split(',');
                            foreach (string item in items)
                            {
                                if (item.IsInt() && Convert.ToInt32(item) > 0)
                                {
                                    allowedmembers.Add(Convert.ToInt32(item));
                                }
                                else
                                {
                                    try
                                    {
                                        var userid = WebSecurity.GetUserId(item);
                                        allowedmembers.Add(userid);
                                    }
                                    catch (Exception)
                                    {
                                        //invalid username so just ignore
                                    }
                                }
                            }
                        }
                        AllowedMembers.Save(forum.Id, allowedmembers.ToList());

                        foreach (var allowedmember in allowedmembers)
                        {
                            var member = MemberManager.GetUsername(allowedmember);
                            if (!WebSecurity.IsUserInRole(member, "Forum_" + forum.Id))
                            {
                                Roles.AddUserToRole(member, "Forum_" + forum.Id);
                            }
                        }
                        
                    }
                    else
                    {
                        AllowedMembers.Remove(forum.Id, null);
                        var currentmembers = Roles.GetUsersInRole("Forum_" + forum.Id);
                        if (currentmembers.Any())
                        {
                            Roles.RemoveUsersFromRole(currentmembers, "Forum_" + forum.Id);
                        }
                    }
                }

                ViewBag.AllModerators = ForumModerators.All();
                ViewBag.Title = ResourceManager.GetLocalisedString("forumProps", "Title") + forum.Subject;

                ViewBag.CategoryList = Category.List();
                //ViewBag.ReturnUrl = returnUrl;
                forum.Category = Category.Fetch(forum.CatId);
                return Json(new { success = true, responseText = forum.Id });
                
            }

            ViewBag.AllModerators = ForumModerators.All();
            ViewBag.Title = ResourceManager.GetLocalisedString("forumProps", "Title") + forum.Subject;

            ViewBag.CategoryList = Category.List();
            //ViewBag.ReturnUrl = returnUrl;
            forum.Category = Category.Fetch(forum.CatId);
            var validationErrors = ModelState.Values.Where(e => e.Errors.Count > 0)
            .SelectMany(e => e.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
            return Json(new { success = false, responseText = validationErrors });

        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult LockTopic(int id, int forumid)
        {
            Enumerators.PostStatus status = Enumerators.PostStatus.Closed;

            if (SessionData.Contains("TopicList"))
            {
                List<int> selectedtopics = SessionData.Get< List<int>>("TopicList");
                if (!selectedtopics.Contains(id))
                {
                    selectedtopics.Add(id);
                }
                foreach (int topicid in selectedtopics)
                {
                    Topic topic = Topic.FetchTopic(topicid);
                    status = topic.ToggleLock();
                }
                SessionData.Clear("TopicList");
            }
            else
            {
                Topic topic = Topic.FetchTopic(id);
                status = topic.ToggleLock();
            }

            if (status == Enumerators.PostStatus.Open)
            {
                TempData["successMessage"] = ResourceManager.GetLocalisedString("topicUnlockSuccess", "labels");
            }
            else
            {
                TempData["successMessage"] = ResourceManager.GetLocalisedString("topicLockSuccess", "labels");
            }

            var fromPage = Request.UrlReferrer;
            string redirectUrl = Request.UrlReferrer.AbsoluteUri;
            if (fromPage != null)
            {
                if (fromPage.AbsolutePath == "/")
                {
                    redirectUrl = Url.Action("Index", "Home");
                }
                if (fromPage.AbsolutePath.EndsWith("Active"))
                {
                    redirectUrl = Url.Action("Active", "Topic");
                }

            }
            else
            {
                redirectUrl = Url.Action("Posts", new { id = forumid, pagenum = 1 });
            }
            
            return Json(new { redirectUrl, successMsg= TempData["successMessage"] }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public JsonResult UnSubscribe(int id, int forumid, int catid)
        {
            try
            {
                Subscriptions.UnSubscribe(catid, forumid, 0, WebSecurity.CurrentUserId);
                var redirectUrl = Url.Action("Posts", new { id = forumid, pagenum = 1 });
                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(e.Message);
            }
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult MakeSticky(int id, int sticky)
        {
            var topic = Topic.FetchTopic(id);
            if (SessionData.Contains("TopicList"))
            {
                List<int> selectedtopics = SessionData.Get<List<int>>("TopicList");
                if (!selectedtopics.Contains(id))
                {
                    selectedtopics.Add(id);
                }
                Dbcontext.SetStickyTopic(selectedtopics, sticky);
                SessionData.Clear("TopicList");
            }
            else
            {
                Dbcontext.SetStickyTopic(new List<int> {id}, sticky);
            }
            
            return RedirectToAction("Posts", new { id = topic.ForumId, pagenum = 1 });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult Delete(int id, int catid)
        {
            var forum = Forum.FetchForum(id);
            if (Request.UrlReferrer != null)
            {
                var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());
                if (forum.PostCount > 0)
                {
                    //set error message to pass to referring controller
                    TempData["errTitle"] = ResourceManager.GetLocalisedString("delForum","Admin");
                    TempData["errMessage"] = ResourceManager.GetLocalisedString("delForumErr", "Admin");
                }
                else
                {
                    forum.Delete();
                    var cacheService = new InMemoryCache();
                    cacheService.Remove("category.forums");
                    cacheService.Remove("category.forumlist");
                    SessionData.Clear("AllowedForums");
                }
            
                //db.Update(forum);
                //return RedirectToRoute(routinfo.Values);
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;

            }
            else
            {
                redirectUrl = Url.Action("Posts", new { id });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Posts",new{id});
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult DeleteTopics(FormCollection form)
        {
            if (!String.IsNullOrWhiteSpace(form["ForumId"]))
            {
                try
                {
                    Forum forum = Forum.FetchForum(Convert.ToInt32(form["ForumId"]));
                    forum.DeleteTopics();
                    forum.UpdateLastPost();
                }
                catch (Exception ex)
                {
                    TempData["errTitle"] = "Delete Forum Topics";
                    TempData["errMessage"] = ex.Message;
                }
            }
            return Redirect("~/Admin/Tools");
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult MoveTopics(FormCollection form)
        {
            if (!String.IsNullOrWhiteSpace(form["ForumId"]) && !String.IsNullOrWhiteSpace(form["TargetForumId"]))
            {
                try
                {
                    Forum forum = Forum.FetchForum(Convert.ToInt32(form["ForumId"]));
                    forum.MoveTopics(Convert.ToInt32(form["TargetForumId"]));
                    forum.UpdateLastPost();
                    Forum target = Forum.FetchForum(Convert.ToInt32(form["TargetForumId"]));
                    target.UpdateLastPost();
                }
                catch (Exception ex)
                {
                    TempData["errTitle"] = "Delete Forum Topics";
                    TempData["errMessage"] = ex.Message;
                }
            }
            return Redirect("~/Admin/Tools");
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult Lock(int id, bool @lock)
        {
            var forum = Forum.FetchForum(id);
            if (Request.UrlReferrer != null)
            {
                //var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());
                forum.Status = @lock ? Enumerators.PostStatus.Closed : Enumerators.PostStatus.Open;
                forum.Save();
                var cacheService = new InMemoryCache();
                cacheService.Remove("category.forums");
                cacheService.Remove("category.forumlist");
                //return RedirectToRoute(routinfo.Values);
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;

            }
            else
            {
                redirectUrl = Url.Action("Posts", new { id });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
        }

    }
}
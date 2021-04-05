using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using WWW.Filters;
using WWW.ViewModels;
using PrivateMessage = SnitzDataModel.Models.PrivateMessage;

namespace WWW.Controllers
{
    
    public class HomeController : CommonController
    {
        public HomeController()
        {
            Dbcontext = new SnitzDataContext();

        }

        [OutputCache(Duration = int.MaxValue, VaryByParam = "none", Location = OutputCacheLocation.ServerAndClient, NoStore = true)]
        public ActionResult FAQ()
        {
            HomeViewModel vm = new HomeViewModel();
            return RedirectToAction("Index","Help",null);
        }

        [OutputCache(Duration = 120, VaryByParam = "none", Location = OutputCacheLocation.Client, NoStore = true)]
        public ActionResult Active()
        {
            return RedirectToActionPermanent("Active","Topic");
        }

        [OutputCache(Duration = 120, VaryByParam = "*", Location = OutputCacheLocation.Client, NoStore = true)]
        public ActionResult Latest()
        {
            if (TempData["errMessage"] != null)
            {
                ViewBag.ErrTitle = TempData["errTitle"];
                ViewBag.Error = TempData["errMessage"];
            }
            
            HomeViewModel vm = new HomeViewModel();
            SessionData.Clear("SnitzMenu");
            vm.RecentTopics = Dbcontext.FetchRecentTopicList(User, WebSecurity.CurrentUserId, Config.RecentTopicListSize);
            //vm.ForumCategories = CategoryForumList;
            ViewBag.Subscription = Enumerators.Subscription.None;

            vm.ForumStats = new ForumStatistics(LastVisitDate().ToSnitzServerDateString(ClassicConfig.ForumServerOffset), User);
            return View(vm);

        }
        
        [OutputCache(Duration = 30, VaryByParam = "groupId", Location = OutputCacheLocation.Server, NoStore = true)]
        public ActionResult Index(int groupId = 0)
        {
            if (ClassicConfig.GetIntValue("STRGROUPCATEGORIES") == 1)
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

            ViewBag.GroupId = groupId;
            var categories = Dbcontext.FetchCategoryForumList(User);
            if (groupId > 1)
            {
                var vm = new AdminGroupsViewModel(groupId);
                var check = vm.CurrentGroupForums.Keys;
                categories = categories.Where(c => check.Contains(c.Id)).ToList();
            }

            return ClassicConfig.GetIntValue("INTNEWLAYOUT",0) == 1 ? View("IndexNew",categories as List<Category>) : View(categories as List<Category>);
            //return View(categories);
        }

        [Authorize]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult FileUpload()
        {
            if (ClassicConfig.GetValue("STRALLOWUPLOADS") != "1")
            {
                ViewBag.Error = "File uploads not permitted";
                return View("Error");
            }
            return PartialView("popFileUpload");
        }

        [DoNotLogActionFilter]
        [OutputCache(VaryByParam = "*", Duration = 1)]
        public PartialViewResult RefreshRecentTopics(int id = 0, int forumid= 0, bool sidebar=false)
        {
            return PartialView("_RecentTopics", Dbcontext.FetchRecentTopicList(User, WebSecurity.CurrentUserId, Config.RecentTopicListSize,id,forumid));
        }

        [DoNotLogActionFilter]
        [OutputCache(VaryByParam = "*", Duration = 10)]
        public ActionResult PMNotify()
        {
            if (WebSecurity.IsAuthenticated)
            {
                TempData["PMCount"] = PrivateMessage.Check(WebSecurity.CurrentUserId);

            }

            return PartialView("_PMNotify");
        }

        [DoNotLogActionFilter]
        [OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Client, NoStore = true)]
        public PartialViewResult ForumStats()
        {
            var stats = new ForumStatistics(LastVisitDate().ToSnitzServerDateString(ClassicConfig.ForumServerOffset),User);
            return PartialView("_ForumStats",stats);
        }

    }
}

using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzMembership;
using SnitzMembership.Models;
using SnitzMembership.Repositories;
using Category = SnitzDataModel.Models.Category;
using Subscriptions = SnitzDataModel.Models.Subscriptions;

namespace WWW.Controllers
{
    public class CategoryController : CommonController
    {
        public CategoryController()
        {
            Dbcontext = new SnitzDataContext();

        }
        [OutputCache(Duration = 120, VaryByParam = "id", Location = OutputCacheLocation.Server, NoStore = true)]
        public ActionResult Index(int id)
        {
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                UserProfile userProfile = udb.UserProfiles.SingleOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.LastVisit = userProfile != null ? userProfile.LastVisitDate : DateTime.UtcNow.AddDays(-30);
            }

            return ClassicConfig.GetIntValue("INTNEWLAYOUT",0) == 1 ? View("IndexNew",Dbcontext.FetchCategoryForumList(User).SingleOrDefault(c => c.Id == id)) : View(Dbcontext.FetchCategoryForumList(User).SingleOrDefault(c => c.Id == id));

        }

        [Authorize]
        public JsonResult Subscribe(int id)
        {

            try
            {
                Subscriptions.Subscribe(id, 0, 0, WebSecurity.CurrentUserId);
                string redirectUrl = Url.Action("Index", new { id = id });
                if (Request.UrlReferrer != null)
                {
                    redirectUrl = Request.UrlReferrer.AbsoluteUri;
                    
                }

                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ex.Message);
            }
        }

        [Authorize]
        public JsonResult UnSubscribe(int id)
        {
            Subscriptions.UnSubscribe(id, 0, 0, WebSecurity.CurrentUserId);
            string redirectUrl = Url.Action("Index", new { id = id });
            if (Request.UrlReferrer != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;

            }
            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());
            //return RedirectToRoute(routinfo.Values);
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult CreateEdit(int id)
        {
            Category cat;
            if (id == 0)
            {
                ViewBag.Title = "New Category";
                cat = new Category();
            }
            else
            {
                cat = Category.Fetch(id);
                ViewBag.Title = "Edit Category: " + cat.Title;
            }

            return PartialView("Details", cat);

        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEdit(Category cat)
        {
            if (ModelState.IsValid)
            {
                Category originalCat = Category.SingleOrDefaultById(cat.Id);
                cat.Save();
                var cacheService = new InMemoryCache();
                cacheService.Remove("category.forums");
                cacheService.Remove("category.forumlist");
                SessionData.Clear("AllowedForums");
                //if subscription changes remove any invalid subs
                if (originalCat != null && originalCat.Subscription != cat.Subscription)
                {
                    //need to remove subs here if changed
                    switch (cat.Subscription)
                    {
                        case Enumerators.CategorySubscription.ForumSubscription:
                            //remove category subs
                            Subscriptions.Delete("WHERE CAT_ID=@0 AND FORUM_ID=0 AND TOPIC_ID=0", cat.Id);
                            break;
                        case Enumerators.CategorySubscription.TopicSubscription:
                            //remove forum subs
                            Subscriptions.Delete("WHERE CAT_ID=@0 AND FORUM_ID>0 AND TOPIC_ID=0", cat.Id);
                            break;
                    }
                }
            }
            return View("Details", cat);
        }

        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id)
        {
            var cat = Category.Fetch(id);
            //var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());
            if (cat.HasForums)
            {
                //set error message to pass to referring controller
                TempData["errTitle"] = "Category delete";
                TempData["errMessage"] = "Unable to delete Categories that contain Forums.";
            }
            else
            {
                cat.Delete();
                var cacheService = new InMemoryCache();
                cacheService.Remove("category.forums");
                cacheService.Remove("category.forumlist");
                SessionData.Clear("AllowedForums");
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;

            }
            else
            {
                redirectUrl = Url.Action("Index", new { id });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //db.Update(forum);
            //return RedirectToRoute(routinfo.Values);
        }

        [Authorize(Roles = "Administrator")]
        public JsonResult Lock(int id, bool @lock)
        {
            var cat = Category.Fetch(id);
            //var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());
            cat.Status = @lock ? Enumerators.Status.Closed : Enumerators.Status.Open;
            cat.Save();
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;

            }
            else
            {
                redirectUrl = Url.Action("Index", new { id });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
        }

    }
}
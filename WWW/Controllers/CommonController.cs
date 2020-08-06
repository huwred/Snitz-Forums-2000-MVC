using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Controllers;
using SnitzDataModel.Extensions;
using SnitzMembership;
using WWW.Filters;
using WWW.Models;
using PrivateMessage = SnitzDataModel.Models.PrivateMessage;

namespace WWW.Controllers
{
    //[RemoteRequireHttpsAttribute]

    public class CommonController : BaseController 
    {

        [HttpPost]
        [Authorize]
        public virtual JsonResult Upload()
        {
            string uploadPath = Path.Combine(Server.MapPath("~/" + Config.ContentFolder + "/Members/"), WebSecurity.CurrentUserId.ToString());

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string fileName = "";
            string mimeType = "";
            string filesize = "";
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i]; //Uploaded file                                       
                if (file != null && file.ContentLength > Convert.ToInt32(ClassicConfig.GetValue("INTMAXFILESIZE"))*1024*1024)
                {
                    return Json("error|File too large");
                }
                if (file != null)
                {
                    mimeType = file.ContentType;
                    filesize = "|" + file.ContentLength/1024 + " KB";

                    var extension = Path.GetExtension(file.FileName);
                    if (extension != null)
                    {
                        string fileExt = extension.ToLower();
                        bool contains = false;
                        foreach (string name in ClassicConfig.GetValue("STRFILETYPES").ToLower().Split(','))
                            if (fileExt.Contains(name) || name==mimeType)
                                contains = true;
                        if (!contains)
                        {
                            return Json("error|Invalid File type");
                        }
                    }
                    fileName = Path.GetFileName(file.FileName);//Use the following properties to get file's name, size and MIMEType
                    
                    if (fileName != null)
                        file.SaveAs(Path.Combine(uploadPath, fileName)); //File will be saved in users folder
                }
            }
            return Json(Common.RootFolder + "/" + Config.ContentFolder + "/Members/" + WebSecurity.CurrentUserId + "/" + fileName + "|" + mimeType + "|" + filesize, JsonRequestBehavior.AllowGet);
        }

        [DoNotLogActionFilter]
        [IsCurrentDomain(ThrowNotFoundException = true)]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public FileResult Content()
        {
            var mimetype = MimeMapping.GetMimeMapping(Request.RawUrl);

            return File(Request.RawUrl, mimetype);
        }

        [Authorize]
        public JsonResult AutoCompleteUsername(string term)
        {

            IEnumerable<string> result = MemberManager.CachedUsernames().Where(r => r.ToLower().Contains(term.ToLower()));
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public JsonResult LookupUser(string term)
        {

            var result = SnitzDataModel.Models.Member.CachedUsers(term);
            result = result.Where(r => r.Value.ToLower().Contains(term.ToLower()));
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [DoNotLogActionFilter]
        public PartialViewResult OnlineUsers(bool sidebar = false)
        {
            var cacheService = new InMemoryCache(1);
            var users = cacheService.GetOrSet("online.users", OnlineUserViewModel);

            if (sidebar)
            {
                return PartialView("_OnlineUsersSide", users);
            }
            return PartialView("_OnlineUsers", users);
        }

        [DoNotLogActionFilter]
        public PartialViewResult ThemeChanger()
        {
            var cacheService = new InMemoryCache(1);
            var themefolder = Server.MapPath("~/Content/Themes");

            var folders = cacheService.GetOrSet("theme.folders", () => Directory.GetDirectories(themefolder));
            List<string> themes = new List<string>();
            foreach (var folder in folders)
            {
                themes.Add(folder.Remove(0, themefolder.Length+1));
            }

            return PartialView("_ThemeChanger", themes);
        }
        private static OnlineUserViewModel OnlineUserViewModel()
        {
            OnlineUserViewModel users = new OnlineUserViewModel
            {
                ActiveUsers = OnlineUsersInstance.OnlineUsers.RegisteredUsersCount,
                ActiveGuests = OnlineUsersInstance.OnlineUsers.GuestUsersCount
            };

            var test = OnlineUsersInstance.OnlineUsers.OnlineHashtable;
            List<string> userlist = new List<string>();
            foreach (KeyValuePair<string, OnlineUser> onlineUser in test)
            {
                if (!onlineUser.Key.StartsWith("_'?Unknown"))
                {
                    userlist.Add(onlineUser.Key);
                }
            }
            users.Usernames = userlist;
            users.Members = new List<KeyValuePair<string, string>>();
            if (users.ActiveUsers < 20)
            {
                users.Members = MemberManager.ActiveUsersList(userlist);
            }
            users.RecentMembers = MemberManager.RecentUsersList(userlist);
            return users;
        }
        public ActionResult AllowCookies(string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = "Invalid return Url";
                return View("Error");
            }
            CookieConsent.SetCookieConsent(Response, true);
            return Redirect(returnUrl);
        }
        public ActionResult NoCookies(string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = "Invalid return Url";
                return View("Error");
            }
            CookieConsent.SetCookieConsent(Response, false);
            if (Request.IsAjaxRequest())
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
            else
                return Redirect(returnUrl);
        }

        public PartialViewResult CookiePolicy()
        {
            return PartialView("_CookiePolicy");
        }

        /// <summary>
        /// Action method for serving static html pages with no controller action
        /// </summary>
        /// <param name="viewName">Name of the View file</param>
        /// <returns></returns>
        public ActionResult Public(string viewName)
        {
            return View("~/Views/Static/" + viewName + ".cshtml");
        }
        public HttpStatusCodeResult PayPalPaymentNotification(PayPalCheckoutInfo payPalCheckoutInfo)
        {
            PayPalListenerModel model = new PayPalListenerModel {_PayPalCheckoutInfo = payPalCheckoutInfo};
            using (StreamWriter file = System.IO.File.AppendText(Server.MapPath("~/ProtectedContent/IPNLog.txt")))
            {
                file.WriteLine("----------------" + DateTime.UtcNow.ToShortDateString() + "----------------");
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, model._PayPalCheckoutInfo);
            }
            byte[] parameters = Request.BinaryRead(Request.ContentLength);
            if (parameters != null)
            {
                model.GetStatus(parameters);
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        internal ActionResult RedirectToLocal(string returnUrl)
        {
            if (ControllerContext.HttpContext.IsMyDomain())
            {
                return Redirect(Url.Content(returnUrl));
            }
            return RedirectToAction("Index", "Home");
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is InvalidCastException)
            {

                //if (Request.Url != null) filterContext.Result = this.Redirect(Request.Url.AbsoluteUri);
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
                    ViewData = new ViewDataDictionary { { "Error", filterContext.Exception.Message } }
                };
                filterContext.ExceptionHandled = true;
            }
            if (filterContext.Exception is InvalidOperationException)
            {
                filterContext.ExceptionHandled = true;
                string CurrentController = (string)this.RouteData.Values["controller"];
                string CurrentAction = (string)this.RouteData.Values["action"];
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
                    ViewData = new ViewDataDictionary { { "Error", CurrentController + ":" + CurrentAction } }
                };
            }
            // if the request is AJAX return JSON else view.
            if (IsAjax(filterContext))
            {
                //Because its a exception raised after ajax invocation
                //Lets return Json
                filterContext.Result = new JsonResult()
                {
                    Data = filterContext.Exception.Message,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };

                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            else
            {

                //Normal Exception
                //So let it handle by its default ways.
                base.OnException(filterContext);

            }

            // Write error logging code here if you wish.

        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            #region Get Counts
            ViewBag.PendingMembers = false;

            if (WebSecurity.IsAuthenticated)
            {
                TempData["PMCount"] = PrivateMessage.Check(WebSecurity.CurrentUserId);
                if (User.IsAdministrator())
                {
                    if (ClassicConfig.RestrictReg)
                    {
                        if(MemberManager.PendingMembers().Any())
                            ViewBag.PendingMembers = true;
                    }
                }
            }
            if (HttpContext.Request.Cookies.AllKeys.Contains("timezoneoffset"))
            {
                var timezoneCookie = HttpContext.Request.Cookies["timezoneoffset"];
                if (timezoneCookie != null)
                    Session["timezoneoffset"] = timezoneCookie.Value;
            }

            base.OnActionExecuting(filterContext);


            #endregion            
        }

        private bool IsAjax(ExceptionContext filterContext)
        {
            return filterContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }


    }
}
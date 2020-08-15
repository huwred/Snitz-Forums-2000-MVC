using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using Hangfire;
using LangResources.Utility;
using log4net;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using SnitzMembership.Models;
using SnitzMembership.Repositories;
using WWW.Models;
using WWW.ViewModels;
using AllowedMembers = SnitzDataModel.Models.AllowedMembers;
using BadwordFilter = SnitzDataModel.Models.BadwordFilter;
using ForumModerators = SnitzDataModel.Models.ForumModerators;
using ForumTotals = SnitzDataModel.Models.ForumTotals;
using Member = SnitzDataModel.Models.Member;
using NameFilter = SnitzDataModel.Models.NameFilter;
using SpamFilter = SnitzDataModel.Models.SpamFilter;
using Subscriptions = SnitzDataModel.Models.Subscriptions;
using WebSecurity = SnitzMembership.WebSecurity;


namespace WWW.Controllers
{

    public class AccountController : CommonController
    {
        /// <summary>
        /// GET: Members List
        /// </summary>
        /// <param name="pagenum"></param>
        /// <param name="sortOrder"></param>
        /// <param name="sortCol"></param>
        /// <param name="initial"></param>
        /// <returns></returns>
        [Authorize]
        [OutputCache(Duration = 300, VaryByParam = "pagenum", Location = OutputCacheLocation.Client, NoStore = true)]
        public ActionResult Index(int pagenum = 1, string sortOrder = "desc", string sortCol = "", string initial = "")
        {
            ViewBag.SortDir = sortOrder == "desc" ? "asc" : "desc";
            ViewBag.SortCol = sortCol;
            if (sortCol == "") ViewBag.SortCol = "posts";
            ListUserViewModel vm = new ListUserViewModel();
            int pagesize = Config.MemberPageSize;
            if (HttpContext.Request.Cookies.AllKeys.Contains("account-pagesize") && ClassicConfig.GetValue("STRACCOUNTPAGESIZES", Config.DefaultPageSize.ToString()).Split(',').Count() > 1)
            {
                var pagesizeCookie = HttpContext.Request.Cookies["account-pagesize"];
                if (pagesizeCookie != null)
                    pagesize = Convert.ToInt32(pagesizeCookie.Value);
            }

            ViewData["LastVisitDateTime"] = LastVisitDate();

            var test = MemberManager.GetMembers(User.IsAdministrator(),pagesize, pagenum, sortOrder, sortCol, initial);
            
            vm.SearchForm = new UserSearchViewModel();

            vm.Initial = initial;
            vm.Users = test.Items;
            ViewBag.PageCount = test.TotalPages;
            ViewBag.Page = test.CurrentPage;
            return View(vm);

        }

        public ActionResult JumpToPage(FormCollection form)
        {
            var page = form["PageNum"];
            return RedirectToAction("Index", new {pagenum = page});
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {

            ViewData["LastVisitDateTime"] = LastVisitDate();

            if (string.IsNullOrEmpty(returnUrl) && Request.UrlReferrer != null)
            {
                returnUrl = Server.UrlEncode(Request.UrlReferrer.PathAndQuery);
            }
            //if (string.IsNullOrEmpty(returnUrl) || returnUrl.Contains("Account"))
            //{
            //    returnUrl = Url.Content("~/");
            //}
            if (!Url.IsLocalUrl(Server.UrlDecode(returnUrl)))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = "Invalid return Url";
                return View("Error");
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Redirect = false;
            LoginModel model = new LoginModel {IsConfirmed = true,RememberMe = true};
            return View(model);
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string ReturnUrl)
        {
            //returnURL needs to be decoded
            string decodedUrl = "";
            if (!string.IsNullOrEmpty(ReturnUrl))
                decodedUrl = Server.UrlDecode(ReturnUrl);
            if (!Url.IsLocalUrl(decodedUrl))
            {
                //ViewBag.Error = "Invalid return URL";
                //return View("Error");
                decodedUrl = "";
            }
            ViewData["LastVisitDateTime"] = LastVisitDate();

            if (model.IsConfirmed)
            {
                try
                {
                    if (ModelState.IsValid &&
                        WebSecurity.Login(model.UserName, model.Password, model.RememberMe))
                    {

                        SessionData.ClearAll();
                        var appUrl = HttpRuntime.AppDomainAppVirtualPath;
                        if (appUrl == "/")
                            appUrl = "";

                        var landing = "Index"; //ConfigurationManager.AppSettings["LandingPage"];
                        if (decodedUrl == appUrl || decodedUrl == appUrl + "/Home/Index" || decodedUrl == "")
                        {
                            return landing != "Index"
                                ? RedirectToLocal(appUrl + "/Home/" + landing)
                                : RedirectToLocal(appUrl + "/Home/Index");
                        }

                        return RedirectToLocal(decodedUrl);
                    }
                }
                catch (Exception e)
                {
                    switch (e.Message)
                    {
                        case "UsernameNotFound":

                            ModelState.AddModelError("",
                                ResourceManager.GetLocalisedString("UsernameNotFound", "ErrorMessage"));
                            return View(model);
                        case "UnApprovedAccount":
                            return RedirectToAction("UnApprovedAccount");
                        case "LockedAccount":
                            return RedirectToAction("LockedAccount");
                        case "IncompleteReg":
                            model.IsConfirmed = false;
                            ModelState.AddModelError("",
                                ResourceManager.GetLocalisedString("IncompleteReg", "ErrorMessage"));
                            return View(model);
                        case "Disabled":
                            ModelState.AddModelError("",
                                ResourceManager.GetLocalisedString("Account_Disabled", "ErrorMessage"));
                            return View(model);
                        default:
                            ViewBag.Error = "Login problem: " + e.Message;
                            return View("Error");
                    }
                }
            }
            else
            {
                EmailController.ResendConfirmationEmail(ControllerContext,model.UserName);
                model.IsConfirmed = true;
                ModelState.AddModelError("", ResourceManager.GetLocalisedString("RegEmailResent", "General"));
                return View(model);
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", ResourceManager.GetLocalisedString("LoginFailed", "ErrorMessage"));
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        public ActionResult LogOff()
        {
            OnlineUsersInstance.OnlineUsers.SetUserOffline(WebSecurity.CurrentUserName);
            WebSecurity.Logout();
            SnitzCookie.LogOut();
            SessionData.ClearAll();
            return RedirectToAction("Index", "Home");
        }

        #region Password Forms

        [AllowAnonymous]
        public ActionResult ResetPassword(string id)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return View();
            }

            var userid = WebSecurity.GetUserIdFromPasswordResetToken(id);
            ViewBag.Message = ResourceManager.GetLocalisedString("PasswordInvalidToken", "General"); //"Did not recognise the Reset Key";
            string token;
            using (var db = new SnitzDataContext())
            {
                string query = "select PasswordVerificationToken from webpages_Membership where UserId = " + userid;
                token = db.Query<string>(query).FirstOrDefault();
            }
            if (token == id)
            {
                ViewBag.Message = ResourceManager.GetLocalisedString("PasswordTokenSuccess", "General"); //"Token confirmation succesful";
                ViewBag.Change = true;
                ViewBag.Token = token;
                return View();
            }
            ViewBag.Error = ResourceManager.GetLocalisedString("PasswordTokenExpired", "General"); //"The Reset token is not valid, it may have expired or been used already.";

            return View("Error");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(FormCollection form)
        {
            //check for a password field
            bool ispPasswordForm = form.AllKeys.Contains("NewPassword");
            if (ispPasswordForm)
            {
                //there was a password, so lets do the reset
                WebSecurity.ResetPassword(form["token"], form["NewPassword"]);
                ViewBag.Message = "<h5>" + ResourceManager.GetLocalisedString("PasswordChangeSuccess", "General") +
                                  "</h5>";
                return RedirectToAction("Login", "Account");
            }

            //not the new password form, so generate token and send email
            var userid = WebSecurity.GetUserId(form["username"]);
            if (userid < 0)
            {
                ModelState.AddModelError("", ResourceManager.GetLocalisedString("UsernameNotFound", "ErrorMessage"));
            }
            else
            {
                string token = WebSecurity.GeneratePasswordResetToken(form["username"], 2880); //Expires in 48 hours
                EmailController.SendResetPasswordConfirmation(ControllerContext,form["username"], token);
                ViewBag.Change = false;
                ViewBag.Message = ResourceManager.GetLocalisedString("PasswordResetMsg", "General");
            }
            return View();
        }

        /// <summary>
        /// POST: Process password change form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(LocalPasswordModel model)
        {

            ViewBag.HasLocalPassword = true;
            string result = "";

            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword,
                        model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    //need to update the snitz member table now
                    bool success = Member.SetPassword(WebSecurity.CurrentUserId, model.OldPassword,
                        model.NewPassword);
                    if (success)
                        result = ResourceManager.GetLocalisedString("PasswordChangeSuccess", "General");
                    else
                    {
                        result = ResourceManager.GetLocalisedString("PasswordChangeAdminNotify", "General");
                        ModelState.AddModelError("",
                            ResourceManager.GetLocalisedString("PasswordChangeAdminNotify", "General"));
                    }
                    return Json(result);
                }
                result = ResourceManager.GetLocalisedString("PasswordChangeError", "ErrorMessage");
                ModelState.AddModelError("", ResourceManager.GetLocalisedString("PasswordChangeError", "ErrorMessage"));
            }

            return Json(result);
        }

        #endregion

        #region Change Email

        /// <summary>
        /// POST: Process email change form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeEmail(ChangeEmailModel model)
        {
            string result;

            if (ModelState.IsValid)
            {
                //var membership = (WebMatrix.WebData.SimpleMembershipProvider) Membership.Provider;
                var test = MemberManager.ValidateUser(model.Username, model.Password);

                if (test == false)
                {
                    return Json(new {success = false, responseText = "Inalid Password"}, JsonRequestBehavior.AllowGet);
                }

                string confirmationToken = WebSecurity.GeneratePasswordResetToken(model.Username, 43200);
                    //expires in 30 days
                EmailController.SendChangeEmailConfirmation(ControllerContext, model.NewEmail, model.Username, confirmationToken);
                result = ResourceManager.GetLocalisedString("EmailConfirm", "labels"); //"";
            }
            else
            {
                result = string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage));
                return Json(new {success = false, responseText = result});
            }
            return Json(new {success = true, responseText = result});
            //return Redirect(Url.RouteUrl(new { controller = "Account", action = "UserProfile", id = WebSecurity.CurrentUserName }) + "#changeemail");
        }

        /// <summary>
        /// GET: process the confirmation token 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult EmailConfirmation(string id)
        {
            var userid = WebSecurity.GetUserIdFromPasswordResetToken(id);
            string token;
            using (var db = new SnitzDataContext())
            {
                string query = "select PasswordVerificationToken from webpages_Membership where UserId = " + userid;
                token = db.Query<string>(query).FirstOrDefault();
            }

            if (token == id)
            {

                using (SnitzDataContext db = new SnitzDataContext())
                {
                    Member user = Member.GetById(userid);
                    if (user != null)
                    {
                        if (!String.IsNullOrWhiteSpace(user.NewEmail))
                        {
                            user.Email = user.NewEmail;
                            user.NewEmail = "";
                            user.Update(new[] {"M_EMAIL", "M_NEWEMAIL"});
                        }
                        else
                        {
                            return RedirectToAction("ChangeEmailFailure");
                        }

                    }
                    //clear the token
                    db.Execute(
                        "UPDATE webpages_Membership SET PasswordVerificationToken=NULL,PasswordVerificationTokenExpirationDate=NULL where UserId=" +
                        userid);

                }
                return RedirectToAction("ChangeEmailSuccess");
            }
            return RedirectToAction("ChangeEmailFailure");
        }

        public ActionResult ChangeEmailSuccess()
        {
            return View();
        }

        public ActionResult ChangeEmailFailure()
        {
            ViewBag.Error = ResourceManager.GetLocalisedString("ProfileNoView", "ErrorMessage"); //"There was an error changing your email address. Please try again.";
            return View("Error");
        }

        public PartialViewResult ChangeEmail(int id)
        {
            ChangeEmailModel vm = new ChangeEmailModel();
            //{ Username = vm.Profile.UserName, CurrentEmail = vm.Profile.Email }
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                var profile = udb.UserProfiles.SingleOrDefault(u => u.UserId == id);
                if (profile != null)
                {
                    vm.Username = profile.UserName;
                    vm.CurrentEmail = profile.Email;
                }
            }
            return PartialView("popChangeEmail",vm);
        }

        #endregion


        #region Helpers


        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return ResourceManager.GetLocalisedString("UserNameExists", "ErrorMessage");

                case MembershipCreateStatus.DuplicateEmail:
                    return ResourceManager.GetLocalisedString("DuplicateEmail", "ErrorMessage");
                //return "A username for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return ResourceManager.GetLocalisedString("InvalidPassword", "ErrorMessage");
                //return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return ResourceManager.GetLocalisedString("InvalidEmail", "ErrorMessage");
                //return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return ResourceManager.GetLocalisedString("InvalidUsername", "ErrorMessage");

                case MembershipCreateStatus.ProviderError:
                    return
                        "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return ResourceManager.GetLocalisedString("UserRejected", "ErrorMessage");
                //return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return ResourceManager.GetLocalisedString("CreateUserDefault", "ErrorMessage");
                //return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }


        /// <summary>
        /// Updates members last activity date
        /// </summary>
        /// <param name="id"></param>
        /// <param name="returnProfile"></param>
        /// <returns>UserProfile Object</returns>
        [NonAction]
        private UserProfile UpdateLastActivity(int id, bool returnProfile = false)
        {
            if (id == 0 || id == WebSecurity.CurrentUserId)
            {
                //current user so update last activity
                if (id == 0)
                    id = WebSecurity.CurrentUserId;

                Member.UpdateLastActivity(id);

                //member.Update(new List<String> { "M_LASTACTIVITY", "M_LAST_IP" });
            }
            if (returnProfile)
            {
                UserProfile userProfile;
                using (SnitzMemberContext udb = new SnitzMemberContext())
                {
                    userProfile = id == 0
                        ? udb.UserProfiles.SingleOrDefault(u => u.UserId == WebSecurity.CurrentUserId)
                        : udb.UserProfiles.SingleOrDefault(u => u.UserId == id);
                }

                return userProfile;
            }
            return null;
        }

        #endregion

        /// <summary>
        /// POST: Process username change form
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeUsername(ChangeUsernameModel model)
        {
            //store currentuserid
            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Password))
                {
                    return RedirectToAction("Index", "Home");
                }
                var membership = Membership.Provider;
                var test = MemberManager.ValidateUser(model.Username, model.Password);

                if (test == false)
                {

                    return Json(new {success = false, responseText = ResourceManager.GetLocalisedString("LoginFailed", "ErrorMessage")}, JsonRequestBehavior.AllowGet);

                }
                string status;
                if (MemberManager.UsernameExists(model.NewUserName, out status))
                {
                    return Json(new { success = false, responseText = status }, JsonRequestBehavior.AllowGet);
                }
                try
                {

                    MemberManager.ChangeUsername(model.Username, model.NewUserName);

                    OnlineUsersInstance.OnlineUsers.SetUserOffline(model.Username);
                    WebSecurity.Logout();
                    SnitzCookie.LogOut();
                    SessionData.Clear("SnitzMenu");

                    WebSecurity.Login(model.NewUserName, model.Password);
                }
                catch (Exception ex)
                {
                    return
                        Json(
                            new
                            {
                                success = false,
                                responseText = ex.Message
                            },
                            JsonRequestBehavior.AllowGet);
                }


            }

            return
                Json(
                    new
                    {
                        success = true,
                        responseText = Url.Action("UserProfile", "Account", new {id = model.NewUserName})
                    },
                    JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult ChangeUsername(int id)
        {
            ChangeUsernameModel vm = new ChangeUsernameModel();
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                var profile  = udb.UserProfiles.SingleOrDefault(u => u.UserId == id);
                if (profile != null) vm.Username = profile.UserName;
            }
            return PartialView("popChangeUsername",vm);
        }


        /// <summary>
        /// GET: UserProfile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult UserProfile(string id = "")
        {
            int memberid = 0;
            UserProfileViewModel vm = new UserProfileViewModel();
            ViewBag.Message = "";
            if (id == "zapped" || id == "deleted" || id == "n/a")
            {
                ViewBag.Error = String.Format(ResourceManager.GetLocalisedString("ProfileNoView", "ErrorMessage"),id);  
                return View("Error");
            }
            ViewData["LastVisitDateTime"] = LastVisitDate();

            if (id == "" || id == WebSecurity.CurrentUserName)
            {
                var allsubs = Subscriptions.Member(WebSecurity.CurrentUserId).ToList();
                ViewBag.HasSubs = allsubs.Any();
                if (ClassicConfig.SubscriptionLevel == Enumerators.SubscriptionLevel.Category)
                    ViewData.Add("CatSubs", allsubs.Where(s => s.ForumId == 0));
                if (ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.Category,
                    Enumerators.SubscriptionLevel.Forum))
                    ViewData.Add("ForumSubs", allsubs.Where(s => s.TopicId == 0));
                if (ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.Category,
                    Enumerators.SubscriptionLevel.Forum, Enumerators.SubscriptionLevel.Topic))
                    ViewData.Add("TopicSubs", allsubs.Where(s => s.TopicId > 0));

            }
            else
            {
                memberid = WebSecurity.GetUserId(id);
                ViewBag.HasSubs = false;
            }
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                vm.Profile = memberid == 0
                    ? udb.UserProfiles.SingleOrDefault(u => u.UserName == WebSecurity.CurrentUserName)
                    : udb.UserProfiles.SingleOrDefault(u => u.UserId == memberid);
            }
            vm.Profile = UpdateLastActivity(memberid, true);

            if (TempData != null)
            {
                if (TempData["shortMessage"] != null)
                    ViewBag.Message = TempData["shortMessage"];

            }
            return View(vm);

        }

        public ActionResult MySubscriptions()
        {
            var allsubs = Subscriptions.Member(WebSecurity.CurrentUserId).ToList();
            ViewBag.HasSubs = allsubs.Any();
            if (ClassicConfig.SubscriptionLevel == Enumerators.SubscriptionLevel.Category)
                ViewData.Add("CatSubs", allsubs.Where(s => s.ForumId == 0));
            if (ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.Category, Enumerators.SubscriptionLevel.Forum))
                ViewData.Add("ForumSubs", allsubs.Where(s => s.TopicId == 0));
            if (ClassicConfig.SubscriptionLevel.In(Enumerators.SubscriptionLevel.Category, Enumerators.SubscriptionLevel.Forum, Enumerators.SubscriptionLevel.Topic))
                ViewData.Add("TopicSubs", allsubs.Where(s => s.TopicId > 0));
            return PartialView();
        }

        /// <summary>
        /// GET: Editable UserProfile form
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult UserProfileEdit(int id = 0)
        {
            bool canEdit = User.IsAdministrator() || WebSecurity.CurrentUserId == id;
            if (!canEdit)
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("EditProfilePermission", "ErrorMessage");  //"You do not have permission to edit this profile";
                return View("Error");
            }
            ViewData["LastVisitDateTime"] = LastVisitDate();
            ViewData.Add("Subscriptions", Subscriptions.Member(WebSecurity.CurrentUserId));
            UserProfile userProfile = UpdateLastActivity(id, true);
            userProfile.NewEmail = String.Empty;
            ViewBag.CanEdit = true;
            ViewBag.CountryList = Common.GetCountries();
            ProfileEditModel vm = new ProfileEditModel {Profile = userProfile};

            return View(vm);

        }
        [Authorize]
        public ActionResult UserProfilePrivate(int id = 0)
        {
            bool canEdit = User.IsAdministrator() || WebSecurity.CurrentUserId == id;
            if (!canEdit)
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("EditProfilePermission", "ErrorMessage");  //"You do not have permission to edit this profile";
                return View("Error");
            }

            if (id != 0)
            {
                Member.TogglePrivacy(id);

            }


            return RedirectToAction("UserProfileEdit",new { id });

        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfileEdit(ProfileEditModel userProfile)
        {
            bool canEdit = User.IsAdministrator() || WebSecurity.CurrentUserId == userProfile.Profile.UserId;
            if (!canEdit)
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("EditProfilePermission", "ErrorMessage");  //"You do not have permission to edit this profile";
                return View("Error");
            }

            if (userProfile.Profile.AvatarPostedFileBase != null &&
                userProfile.Profile.AvatarPostedFileBase.ContentLength > 0)
            {
                var file = userProfile.Profile.AvatarPostedFileBase;

                var fileName = Path.GetExtension(file.FileName);
                var path = Path.Combine(Server.MapPath("~/" + Config.ContentFolder + "/Avatar/"), WebSecurity.CurrentUserName + fileName);
                using (var image = Image.FromStream(file.InputStream, true, true))
                {
                    if (Common.SaveCroppedImage(image, 100, 100, path))
                    {
                        //file.SaveAs(path);
                        userProfile.Profile.PhotoUrl = WebSecurity.CurrentUserName + fileName;
                    }
                    else
                    {
                        ViewBag.Message = ResourceManager.GetLocalisedString("ThumbnailSave", "ErrorMessage"); //"Problem saving thumbnail";
                        return View("Error");
                    }                    
                }

            }

            MemberManager.SaveProfile(userProfile.Profile, User.IsAdministrator());


            var allsubs = Subscriptions.Member(userProfile.Profile.UserId);
            ViewBag.HasSubs = allsubs.Any();
            UserProfileViewModel vm = new UserProfileViewModel {Profile = userProfile.Profile};
            ViewBag.Message = ResourceManager.GetLocalisedString("lblProfileSuccess", "labels"); 
            return View("UserProfile", vm);

        }

        /// <summary>
        /// ASYNC POST: called via Ajax to update user activity column
        /// </summary>
        /// <returns>Empty result</returns>
        //[DoNotLogActionFilter]
        public HttpStatusCodeResult UpdateLastActivityDate()
        {
            UpdateLastActivity(WebSecurity.CurrentUserId);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        #region Remote check methods

        /// <summary>
        /// Code to check if username is valid
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult UsernameAvailable(string username)
        {
            if (!Request.QueryString.HasKeys())
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            if (Request.QueryString.GetKey(0).EndsWith("MemberName") ||
                Request.QueryString.GetKey(0).EndsWith("UserName"))
            {
                username = Request.QueryString[0];
            }
            else
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            // need to check username filter
            var user = Member.GetByName(username);
            bool exists = user != null;
            if (!exists) //name doesn't exist, so check the dissallowed list
            {
                if (ClassicConfig.GetValue("STRUSERNAMEFILTER") == "1")
                {
                    try
                    {
                        exists = NameFilter.All().Any(m => m.Name == username);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                if (ClassicConfig.GetIntValue("STRBADWORDFILTER") == 1)
                {
                    var check = BadwordFilter.All().ToList().Find(s => username.Contains(s.BadWord));
                    if (check != null)
                    {
                        return Json(ErrorCodeToString(MembershipCreateStatus.UserRejected), JsonRequestBehavior.AllowGet);
                    }
                }
            }
            if (exists)
            {
                return Json(ErrorCodeToString(MembershipCreateStatus.DuplicateUserName), JsonRequestBehavior.AllowGet);
            }
            return Json(true, JsonRequestBehavior.AllowGet);

        }

        [AllowAnonymous]
        [HttpGet]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult EmailCheck(string email)
        {
            if (!Request.QueryString.HasKeys())
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            if (!ClassicConfig.UniqueEmail)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }

            if (Request.QueryString.GetKey(0).EndsWith("Email"))
            {
                email = Request.QueryString[0];
            }
            else
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            var user = Member.GetByEmail(email);
            var exists = user != null;

            if (exists)
            {
                return Json(ErrorCodeToString(MembershipCreateStatus.DuplicateEmail), JsonRequestBehavior.AllowGet);
            }
            return Json(true, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Check if user exists
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult UserExists(string username)
        {
            if (!Request.QueryString.HasKeys())
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            if (Request.QueryString.GetKey(0).EndsWith("MemberName"))
            {
                username = Request.QueryString[0];
            }
            else
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            // need to check username filter
            var user = Member.GetByName(username);
            var exists = user != null;
            if (!exists) //name doesn't exist, but check the dissallowed list
            {
                try
                {
                    exists = NameFilter.All().Any(m => m.Name == username);
                }
                catch (Exception e)
                {
                    return Json(e.Message, JsonRequestBehavior.AllowGet);
                }

            }
            if (exists)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(ErrorCodeToString(MembershipCreateStatus.InvalidUserName), JsonRequestBehavior.AllowGet);


        }

        #endregion

        #region Registration

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [HttpParamAction]
        public ActionResult Register(string agreeterms)
        {
            //checks both configs as currently registration via new forum is disabled
            if (ClassicConfig.ProhibitNewMembers || Config.ProhibitNewMembers)
            {
                ViewBag.Error = "Registration is not currently enabled";
                return View("Error");
            }

            RegisterModel model = new RegisterModel {RegisterFields = UserProfileExtensions.RequiredProfileFields()};

            ViewBag.Agreed = agreeterms;
            ViewBag.CountryList = Common.GetCountries();
            return View("Register/Register", model);
        }

        [AllowAnonymous]
        [HttpParamActionAttribute]
        public ActionResult Cancel(string agreeterms)
        {
            return RedirectToAction("Index", "Home");
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        public JsonResult CaptchaCheck(string captcha)
        {
            if (Session["Captcha"] == null || Session["Captcha"].ToString() != captcha)
            {
                ModelState.AddModelError("Captcha",
                    ResourceManager.GetLocalisedString("CaptchaCheck_Wrong", "ErrorMessage"));
                //dispay error and generate a new captcha
                //throw new Exception(LangResources.Utility.ResourceManager.GetLocalisedString("CaptchaCheck_Wrong", "ErrorMessage"));
                Response.StatusCode = (int) HttpStatusCode.BadRequest;

                return
                    Json(
                        new
                        {
                            success = false,
                            responseText = ResourceManager.GetLocalisedString("CaptchaCheck_Wrong", "ErrorMessage")
                        });
            }
            return Json(new {success = true});
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            //check the email against Spam Filter
            // !#$%^&*()=+{}[]|\;:/?>,<'
            if (ClassicConfig.GetValue("STRFILTEREMAILADDRESSES") == "1")
            {
                var check = SpamFilter.All().Find(s => model.Email.EndsWith(s.SpamServer));
                if (check != null)
                {
                    ModelState.AddModelError("Spam Filter",
                        ResourceManager.GetLocalisedString("UserFilterError", "ErrorMessage"));
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string status;
                    UserProfile user = MemberManager.RegisterUser(model.UserName, model.Password, model.Email,
                        model.Profile, out status);
                    if (ClassicConfig.EmailValidation && ClassicConfig.AllowEmail)
                    {
                        using (SnitzMemberContext db = new SnitzMemberContext())
                        {
                            var confirmationToken = db.GetEmailConfirmToken(user.UserName);
                            EmailController.SendEmailConfirmation(ControllerContext,user.Email, model.UserName, confirmationToken.Token, user.UserId);
                        }

                    }
                    if (ClassicConfig.RestrictReg)
                    {
                        //don't send email
                        return RedirectToAction("RegisterApproval", "Account");
                    }
                    if (ClassicConfig.EmailValidation && ClassicConfig.AllowEmail)
                    {
                        return RedirectToAction("RegisterEmailVal", "Account");
                    }
                    //No restrictions so log them in
                    if (user != null)
                    {
                        WebSecurity.Login(user.UserName, model.Password);
                        user.LastVisit = DateTime.UtcNow.ToSnitzDate();
                    }


                    return RedirectToAction("Index", "Home");
                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }
            }
            else
            {
                ViewBag.Agreed = "True";
                ViewBag.CountryList = Common.GetCountries(); //return Json(errors, JsonRequestBehavior.AllowGet);
            }

            // If we got this far, something failed, redisplay form
            model.RegisterFields = UserProfileExtensions.RequiredProfileFields();
            return View("Register/Register", model);
        }



        /// <summary>
        /// Shows page saying email needs approval
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult RegisterEmailVal()
        {
            return View("Register/RegisterEmailVal");
        }

        /// <summary>
        /// Shows page to say registration requires approval
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult RegisterApproval()
        {
            return View("Register/RegisterApproval");
        }

        [AllowAnonymous]
        public ActionResult RegisterConfirmation(string id, int usr)
        {
            if (WebSecurity.ConfirmAccount(id))
            {
                using (SnitzMemberContext udb = new SnitzMemberContext())
                {
                    var user = udb.GetUser(usr);
                    user.Status = 1;
                    if (!ClassicConfig.RestrictReg)
                    {
                        var member = udb.GetMembership(user.UserId);
                        member.IsConfirmed = true;
                        user.LastActivity = DateTime.UtcNow.ToSnitzDate();
                        user.UserLevel = 1;
                    }

                    udb.SaveChanges();
                }
                ForumTotals.AddUser();
                return RedirectToAction("ConfirmationSuccess");
            }
            return RedirectToAction("ConfirmationFailure");
        }

        [AllowAnonymous]
        public ActionResult ConfirmationSuccess()
        {
            return View("Register/ConfirmationSuccess");
        }

        [AllowAnonymous]
        public ActionResult ConfirmationFailure()
        {
            return View("Register/ConfirmationFailure");
        }


        #endregion

        public ActionResult LockedAccount()
        {
            ViewBag.Message = ResourceManager.GetLocalisedString("AccountInactive", "General");
            return View();
        }

        public JsonResult RemoveAvatar(string id)
        {
            var result = Member.RemoveAvatar(Convert.ToInt32(id), Server.MapPath("~/" + Config.ContentFolder + "/Avatar/"));
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UnApprovedAccount()
        {
            return View();
        }

        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        [DoNotLogActionFilter]
        public ContentResult CaptchaImage(string prefix, bool noisy = true)
        {
            var logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);         
            var rand = new Random((int) DateTime.Now.Ticks);
            var allowed = Config.CaptchaOperators.Count==0 ?
                          new List<Enumerators.CaptchaOperator>() {Enumerators.CaptchaOperator.Plus} : Config.CaptchaOperators;
            Random random = new Random();
            int item = random.Next(allowed.Count);
            Enumerators.CaptchaOperator randomBar = allowed[item];
            
            try
            {
                //logger.Debug("generate new question");
                //generate new question
                int a = rand.Next(10, 99);
                int b = rand.Next(0, 9);
                string captcha;
                switch (randomBar)
                {
                    case Enumerators.CaptchaOperator.Plus:
                        Session["Captcha" + prefix] = a + b;
                        captcha = string.Format("{0} + {1} = ?", a, b);
                        break;
                    case Enumerators.CaptchaOperator.Minus:
                        Session["Captcha" + prefix] = a - b;
                        captcha = string.Format("{0} - {1} = ?", a, b);
                        break;
                    case Enumerators.CaptchaOperator.Multiply:
                        Session["Captcha" + prefix] = a*b;
                        captcha = string.Format("{0} x {1} = ?", a, b);
                        break;
                    default:
                        Session["Captcha" + prefix] = a + b;
                        captcha = string.Format("{0} + {1} = ?", a, b);
                        break;
                }

                //logger.Debug("image stream");
                using (var mem = new MemoryStream())
                using (var bmp = new Bitmap(240, 60))
                using (var gfx = Graphics.FromImage(bmp))
                {
                    gfx.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    gfx.SmoothingMode = SmoothingMode.AntiAlias;
                    gfx.FillRectangle(Brushes.White, new Rectangle(0, 0, bmp.Width, bmp.Height));

                    //add noise
                    if (noisy)
                    {
                        logger.Info("adding noise");
                        int i;
                        var pen = new Pen(Color.LightYellow);
                        for (i = 1; i < 10; i++)
                        {
                            pen.Color = Color.FromArgb(
                                (rand.Next(0, 255)),
                                (rand.Next(0, 255)),
                                (rand.Next(0, 255)));

                            int r = rand.Next(0, (240/3));
                            int x = rand.Next(0, 240);
                            int y = rand.Next(0, 60);

                            x = x - r;
                            y = y - r;
                            gfx.DrawEllipse(pen, x, y, r, r);
                        }
                    }

                    //add question
                    //logger.Debug("adding question");
                    gfx.DrawString(captcha, new Font("Tahoma", 28), Brushes.LightSlateGray, 10, 10);

                    //render as Jpeg
                    //logger.Debug("render as Jpeg");
                    bmp.Save(mem, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //img = File(mem.GetBuffer(), "image/Jpeg");
                    
                    byte[] imageBytes = mem.ToArray();

                    // Convert byte[] to Base64 String
                    //logger.Debug("Convert byte[] to Base64 String");
                    string base64String = Convert.ToBase64String(imageBytes);

                    //logger.Debug("data:image/jpg;base64,");
                    return Content("data:image/jpg;base64," + base64String);

                }

            }
            catch (Exception ex)
            {
                if(logger != null)
                    logger.Error(ex);
                throw new HttpException(404, "Not found");
            }

        }

        [DoNotLogActionFilter]
        public ActionResult UnSubscribe(int id, int forumid, int catid, string userid)
        {
            string returnUrl = "";

            if (Request.UrlReferrer != null)
                returnUrl = Request.UrlReferrer.PathAndQuery;

            Subscriptions.UnSubscribe(catid, forumid, id, WebSecurity.GetUserId(userid));
            if (returnUrl != "")
                return RedirectToLocal(returnUrl);

            return RedirectToAction("UserProfile", new {id = userid});

        }

        public ActionResult ViewProfile(int id)
        {
            MemberViewModel vm = new MemberViewModel();
            using (SnitzMemberContext db = new SnitzMemberContext())
            {
                UserProfile user = db.GetUser(id);
                if (user == null)
                {
                    return HttpNotFound();
                }
                vm.User = user;
                vm.EnabledProfileFields = UserProfileExtensions.EnabledProfileFields();

                return PartialView("popViewProfile", vm);
            }
        }

        [HttpGet]
        [Authorize]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult EmailMember(int id)
        {
            int minpostcount = 0;

            if (ClassicConfig.GetValue("INTMAXPOSTSTOEMAIL") != String.Empty)
                minpostcount = ClassicConfig.GetIntValue("INTMAXPOSTSTOEMAIL");

            var toMember = Member.GetById(id);
            var from = WebSecurity.GetCurrentUser(); // Member.GetById(WebSecurity.CurrentUserId);

            if (from.PostCount < minpostcount && !(User.IsAdministrator() || User.IsModerator() || from.AllowSendEmail))
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("BelowEmailThreshold", "General");
                return View("Error");
            }
            if (toMember.ReceiveEmails != 1 && !(User.IsAdministrator() || User.IsModerator()))
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("DonotEmail", "General");
                return View("Error");
            }
            var model = new EmailModel
            {
                //ReturnUrl = Request.UrlReferrer.AbsoluteUri,
                           
                AdminEmail = false,
                ToEmail = toMember.Email,
                ToName = toMember.Username,
                FromName = from.UserName,
                FromEmail = from.Email
            };

            return PartialView("popEmailMember", model);

        }
        [HttpGet]
        [Authorize]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult TestEmail(int id)
        {

            var toMember = Member.GetById(id);
            var from = ClassicConfig.ForumEmail; // Member.GetById(WebSecurity.CurrentUserId);

            var model = new EmailModel
            {
                //ReturnUrl = Request.UrlReferrer.AbsoluteUri,
                           
                AdminEmail = true,
                ToEmail = toMember.Email,
                ToName = toMember.Username,
                FromName = "Forum Administrator",
                FromEmail = from
            };

            return PartialView("popEmailMember", model);

        }

        [HttpGet]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public PartialViewResult EmailAdministrator()
        {
            var model = new EmailModel
            {
                //ReturnUrl = Request.UrlReferrer.AbsoluteUri,
                ToEmail = ClassicConfig.GetValue("STRCONTACTEMAIL"),
                ToName = "Forum Administrator",
                AdminEmail = true
            };

            return PartialView("popEmailMember", model);

        }

        [HttpPost]
        public ActionResult EmailMember(EmailModel model)
        {
            if (!model.AdminEmail && !User.Identity.IsAuthenticated)
            {
                Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return Json("You must be logged in to email members");
            }
            if (!model.AdminEmail)
            {
                int minpostcount = 0;
                if (ClassicConfig.GetValue("INTMAXPOSTSTOEMAIL") != String.Empty)
                    minpostcount = ClassicConfig.GetIntValue("INTMAXPOSTSTOEMAIL");
                var from = WebSecurity.GetCurrentUser();
                if (!(User.IsAdministrator() || User.IsModerator()) && from.PostCount < minpostcount )
                {
                    ViewBag.Error = ResourceManager.GetLocalisedString("BelowEmailThreshold", "General");
                    return View("Error");
                }
                
            }
            if (ModelState.IsValid)
            {
                try
                {

                    EmailController.SendMemberEmail(ControllerContext,model);
                    ViewBag.Sent = true;
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new {success = true, responseText = "OK"});
                    //return new HttpStatusCodeResult(HttpStatusCode.NoContent);

                }
                catch (Exception ex)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    //return Json(ex.Message);
                    throw;
                }

            }
            return PartialView("popEmailMember", model);
        }

        public ActionResult ChangeCulture(string lang)
       {
           if (Request.UrlReferrer != null)
           {
               //var routinfo = Common.GetReferrRouteData(Request.UrlReferrer.ToString());

               SnitzCookie.SetCookie("SnitzCulture",lang);
               SessionData.Set("Culture", new CultureInfo(lang, false));
               SessionData.Clear("SnitzMenu");
               return RedirectToLocal(Request.UrlReferrer.AbsoluteUri);
           }
           return RedirectToAction("Index", new { pagenum = 1 });
        }

        public PartialViewResult ChangePassword()
        {
            return PartialView("popChangePassword", new LocalPasswordModel());
        }

        public ActionResult Search(int pagenum, string sortOrder, string sortCol, FormCollection form)
        {
            if (String.IsNullOrWhiteSpace(form["Username"])
                && String.IsNullOrWhiteSpace(form["Firstname"])
                && String.IsNullOrWhiteSpace(form["Lastname"])
                && String.IsNullOrWhiteSpace(form["Email"])
                && String.IsNullOrWhiteSpace(form["Title"])
                && String.IsNullOrWhiteSpace(form["LastVisit"])
                && String.IsNullOrWhiteSpace(form["LastPost"])
                && String.IsNullOrWhiteSpace(form["Registered"])
                && ! String.IsNullOrWhiteSpace(form["confirmed"]))
            {
                ViewBag.ErrTitle = ResourceManager.GetLocalisedString("srchHeading", "ErrorMessage");//"Search Error";
                ViewBag.Error = ResourceManager.GetLocalisedString("NoValues", "ErrorMessage"); //"No values provided";
            }

            if (form.AllKeys.Contains("new-page") && !String.IsNullOrWhiteSpace(form["new-page"]))
            {
                pagenum = Convert.ToInt32(form["new-page"]);
                sortCol = form["SortCol"];
                sortOrder = form["sortOrder"] == "desc" ? "asc" : "desc";;

            }
            ViewBag.SortDir = sortOrder == "desc" ? "asc" : "desc";
            ViewBag.SortCol = sortCol;
            ListUserViewModel vm = new ListUserViewModel();

            ViewData["LastVisitDateTime"] = LastVisitDate();

            Page<Member> members = MemberManager.SearchMembers(User.IsAdministrator(),pagenum,sortOrder,sortCol,form);
            vm.Users = members.Items;
            vm.SearchForm = new UserSearchViewModel()
            {
                Username = form["Username"],
                Email = form["Email"],
                Firstname = form["Firstname"],
                Lastname = form["Lastname"],
                Title = form["Title"]
                
            };
            vm.Initial = form["Initial"];
            ViewBag.PageCount = members.TotalPages;
            ViewBag.Page = members.CurrentPage;

            return View("Index", vm);

        }

        public ActionResult EmailUsers(int pagenum, string sortOrder, string sortCol, FormCollection form)
        {
            if (String.IsNullOrWhiteSpace(form["EmailMessage"])
                || String.IsNullOrWhiteSpace(form["EmailSubject"])
            )
            {
                ViewBag.ErrTitle = ResourceManager.GetLocalisedString("emailHeader", "ErrorMessage");  //"Send Email Error";
                ViewBag.Error = ResourceManager.GetLocalisedString("EmailError", "ErrorMessage");  //"No Subject or Message provided";
            }

            ViewBag.SortDir = sortOrder == "desc" ? "asc" : "desc";
            ViewBag.SortCol = sortCol;
            ListUserViewModel vm = new ListUserViewModel();

            ViewData["LastVisitDateTime"] = LastVisitDate();

            BackgroundJob.Enqueue(() => EmailController.EmailMembers(ControllerContext,User.IsAdministrator(), "DefEmail", form, sortCol, sortOrder));

            var test = MemberManager.SearchMembers(User.IsAdministrator(), pagenum, sortOrder, sortCol, form);
            vm.Users = test.Items;
            ViewBag.PageCount = test.TotalPages;
            ViewBag.Page = test.CurrentPage;
            return View("Index", vm);

        }

        public ActionResult ClearCookies()
        {
            SnitzCookie.ClearAll();
            return RedirectToAction("Index","Home");
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public PartialViewResult ShowIP(string ip)
        {
            ViewBag.IPAddress = ip;
            ViewBag.HostName = Common.GetReverseDNS(ip, 5); //Dns.GetHostEntry(Model.IpAddress).HostName;
            return PartialView("popUserIP");
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult LockUser(int user)
        {
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                var profile = udb.GetUser(user);
                
                profile.Status = 0;
                profile.Disabled = true;
                udb.SaveChanges();
                //Roles.AddUserToRole(profile.UserName, "Disabled");
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = fromPage != null ? Request.UrlReferrer.AbsoluteUri : Url.Action("Index", new { pagenum = 1 });

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public JsonResult UnLockUser(int user)
        {
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                var profile = udb.GetUser(user);
                profile.Status = 1;
                profile.Disabled = false;
                udb.SaveChanges();
                //Roles.RemoveUserFromRole(profile.UserName, "Disabled");
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;
            }
            else
            {
                redirectUrl = Url.Action("Index", new { pagenum = 1 });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //if (fromPage != null)
            //{
            //    return RedirectToLocal(fromPage.PathAndQuery);
            //}
            //return RedirectToAction("Index",new{pagenum=1});
        }

        [Authorize(Roles = "Administrator")]
        public JsonResult DeleteMember(int id)
        {
            string username;
            UserProfile profile;

            if (id == WebSecurity.CurrentUserId)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ResourceManager.GetLocalisedString("DelSelfErr", "ErrorMessage"));
                //ViewBag.Error = ResourceManager.GetLocalisedString("DelSelfErr", "ErrorMessage");
                //return View("Error");
            }

            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                profile = udb.GetUser(id);
                username = profile.UserName;
                if (username != "zapped" && username != "n/a")
                {
                    if (WebSecurity.IsUserInRole(username, "Administrator"))
                    {
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(ResourceManager.GetLocalisedString("DelAdminErr", "ErrorMessage"));
                        //ViewBag.Error = ResourceManager.GetLocalisedString("DelAdminErr", "ErrorMessage");
                        //return View("Error");                    
                    }                    
                    if (Roles.GetRolesForUser(username).Any())
                    {
                        Roles.RemoveUserFromRoles(username, Roles.GetRolesForUser(username));
                    }
                }
            }

            //Remove the member from the moderator table
                ForumModerators.RemoveMember(id);
            //Remove any subscriptions this member has in the Subscriptions table
                Subscriptions.RemoveMember(id);
                //Remove the member from the Allowed Members and Roles table
                AllowedMembers.Remove(id);
            if (!Member.HasActivePosts(id))
            {
                try
                {
                    if (username != "zapped" && username != "n/a")
                    {
                        WebSecurity.DeleteUser(username);
                    }
                    else
                    {
                        WebSecurity.DeleteUser(profile);
                    }
                    ForumTotals.DeleteUser();
                }
                catch(Exception e)
                {
                    throw new Exception("Problem deleting user:" + username, e.InnerException);
                }

            }
            else if (username != "zapped" && username != "n/a")
            {
                //Zap the user 
                //return ZapUser(id);
                UserProfile blank = new UserProfile();
                var token = WebSecurity.GeneratePasswordResetToken(username,1440);
                WebSecurity.ResetPassword(token, token);
                using (SnitzMemberContext udb = new SnitzMemberContext())
                {
                    profile = udb.GetUser(id);
                    if (!Roles.IsUserInRole("Disabled"))
                        Roles.AddUserToRole(username, "Disabled");
                    blank.CopyProperties(profile, new[] { "UserId", "UserName", "Password","PostCount", "Created", "CreatedDate", "LastPost", "LastPostDate" ,"AnonymousUser"});
                    
                    profile.Email = "Deleted Member";
                    profile.ForumTitle = "Deleted Member";
                    profile.UserName = "n/a";
                    profile.UserLevel = 1;
                    profile.Status = 0;
                    
                    udb.SaveChanges();
                    
                }       
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;
            }
            else
            {
                redirectUrl = Url.Action("Index", new { pagenum = 1 });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //var fromPage = Request.UrlReferrer;
            //if (fromPage != null)
            //{
            //    return RedirectToLocal(fromPage.PathAndQuery);
            //}
            //return RedirectToAction("Index",new{pagenum=1});
        }

        [Authorize(Roles = "Administrator")]
        public JsonResult ZapUser(int id)
        {
            UserProfile blank = new UserProfile();
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                var profile = udb.GetUser(id);
                var token = WebSecurity.GeneratePasswordResetToken(profile.UserName,1440);
                WebSecurity.ResetPassword(token, token);
                if (profile.UserName != "zapped")
                {
                    Roles.RemoveUserFromRoles(profile.UserName,Roles.GetRolesForUser(profile.UserName));
                    if (!Roles.IsUserInRole(profile.UserName,"Disabled"))
                        Roles.AddUserToRole(profile.UserName, "Disabled");
                }
                

                blank.CopyProperties(profile, new[]{"UserId", "UserName", "Password","PostCount", "Created", "CreatedDate","LastPost", "LastPostDate", "AnonymousUser" });
                profile.Email = "Zapped@Member";
                profile.ForumTitle = "Zapped Member";
                profile.UserName = "zapped";
                profile.Status = 0;
                profile.UserLevel = 1;
                udb.SaveChanges();                
            }
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;
            }
            else
            {
                redirectUrl = Url.Action("Index", new { pagenum = 1 });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Administrator")]
        public JsonResult ResendValidation(int id)
        {
            var member = MemberManager.GetUser(id);

            EmailController.ResendConfirmationEmail(ControllerContext, member.UserName);
            var fromPage = Request.UrlReferrer;
            string redirectUrl = "";
            if (fromPage != null)
            {
                redirectUrl = Request.UrlReferrer.AbsoluteUri;
            }
            else
            {
                redirectUrl = Url.Action("Index", new { pagenum = 1 });
            }

            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult DeletePendingMember(string username)
        {
            WebSecurity.DeleteUser(username);

            return RedirectToAction("PendingMembers", "Admin");
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteSelected()
        {
            var res = Request.Form["memberSelect"].Split(',');
            foreach (var s in res)
            {
                WebSecurity.DeleteUser(s);
            }

            return RedirectToAction("PendingMembers", "Admin");
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult ApproveSelected()
        {
            var pending = Request.Form["memberSelect"].Split(',');

                using (SnitzMemberContext db = new SnitzMemberContext())
                {
                    foreach (var mem in pending)
                    {
                        UserProfile user = db.GetUser(mem);
                        var confirmationToken = db.GetEmailConfirmToken(user.UserName);

                        if ( ClassicConfig.AllowEmail && ClassicConfig.EmailValidation && !String.IsNullOrWhiteSpace(confirmationToken.Token))
                        {
                            user.UserLevel = 1;
                            db.SaveChanges();
                            EmailController.SendApprovedEmailConfirmation(ControllerContext, user.Email, user.UserName, confirmationToken.Token, user.UserId);
                        }
                        else
                        {
                            var member = db.GetMembership(user.UserId);
                            member.IsConfirmed = true;
                            user.UserLevel = 1;
                            user.Status = 1;
                            user.LastActivity = DateTime.UtcNow.ToSnitzDate();
                            db.SaveChanges();
                            if(ClassicConfig.AllowEmail)
                                EmailController.SendApprovalConfirmation(ControllerContext, user.Email, user.UserName, confirmationToken.Token, user.UserId);
                            ForumTotals.AddUser();
                        }
                    }                
                }

            return RedirectToAction("PendingMembers", "Admin");
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult SavePendingMember(FormCollection form)
        {
            int uid = Convert.ToInt32(form["item.UserId"]);
            string email = form["item.Email"];
            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                var profile = udb.UserProfiles.SingleOrDefault(u => u.UserId == uid);
                if (profile != null)
                {
                    profile.Email=email;
                    udb.SaveChanges();
                }
            }
            return RedirectToAction("PendingMembers", "Admin");
        }

    }
}

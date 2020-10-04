using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using log4net;
using LangResources.Utility;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using WebMatrix.WebData;
using Member = SnitzDataModel.Models.Member;
using System.Linq;
using System.Web.Routing;
using SnitzCore.Filters;

namespace SnitzDataModel.Controllers
{

    public class BaseController : Controller
    {
        protected SnitzDataContext Dbcontext;
        ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// LastVisitCookie - get r set the cookie
        /// </summary>
        internal string LastVisitCookie
        {
            get { return SnitzCookie.GetLastVisitDate(); }
            set { SnitzCookie.SetLastVisitCookie(value); }
        }

        private StringBuilder _responseContent = new StringBuilder();

        private MemoryStream filter_TransformStream(MemoryStream ms)
        {

            Regex endOfHtml = new Regex("</html>", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex poweredTag = new Regex(@"\[POWERED]", RegexOptions.Compiled | RegexOptions.Multiline);
            Regex timerTag = new Regex(@"\[ExecutionTime]", RegexOptions.Compiled | RegexOptions.Multiline);
            Regex emptyLines = new Regex(@"(^\s*(\r\n|\n|\r))|^\s*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
            Regex textArea = new Regex(@"<textarea[\s\S]+?<\/textarea>");

            try
            {
                Encoding encoding = HttpContext.Response.ContentEncoding;
                string contentInBuffer = encoding.GetString(ms.ToArray());
                _responseContent.Append(contentInBuffer);

                if (endOfHtml.IsMatch(_responseContent.ToString()))
                {

                    string contentWithPowered = poweredTag.Replace(_responseContent.ToString(), ClassicConfig.PoweredBy);

                    Stopwatch timer = (Stopwatch) System.Web.HttpContext.Current.Items["Timer"];
                    if (timer != null)
                    {
                        if (timer.IsRunning)
                            timer.Stop();
                        double seconds = (double) timer.ElapsedTicks/Stopwatch.Frequency;
                        string resultTime = string.Format("{0:F4}", seconds);

                        if (ClassicConfig.GetIntValue("INTPAGETIMER") == 1)
                        {
                            contentWithPowered = timerTag.Replace(contentWithPowered,
                                string.Format(ResourceManager.GetLocalisedString("pageGeneration", "General"), resultTime));
                        }
                        else
                        {
                            contentWithPowered = timerTag.Replace(contentWithPowered,"");
                        }

                    }
                    else
                    {
                        contentWithPowered = timerTag.Replace(contentWithPowered, "");
                    }
                    if (!textArea.IsMatch(contentWithPowered))
                    {
                        contentWithPowered = emptyLines.Replace(contentWithPowered, @"");
                    }
                    byte[] outputBuffer = encoding.GetBytes(contentWithPowered);
                    ms = new MemoryStream(outputBuffer.Length);
                    ms.Write(outputBuffer, 0, outputBuffer.Length);
                }
            }
            catch (Exception e)
            {
                //ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("filter_TransformStream error : " + e.Message);
            }

            return ms;
        }

        [NonAction]
        public DateTime LastVisitDate()
        {
            if (SessionData.Contains("_LastVisit"))
            {
                logger.Debug(Session.SessionID + "-LastVisitDate from session:");
                string lastvisit = SessionData.Get<string>("_LastVisit");

                var dateTime = lastvisit.ToDateTime();
                if (dateTime != null) return dateTime.Value;
            }
            else if (LastVisitCookie != null)
            {
                var dateTime = LastVisitCookie.ToDateTime();
                logger.Debug(Session.SessionID + "-LastVisitDate from cookie:");
                SessionData.Set("_LastVisit", LastVisitCookie);
                if (dateTime != null) return dateTime.Value;
            }
            //logger.Error(Session.SessionID + "-LastVisitDate set to Now?:");
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Refresh the antiforgery token
        /// </summary>
        /// <returns></returns>
        public PartialViewResult RefreshToken()
        {
            return PartialView("_AntiForgeryToken");
        }
        [HttpGet]
        public ActionResult About()
        {
            // do whatever you need to get your model
            return PartialView("popAbout");
        }
        [HttpGet]
        public ActionResult License()
        {
            // do whatever you need to get your model
            return PartialView("popLicense");
        }
        [HttpGet]
        public ActionResult Privacy()
        {
            return PartialView("popPrivacyPolicy");
        }
        protected RouteData UrlToRouteValueDictionary(string path, string query)
        {

            var request = new HttpRequest(null, path.Replace(query,""), query.Replace("?",""));
            var response = new HttpResponse(new StringWriter());
            var httpContext = new HttpContext(request, response);
            var routeData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(httpContext));
            RouteValueDictionary values = routeData.Values;
            RouteValueDictionary qs = request.QueryString.ToRouteValues();
            RouteValueDictionary test = values.Extend(qs);
            // The following should be true for initial version of mvc app.
            return routeData;
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            #region Requires login

            if (ClassicConfig.RequireRegistration && !User.Identity.IsAuthenticated)
            {
                if (
                    !(filterContext.RouteData.Values.ContainsValue("Login") ||
                      filterContext.RouteData.Values.ContainsValue("Account")))
                {
                    filterContext.Result = new RedirectResult("~/Account/Login");
                    return;
                }
            }

            #endregion


            #region Update Last Visit cookie

            try
            {
                if (!SessionData.Contains("_LastVisit") && !filterContext.IsChildAction)
                {
                    logger.Debug(Session.SessionID + "-No _LastVisit Session:" + filterContext.ActionDescriptor.ActionName);
                    //Is there a last vist cookie
                    if (LastVisitCookie != null)
                    {
                        var visit = LastVisitCookie;
                        logger.Debug(Session.SessionID + "-Cookie Exists:" + visit);
                        //check that it matches the db and if not use the newest one
                        if (WebSecurity.IsAuthenticated)
                        {
                            if (Dbcontext == null)
                            {
                                Dbcontext = new SnitzDataContext();
                            }
                            Member member = Dbcontext.GetById<Member>(WebSecurity.CurrentUserId);
                            if (member != null)
                            {
                                DateTime? dbvisit = member.LastVisitDate;
                                var dbactivity = member.LastActivityDate;
                                var cookievisit = LastVisitCookie.ToDateTime();
                                var lastdate = new[] { dbvisit.Value, dbactivity.Value, cookievisit.Value }.Max();
                                logger.Debug(member.Username + String.Format(" dbvisit:{0} dbactivity:{1} cookie:{2}",dbvisit.Value, dbactivity.Value, cookievisit.Value));
                                //
                                if (dbvisit.HasValue)
                                {
                                    visit = (LastVisitCookie.ToDateTime() > dbvisit.Value.AddMinutes(15))
                                        ? LastVisitCookie
                                        : dbactivity.HasValue && (dbactivity.Value > dbvisit.Value.AddMinutes(15))
                                            ? dbactivity.Value.ToSnitzDate()
                                            : dbvisit.Value.ToSnitzDate();
                                }
                                //lastheredate more than 30 minutes ago then update database as probably a new visit
                                logger.Debug(member.Username + "-lastvisit:" + visit);
                                if (visit.ToDateTime().Value > dbvisit.Value.AddMinutes(30))
                                {
                                    logger.Debug(member.Username + "-Updating DB:" + visit);
                                    Dbcontext.Execute("UPDATE " + Dbcontext.MemberTablePrefix + "MEMBERS SET M_LASTHEREDATE=@0 WHERE MEMBER_ID=@1",
                                        visit, WebSecurity.CurrentUserId);
                                }
                            }
                        }

                        SessionData.Set("_LastVisit", visit);
                        logger.Debug("Set _LastVisit Session from cookie:" + visit);
                    }
                    else if (LastVisitCookie == null && WebSecurity.IsAuthenticated)
                    {
                        logger.Debug(Session.SessionID + "-No LastVisitCookie:");
                        if (Dbcontext == null)
                        {
                            Dbcontext = new SnitzDataContext();

                        }
                        Member member = Dbcontext.GetById<Member>(WebSecurity.CurrentUserId);
                        if (member != null)
                        {
                            var dbvisit = member.LastVisitDate;
                            var dbactivity = member.LastActivityDate;
                            if (dbvisit.HasValue)
                            {
                                if (dbactivity.HasValue)
                                {
                                    var visit = (dbactivity.Value > dbvisit.Value)
                                        ? dbactivity.Value.ToSnitzDate()
                                        : dbvisit.Value.ToSnitzDate();
                                    SessionData.Set("_LastVisit", visit);
                                    logger.Debug(member.Username + "Set _LastVisit Session from dbactivity:" + visit);
                                }
                                else
                                {
                                    SessionData.Set("_LastVisit", dbvisit.Value.ToSnitzDate());
                                    logger.Debug(member.Username + "Set _LastVisit Session from dbvisit:" + dbvisit.Value.ToSnitzDate());
                                }

                            }

                            LastVisitCookie = SessionData.Get<string>("_LastVisit");
                            logger.Debug(member.Username + "-SessionLastVisitCookie:" + LastVisitCookie);
                        }
                        //set the last visit cookie now 

                    }
                }
                else
                {
                    //should we update our cookie here??
                    var exclude = new List<string>(){"PMNotify","UpdateLastActivityDate","ForumStats","DownLoads","OnlineUsers","ThemeChanger","FeaturedImage","FeaturedPoll","GetUpcomingEvents","GetBirthDays","GetHolidays"};

                    if (!filterContext.IsChildAction && !exclude.Contains(filterContext.ActionDescriptor.ActionName))
                    {
                        LastVisitCookie = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                        logger.Debug("Session _LastVisit:" + filterContext.ActionDescriptor.ActionName + "-Set LastVisitCookie:" +LastVisitCookie);
                    }
                }
            }
            catch (Exception e)
            {
                //there was a problem so set it to now
                logger.Error(Session.SessionID + "-There was an error : " + e.Message);
                //LastVisitCookie = DateTime.UtcNow.ToSnitzDate();
            }

            #endregion


            ViewBag.SubmitButton = ResourceManager.GetLocalisedString("btnSave", "labels");
            filterContext.Controller.ViewData["LastVisitDateTime"] = LastVisitDate();

            if (!filterContext.IsChildAction)
            {
                #region Response Filter
                var originalFilter = filterContext.HttpContext.Response.Filter;
                ResponseFilterStream filter = new ResponseFilterStream(originalFilter);
                filter.TransformStream += filter_TransformStream;
                filterContext.HttpContext.Response.Filter = filter;


                if (TempData["successMessage"] != null)
                {
                    ViewBag.Success = TempData["successMessage"].ToString();
                    TempData["successMessage"] = null;
                }
                if (TempData["errMessage"] != null)
                {
                    ViewBag.ErrTitle = TempData["errTitle"];
                    ViewBag.Error = "BaseController:" + TempData["errMessage"];
                }

                #endregion
            }


            base.OnActionExecuting(filterContext);

        }
        protected override void HandleUnknownAction(string actionName)
        {
            // Avoid IIS7 getting in the middle
            Response.TrySkipIisCustomErrors = true; 
            throw new HttpException(404, actionName + " not recognised");
            //this.View(actionName).ExecuteResult(this.ControllerContext);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //dispose of the data context
                if(Dbcontext != null)
                    Dbcontext.Dispose();
            }
            base.Dispose(disposing);
        }


    }
    public static class RouteUtils
    {
        public static RouteValueDictionary ToRouteValues(this NameValueCollection queryString)
        {
            if (queryString == null || queryString.HasKeys() == false) return new RouteValueDictionary();

            var routeValues = new RouteValueDictionary();
            foreach (string key in queryString.AllKeys)
                routeValues.Add(key, queryString[key]);

            return routeValues;
        }

        public static RouteValueDictionary Extend(this RouteValueDictionary dest,
            IEnumerable<KeyValuePair<string, object>> src)
        {
            src.ToList().ForEach(x => { dest[x.Key] = x.Value; });
            return dest;
        }
        public static RouteValueDictionary ToRouteValues(this NameValueCollection col, Object obj)
        {
            var values = new RouteValueDictionary(obj);
            if (col != null)
            {
                foreach (string key in col)
                {
                    //values passed in object override those already in collection
                    if (key != null && !values.ContainsKey(key)) values[key] = col[key];
                }
            }
            return values;
        }

    }

}

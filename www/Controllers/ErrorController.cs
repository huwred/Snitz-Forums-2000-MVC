using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using SnitzDataModel.Models;


namespace WWW.Controllers
{

    public class ErrorController : Controller
    {
        //
        // GET: /Error/

        [ValidateInput(false)]
        public ActionResult Index(HandleErrorInfo exception)
        {
            //Exception ex = Server.GetLastError();
            if (exception.Exception is InvalidCastException)
            {
                if (Request.Url != null) return Redirect(Request.Url.AbsoluteUri );
            }
            Response.StatusCode = 500;
            return View("Error",exception);
        }
        public ActionResult NotFound(HandleErrorInfo exception = null)
        {

            //if (Request.Url != null)
            //{
            //    var requestedPath = Request.Url.AbsoluteUri;

            //    if (requestedPath.Contains("active.asp") || requestedPath.Contains("topic.asp"))
            //    {
            //        if (requestedPath.Contains("active.asp"))
            //        {
            //            Response.StatusCode = 301;
            //            return Redirect(Config.ForumUrl + "Topic/Active");
            //        }
            //        if (requestedPath.Contains("topic.asp"))
            //        {
            //            var pattern = @"(?:TOPIC_ID=)(\d+)#*(\d+)*";

            //            var match = Regex.Match(requestedPath, pattern);
            //            if (match.Success)
            //            {
            //                var topicid = match.Groups[1].Value;
            //                string replyid = "";
            //                if (match.Groups.Count > 1)
            //                {
            //                    replyid = "#" + match.Groups[2].Value;
            //                }
            //                Response.StatusCode = 301;
            //                return Redirect(Config.ForumUrl + "Topic/Posts/" + topicid + "?pagenum=-1" + replyid);                        
            //            }

            //        }
            //        //Response.StatusCode = 301;
            //        //return Redirect(Config.ForumUrl);
            //    }
            //}
            Response.CacheControl = "no-cache";
            Response.StatusCode = 404;  //you may want to set this to 200
            return View(exception); 
        }

        public ViewResult Forbidden(HandleErrorInfo exception)
        {
            var httpException = exception.Exception as HttpException;

            if (httpException != null) Response.StatusCode = httpException.GetHttpCode();

            return View(exception); 
        }

        public ActionResult Status301()
        {
            var routeValues = this.Request.RequestContext.RouteData.DataTokens["routeValues"];
            var url = this.GetAbsoluteUrl(routeValues);

            Response.CacheControl = "no-cache";
            Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            Response.RedirectLocation = url;

            ViewBag.DestinationUrl = url;
            return View();
        }

        private string GetAbsoluteUrl(object routeValues)
        {
            var requestedPath = Request.Url.AbsoluteUri.ToLower();

            var urlBuilder = new UriBuilder(requestedPath)
            {
                Path = Url.RouteUrl(routeValues)
            };
            if (requestedPath.Contains("topic.asp"))
            {
                var pattern = @"(?:TOPIC_ID=)(\d+)#*(\d+)*";

                var match = Regex.Match(requestedPath, pattern,RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var topicid = match.Groups[1].Value;
                    string replyid = "";
                    if (match.Groups.Count > 1 && !String.IsNullOrWhiteSpace(match.Groups[2].Value))
                    {
                        replyid = "#" + match.Groups[2].Value;
                    }
                    //Response.StatusCode = 301;
                    //return Redirect(Config.ForumUrl + "Topic/Posts/" + topicid + "?pagenum=-1" + replyid);
                    urlBuilder.Path += "/" + topicid;
                    //does the topic exist ? if not assume it's archived
                    if (Topic.Exists(topicid))
                    {
                        urlBuilder.Query = "pagenum=-1" + replyid;
                    }
                    else
                    {
                        urlBuilder.Query = "pagenum=-1&archived=1" + replyid;
                    }
                    //urlBuilder.Query = "pagenum=-1" + replyid;
                }

            }
            if (requestedPath.Contains("forum.asp"))
            {
                var pattern = @"(?:FORUM_ID=)(\d+)#*(\d+)*";

                var match = Regex.Match(requestedPath, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var forumid = match.Groups[1].Value;

                    //Response.StatusCode = 301;
                    //return Redirect(Config.ForumUrl + "Topic/Posts/" + topicid + "?pagenum=-1" + replyid);
                    urlBuilder.Path += "/" + forumid;
                    urlBuilder.Query = "pagenum=-1";
                }

            }
            //pop_slideshow.asp
            var encodedAbsoluteUrl = urlBuilder.Uri.ToString();
            return HttpUtility.UrlDecode(encodedAbsoluteUrl);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using WWW.Filters;
using WWW.Models;

namespace WWW.Controllers
{
    public class RssFeedController : CommonController
    {
        public ActionResult Active()
        {
            var feed = RssFeed.ActiveFeed(ControllerContext.RequestContext.HttpContext.Request);
            if (feed == null)
            {
                return RedirectToAction("NotFound", "Error", new HandleErrorInfo(new HttpException(404, LangResources.Utility.ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage")), "RssFeedController", "Active"));
            }
            return new RssActionResult { Feed = feed };
        }

        public ActionResult Topic(int id)
        {
            var feed = RssFeed.TopicFeed(id, ControllerContext.RequestContext);
            if (feed == null)
            {
                return RedirectToAction("NotFound", "Error", new HandleErrorInfo(new HttpException(404, LangResources.Utility.ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage")), "RssFeedController", "Topic"));
            }
            return new RssActionResult { Feed = feed };
        }

        public ActionResult Forum(ICollection<int> id)
        {
            var feed = RssFeed.ForumFeed(id.ToList(), ControllerContext.RequestContext);
            if (feed == null)
            {
                return RedirectToAction("NotFound", "Error", new HandleErrorInfo(new HttpException(404, LangResources.Utility.ResourceManager.GetLocalisedString("InvalidID", "ErrorMessage")), "RssFeedController", "Forum"));
            }
            return new RssActionResult { Feed = feed };
        }
    }

}

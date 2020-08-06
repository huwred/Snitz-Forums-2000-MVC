using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace WWW.Filters
{
    public class RssActionResult : ActionResult
    {
        public SyndicationFeed Feed { get; set; }
        public override void ExecuteResult(ControllerContext context)
        {
            if (Feed == null)
            {
                throw new HttpException(404, "HTTP/1.1 404 Not Found");
            }
            context.HttpContext.Response.ContentType = "application/rss+xml";
            
            var rssFormatter = new Atom10FeedFormatter(Feed);
            using (var writer = XmlWriter.Create(context.HttpContext.Response.Output))
            {
                rssFormatter.WriteTo(writer);
            }
        }
    }

}
using System;
using System.Web.Mvc;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Security;
using System.IO;

namespace WWW.Controllers
{
    public class RssWidgetController : Controller
    {
        public ActionResult Index(string feed, string template="", int items = 3)
        {
            string errorString;

            try
            {
                if (String.IsNullOrEmpty(feed))
                {
                    throw new ArgumentNullException(nameof(feed));
                }
                using (XmlReader reader = XmlReader.Create(feed))
                {
                    RssWidgetViewModel vm = new RssWidgetViewModel();
                    SyndicationFeed rssData = SyndicationFeed.Load(reader);
                    vm.RssFeed = rssData;
                    vm.Template = template;
                    vm.ViewItems = items;
                    return PartialView("RSSView",vm);
                }
                
            }
            catch (ArgumentNullException)
            {
                errorString = "No url for Rss feed specified.";
            }
            catch (SecurityException)
            {
                errorString = "You do not have permission to access the specified Rss feed.";
            }
            catch (FileNotFoundException)
            {
                errorString = "The Rss feed was not found.";
            }
            catch (UriFormatException)
            {
                errorString = "The Rss feed specified was not a valid URI.";
            }
            catch (Exception)
            {
                errorString = "An error occured accessing the RSS feed.";
            }

            var errorResult = new ContentResult {Content = errorString};
            return errorResult;

        }
    }

    public class RssWidgetViewModel
    {
        public SyndicationFeed RssFeed { get; set; }
        public String Template { get; set; }
        public int ViewItems { get; set; }
    }
}
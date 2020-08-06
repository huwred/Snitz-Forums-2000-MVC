using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Sitemap;
using SnitzCore.Utility;
using SnitzDataModel.Controllers;
using SnitzDataModel.Database;
using SnitzMembership;

namespace wwwTest.Controllers
{
    /// <summary>
    /// Generates an XML sitemap from a collection of <see cref="ISitemapItem"/>
    /// </summary>
    public class SitemapResult : ActionResult
    {
        private readonly IEnumerable<ISitemapItem> _items;
        private readonly ISitemapGenerator _generator;

        public SitemapResult(IEnumerable<ISitemapItem> items)
            : this(items, new SitemapGenerator())
        {
        }

        public SitemapResult(IEnumerable<ISitemapItem> items, ISitemapGenerator generator)
        {
            //Ensure.Argument.NotNull(items, "items");
            //Ensure.Argument.NotNull(generator, "generator");

            this._items = items;
            this._generator = generator;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;

            response.ContentType = "text/xml";
            response.ContentEncoding = Encoding.UTF8;

            using (var writer = new XmlTextWriter(response.Output))
            {
                writer.Formatting = Formatting.Indented;
                var sitemap = _generator.GenerateSiteMap(_items);

                sitemap.WriteTo(writer);
            }
        }
    }
    public class SitemapController : BaseController
    {
        // GET: /Sitemap/
        [OutputCache(Duration = 300, VaryByParam = "none", Location = OutputCacheLocation.Client, NoStore = true)]
        public SitemapResult Index()
        {
            var sitemapItems = new List<SitemapItem>
            {
                new SitemapItem(Url.QualifiedAction("allforums", "home"), changeFrequency: SitemapChangeFrequency.Monthly, priority: 0.9),
                new SitemapItem(Url.QualifiedAction("index", "home"), changeFrequency: SitemapChangeFrequency.Daily, priority: 1.0),
                new SitemapItem(Url.QualifiedAction("index", "help"), changeFrequency: SitemapChangeFrequency.Yearly, priority: 0.1),
                new SitemapItem(Url.QualifiedAction("active", "topic"), changeFrequency: SitemapChangeFrequency.Hourly, priority: 1.0)


            };
            if (ClassicConfig.GetIntValue("INTCALEVENTS") == 1)
            {
                sitemapItems.Add(new SitemapItem(Url.QualifiedAction("index", "calendar"), changeFrequency: SitemapChangeFrequency.Monthly, priority: 1.0));
            }
            if (ClassicConfig.GetIntValue("INTPOLLS") == 1)
            {
                sitemapItems.Add(new SitemapItem(Url.QualifiedAction("index", "polls"), changeFrequency: SitemapChangeFrequency.Monthly, priority: 0.2));                
            }
            if (ClassicConfig.GetValue("STRPHOTOALBUM") == "1")
            {
                sitemapItems.Add(new SitemapItem(Url.QualifiedAction("index", "photoalbum"), changeFrequency: SitemapChangeFrequency.Monthly, priority: 0.5));
                if (ClassicConfig.GetValue("INTCOMMONALBUM") == "1")
                {
                    sitemapItems.Add(new SitemapItem(Url.QualifiedAction("album", "photoalbum"), changeFrequency: SitemapChangeFrequency.Monthly, priority: 0.5));
                }
            }
                //generate forum links
            using (var db = new SnitzDataContext())
            {
                //fetch public forums
                var catforums = db.FetchCategoryForumList(User);
                foreach (var category in catforums)
                {
                    foreach (var forum in category.Forums)
                    {
                        var ffreq = SitemapChangeFrequency.Monthly;
                        var priority = 0.5;

                        if (!forum.LastPostDate.HasValue)
                        {
                            ffreq = SitemapChangeFrequency.Never;
                        }
                        if (forum.LastPostDate.HasValue && forum.LastPostDate.Value.Date == DateTime.UtcNow.Date)
                        {
                            ffreq = SitemapChangeFrequency.Hourly;
                            priority = 1.0;
                        }
                        else if (forum.LastPostDate.HasValue && forum.LastPostDate.Value.Date > DateTime.UtcNow.AddDays(-7).Date)
                        {
                            ffreq = SitemapChangeFrequency.Daily;
                            priority = 0.7;
                        }
                        else if (forum.LastPostDate.HasValue && forum.LastPostDate.Value.Year != DateTime.UtcNow.Year)
                        {
                            ffreq = SitemapChangeFrequency.Yearly;
                            priority = 0.3;
                        }
                        else if (forum.LastPostDate.HasValue && forum.LastPostDate.Value.Month != DateTime.UtcNow.Month)
                        {
                            ffreq = SitemapChangeFrequency.Monthly;
                        }

                        sitemapItems.Add(
                            new SitemapItem(Url.QualifiedAction("title", "forum", new { id = forum.Subject, forumid = forum.Id }), lastModified: forum.LastPostDate, changeFrequency: ffreq, priority: priority)
                            );
                        //generate top 500 topic links for forum
                        var topics = forum.Topics(500, 1, User, WebSecurity.CurrentUserId);
                        
                        foreach (var topic in topics.Items)
                        {
                            ffreq = SitemapChangeFrequency.Monthly;
                            priority = 0.4;
                            if (topic.LastPostDate == DateTime.UtcNow.Date)
                            {
                                ffreq = SitemapChangeFrequency.Hourly;
                                priority = 0.8;
                            }
                            else if (topic.LastPostDate > DateTime.UtcNow.AddDays(-7).Date)
                            {
                                ffreq = SitemapChangeFrequency.Daily;
                                priority = 0.7;
                            }
                            sitemapItems.Add(
                                new SitemapItem(Url.QualifiedAction("subject", "topic", new { id = topic.Subject.Sanitize(), topic = topic.Id }), lastModified: topic.LastPostDate, changeFrequency: ffreq, priority: priority)
                                );                            
                        }
                    }
                }
            }
                
            return new SitemapResult(sitemapItems);
        }

         public static string CombinePaths(string path1, string path2) 
         { 
             //Ensure.Argument.NotNull(path1, "path1"); 
             //Ensure.Argument.NotNull(path2, "path2"); 

             if (String.IsNullOrEmpty(path2)) 
                 return path1;


             if (String.IsNullOrEmpty(path1)) 
                 return path2; 
 

             if (path2.StartsWith("http://") || path2.StartsWith("https://")) 
                 return path2; 
 

             var ch = path1[path1.Length - 1]; 
 

             if (ch != '/') 
                 return (path1.TrimEnd('/') + '/' + path2.TrimStart('/')); 
 

             return (path1 + path2); 
         } 

    }

}
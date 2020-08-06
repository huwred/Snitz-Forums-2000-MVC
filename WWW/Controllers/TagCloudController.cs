using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using Sparc.TagCloud;

namespace WWW.Controllers
{
    public class TagCloudController : Controller
    {
        public ActionResult Index(int id)
        {
            TagCloudSetting setting = new TagCloudSetting();
            setting.NumCategories = 20;
            setting.MaxCloudSize = 50;

            setting.StopWords = LoadStopWords();
            List<string> phrases;
            phrases = id == -1 ? Forum.GetTagStrings(User.ForumSubscriptions()) : Forum.GetTagStrings(id);
            var tagfree = new List<string>();

            foreach (var phrase in phrases)
            {
                string newphrase = BbCodeFormatter.BbCodeProcessor.CleanCode(phrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.StripCodeContents(newphrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.StripTags(newphrase);
                newphrase = BbCodeFormatter.BbCodeProcessor.RemoveHtmlTags(newphrase);
                tagfree.Add(newphrase);

            }
            var model = new TagCloudAnalyzer(setting)
                .ComputeTagCloud(tagfree)
                .Shuffle();
            return PartialView("_TagCloud",model);
        }

        private  HashSet<string> LoadStopWords()
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (culture == "nn" || culture == "nb")
            {
                culture = "no";
            }
            var path = Server.MapPath("~/App_Data/stopwords-" + culture + ".txt");
            string logFile = "";
            if (System.IO.File.Exists(path))
            {
                logFile = System.IO.File.ReadAllText(path);
                
            }

            var wordList = logFile.Split(new string[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(wordList, StringComparer.OrdinalIgnoreCase);
        }

    }
}

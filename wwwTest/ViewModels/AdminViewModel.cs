using System;
using System.Security.Principal;
using System.Xml.Linq;
using SnitzConfig;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Snitz.Base;
using SnitzDataModel.Database;
using SnitzDataModel.Models;
using Forum = SnitzDataModel.Models.Forum;
using System.Collections.ObjectModel;

namespace WWW.ViewModels
{
    public class AdminViewModel
    {
        public Enumerators.SubscriptionLevel SubscriptionLevel
        {
            get
            {
                return ClassicConfig.SubscriptionLevel;
            }
        }

        public Dictionary<string, string> Config
        {
            get
            {
                return ClassicConfig.ConfigDictionary;
            }
        }

        public string ServerTime { get; set; }
        public string ForumTime { get; set; }
        public ReadOnlyCollection<TimeZoneInfo> TimeZones = TimeZoneInfo.GetSystemTimeZones();
        public List<string> Themes
        {
            get
            {
                var cacheService = new InMemoryCache(1);
                var themefolder = HostingEnvironment.MapPath("~/Content/Themes");

                var folders = cacheService.GetOrSet("theme.folders", () => Directory.GetDirectories(themefolder));
                List<string> themes = new List<string>();
                foreach (var folder in folders)
                {
                    themes.Add(folder.Remove(0, themefolder.Length + 1));
                }
                return themes;
            }
        }
    }

    public class AdminToolsViewModel
    {
        public int ForumId { get; set; }

        [SnitzCore.Filters.Required]
        public string MemberName { get; set; }
        public string MemberToMerge { get; set; }
        public Dictionary<int, string> ForumList { get; set; }
        public Dictionary<int, string> MemberList { get; set; }
        public int TargetForumId { get; set; }

        public AdminToolsViewModel(IPrincipal user)
        {

            this.ForumList = new Dictionary<int, string> { { -1, "Select Forum" } };
            foreach (KeyValuePair<int, string> forum in Forum.List(user).ToDictionary(t => t.Key, t => t.Value))
            {
                this.ForumList.Add(forum.Key, forum.Value);
            }
        }
    }
    public class MergeMemberViewModel{
        public string MemberName { get; set; }
        public string MemberToMerge { get; set; }

        public Member Primary { get; set; }
        public Member ToMerge { get; set; }
    }
    public class RankingViewModel
    {
        public Dictionary<int, Ranking> Ranks
        {
            get { return SnitzDataContext.GetRankings(); }
        }

        public Enumerators.RankType Type
        {
            get { return ClassicConfig.ShowRankType; }
        }
    }

    public class DbsFile
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Applied { get; set; }

        public DbsFile(string filepath)
        {
            var dbsDocument = XDocument.Load(filepath);
            Name = filepath;
            XElement root = dbsDocument.Element("Tables");
            if (root != null)
            {
                Applied = Convert.ToBoolean(root.Attribute("applied").Value);
                Description = root.Attribute("title").Value;
            }

        }
    }
}
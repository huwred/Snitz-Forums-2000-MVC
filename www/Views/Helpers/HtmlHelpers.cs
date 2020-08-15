using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using WWW.Models;

namespace WWW.Views.Helpers
{
    public static partial class HtmlHelpers
    {

        private class ScriptBlock : IDisposable
        {
            private const string scriptsKey = "scripts";
            public static List<string> pageScripts
            {
                get
                {
                    if (HttpContext.Current.Items[scriptsKey] == null)
                        HttpContext.Current.Items[scriptsKey] = new List<string>();
                    return (List<string>)HttpContext.Current.Items[scriptsKey];
                }
            }


            WebViewPage webPageBase;

            public ScriptBlock(WebViewPage webPageBase)
            {

                this.webPageBase = webPageBase;
                this.webPageBase.OutputStack.Push(new StringWriter());
            }

            public void Dispose()
            {
                pageScripts.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
            }
        }
        private class StyleBlock : IDisposable
        {
            private const string stylesKey = "styles";
            public static List<string> pageStyles
            {
                get
                {
                    if (HttpContext.Current.Items[stylesKey] == null)
                        HttpContext.Current.Items[stylesKey] = new List<string>();
                    return (List<string>)HttpContext.Current.Items[stylesKey];
                }
            }

            WebViewPage webPageBase;

            public StyleBlock(WebViewPage webPageBase)
            {
                this.webPageBase = webPageBase;
                this.webPageBase.OutputStack.Push(new StringWriter());
            }

            public void Dispose()
            {
                pageStyles.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
            }
        }

        public static IDisposable BeginScripts(this HtmlHelper helper)
        {
            //beginscripts.Add(((WebViewPage)helper.ViewDataContainer).PageContext.Page.VirtualPath);
            return new ScriptBlock((WebViewPage)helper.ViewDataContainer);
        }

        public static MvcHtmlString PageScripts(this HtmlHelper helper)
        {

            var test = MvcHtmlString.Create(string.Join(Environment.NewLine,
                ScriptBlock.pageScripts.Distinct().Select(s => s.ToString())));
            return test;
        }
        public static IDisposable BeginStyles(this HtmlHelper helper)
        {
            return new StyleBlock((WebViewPage)helper.ViewDataContainer);
        }

        public static MvcHtmlString PageStyles(this HtmlHelper helper)
        {
            return MvcHtmlString.Create(string.Join(Environment.NewLine, StyleBlock.pageStyles.Select(s => s.ToString())));
        }

        /// <summary>
        /// Adds icons to indicate post statuses
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="topic"></param>
        /// <param name="imgclass"></param>
        /// <returns></returns>
        public static MvcHtmlString Overlay(this HtmlHelper helper, Topic topic, string imgclass)
        {
            int hottopicount = 0;
            if (ClassicConfig.GetIntValue("STRHOTTOPIC") == 1)
            {
                hottopicount = ClassicConfig.GetIntValue("INTHOTTOPICNUM");
            }

            TagBuilder builder = new TagBuilder("img");
            topic.ForumStatus = topic.Forum != null ? topic.Forum.Status : topic.ForumStatus;
            TagBuilder sticky = null;
            TagBuilder newposts = null;
            bool newPosts = false;
            var tracked = SnitzCookie.Tracked(topic.Id.ToString());
            if (tracked != null)
            {
                var today = DateTime.UtcNow.ToString("yyyyMM");
                newPosts = (today + tracked).ToDateTime() < topic.LastPostDate;
            }
            else
            {
                newPosts = (DateTime)helper.ViewData["LastVisitDateTime"] < topic.LastPostDate;
            }

            if (topic.Archived == 1)
            {
                builder.Attributes.Add("src", Common.RootFolder + Config.ThemeFolder() + "/Images/archived.png");
                builder.Attributes.Add("alt", "Archived");
                builder.Attributes.Add("title", LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicArchived", "Tooltip"));
                builder.Attributes.Add("data-toggle", "tooltip");
            }
            else if (topic.PostStatus == Enumerators.PostStatus.Closed || topic.ForumStatus == Enumerators.PostStatus.Closed)
            {
                builder.Attributes.Add("src", Common.RootFolder + Config.ThemeFolder() + "/Images/lock.png");
                builder.Attributes.Add("alt", "locked");
                builder.Attributes.Add("title", LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicLocked", "Tooltip"));
                builder.AddCssClass("locked-topic");
                builder.Attributes.Add("data-toggle", "tooltip");
            }
            else if (topic.PostStatus == Enumerators.PostStatus.UnModerated)
            {
                builder.Attributes.Add("src", Common.RootFolder + Config.ThemeFolder() + "/Images/warning.png");
                builder.Attributes.Add("alt", "unmoderated");
                builder.Attributes.Add("title", LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicUnmoderated", "Tooltip"));
                builder.Attributes.Add("data-toggle", "tooltip");
                builder.AddCssClass("unmoderated-topic");
            }
            else if (topic.PostStatus == Enumerators.PostStatus.OnHold)
            {
                builder.Attributes.Add("src", Common.RootFolder + Config.ThemeFolder() + "/Images/OnHold.png");
                builder.Attributes.Add("alt", "on hold");
                builder.Attributes.Add("title", LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicOnHold", "Tooltip"));
                builder.Attributes.Add("data-toggle", "tooltip");
                builder.AddCssClass("hold-topic");
            }
            else if (hottopicount > 0 && topic.ReplyCount > hottopicount)
            {
                builder.Attributes.Add("src", Common.RootFolder + Config.ThemeFolder() + "/Images/hot.gif");
                builder.Attributes.Add("alt", "hot topic");
                builder.Attributes.Add("title", LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicHot", "Tooltip"));
                builder.Attributes.Add("data-toggle", "tooltip");
                builder.AddCssClass("hot-topic");
            }

            if (topic.UnmoderatedReplyCount > 0)
            {
                var user = HttpContext.Current.User;
                if (user.IsAdministrator() || user.IsForumModerator(topic.ForumId))
                {
                    builder.Attributes["src"] = Common.RootFolder + Config.ThemeFolder() + "/Images/warning.png";
                    builder.MergeAttribute("alt", "unmoderated replies", true);
                    builder.Attributes["title"] = LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicUnmoderatedReplies", "Tooltip");
                    builder.AddCssClass("unmoderated-topic");
                    if (!builder.Attributes.ContainsKey("data-toggle"))
                    { builder.Attributes.Add("data-toggle", "tooltip"); }
                }

            }
            else if (newPosts)
            {
                newposts = new TagBuilder("img");
                newposts.Attributes["src"] = Common.RootFolder + Config.ThemeFolder() + "/Images/new.png";
                newposts.Attributes.Add("alt", "new replies");
                newposts.Attributes["title"] = LangResources.Utility.ResourceManager.GetLocalisedString("lblNewPosts",
                    "labels");
                newposts.Attributes.Add("data-toggle", "tooltip");
                newposts.Attributes.Add("class", imgclass);
                newposts.AddCssClass("new-posts");
            }
            else
            {
                newposts = new TagBuilder("img");
                newposts.Attributes["src"] = Common.RootFolder + Config.ThemeFolder() + "/Images/blank.png";
                newposts.Attributes.Add("alt", " no new replies");
                newposts.Attributes["title"] = LangResources.Utility.ResourceManager.GetLocalisedString("lblOldPosts",
                    "labels");
                newposts.Attributes.Add("data-toggle", "tooltip");
                newposts.Attributes.Add("class", imgclass);
                newposts.AddCssClass("no-new-posts");
            }

            //builder.AddCssClass(sticky == null && newposts == null ? "" : " seethrough");
            var bText = builder.ToString(TagRenderMode.SelfClosing);
            if (!builder.Attributes.Keys.Contains("src"))
            {
                bText = "";
            }
            var result = (sticky == null ? "" : sticky.ToString(TagRenderMode.SelfClosing)) + bText;

            if (newposts != null)
            {
                result += newposts.ToString(TagRenderMode.SelfClosing);
            }

            return new MvcHtmlString(result);
        }
        public static MvcHtmlString OverlaySticky(this HtmlHelper helper, Topic topic, string imgclass)
        {

            TagBuilder builder = new TagBuilder("img");
            topic.ForumStatus = topic.Forum != null ? topic.Forum.Status : topic.ForumStatus;
            TagBuilder sticky = null;

            if (topic.IsSticky == 1 && ClassicConfig.GetIntValue("STRSTICKYTOPIC") == 1)
            {
                sticky = new TagBuilder("img");
                sticky.Attributes.Add("src", Common.RootFolder + Config.ThemeFolder() + "/Images/stick.png");
                sticky.Attributes.Add("title", LangResources.Utility.ResourceManager.GetLocalisedString("tipTopicSticky", "Tooltip"));
                sticky.Attributes.Add("alt", "sticky");
                sticky.Attributes.Add("class", imgclass);
                sticky.AddCssClass("stick-topic");
                sticky.Attributes.Add("data-toggle", "tooltip");

            }
            var bText = builder.ToString(TagRenderMode.SelfClosing);
            if (!builder.Attributes.Keys.Contains("src"))
            {
                bText = "";
            }
            var result = (sticky == null ? "" : sticky.ToString(TagRenderMode.SelfClosing)) + bText;

            return new MvcHtmlString(result);
        }

        public static MvcHtmlString ForumLogo(this HtmlHelper helper)
        {
            return new MvcHtmlString("<div id='ForumLogo' title=\"" + (Config.ForumTitle ?? ClassicConfig.ForumTitle) + "\"></div>");
        }

        public static MvcHtmlString ShowHideForums(this HtmlHelper helper, Forum model, IEnumerable<string> roles, string wrapper = "{0}")
        {
            if (wrapper == "li")
            {
                wrapper = "<li class=\"list-unstyled\">{0}</li>";
            }
            var user = HttpContext.Current.User;
            string forumlink = "<a data-title='" + LangResources.Utility.ResourceManager.GetLocalisedString("tipForumViewPosts", "Tooltip") + "' data-toggle='tooltip' class='forum-link' href='" + Common.RootFolder + "/Forum/" + model.GenerateSlug() + "?pagenum=1' >" + model.Subject + "</a>";
            string archivelink = "&nbsp;<a data-title='" + LangResources.Utility.ResourceManager.GetLocalisedString("tipForumViewArchived", "Tooltip") + "' data-toggle='tooltip' href='" + Common.RootFolder + "/Forum/" + model.GenerateSlug() + "?pagenum=1&archived=1' ><i class='fa fa-file-text'></i></a>";
            if (model.ArchivedPostCount > 0 && user.Identity.IsAuthenticated)
            {
                forumlink += archivelink;
            }

            if (user.IsAdministrator())
                return MvcHtmlString.Create(String.Format(wrapper, forumlink));
            if (user.CanViewForum(model, roles))
            {
                return MvcHtmlString.Create(String.Format(wrapper, forumlink));
            }

            return MvcHtmlString.Empty;
        }

        public static MvcHtmlString FileImage(this HtmlHelper helper, string mimetype, string ext = "")
        {
            //<i class="fa fa-file-image-o"></i>
            var tag = new TagBuilder("i");
            tag.AddCssClass("fa");
            if (mimetype.Contains("compress"))
            {
                tag.AddCssClass("fa-file-archive-o");
            }
            else if (mimetype.Contains("image"))
            {
                tag.AddCssClass("fa-file-image-o");
            }
            else if (mimetype.Contains("pdf"))
            {
                tag.AddCssClass("fa-file-pdf-o");
            }
            else if (mimetype.Contains("text"))
            {
                if (ext != "")
                {
                    switch (ext)
                    {
                        case ".css":
                            tag.AddCssClass("fa-file-code-o");
                            break;
                        default:
                            tag.AddCssClass("fa-file-text-o");
                            break;
                    }
                }
                else
                {
                    tag.AddCssClass("fa-file-text-o");
                }

            }
            else
            {
                tag.AddCssClass("fa-file-o");
            }
            //bool b = listOfStrings.Any(mimetype.Contains);
            return MvcHtmlString.Create(tag.ToString());
        }

        public static MvcHtmlString FileSize(this HtmlHelper helper, long bytes)
        {
            if (bytes < 1024)
                return MvcHtmlString.Create(bytes + " bytes");
            if (bytes > 1024 * 1024)
                return MvcHtmlString.Create(((float)bytes / (1024 * 1024)).ToString("0.00") + " Mb");
            return MvcHtmlString.Create(((float)bytes / 1024).ToString("0.00") + " Kb");
        }

        public static string Age(this HtmlHelper helper, DateTime birthday)
        {
            DateTime now = DateTime.Today;
            int age = now.Year - birthday.Year;
            if (now < birthday.AddYears(age)) age--;

            return age.ToString();
        }

        /// <summary>
        /// Display members age
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="birthday">Date of birth datestring</param>
        /// <returns></returns>
        public static string Age(this HtmlHelper helper, string birthday)
        {
            if (String.IsNullOrWhiteSpace(birthday))
                return String.Empty;
            DateTime now = DateTime.Today;
            int age = now.Year - birthday.ToSnitzDateTime().Year;
            if (now < birthday.ToSnitzDateTime().AddYears(age)) age--;

            return age.ToString();
        }

        public static string TruncateLongString(this string str, int maxLength)
        {
            if (String.IsNullOrWhiteSpace(str))
                return str;
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }
        public static MvcHtmlString DisplayNameForEnum<TModel, TEnum>(this HtmlHelper<TModel> html, TEnum expression, bool newline=false)
        {
            //html.ViewData[""]
            var resourceDisplayName = LangResources.Utility.ResourceManager.GetLocalisedString(expression.GetType().Name + "_" + expression);
            resourceDisplayName = resourceDisplayName.Replace("[[LASTVISIT]]", ((DateTime)html.ViewData["LastVisitDateTime"]).ToClientTime().ToFormattedString());
            if (newline)
            {
                resourceDisplayName += "<br/>";
            }
            return string.IsNullOrWhiteSpace(resourceDisplayName) ? MvcHtmlString.Create(String.Format("[[{0}_{1}]]", expression.GetType().Name, expression)) : MvcHtmlString.Create(resourceDisplayName);

        }
        public static string FormatKB(this HtmlHelper helper, long fileLength)
        {
            return string.Format("{0:N0}", (fileLength / 1024));
        }

        public static string FileIcon(this HtmlHelper helper, DataRowView row, string path)
        {
            var strImagePath = VirtualPathUtility.ToAppRelative("~/Content/images/");

            if (FileManager.IsDirectory(row))
            {
                return FileManager.WebPathCombine(strImagePath, "file/folder.gif");
            }

            if (File.Exists(Path.Combine(helper.ViewContext.HttpContext.Server.MapPath(strImagePath), "file/file_extension_" + Convert.ToString(row["FileExtension"]).Replace(".", "") + ".png")))
            {
                return FileManager.WebPathCombine(strImagePath, "file/file_extension_" + Convert.ToString(row["FileExtension"]).Replace(".", "") + ".png");
            }

            switch (Convert.ToString(row["FileExtension"]).ToLowerInvariant())
            {
                case ".gif":
                case ".peg":
                case ".jpe":
                case ".jpg":
                case ".png":
                    return FileManager.WebPathCombine(VirtualPathUtility.ToAppRelative(path), Convert.ToString(row["Name"]));

                case ".txt":
                    return FileManager.WebPathCombine(strImagePath, "file/text.gif");
                case ".htm":
                case ".xml":
                case ".xsl":
                case ".css":
                case ".html":
                case ".config":
                    return FileManager.WebPathCombine(strImagePath, "file/html.gif");
                case ".mp3":
                case ".wav":
                case ".wma":
                case ".au":
                case ".mid":
                case ".ram":
                case ".rm":
                case ".snd":
                case ".asf":
                    return FileManager.WebPathCombine(strImagePath, "file/audio.gif");
                case ".zip":
                case "tar":
                case ".gz":
                case ".rar":
                case ".cab":
                case ".tgz":
                    return FileManager.WebPathCombine(strImagePath, "file/compressed.gif");
                case ".asp":
                case ".wsh":
                case ".js":
                case ".vbs":
                case ".aspx":
                case ".cs":
                case ".vb":
                    return FileManager.WebPathCombine(strImagePath, "file/script.gif");
                default:
                    return FileManager.WebPathCombine(strImagePath, "file/generic.gif");
            }

        }

        public static MvcHtmlString FileLink(this HtmlHelper helper, DataRowView row, string path)
        {

            string strFileName = Convert.ToString(row["Name"]);
            string strFilePath = FileManager.WebPathCombine(path, strFileName);
            bool blnFolder = FileManager.IsDirectory(row);

            var urlHelper = new UrlHelper(helper.ViewContext.RequestContext);
            TagBuilder link = new TagBuilder("a");

            var url = urlHelper.Action("Index", "WebFileManager", new { path = strFilePath });
            if (!blnFolder)
            {
                url = urlHelper.Action("File", "WebFileManager", new { path = strFilePath });
                link.Attributes.Add("target", "_blank");
            }
            else
            {
                url = FileManager.PageUrl(strFilePath);
            }

            link.Attributes.Add("title", strFileName);
            link.Attributes.Add("href", url);
            link.SetInnerText(strFileName);

            return MvcHtmlString.Create(link.ToString());
        }
    }

}
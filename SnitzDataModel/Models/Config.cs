// /*
// ####################################################################################################################
// ##
// ## Config
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Web;
using System.Web.Hosting;
using System.Web.Security;
using System.Xml;
using MySql.Data.MySqlClient;
using PetaPoco;
using Snitz.Base;
using SnitzDataModel.Database;
using SnitzDataModel.Models;


namespace SnitzConfig
{
    /// <summary>
    /// Config variables from app settings file snitz.config
    /// </summary>
    public static class Config
    {
        static readonly XmlDocument XmlDoc = new XmlDocument();


        #region Config Properties

        [Description("strDefaultTheme")]
        public static string DefaultTheme
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strDefaultTheme"))
                {
                    return ConfigurationManager.AppSettings["strDefaultTheme"];
                }
                return "Snitz";
            }
            set { UpdateConfig("strDefaultTheme", value); }
        }

        //TopicAvatar
        [Description("boolShowAvatar")]
        public static bool ShowAvatar
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolShowAvatar"))
                {
                    bool showAvatar = ConfigurationManager.AppSettings["boolShowAvatar"] != null &&
                                      Convert.ToInt32(ConfigurationManager.AppSettings["boolShowAvatar"]) == 1;
                    return showAvatar && ClassicConfig.GetIntValue("STRPICTURE") == 1;


                }
                return false;
            }
            set { UpdateConfig("boolShowAvatar", value ? "1" : "0"); }
        }

        [Description("boolRunSetup")]
        public static bool RunSetup
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolRunSetup"))
                {
                    return ConfigurationManager.AppSettings["boolRunSetup"] != null &&
                           ConfigurationManager.AppSettings["boolRunSetup"] == "1";

                }
                return false;
            }
            set { UpdateConfig("boolRunSetup", value ? "1" : "0"); }
        }

        [Description("boolDisablePosting")]
        public static bool DisablePosting
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolDisablePosting"))
                {
                    return (ConfigurationManager.AppSettings["boolDisablePosting"] == "1");
                }
                return false;
            }
            set { UpdateConfig("boolDisablePosting", value ? "1" : "0"); }
        }

        public static string ContentFolder
        {
            get { return ClassicConfig.GetIntValue("INTPROTECTCONTENT", 0) == 1 ? "ProtectedContent" : "Content"; }
        }

        /// <summary>
        /// Members in this list will not appear as online
        /// </summary>
        //[Description("strAnonMembers")]
        //public static List<string> AnonymousMembers
        //{
        //    get
        //    {
        //        if (ConfigurationManager.AppSettings.AllKeys.Contains("strAnonMembers"))
        //        {
        //            return (ConfigurationManager.AppSettings["strAnonMembers"].Split(',').ToList());

        //        }
        //        return new List<string>();


        //    }
        //    set { UpdateConfig("strAnonMembers", String.Join(",", value.ToArray())); }

        //}
        public static List<string> AnonymousMembers
        {
            get
            {
                if (Roles.GetAllRoles().Contains("HiddenMembers"))
                {
                    return Roles.GetUsersInRole("HiddenMembers").ToList();

                }
                return new List<string>();


            }


        }

        #region these are currently set in web.config email section

        [Description("smtpUserName")]
        public static string EmailAuthUser
        {
            get
            {
                SmtpSection smtpSec = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                return smtpSec.Network.UserName;
            }
        }

        [Description("smtpPassword")]
        public static string EmailAuthPwd
        {
            get
            {
                SmtpSection smtpSec = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                return smtpSec.Network.Password;
            }
        }

        [Description("smtpServer")]
        public static string EmailHost
        {
            get
            {
                SmtpSection smtpSec = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                return smtpSec.Network.Host;
            }
        }

        [Description("smtpPort")]
        public static int EmailPort
        {
            get
            {
                SmtpSection smtpSec = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                return smtpSec.Network.Port;
            }
        }

        #endregion

        #region Overrides of Classic config

        [Description("strForumTitle")]
        public static string ForumTitle
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strForumTitle"))
                {
                    return ConfigurationManager.AppSettings["strForumTitle"] ?? ClassicConfig.ForumTitle;

                }
                return null;

            }
            set { UpdateConfig("strForumTitle", value); }
        }

        [Description("strForumUrl")]
        public static string ForumUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strForumUrl"))
                {
                    string url = ConfigurationManager.AppSettings["strForumUrl"] ?? ClassicConfig.ForumUrl;
                    if (!url.EndsWith("/"))
                        url += "/";
                    return url;
                }
                return ClassicConfig.ForumUrl;

            }
            set { UpdateConfig("strForumUrl", value); }
        }

        [Description("strForumImage")]
        public static string TitleImage
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strForumImage"))
                {
                    string img = ConfigurationManager.AppSettings["strForumImage"];

                    return img;
                }
                return null;

            }
            set { UpdateConfig("strForumImage", value); }
        }

        [Description("boolProhibitNewMembers")]
        public static bool ProhibitNewMembers
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolProhibitNewMembers"))
                {
                    return ConfigurationManager.AppSettings["boolProhibitNewMembers"] == "1";
                }
                return false;
            }
            set { UpdateConfig("boolProhibitNewMembers", value ? "1" : "0"); }
        }

        [Description("strForumDescription")]
        public static string ForumDescription
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strForumDescription"))
                {
                    return ConfigurationManager.AppSettings["strForumDescription"];
                }
                return null;

            }
            set { UpdateConfig("strForumDescription", value); }
        }

        [Description("strCookiePath")]
        public static string CookiePath
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strCookiePath"))
                {
                    return ConfigurationManager.AppSettings["strCookiePath"] ?? ClassicConfig.CookiePath;
                }
                return null;

            }
            set { UpdateConfig("strCookiePath", value); }
        }

        #endregion

        /// <summary>
        /// Set this to true if Db has Fulltext searching enabled
        /// </summary>
        public static bool FullTextSearch
        {
            get
            {
                return ClassicConfig.GetIntValue("INTFULLTEXT", 0) == 1;
            }
        }

        /// <summary>
        /// Page sizes 
        /// </summary>
        public static int DefaultPageSize
        {
            get
            {
                return ClassicConfig.GetIntValue("STRPAGESIZE", 10);
            }
        }
        public static int TopicPageSize
        {
            get
            {
                var pagesizes = ClassicConfig.GetValue("STRTOPICPAGESIZES", DefaultPageSize.ToString()).Split(',');
                return Convert.ToInt32(pagesizes[0]);

            }

        }
        public static int ActiveTopicPageSize
        {
            get
            {
                var pagesizes = ClassicConfig.GetValue("STRACTIVEPAGESIZES", DefaultPageSize.ToString()).Split(',');
                return Convert.ToInt32(pagesizes[0]);

            }

        }
        public static int ForumPageSize
        {
            get
            {
                var pagesizes = ClassicConfig.GetValue("STRFORUMPAGESIZES", DefaultPageSize.ToString()).Split(',');
                return Convert.ToInt32(pagesizes[0]);

            }

        }
        public static int SearchPageSize
        {
            get
            {
                var pagesizes = ClassicConfig.GetValue("STRSEARCHPAGESIZES", DefaultPageSize.ToString()).Split(',');
                return Convert.ToInt32(pagesizes[0]);

            }
        }
        public static int MemberPageSize
        {
            get
            {
                var pagesizes = ClassicConfig.GetValue("STRACCOUNTPAGESIZES", DefaultPageSize.ToString()).Split(',');
                return Convert.ToInt32(pagesizes[0]);
            }
        }
        public static int RecentTopicListSize
        {
            get
            {
                return ClassicConfig.GetIntValue("INTRECENTCOUNT", 5);

            }

        }

        public static bool ShowRankStars
        {
            get
            {
                return ((ClassicConfig.ShowRankType == Enumerators.RankType.StarsOnly) ||
                        (ClassicConfig.ShowRankType == Enumerators.RankType.Both));
            }
        }

        public static bool ShowRankTitle
        {
            get
            {
                return ((ClassicConfig.ShowRankType == Enumerators.RankType.RankOnly) ||
                        (ClassicConfig.ShowRankType == Enumerators.RankType.Both));
            }
        }

        [Description("boolAllowSearchAllForums")]
        public static bool AllowSearchAllForums
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolAllowSearchAllForums"))
                {
                    return ConfigurationManager.AppSettings["boolAllowSearchAllForums"] != null &&
                           ConfigurationManager.AppSettings["boolAllowSearchAllForums"] == "1";

                }
                return false;
            }
            set { UpdateConfig("boolAllowSearchAllForums", value ? "1" : "0"); }
        }

        [Description("strTimeZone")]
        public static string TimeZoneString
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolDayLightSavingAdjust"))
                {
                    return ConfigurationManager.AppSettings["strTimeZone"] ?? "00:00:00";
                }
                return "00:00:00";

            }
            set { UpdateConfig("strTimeZone", value.ToString()); }
        }

        [Description("boolDayLightSavingAdjust")]
        public static bool DayLightSavingAdjust
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("boolDayLightSavingAdjust"))
                {
                    return (ConfigurationManager.AppSettings["boolDayLightSavingAdjust"] == "1");
                }
                return false;
            }
            set { UpdateConfig("boolDayLightSavingAdjust", value ? "1" : "0"); }
        }

        public static bool IsDayLightSaving
        {
            get
            {
                DaylightTime dtt = TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year);
                return (dtt.Start < DateTime.Now || dtt.End > DateTime.Now);
            }

        }

        [Description("intPreferredPasswordLength")]
        public static int PreferredPasswordLength
        {
            get
            {
                return ConfigurationManager.AppSettings["intPreferredPasswordLength"] == null
                    ? 10
                    : Convert.ToInt32(ConfigurationManager.AppSettings["intPreferredPasswordLength"]);
            }
            set { UpdateConfig("intPreferredPasswordLength", value.ToString()); }
        }

        [Description("intMinimumNumericCharacters")]
        public static int MinimumNumericCharacters
        {
            get
            {
                return ConfigurationManager.AppSettings["intMinimumNumericCharacters"] == null
                    ? 0
                    : Convert.ToInt32(ConfigurationManager.AppSettings["intMinimumNumericCharacters"]);
            }
            set { UpdateConfig("intMinimumNumericCharacters", value.ToString()); }
        }

        [Description("intMinimumSymbolCharacters")]
        public static int MinimumSymbolCharacters
        {
            get
            {
                return ConfigurationManager.AppSettings["intMinimumSymbolCharacters"] == null
                    ? 0
                    : Convert.ToInt32(ConfigurationManager.AppSettings["intMinimumSymbolCharacters"]);
            }
            set { UpdateConfig("intMinimumSymbolCharacters", value.ToString()); }
        }

        [Description("intMinimumLowerCaseCharacters")]
        public static int MinimumLowerCaseCharacters
        {
            get
            {
                return ConfigurationManager.AppSettings["intMinimumLowerCaseCharacters"] == null
                    ? 0
                    : Convert.ToInt32(ConfigurationManager.AppSettings["intMinimumLowerCaseCharacters"]);
            }
            set { UpdateConfig("intMinimumLowerCaseCharacters", value.ToString()); }
        }

        [Description("intMinimumUpperCaseCharacters")]
        public static int MinimumUpperCaseCharacters
        {
            get
            {
                return ConfigurationManager.AppSettings["intMinimumUpperCaseCharacters"] == null
                    ? 0
                    : Convert.ToInt32(ConfigurationManager.AppSettings["intMinimumUpperCaseCharacters"]);
            }
            set { UpdateConfig("intMinimumUpperCaseCharacters", value.ToString()); }
        }

        [Description("boolRequiresUpperAndLowerCaseCharacters")]
        public static bool RequiresUpperAndLowerCaseCharacters
        {
            get
            {
                return ConfigurationManager.AppSettings["boolAllowSearchAllForums"] != null &&
                       Convert.ToInt32(ConfigurationManager.AppSettings["boolRequiresUpperAndLowerCaseCharacters"]) == 1;
            }
            set { UpdateConfig("boolRequiresUpperAndLowerCaseCharacters", value ? "1" : "0"); }
        }

        [Description("strUniqueId")]
        public static string UniqueId
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("strUniqueId"))
                {
                    return ConfigurationManager.AppSettings["strUniqueId"];
                }
                return "Snitz00";
            }
            set { UpdateConfig("strUniqueId", value); }
        }

        public static bool UseCaptcha
        {
            get { return ClassicConfig.GetIntValue("STRUSECAPTCHA") == 1; }

        }

        public static List<Enumerators.CaptchaOperator> CaptchaOperators
        {
            get
            {
                List<Enumerators.CaptchaOperator> operators = null;
                if (ClassicConfig.ConfigDictionary.ContainsKey("STRCAPTCHAOPERATORS"))
                {
                    operators = new List<Enumerators.CaptchaOperator>();
                    var stringCaptcha = ClassicConfig.ConfigDictionary["STRCAPTCHAOPERATORS"].Split(new char[] { ';' },
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in stringCaptcha)
                    {
                        operators.Add((Enumerators.CaptchaOperator)Enum.Parse(typeof(Enumerators.CaptchaOperator), s));
                    }

                }
                return operators;
            }
            set
            {
                var strcap = "";
                foreach (var captchaOperator in value)
                {
                    if (strcap != "")
                        strcap += ";";
                    strcap += captchaOperator.ToString();
                }
                UpdateConfig("strCaptchaOperators", strcap);
            }
        }

        #endregion

        #region Config Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void UpdateConfig(string key, string value)
        {
            if (!XmlDoc.HasChildNodes)
                XmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "snitz.config");
            UpdateKey(key, value);

        }

        /// <summary>
        /// Updates the appsettings key with a new value
        /// </summary>
        /// <param name="strKey">Key to update</param>
        /// <param name="newValue">ne value for the key</param>
        private static void UpdateKey(string strKey, string newValue)
        {
            if (!KeyExists(strKey))
            {
                AddKey(strKey, newValue);
                return;
            }
            XmlNode appSettingsNode = XmlDoc.SelectSingleNode("appSettings");
            foreach (XmlNode childNode in appSettingsNode)
            {
                if ((childNode.NodeType == XmlNodeType.Element) && (childNode.Name == "add"))
                    if (childNode.Attributes["key"].Value == strKey)
                        childNode.Attributes["value"].Value = newValue;
            }

        }

        /// <summary>
        /// Adds a new key + value to the appsettings files
        /// </summary>
        /// <param name="strKey">Key to add to appsettings</param>
        /// <param name="newValue">value for the new setting</param>
        private static void AddKey(string strKey, string newValue)
        {
            XmlNode appSettingsNode = XmlDoc.SelectSingleNode("appSettings");

            XmlElement newnode = XmlDoc.CreateElement("add");
            newnode.SetAttribute("key", strKey);
            newnode.SetAttribute("value", newValue);

            appSettingsNode.AppendChild(newnode);

        }

        /// <summary>
        /// Checks if the key exists in the appsettings file
        /// </summary>
        /// <param name="strKey">Key to look for</param>
        /// <returns></returns>
        private static bool KeyExists(string strKey)
        {
            XmlNode appSettingsNode = XmlDoc.SelectSingleNode("appSettings");

            var result = appSettingsNode != null &&
                         appSettingsNode.Cast<XmlNode>()
                             .Where(n => n.NodeType != XmlNodeType.Comment)
                             .Any(childNode => childNode.Attributes["key"].Value == strKey);
            return result;
        }

        /// <summary>
        /// Save the appsettings file
        /// </summary>
        private static void SaveAppSettings()
        {
            //save the appsettings file snitz.config
            XmlDoc.Save(AppDomain.CurrentDomain.BaseDirectory + "snitz.config");
        }

        public static void Update()
        {
            SaveAppSettings();
            //Touch the web.config file to force the Application Settings to reload
            XmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "web.config");
            XmlDoc.Save(AppDomain.CurrentDomain.BaseDirectory + "web.config");
            //reload the appsettings
            ConfigurationManager.RefreshSection("appSettings");

        }

        /// <summary>
        /// Removes a key from the appsettings file
        /// </summary>
        /// <param name="strKey">key to remove</param>
        private static void DeleteKey(string strKey)
        {
            throw new Exception("Can not delete config keys");
        }


        #endregion

        public static bool TableExists(string table, HttpContextBase context)
        {
            try
            {
                System.Web.HttpContext.Current.Application.Lock();
                var exists = System.Web.HttpContext.Current.Application.Get(table);
                if (exists != null)
                {
                    System.Web.HttpContext.Current.Application.UnLock();
                    return (int)exists == 1;
                }
                System.Web.HttpContext.Current.Application[table] = 0;
                System.Web.HttpContext.Current.Application.UnLock();
            }
            catch (Exception)
            {

            }


            using (SnitzDataContext db = new SnitzDataContext())
            {
                var database = "";
                if (db.dbtype == "mysql")
                {
                    database = new MySqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString).Database;

                }
                else
                {
                    database = new SqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString).InitialCatalog;

                }

                Sql sql = new Sql();
                sql.Select("COUNT(*) FROM INFORMATION_SCHEMA.TABLES");
                sql.Where(db.dbtype == "mssql" ? "TABLE_CATALOG = @0" : "TABLE_SCHEMA = @0", database);
                sql.Where("TABLE_NAME = @0", table);
                int res = db.ExecuteScalar<int>(sql);
                if (res == 1)
                {
                    try
                    {
                        System.Web.HttpContext.Current.Application.Lock();
                        System.Web.HttpContext.Current.Application[table] = res;
                        System.Web.HttpContext.Current.Application.UnLock();
                    }
                    catch (Exception)
                    {

                    }

                    return true;
                }
            }

            return false;
        }

        public static bool TableExists(string table, bool prefix)
        {

            using (SnitzDataContext db = new SnitzDataContext())
            {
                var database = "";
                if (db.dbtype == "mysql")
                {
                    database = new MySqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString).Database;

                }
                else
                {
                    database = new SqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString).InitialCatalog;

                }

                if (prefix)
                {
                    table = db.ForumTablePrefix + table;
                }
                Sql sql = new Sql();
                sql.Select("COUNT(*) FROM INFORMATION_SCHEMA.TABLES");
                sql.Where(db.dbtype == "mssql" ? "TABLE_CATALOG = @0" : "TABLE_SCHEMA = @0", database);
                sql.Where("TABLE_NAME = @0", table);
                int res = db.ExecuteScalar<int>(sql);
                if (res == 1)
                {
                    return true;
                }
            }

            return false;
        }
        public static bool TableExists(string table)
        {

            using (SnitzDataContext db = new SnitzDataContext())
            {
                var database = "";
                if (db.dbtype == "mysql")
                {
                    database = new MySqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString).Database;

                }
                else
                {
                    database = new SqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString).InitialCatalog;

                }

                Sql sql = new Sql();
                sql.Select("COUNT(*) FROM INFORMATION_SCHEMA.TABLES");
                sql.Where(db.dbtype == "mssql" ? "TABLE_CATALOG = @0" : "TABLE_SCHEMA = @0", database);
                sql.Where("TABLE_NAME = @0", table);
                int res = db.ExecuteScalar<int>(sql);
                if (res == 1)
                {
                    return true;
                }
            }

            return false;
        }

        public static string DateStr
        {
            get
            {
                var dateformat = ClassicConfig.GetValue("STRDATETYPE");

                switch (dateformat)
                {
                    case "mdy":
                        dateformat = "MM/dd/yy";
                        break;
                    case "dmy":
                        dateformat = "dd/MM/yy";
                        break;
                    case "ymd":
                        dateformat = "yy/MM/dd";
                        break;
                    case "ydm":
                        dateformat = "yy/dd/MM";
                        break;
                    case "dmmy":
                        dateformat = "dd MMM yyyy";
                        break;
                    case "mmdy":
                        dateformat = "MMM dd yyyy";
                        break;
                    case "mmmdy":
                        dateformat = "MMMM dd yyyy";
                        break;
                    case "dmmmy":
                        dateformat = "dd MMMM yyyy";
                        break;
                }
                return dateformat;
            }
        }

        public static string TimeStr
        {
            get
            {
                var hourformat = ClassicConfig.GetValue("STRTIMETYPE");
                var timestring = "H:mm";
                if (hourformat == "24")
                {
                    timestring = "HH:mm";
                }
                return timestring;
            }
        }

        public static string DbType { get; set; }

        public static bool FolderExists(int currentUserId)
        {
            return Directory.Exists(HostingEnvironment.MapPath("~/" + ContentFolder + "/Members/") + currentUserId);
        }
        /// <summary>
        /// ThemeFolder no trailing slash
        /// </summary>
        /// <returns></returns>
        public static string ThemeFolder()
        {
            var theme = Config.DefaultTheme;
            var cssPath = "/Content/Themes/{0}";

            var httpCookie = SnitzCookie.GetCookieValue("snitztheme");
            if (httpCookie != null && httpCookie != theme.ToLower())
            {
                var cookietheme = httpCookie;
                theme = cookietheme.FirstLetterToUpperCase();

            }

            return String.Format(cssPath, theme);
        }
        public static string ThemeCss(string cssfile)
        {
            var cssPath = "~/Content/Themes/{0}/{1}";
            var theme = Config.DefaultTheme;

            var httpCookie = SnitzCookie.GetCookieValue("snitztheme");
            if (httpCookie != null && httpCookie != theme.ToLower())
            {
                var cookietheme = httpCookie;
                theme = cookietheme.FirstLetterToUpperCase();

            }
            return "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + VirtualPathUtility.ToAbsolute(String.Format(cssPath, theme, cssfile)) + "\" title=\"" + theme.ToLower() + "\">";
        }
        public static string Theme()
        {
            var theme = Config.DefaultTheme;

            var httpCookie = SnitzCookie.GetCookieValue("snitztheme");
            if (httpCookie != null && httpCookie != theme.ToLower())
            {
                var cookietheme = httpCookie;
                theme = cookietheme.FirstLetterToUpperCase();
            }
            return theme;
        }

        internal static string FirstLetterToUpperCase(this string s)
        {
            if (String.IsNullOrEmpty(s))
                throw new ArgumentException("There is no first letter");

            char[] a = s.ToCharArray();
            a[0] = Char.ToUpper(a[0]);
            return new string(a);
        }
    }
}

// /*
// ####################################################################################################################
// ##
// ## ClassicConfig
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

using LangResources.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Configuration;
using System.Web;
using System.Web.Configuration;
using Snitz.Base;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;


namespace SnitzConfig
{
    /// <summary>
    /// Config variables from Snitz FORUM_CONFIG_NEW table
    /// </summary>
    public static class ClassicConfig
    {

        public static Dictionary<string, string> ConfigDictionary
        {
            get
            {
                var cacheService = new InMemoryCache() { DoNotExpire = true };
                using (SnitzDataContext db = new SnitzDataContext())
                {
                    return cacheService.GetOrSet("snitz.appvars", () => db.Fetch<Pair<string, string>>("SELECT C_VARIABLE As 'Key',C_VALUE As 'Value' FROM " + db.ForumTablePrefix + "CONFIG_NEW").ToDictionary(i => i.Key.ToUpper(), i => i.Value));
                }

            }
        }
        /// <summary>
        /// Reset the cached config
        /// </summary>
        private static void Reset()
        {
            new InMemoryCache().Remove("snitz.appvars");
        }
        /// <summary>
        /// Get a string value from the dictionary
        /// </summary>
        /// <param name="key">KEY to find</param>
        /// <returns>Returns the value or empty string if key not found</returns>
        public static string GetValue(string key)
        {
            string defaultValue = String.Empty;
            if (key.StartsWith("INT"))
                defaultValue = "0";
            return ConfigDictionary.ContainsKey(key.ToUpper()) ? ConfigDictionary[key.ToUpper()] : defaultValue;
        }
        public static bool KeyExists(string key)
        {
            return ConfigDictionary.ContainsKey(key.ToUpper());
        }
        /// <summary>
        /// Get a string value from the dictionary
        /// </summary>
        /// <param name="key">KEY to find</param>
        /// <param name="def">String to return if key not found</param>
        /// <returns>Returns the value or [def] if key not found</returns>
        public static string GetValue(string key, string def)
        {
            return ConfigDictionary.ContainsKey(key.ToUpper()) ? ConfigDictionary[key.ToUpper()] : def;
        }
        /// <summary>
        /// Get an integer value from the dictionary
        /// </summary>
        /// <param name="key">KEY to find-returns def if key not found</param>
        /// <param name="def">Value to return if key not found</param>
        /// <returns>Returns the value or [def] if key not found</returns>
        public static int GetIntValue(string key, int def)
        {
            return ConfigDictionary.ContainsKey(key.ToUpper()) && !String.IsNullOrEmpty(ConfigDictionary[key.ToUpper()]) ? Convert.ToInt32(ConfigDictionary[key.ToUpper()]) : def;
        }
        /// <summary>
        /// Get an integer value from the dictionary
        /// </summary>
        /// <param name="key">KEY to find</param>
        /// <returns>Returns the value or 0 if key not found</returns>
        public static int GetIntValue(string key)
        {
            if (Config.RunSetup)
                return 0;
            return ConfigDictionary.ContainsKey(key.ToUpper()) && !String.IsNullOrEmpty(ConfigDictionary[key.ToUpper()]) ? Convert.ToInt32(ConfigDictionary[key.ToUpper()]) : 0;
        }

        #region config settings duplicated in Config

        public static int ForumServerOffset
        {
            get
            {

                return GetIntValue("STRTIMEADJUST");

            }

        }
        public static int ForumTimeZoneOffset
        {
            get
            {
                string zone;

                if (ConfigDictionary.TryGetValue("STRCURRENTTIMEZONE", out zone))
                {
                    TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(zone);
                    var tzOffset = timeZoneInfo.BaseUtcOffset;
                    return tzOffset.Minutes;
                }

                return 0;
            }

        }
        public static string ForumTitle
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRFORUMTITLE", out @value))
                    return @value;
                return "Snitz Forums 2000";
            }
        }
        public static string ForumUrl
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRFORUMURL", out @value))
                    return @value;
                return "";
            }
        }
        public static string HomeUrl
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRHOMEURL", out @value))
                    return @value;
                return "";
            }
        }
        public static bool ProhibitNewMembers
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRPROHIBITNEWMEMBERS", out @value))
                    return @value == "1";
                return true;
            }
        }
        public static bool RequireRegistration
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRREQUIREREG", out @value))
                    return @value == "1";
                return true;
            }
        }
        public static string CookiePath
        {
            get
            {
                string path;
                if (ConfigDictionary.TryGetValue("STRSETCOOKIETOFORUM", out path))
                {
                    if (path == "1")
                    {
                        // create an absolute path for the application root
                        var appUrl = VirtualPathUtility.ToAbsolute("~/");

                        // remove the app path (exclude the last slash)
                        var relativeUrl = HttpContext.Current.Request.Url.AbsolutePath.Remove(0, appUrl.Length - 1);
                        return relativeUrl;
                    }
                }
                return "/";
            }

        }

        #endregion

        #region Required for parsing old bbcode tags

        public static int DefaultFontsize
        {
            get
            {
                string @value;
                ConfigDictionary.TryGetValue("STRDEFAULTFONTSIZE", out @value);
                return Convert.ToInt32(@value);
            }
        }
        public static int FooterFontsize
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRFOOTERFONTSIZE", out @value))
                    return Convert.ToInt32(@value);
                return 1;
            }
        }
        public static string DefaultFontFace
        {
            get
            {
                string @value;
                if (ConfigDictionary.TryGetValue("STRDEFAULTFONTFACE", out @value))
                    return @value;
                return "Arial";
            }
        }

        #endregion


        public static Enumerators.RankType ShowRankType
        {
            get
            {
                string rank;
                int val = 0;
                if (ConfigDictionary.TryGetValue("STRSHOWRANK", out rank))
                {
                    val = Convert.ToInt32(rank);
                }
                return (Enumerators.RankType)val;
            }

        }

        public static bool EmailValidation
        {
            get
            {
                return GetIntValue("STREMAILVAL") == 1;
            }
        }

        public static bool ShowPoweredByImage
        {
            get
            {
                string @value;
                ConfigDictionary.TryGetValue("STRSHOWIMAGEPOWEREDBY", out @value);
                return @value == "1";
            }
        }
        public static string PoweredBy
        {
            get
            {
                return String.Format("{0} <a href='https://forum.snitz.com/forum' data-toggle='tooltip' title='{0} Snitz Forums MVC'>Snitz Forums MVC</a> &copy; 2000-{1}", ResourceManager.GetLocalisedString("lblPowered", "labels"), DateTime.UtcNow.Year);
            }
        }
        public static string Copyright
        {
            get
            {
                return GetValue("STRCOPYRIGHT");
            }
        }
        public static bool EmoticonTable
        {
            get
            {
                if (GetIntValue("STRICONS") == 1)
                {
                    return GetIntValue("STRSHOWSMILIESTABLE") == 1;
                }
                return false;
            }
        }

        public static bool AllowEmail
        {
            get
            {
                return GetIntValue("STREMAIL") == 1;
            }
        }

        public static bool ShowQuickReply
        {
            get
            {
                return GetIntValue("STRSHOWQUICKREPLY") == 1;
            }
        }

        public static bool ShowEditedBy
        {
            get
            {
                return GetIntValue("STREDITEDBYDATE") == 1;
            }
        }

        public static string ForumEmail
        {
            get
            {
                string @value = null;
                Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration("~");
                MailSettingsSectionGroup mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;
                if (mailSettings != null) @value = mailSettings.Smtp.From;
                if (string.IsNullOrWhiteSpace(@value))
                {
                    @value = GetValue("STRSENDER");
                }
                if (string.IsNullOrWhiteSpace(@value))
                {
                    throw new ArgumentNullException("ForumEmail","Has not been set");
                }
                return @value;
            }
        }

        public static int MinAge
        {
            get
            {
                return GetIntValue("STRMINAGE", 14);
            }
        }

        public static bool RestrictReg
        {
            get
            {
                return GetIntValue("STRRESTRICTREG") == 1;
            }
        }

        public static bool UniqueEmail
        {
            get
            {
                return GetIntValue("STRUNIQUEEMAIL") == 1;
            }
        }

        public static Enumerators.SubscriptionLevel SubscriptionLevel
        {
            get
            {
                return (Enumerators.SubscriptionLevel)GetIntValue("STRSUBSCRIPTION");
            }
        }

        public static bool ShowDebug { get; set; }

        public static void Update(string[] updates)
        {
            using (SnitzDataContext db = new SnitzDataContext())
            {
                ConfigNew conf = null;
                try
                {
                    foreach (string cVar in updates)
                    {
                        conf = db.SingleOrDefault<ConfigNew>("SELECT * FROM " + db.ForumTablePrefix + "CONFIG_NEW WHERE C_VARIABLE=@0", cVar.ToUpper()) ?? new ConfigNew();

                        if (conf.ID > 0)
                        {
                            conf.Value = ConfigDictionary[cVar];
                            conf.Update(new String[] { "C_VALUE" });

                        }
                        else
                        {
                            conf.Variable = cVar;
                            conf.Value = ConfigDictionary[cVar];
                            conf.Insert();
                        }

                    }
                    Reset();
                }
                catch (Exception )
                {
                    var test = conf;
                }

            }
        }


    }
}
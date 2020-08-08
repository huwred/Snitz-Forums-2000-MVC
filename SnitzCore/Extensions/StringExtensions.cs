using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using SnitzCore.Utility;


namespace SnitzCore.Extensions
{
    public static partial class Extensions
    {
        public static bool IsMyDomain(this HttpContextBase context)
        {
            if (context.Request.Url != null && (context.Request.UrlReferrer != null && context.Request.Url.Host == context.Request.UrlReferrer.Host))
            {
                return true;
            }
            return false;
        }
        //public static string TruncateLongString(this string str, int maxLength)
        //{
        //    if (String.IsNullOrWhiteSpace(str))
        //        return str;
        //    return str.Substring(0, Math.Min(str.Length, maxLength));
        //}
        public static string ToLangNum(this long i)
        {
            CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
            if (ci.TwoLetterISOLanguageName == "fa")
            {
                return i.ConvertDigitChar(ci);
            }
            return i.ToString();
        }
        public static string ToLangNum(this int i)
        {
            CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
            if (ci.TwoLetterISOLanguageName == "fa")
            {
                return i.ConvertDigitChar(ci);
            }
            return i.ToString();
        }
        /// <summary>
        /// Converts a FormCollection into a Json Array
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static string ToJson(this FormCollection collection)
        {
            var list = new Dictionary<string, string>();
            foreach (string key in collection.Keys)
            {
                if (!(key.EndsWith("RequestVerificationToken", StringComparison.Ordinal)))
                    list.Add(key, collection[key]);
            }
            return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(list);
        }

        /// <summary>
        /// convert comma seperated list of integers to a IEnumerable<int/>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IEnumerable<int> StringToIntList(this string str)
        {
            if (String.IsNullOrEmpty(str))
                yield break;

            foreach (var s in str.Split(','))
            {
                int num;
                if (int.TryParse(s, out num))
                    yield return num;
            }
        }
        /// <summary>
        /// Turns the first letter of a string to Uppercase
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string UppercaseFirst(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
        /// <summary>
        /// Replaces unwanted characters in a path string with underscores
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sanitize(this string str)
        {
            var res = Regex.Replace(str, "[ :?/]", "_");
            return Regex.Replace(res, "__", "_");
        }

        /// <summary>
        /// Converts a Snitz date string into a DateTime Object
        /// </summary>
        /// <param name="forumdate">string to convert</param>
        /// <returns></returns>
        public static DateTime? ToDateTime(this string forumdate)
        {
            //CultureInfo ci = CultureInfo.CreateSpecificCulture("en-GB");
            if (string.IsNullOrWhiteSpace(forumdate))
            {
                return null;
            }
            try
            {
                //pad the forumdate incase we are converting the DOB and trim incase image
                //var newdate = DateTime.ParseExact(forumdate.PadRight(14, '0').Substring(0, 14), "yyyyMMddHHmmss", ci);
                return DateTime.ParseExact(forumdate.PadRight(14, '0').Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return null;
            }

        }
        public static DateTime ToDateTime(this string forumdate, bool notnull)
        {
            //CultureInfo ci = CultureInfo.CreateSpecificCulture("en-GB");

            try
            {
                //pad the forumdate incase we are converting the DOB and trim incase image
                //var newdate = DateTime.ParseExact(forumdate.PadRight(14, '0').Substring(0, 14), "yyyyMMddHHmmss", ci);
                return DateTime.ParseExact(forumdate.PadRight(14, '0').Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }

        }


        /// <summary>
        /// turns a string into it's equivalent enum value
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">string to convert</param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        /// <summary>
        /// Generates fully qualified url for the sitemap controller
        /// </summary>
        /// <param name="url"></param>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="routeValues"></param>
        /// <returns></returns>
        public static string QualifiedAction(this UrlHelper url, string actionName, string controllerName, object routeValues = null)
        {
            return url.Action(actionName, controllerName, routeValues, url.RequestContext.HttpContext.Request.Url.Scheme);
        }


        public static Type GetNonNullableModelType(ModelMetadata modelMetadata)
        {
            Type realModelType = modelMetadata.ModelType;

            Type underlyingType = Nullable.GetUnderlyingType(realModelType);
            if (underlyingType != null)
            {
                realModelType = underlyingType;
            }
            return realModelType;
        }

        #region Private helper methods   
        private static readonly SelectListItem[] SingleEmptyItem = { new SelectListItem { Text = "", Value = "" } };

        /// <summary>
        /// lowerceses words
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string ToSentenceCase(this string str)
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]));
        }

        /// <summary>
        /// turns a camel case string into seperate words
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Titleize(this string text)
        {
            return text.ToSentenceCase();
        }

        private static string ConvertDigitChar(this int digit, CultureInfo destination)
        {
            string res = digit.ToString();
            for (int i = 0; i <= 9; i++)
            {
                res = res.Replace(i.ToString(), destination.NumberFormat.NativeDigits[i]);
            }
            return res;
        }
        private static string ConvertDigitChar(this long digit, CultureInfo destination)
        {
            string res = digit.ToString();
            for (int i = 0; i <= 9; i++)
            {
                res = res.Replace(i.ToString(), destination.NumberFormat.NativeDigits[i]);
            }
            return res;
        }
        #endregion
    }


}

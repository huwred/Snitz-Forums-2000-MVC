using System;
using System.Globalization;
using System.Web;
using LangResources.Utility;
using SnitzCore.Utility;

namespace SnitzCore.Extensions
{
    public static partial class Extensions
    {
        /// <summary>
        /// converts a DateTime object into a Snitz date string
        /// </summary>
        /// <param name="time">DateTime to convert</param>
        /// <returns>Snitz date formatted string</returns>
        public static string ToSnitzDate(this DateTime time)
        {
            return time.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Converts DateTime to ISO8601 date format
        /// </summary>
        /// <param name="date"></param>
        /// <returns>ISO8601 date string</returns>
        public static string ToISO8601Date(this DateTime date)
        {
            //2008-07-17T09:24:17Z
            CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
            //DateTime dtForum = fDate.ToForumDateTime();
            return date.ToString("yyyy-MM-ddTHH:mm:ssZ", ci);

        }
        /// <summary>
        /// Converts DateTime to client Time zone
        /// </summary>
        /// <param name="date"></param>
        /// <returns>CClient DateTime</returns>
        public static DateTime ToClientTime(this DateTime? date)
        {
            var timeOffSet = HttpContext.Current.Session["timezoneoffset"];  // read the value from session

            if (timeOffSet != null)
            {
                var offset = int.Parse(timeOffSet.ToString());
                if (date != null)
                {
                    date = date.Value.AddMinutes(-1 * offset);

                    return date.Value;
                }
            }

            // if there is no offset in session return the datetime in local timezone
            return date.Value.ToUniversalTime(); //.ToLocalTime();
        }
        public static DateTime ToClientTime(this DateTime date)
        {
            var timeOffSet = HttpContext.Current.Session["timezoneoffset"];  // read the value from session

            if (timeOffSet != null)
            {
                var offset = int.Parse(timeOffSet.ToString());
                date = date.AddMinutes(-1 * offset);

                return date;
            }

            // if there is no offset in session return the datetime in local timezone
            return date.ToUniversalTime(); //.ToLocalTime();
        }

        public static string ToSnitzServerDateString(this DateTime utcdate, int offset)
        {
            var forumdate = utcdate.AddHours(offset);
            return forumdate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a datetime object using the forums defined format
        /// </summary>
        /// <param name="date"></param>
        /// <param name="showtime"></param>
        /// <returns></returns>
        public static string ToFormattedString(this DateTime date, string TimeStr, bool showtime = true)
        {
            if (date == DateTime.MinValue)
            {
                return "";
            }
            CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
            //var dateformat = Config.DateStr + (showtime ? " " + Config.TimeStr : "");
            var dateformat = ResourceManager.GetLocalisedString("dateLong", "dateFormat");
            var result = "";

            if (ci.TwoLetterISOLanguageName.ToLower() == "fa")
            {
                if (showtime)
                {
                    dateformat = dateformat + " " + TimeStr;
                }
                PersianCalendar persianCal = new PersianCalendar();
                CalendarUtility persianUtil = new CalendarUtility(persianCal, dateformat);
                CultureInfo ic = CultureInfo.CreateSpecificCulture("fa-IR");

                result = persianUtil.DisplayDate(date, ic);

            }
            else
            {
                if (showtime)
                {
                    dateformat = dateformat + " " + TimeStr;
                }
                result = date.ToString(dateformat, ci);
            }

            return result;
        }


    }
}
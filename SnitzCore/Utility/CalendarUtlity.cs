// /*
// ####################################################################################################################
// ##
// ## CalendarUtlity
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
using System.Globalization;


namespace SnitzCore.Utility
{
    public class CalendarUtility
    {
        private Calendar thisCalendar;
        private CultureInfo targetCulture;
        private string specifier;

        public CalendarUtility(Calendar cal, string format)
        {
            this.thisCalendar = cal;
            this.specifier = format;

        }

        private bool CalendarExists(CultureInfo culture)
        {
            this.targetCulture = culture;
            return Array.Exists(this.targetCulture.OptionalCalendars,
                this.HasSameName);
        }

        private bool HasSameName(Calendar cal)
        {
            if (cal.ToString() == thisCalendar.ToString())
                return true;
            return false;
        }

        public string DisplayDate(DateTime dateToDisplay, CultureInfo culture)
        {
            DateTimeOffset displayOffsetDate = dateToDisplay;
            return DisplayDate(displayOffsetDate, culture);
        }

        public string DisplayDate(DateTimeOffset dateToDisplay, CultureInfo culture)
        {
            //string format = "";

            if (this.CalendarExists(culture))
            {
                culture.DateTimeFormat.Calendar = this.thisCalendar;
                return dateToDisplay.ToString(specifier, culture);
            }
            var months = new[]
            {
                    "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی",
                    "بهمن", "اسفند", ""
                };
            //13:44 30 ???????? 1399
            var result = "";
            string separator = specifier.Contains("/") ? "/" : specifier.Contains("-") ? "-" : " ";
            var parts = specifier.Split(new[] { ' ', '-', '/' });
            int idx = 0;
            while (idx < parts.Length)
            {

                switch (parts[idx][0])
                {
                    case 'd':
                        result += thisCalendar.GetDayOfMonth(dateToDisplay.DateTime).ToString(parts[idx]);
                        break;
                    case 'M':
                        var test = culture.DateTimeFormat.MonthNames;
                        var m = thisCalendar.GetMonth(dateToDisplay.DateTime);
                        result += months[m - 1];
                        break;
                    case 'y':
                        result += thisCalendar.GetYear(dateToDisplay.DateTime).ToString(new String('0', parts[idx].Length));
                        break;
                    case 'H':
                        result += (dateToDisplay.DateTime).ToString(parts[idx]);
                        break;
                    default:
                        break;
                }
                result += separator;
                idx += 1;
            }
            return result;
        }
    }
}


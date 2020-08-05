// /*
// ####################################################################################################################
// ##
// ## OfflineActionAttribute
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
using System.IO;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LangResources.Utility;
using SnitzCore.Utility;

namespace SnitzCore.Filters
{
    public class OfflineActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ipAddress = Common.GetUserIP(HttpContext.Current);

            var offlineHelper = new OfflineHelper(ipAddress,
                 filterContext.HttpContext.Server.MapPath);
            if (offlineHelper.ThisUserShouldBeOffline)
            {
                //Yes, we are "down for maintenance" for this user
                if (filterContext.IsChildAction)
                {
                    filterContext.Result = new ContentResult { Content = string.Empty };
                    return;
                }

                filterContext.Result = new ViewResult
                {
                    ViewName = "Offline"
                };
                var response = filterContext.HttpContext.Response;
                response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                response.TrySkipIisCustomErrors = true;

                return;
            }

            //otherwise we let this through as normal
            base.OnActionExecuting(filterContext);
        }
    }

    public class OfflineFileData
    {
        internal const string OfflineFilePath = "~/App_Data/offline_file.txt";
        //The offline file contains three fields separated by the 'TextSeparator' char
        //a) datetimeUtc to go offline
        //b) the ip address to allow through
        //c) Message to show the user

        private const char TextSeparator = '|';
        private static string DefaultOfflineMessage = ResourceManager.GetLocalisedString("defaultMessage", "Offline");

        /// <summary>
        /// This contains the datatime when the site should go offline should be offline
        /// </summary>
        public DateTime TimeWhenSiteWillGoOfflineUtc { get; private set; }

        /// <summary>
        /// This contains the IP address of the authprised person to let through
        /// </summary>
        public string IpAddressToLetThrough { get; private set; }

        /// <summary>
        /// A message to display in the Offline View
        /// </summary>
        public string Message { get; private set; }


        public OfflineFileData(string offlineFilePath)
        {
            var offlineContent = File.ReadAllText(offlineFilePath).Split(TextSeparator);

            DateTime parsedDateTime;
            TimeWhenSiteWillGoOfflineUtc = DateTime.TryParse(offlineContent[0],
                null, System.Globalization.DateTimeStyles.RoundtripKind,
                out parsedDateTime) ? parsedDateTime : DateTime.UtcNow;
            IpAddressToLetThrough = offlineContent[1];
            Message = offlineContent[2];

        }
        public static void SetOffline(int delayByMinutes,
                string currentIpAddress, string optionalMessage,
                Func<string, string> mapPath)
        {
            var offlineFilePath = mapPath(OfflineFilePath);

            var fields = string.Format("{0:O}{1}{2}{1}{3}",
                DateTime.UtcNow.AddMinutes(delayByMinutes), TextSeparator,
                currentIpAddress, optionalMessage ?? DefaultOfflineMessage);


            File.WriteAllText(offlineFilePath, fields);

        }

        public static void RemoveOffline(Func<string, string> mapPath)
        {
            var offlineFilePath = mapPath(OfflineFilePath);
            File.Delete(offlineFilePath);
        }
    }

    public class OfflineHelper
    {
        public static OfflineFileData OfflineData { get; private set; }

        /// <summary>
        /// This is true if we should redirect the user to the Offline View
        /// </summary>
        public bool ThisUserShouldBeOffline { get; private set; }

        public OfflineHelper(string currentIpAddress, Func<string, string> mapPath)
        {

            var offlineFilePath = mapPath(OfflineFileData.OfflineFilePath);
            if (File.Exists(offlineFilePath))
            {
                //The existance of the file says we want to go offline

                if (OfflineData == null)
                    //We need to read the data as new file was found
                    OfflineData = new OfflineFileData(offlineFilePath);

                ThisUserShouldBeOffline =
                    DateTime.UtcNow.Subtract(OfflineData.TimeWhenSiteWillGoOfflineUtc)
                        .TotalSeconds > 0
                    && currentIpAddress != OfflineData.IpAddressToLetThrough;
            }
            else
            {
                //No file so not offline
                OfflineData = null;
            }
        }
    }
}

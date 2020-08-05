// /*
// ####################################################################################################################
// ##
// ## LogActionAttribute
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
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SnitzCore.Extensions;
using SnitzCore.Utility;

namespace SnitzCore.Filters
{
    public class LogActionAttribute : ActionFilterAttribute
    {
        private static object _synRoot = new object();
        private static string _path = HttpContext.Current.Server.MapPath("~/App_Data/");
        public int LogRequests { get; set; }
        public int LogSearch { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.ActionDescriptor.GetCustomAttributes(typeof(DoNotLogActionFilterAttribute), false).Any())
            {
                base.OnActionExecuting(filterContext);
                return;
            }
            if (LogRequests != 1)
            {
                base.OnActionExecuting(filterContext);
                return;
            }
            if (LogSearch == 1)
            {
                if (filterContext.ActionDescriptor.ActionName != "Search")
                {
                    base.OnActionExecuting(filterContext);
                    return;
                }
            }


            var user = filterContext.HttpContext.User.Identity.Name;
            var url = filterContext.HttpContext.Request.RawUrl;
            var filters = filterContext.ActionParameters;
            string parms = "";

            if (user == null)
            {
                user = Common.GetUserIP(HttpContext.Current);
            }
            if (filters.ContainsKey("form"))
            {
                FormCollection f = (filters["form"] as FormCollection);
                parms = f.ToJson();
            }
            if (filters.ContainsKey("viewModel"))
            {
                parms = Newtonsoft.Json.JsonConvert.SerializeObject(filters["viewModel"]);
            }
            Task.Factory.StartNew(() =>
            {
                lock (_synRoot)
                {

                    var msg = String.Format(":{0}:{1} {2}", user, url, parms);
                    var data = string.Format("log:{0}{1}{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff"), msg, Environment.NewLine);
                    System.IO.File.AppendAllText(_path + "log.txt", data);
                }
            });

            base.OnActionExecuting(filterContext);
        }
    }

    public class DoNotLogActionFilterAttribute : Attribute
    {

    }


}

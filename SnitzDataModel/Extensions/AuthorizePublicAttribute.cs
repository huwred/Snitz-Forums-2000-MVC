// /*
// ####################################################################################################################
// ##
// ## AuthorizePublicAttribute
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SnitzConfig;
using WebMatrix.WebData;

namespace SnitzDataModel.Extensions
{
    /// <summary>
    /// Use SnitzConfig parameter to override Authorization requirements
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizePublicAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// The SnitzConfig variable to check for access
        /// </summary>
        public string SnitzVar { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (ClassicConfig.GetIntValue(SnitzVar.ToUpper()) == 1)
            {
                return true;
            }
            this.Roles = null;

            if (ClassicConfig.GetIntValue("INTCLUBEVENTS") == 1)
                this.Roles = ClassicConfig.GetValue("STRRESTRICTROLES");

            return base.AuthorizeCore(httpContext);
        }
    }

    public class SuperAdminAttribute : AuthorizeAttribute
    {
        public string AllowedUser { get; set; }
        //public override void OnAuthorization(AuthorizationContext filterContext)
        //{
        //    var result = new ViewResult();
        //    result.ViewName = "Login.cshtml";        //this can be a property you don't have to hard code it
        //    result.MasterName = "_Layout.cshtml";    //this can also be a property
        //    result.ViewBag.Message = this.Message;
        //    filterContext.Result = result;
        //}

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (ClassicConfig.GetValue("STRADMINUSER","") == "")
            {
                return true;
            }
            else
            {
                if(WebSecurity.CurrentUserName == ClassicConfig.GetValue("STRADMINUSER","") || WebSecurity.CurrentUserName == AllowedUser)
                    return true;
            }

            return false;
        }

        protected override  void HandleUnauthorizedRequest(AuthorizationContext filterContext) {
            // Returns HTTP 401 - see comment in HttpUnauthorizedResult.cs.
            //filterContext.Result = new HttpUnauthorizedResult();
            string action = filterContext.ActionDescriptor.ActionName;
            object[] test = filterContext.ActionDescriptor.GetCustomAttributes(true);

            if (test.Length > 1)
            {
                for (int i = 0; i < test.Length; i++)
                {
                    if(test[i] is DisplayNameAttribute )
                    {
                        action = ((DisplayNameAttribute) test[i]).DisplayName;
                    }
                }
            }

            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "action", "Index" },
                    { "controller", "Admin" },
                    {"id", action }
                });
        }

        public void properties()
        {

        }
    }
}
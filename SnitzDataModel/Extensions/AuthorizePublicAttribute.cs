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
using System.Web;
using System.Web.Mvc;
using SnitzConfig;

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
}
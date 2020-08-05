// /*
// ####################################################################################################################
// ##
// ## RemoteAttribute
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

using System.Web.Mvc;
using LangResources.Utility;

namespace SnitzCore.Filters
{
    public class Remote : RemoteAttribute
    {
        public string ResourceName { get; set; }
        private string _defaultErrorMessage = "'{0}' does not exist.";
        public Remote(string routeName)
            : base(routeName)
        {
        }

        public Remote(string action, string controller)
            : base(action, controller)
        {
        }

        public Remote(string action, string controller,
            string areaName)
            : base(action, controller, areaName)
        {
        }


        public override string FormatErrorMessage(string name)
        {
            _defaultErrorMessage = ResourceManager.GetLocalisedString(ResourceName ?? name, "ErrorMessage");
            return string.Format(_defaultErrorMessage, name);
        }

    }
}

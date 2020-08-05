// HttpParamActionAttribute-17/06/2020
// Huw Reddick
using System;
using System.Reflection;
using System.Web.Mvc;


namespace SnitzCore.Filters
{
    /// <summary>
    /// HttpParamActionAttribute
    /// Allows multiple actions against a single view
    /// </summary>
    public class HttpParamActionAttribute : ActionNameSelectorAttribute
    {
        public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
        {
            if (actionName.Equals(methodInfo.Name, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (!actionName.Equals("Action", StringComparison.InvariantCultureIgnoreCase))
                return false;

            var request = controllerContext.RequestContext.HttpContext.Request;
            return request[methodInfo.Name] != null;
        }
    }
}
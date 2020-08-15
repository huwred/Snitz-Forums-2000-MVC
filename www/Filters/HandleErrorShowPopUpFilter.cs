using System;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using SnitzCore.Filters;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using WWW.Helpers;

namespace WWW.Filters
{
    public class HandleErrorShowPopUpFilter : IExceptionFilter, IResultFilter
    {
        public bool ShowPopUp { get; set; }

        public HandleErrorShowPopUpFilter(bool showPopup)
        {
            this.ShowPopUp = showPopup;
        }

        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is HttpException)
                ShowPopUp = false;
            if (!ShowPopUp)
            {
                return;
            }
            if (!filterContext.HttpContext.Items.Contains("apperror"))
            {
                filterContext.HttpContext.Items.Add("apperror", "apperror");
            }
            
            string controllerName = (string) filterContext.RouteData.Values["controller"];
            string actionName = (string) filterContext.RouteData.Values["action"];
            HandleErrorInfo model = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);

            filterContext.Controller.TempData["errorpopup"] =
                new PartialViewSerializer().RenderPartialViewToString(filterContext.Controller, "popupError", model);

            var urlCookie = filterContext.HttpContext.Request.Cookies["preservedurl"];
            if (urlCookie != null && urlCookie.Value != null)
            {
                filterContext.ExceptionHandled = true;
                filterContext.Result = new RedirectResult(urlCookie.Value);
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            string originController = filterContext.RouteData.Values["controller"].ToString();
            string originAction = filterContext.RouteData.Values["action"].ToString();

            try
            {
                Type actionType = null;
                System.Reflection.MethodInfo[] matchedMethods = filterContext.Controller.GetType().GetMethods(
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == originAction).ToArray<System.Reflection.MethodInfo>();
                if (matchedMethods.Length > 1)
                {
                    string attributeTypeString = "HttpGet"; // Change this to "HttpPut" or the text of any custom attribute filter
                    foreach (System.Reflection.MethodInfo methodInfo in matchedMethods)
                    {
                        if (!methodInfo.CustomAttributes.Any()) { continue; }
                        // An alternative below is to explicitly check against a defined attribute type (e.g. `ca.AttributeType == ...`).
                        if (methodInfo.CustomAttributes.FirstOrDefault(ca => ca.ToString().IndexOf(attributeTypeString) == 0) != null)
                        {
                            actionType = methodInfo.ReturnType;
                            break; // Break out of the 'foreach' loop since a match was found
                        }
                        if (methodInfo.GetCustomAttributes(typeof(DoNotLogActionFilterAttribute), false).Any())
                        {
                            return;
                        }
                    }
                }
                else
                {
                    if (matchedMethods.Any() && matchedMethods[0].GetCustomAttributes(typeof(DoNotLogActionFilterAttribute), false).Any())
                    {
                        return;
                    }
                    var check = filterContext.Controller.GetType().GetMethod(originAction, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (check == null)
                        return;
                    actionType = check.ReturnType;
                }
                if (actionType != typeof(ActionResult))
                return;
                
            }
            catch (Exception)
            {
                
                throw;
            }


            if (!filterContext.IsChildAction && originAction != "JavaScriptSettings"
                && originAction != "GetUpcomingEvents")
            {
                SnitzCookie.SetCookie("preservedurl", filterContext.HttpContext.Request.Url.AbsoluteUri);
                
            }
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {

        }
    }

}
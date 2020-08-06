using System.Web.Mvc;
using SnitzCore.Filters;
using SnitzDataModel.Extensions;
using WWW.Filters;

namespace WWW
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new HandleErrorShowPopUpFilter(true));//this is ur filter
            filters.Add(new LogActionAttribute());
        }
    }
}
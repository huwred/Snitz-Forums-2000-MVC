using System.Web;
using Hangfire.Dashboard;
using SnitzDataModel.Extensions;

namespace WWW.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {

        public bool Authorize(DashboardContext context)
        {
            if (HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return HttpContext.Current.User.IsInRole("Administrator");
            }
            return false;

        }
    }

}
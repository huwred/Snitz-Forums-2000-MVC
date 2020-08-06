using System.Web.Mvc;
using System.Web.UI;
using WWW.Filters;

namespace WWW.Controllers
{
    public class JavaScriptSettingsController : Controller
    {
        [ExternalJavaScriptFile]
        [OutputCache(Duration = 86400, VaryByParam = "none", Location = OutputCacheLocation.Client, NoStore = true)]
        public PartialViewResult Index()
        {
            return PartialView();
        }

        [ExternalJavaScriptFile]
        [OutputCache(Duration = 86400, VaryByParam = "none", Location = OutputCacheLocation.Client, NoStore = true)]
        public PartialViewResult Resources()
        {
            return PartialView();
        }
    }

}
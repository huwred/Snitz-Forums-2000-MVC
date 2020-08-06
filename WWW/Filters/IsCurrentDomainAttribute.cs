using System.IO;
using System.Security;
using System.Web;
using System.Web.Mvc;

namespace WWW.Filters
{
    public class IsCurrentDomainAttribute : AuthorizeAttribute
    {
        public bool ThrowNotFoundException { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Request.IsAuthenticated)
            {
                return true;
            }
            var isLocal = false;
            if (httpContext.Request.UrlReferrer != null)
            {
                string requestDomianName = httpContext.Request.UrlReferrer.Authority; //.Host;
                if (httpContext.Request.Url != null)
                {
                    string ourDomain = httpContext.Request.Url.Authority;
                    isLocal = requestDomianName == ourDomain;

                    if (!isLocal && ThrowNotFoundException)
                    {
                        throw new FileNotFoundException();
                    }

                }
            }
            if (!isLocal && ThrowNotFoundException)
            {
                throw new FileNotFoundException();
            }
            return isLocal;
        }
    }
}
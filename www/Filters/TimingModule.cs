using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace WWW.Filters
{
    public class TimingModule : IHttpModule
    {

        public void Init(HttpApplication context)
        {
            //if (HttpContext.Current.IsDebuggingEnabled)
            //{

            //}
            context.PreRequestHandlerExecute += delegate (object sender, EventArgs e)
            {
                //Set Page Timing Star
                HttpContext requestContext = ((HttpApplication)sender).Context;
                Stopwatch timer = new Stopwatch();
                requestContext.Items["Timer"] = timer;
                timer.Start();
            };
            context.PostRequestHandlerExecute += delegate (object sender, EventArgs e)
            {
                HttpApplication app = (HttpApplication)sender;
                HttpContext httpContext = app.Context;
                HttpResponse response = httpContext.Response;
                Stopwatch timer = (Stopwatch)httpContext.Items["Timer"];
                timer.Stop();
            };
        }
        
        public void Dispose() { /* Not needed */ }
    }

}
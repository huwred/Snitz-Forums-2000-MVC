using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WWW.Controllers
{
    public class SearchController : CommonController
    {
        // GET: Search
        public ActionResult Index(int id,string phrase = "")
        {
            return RedirectToAction("Search","Forum",new {id,phrase});
        }
    }
}
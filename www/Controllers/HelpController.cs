using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using BbCodeFormatter;
using SnitzDataModel;
using SnitzDataModel.Database;


namespace WWW.Controllers
{
    public class HelpController : CommonController
    {

        public HelpController()
        {

            Dbcontext = new SnitzDataContext();

        }

        [OutputCache(Duration = int.MaxValue, VaryByParam = "refresh", Location = OutputCacheLocation.Server,
             NoStore = true)]
        public ActionResult Index(string refresh = null)
        {
            FaqProcessor faq = new FaqProcessor();
            return View("IndexNew",faq);
        }

        [OutputCache(Duration = int.MaxValue, VaryByParam = "refresh", Location = OutputCacheLocation.Server,
             NoStore = true)]
        public ActionResult Admin(string refresh = null)
        {
            FaqProcessor faq = new FaqProcessor();
            return View(faq);
        }
        /// <summary>
        /// Create/Edit a question
        /// </summary>
        /// <param name="id">question id, -1 for new</param>
        /// <param name="category">category id</param>
        /// <returns></returns>
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult Edit(int id, int category)
        {
            FaqProcessor faq = new FaqProcessor();
            Question q = new Question() {CategoryId = category, Id = faq.NextId};
            if (id > 0)
                q = faq.GetQuestion(id);
            ViewBag.CategoryList = faq.Categories();
            return View("popEdit", q);
        }

        [HttpPost]
        public RedirectResult Edit(Question model)
        {
            FaqProcessor faq = new FaqProcessor();
            model.Answer = BbCodeProcessor.Post(model.Answer);

            faq.SaveQuestion(model);

            return
                new RedirectResult(Url.Action("Index", new {refresh = Guid.NewGuid().ToString("N")}) + "#q_" + model.Id);

        }
        /// <summary>
        /// Delete Question
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult Delete(int id)
        {
            FaqProcessor faq = new FaqProcessor();
            faq.DeleteQuestion(id);
            var redirectUrl = Url.Action("Index", new { refresh = Guid.NewGuid().ToString("N") });
            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Index", new {refresh = Guid.NewGuid().ToString("N")});
        }

        [HttpPost]
        public JsonResult AddCategory(FormCollection form)
        {
            FaqProcessor faq = new FaqProcessor();
            var maxCatId = faq.Categories().Select(x => x.Key).Max();
            faq.AddCategory(maxCatId + 1, form["Description"]);
            var redirectUrl = Url.Action("Index", new { refresh = Guid.NewGuid().ToString("N") });
            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult EditCategory(int id)
        {
            FaqProcessor faq = new FaqProcessor();
            var c = faq.GetCategory(id);
            FAQCategory fc = new FAQCategory();
            fc.Id = c.Id;
            fc.Description = c.Description;
            fc.Roles = c.Roles;

            return View("popEditCategory", fc);
        }
        [HttpPost]
        public RedirectResult EditCategory(FAQCategory cat)
        {
            FaqProcessor faq = new FaqProcessor();
            faq.SaveCategory(cat);
            return
                new RedirectResult(Url.Action("Index", new {refresh = Guid.NewGuid().ToString("N")}));
        }

        /// <summary>
        /// Delete Question
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelCategory(int id)
        {
            FaqProcessor faq = new FaqProcessor();
            faq.DeleteCategory(id);

            var redirectUrl = Url.Action("Index", new { refresh = Guid.NewGuid().ToString("N") });
            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("Index", new {refresh = Guid.NewGuid().ToString("N")});
        }
    }
}
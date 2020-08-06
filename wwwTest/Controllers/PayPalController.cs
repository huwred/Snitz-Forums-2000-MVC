using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SiteManage.Models;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Controllers;
using SnitzMembership;


namespace SiteManage.Controllers
{
    public class PayPalController : BaseController
    {
        /// <summary>
        /// Donation Page loader
        /// </summary>
        /// <param name="id">Name of View Template</param>
        /// <returns>A donation page</returns>
        public ActionResult Index(string id = "PayPalIndex")
        {

            return View();
        }

        /// <summary>
        /// Get Forum Donations
        /// </summary>
        /// <returns>Partial View Donation List</returns>
        public PartialViewResult Donations()
        {
            var db = new PaypalRepository();

            return PartialView(db.GetDonations());
        }

        #region PayPal buttons
        /// <summary>
        /// Render Donate form
        /// </summary>
        /// <param name="id">Id of Product for button</param>
        /// <returns>Partial View form</returns>
        public PartialViewResult DonateNow(int id = 0)
        {
            var db = new PaypalRepository();
            return PartialView(db.GetProductById(id));
        }
        /// <summary>
        /// Render Subscription form
        /// </summary>
        /// <param name="id">Id of Product for button</param>
        /// <returns>Partial View form</returns>
        public PartialViewResult Subscribe(int id = 0)
        {
            var db = new PaypalRepository();
            return PartialView(db.GetProductById(id));
        }
        #endregion

        /// <summary>
        /// Hidden PayPal form
        /// </summary>
        /// <param name="formCollection"></param>
        /// <returns>Redirects to hidden form and posts to PayPal</returns>
        public ActionResult ValidateCommand(FormCollection formCollection)
        {
            Dictionary<string, string> form = formCollection.AllKeys.ToDictionary(k => k, v => formCollection[v]);
            bool useSandbox = ClassicConfig.GetIntValue("INTPAYPALSANDBOX")==1;

            try
            {

                PayPalModel paypal = new PayPalModel(useSandbox)
                {
                    item_name = form["Name"],
                    item_number = WebSecurity.CurrentUserId.ToString(),
                    amount = form["Amount"],
                    currency_code = form["Currency"]
                };
                string buttontype = form["Mode"];
                if (buttontype == "subscription")
                {
                        paypal.cmd = "_xclick-subscriptions";
                        paypal.p3 = Convert.ToInt32(form["RecurLength"]);
                        paypal.t3 = form["RecurPeriod"];                    
                }

                return View(paypal);
            }
            catch (Exception ex)
            {
                ViewBag.Err = ex.Message;
                return View("Error");
            }

        }


        #region PayPal Configuration
        /// <summary>
        /// Get Products
        /// </summary>
        /// <returns>Partial View Donation List</returns>
        [Authorize(Roles = "Administrator")]
        public PartialViewResult Products()
        {
            var db = new PaypalRepository();

            return PartialView(db.GetProducts());
        }
        /// <summary>
        /// Load Paypal Admin Configuration
        /// </summary>
        /// <returns>PayPal config page</returns>
        [Authorize(Roles = "Administrator")]
        public ActionResult Configuration()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public RedirectToRouteResult SaveConfig(FormCollection form)
        {
            foreach (var key in form.AllKeys)
            {
                if (key.ToLower().StartsWith("str", StringComparison.Ordinal) || key.ToLower().StartsWith("int", StringComparison.Ordinal))
                {
                    string[] amounts = Request.Form.GetValues(key);
                    if (amounts != null)
                        ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting(key.ToUpper(), amounts[0]);
                }
                ClassicConfig.Update(new string[] { key.ToUpper() });
            }
            return RedirectToAction("Configuration");
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public PartialViewResult AddProduct()
        {
            return PartialView("CreateEditProduct", new PayPalProduct());
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public PartialViewResult EditProduct(int id)
        {
            var db = new PaypalRepository();
            var product = db.GetProductById(id);

            return PartialView("CreateEditProduct", product);
        }
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public RedirectToRouteResult DeleteProduct(int id)
        {
            var db = new PaypalRepository();
            db.DeleteProduct(id);

            return RedirectToAction("Index");
        }
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult UpsertProduct(PayPalProduct product)
        {
            try
            {
                var db = new PaypalRepository();
                db.AddProduct(product);
                return Json("success", JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;

                return Json(e.Message);
            }
            //return PartialView(product);
        }

        #endregion

        #region Donations
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public PartialViewResult AddDonation()
        {
            return PartialView("CreateEditDonation", new PayPalDonation());
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public PartialViewResult EditDonation(int id)
        {
            var db = new PaypalRepository();
            var product = db.GetDonationById(id);

            return PartialView("CreateEditDonation", product);
        }
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public RedirectToRouteResult DeleteDonation(int id)
        {
            var db = new PaypalRepository();
            db.DeleteDonation(id);

            return RedirectToAction("Index");
        }
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult UpsertDonation(PayPalDonation donation)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var db = new PaypalRepository();
                    db.AddDonation(donation);
                    return Json("success", JsonRequestBehavior.AllowGet);

                }
                catch (Exception e)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;

                    return Json(e.Message);
                }
                
            }
            Response.StatusCode = (int)HttpStatusCode.BadRequest;

            return Json("Invalid Data");
        }

        #endregion

        public ActionResult TestPost(FormCollection formCollection)
        {
            var form = formCollection.AllKeys.Any() ? formCollection.AllKeys.ToDictionary(k => k, v => formCollection[v]) : Request.QueryString.AllKeys.ToDictionary(k => k, v => Request.QueryString[v]);

            return View(form);
        }

        #region paypal returns
        public ActionResult Confirmation()
        {
            return View();
        }

        public ActionResult Cancel()
        {
            return View();
        }

        public ActionResult NotifyFromPaypal(FormCollection formCollection)
        {
            var form = formCollection.AllKeys.Any() ? formCollection.AllKeys.ToDictionary(k => k, v => formCollection[v]) : Request.QueryString.AllKeys.ToDictionary(k => k, v => Request.QueryString[v]);

            return View(form);
        }

        #endregion
    }
}
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using SnitzConfig;
using SnitzDataModel;
using SnitzDataModel.Models;

namespace WWW.Controllers
{
    public class AdController : CommonController
    {
        //
        // GET: /Ad/

        public PartialViewResult Index()
        {

            var ads = AdRotator.GetAds(System.Web.HttpContext.Current).Ads;
            int totalWeight = 0;
            foreach (Ad ad in ads)
            {
                totalWeight += ad.Weight;
            }
            var selectedAd = ads[0]; //AdRotator.GetAd(ads, totalWeight);
            if (totalWeight > 0)
            {
                selectedAd = AdRotator.GetAd(ads, totalWeight);
                selectedAd.Impressions += 1;
                AdRotator.Save();
            }
            return PartialView(selectedAd);
        }
        public ActionResult RecordClick(string id)
        {
            // ..
            // log what you need here
            // ..
            var thisUrl = "/";

            var ads = AdRotator.GetAds(System.Web.HttpContext.Current);
            if (ads != null)
            {
                var singleOrDefault = ads.Ads.SingleOrDefault(a => a.Id.ToString() == id);
                if (singleOrDefault != null)
                {
                    singleOrDefault.Clicks += 1;
                    thisUrl = singleOrDefault.Url;
                    AdRotator.Save();
                }
            }
            // finally redirect to the link URL
            return Redirect(thisUrl);
        }

        public PartialViewResult SideBanner()
        {
            var ads = AdRotator.GetAds(System.Web.HttpContext.Current).Ads.Where(a => a.Keyword == "side").ToArray();
            if (ads.Any())
            {
                int totalWeight = 0;
                foreach (Ad ad in ads)
                {
                    totalWeight += ad.Weight;
                }
                var selectedAd = AdRotator.GetAd(ads, totalWeight);
                if (selectedAd != null)
                {
                    selectedAd.Impressions += 1;
                    AdRotator.Save();
                    return PartialView("Index", selectedAd);
                }              
            }
            return null;
        }
        public PartialViewResult TopBanner()
        {
            var ads = AdRotator.GetAds(System.Web.HttpContext.Current).Ads.Where(a => a.Keyword == "top").ToArray();
            int totalWeight = 0;
            if (ads.Any())
            {
                foreach (Ad ad in ads)
                {
                    totalWeight += ad.Weight;
                }
                var selectedAd = AdRotator.GetAd(ads, totalWeight);
                selectedAd.Impressions += 1;
                AdRotator.Save();
                return PartialView("Index", selectedAd);                
            }
            return null;
        }

        public PartialViewResult AddEdit(string id)
        {
            var selectedAd = AdRotator.GetAds(System.Web.HttpContext.Current).Ads.SingleOrDefault(a => a.Id.ToString() == id);

            return PartialView("popAddEditBanner", selectedAd ?? new Ad());
        }

        [HttpPost]
        [Authorize]
        [OutputCache(Location = System.Web.UI.OutputCacheLocation.None)]
        public JsonResult AddEdit(Ad bannerAd )
        {
            string uploadPath = Server.MapPath("~/Content/BannerAds/");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string fileName = null;
            //If there is a file then save it
            if (bannerAd.fileInput != null)
            {
                if (bannerAd.fileInput.ContentLength > Convert.ToInt32(ClassicConfig.GetValue("INTMAXFILESIZE")) * 1024 * 1024)
                {
                    return Json("error|File too large");
                }
                string mimeType = bannerAd.fileInput.ContentType;
               
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    fileName = Path.GetFileName(bannerAd.fileInput.FileName);

                    if (fileName != null)
                        bannerAd.fileInput.SaveAs(Path.Combine(uploadPath, fileName)); //File will be saved in banner folder

                }
            }
            var selectedAd = new Ad();
            selectedAd.Id = Guid.NewGuid();
            selectedAd.Url = bannerAd.Url;
            selectedAd.AltText = bannerAd.AltText;
            if (bannerAd.fileInput != null && fileName != null)
                selectedAd.Image = fileName;
            selectedAd.Keyword = bannerAd.Keyword;
            selectedAd.Impressions = bannerAd.Impressions;
            selectedAd.Clicks = bannerAd.Clicks;
            selectedAd.Weight = bannerAd.Weight;
            selectedAd.Width = bannerAd.Width;
            selectedAd.Height = bannerAd.Height;

            AdRotator.Add(selectedAd);

            var ret = Url.Action("ManageBanners", "Admin");
            return Json(ret);
        }

        public JsonResult ChangeBanner(Ad model)
        {
            // Update model to your db
            var selectedAd = AdRotator.GetAds(System.Web.HttpContext.Current).Ads.SingleOrDefault(a => a.Id == model.Id);
            if (selectedAd != null)
            {
                selectedAd.Url = model.Url;
                selectedAd.AltText = model.AltText;
                selectedAd.Weight = model.Weight;
                AdRotator.Save();
            }
            string message = "Banner added successfully";
            return Json(message, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DeleteBanner(string id)
        {
            // Update model to your db
            
            AdRotator.Delete(id);
            string message = "Delete Success";
            return Json(message, JsonRequestBehavior.AllowGet);
        }
    }
}

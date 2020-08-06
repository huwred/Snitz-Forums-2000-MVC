using System.Web;
using SnitzDataModel;
using SnitzDataModel.Models;

namespace WWW.ViewModels
{
    public class AdminBannersViewModel
    {
        public Ad[] Banners { get; set; }

        public AdminBannersViewModel()
        {
            Banners = AdRotator.GetAds(HttpContext.Current).Ads;
        }
    }
}
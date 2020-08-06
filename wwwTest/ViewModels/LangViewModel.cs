using System.Collections.Generic;
using LangResources.Models;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace WWW.ViewModels
{
    public class LangViewModel
    {
        public List<string> ResourceSets { get; set; } 
        public List<ResourceEntry> Resources { get; set; }

    }

    public class LangUpdateViewModel
    {
        [Required]
        public string ResourceSet { get; set; }
        [Required]
        public string ResourceId { get; set; }
        [AllowHtml]
        public Dictionary<string,string> ResourceTranslations { get; set; }

        public string rownum { get; set; }

    }
}
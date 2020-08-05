


// /*
// ####################################################################################################################
// ##
// ## AdBanner
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Xml.Serialization;

namespace SnitzDataModel.Models
{
    [Serializable()]
    public class Ad
    {
        [XmlElement("ID")]
        public Guid Id { get; set; }
        [XmlElement("Impressions")]
        public int Impressions { get; set; }
        [XmlElement("Width")]
        public int Width { get; set; }
        [XmlElement("Height")]
        public int Height { get; set; }

        [XmlElement("ImageUrl")]
        public string Image { get; set; }
        [Required]
        [XmlElement("NavigateUrl")]
        public string Url { get; set; }
        [Required]
        [XmlElement("AlternateText")]
        public string AltText { get; set; }
        [Required]
        [XmlElement("Keyword")]
        public string Keyword { get; set; }
        [XmlElement("Weight")]
        public int Weight { get; set; }
        [XmlElement("Clicks")]
        public int Clicks { get; set; }

        public HttpPostedFileBase fileInput { get; set; }
        public Ad()
        {
            Clicks = 0;
            Impressions = 0;
            Weight = 0;
            Keyword = "side";
        }
    }
    [Serializable()]
    [System.Xml.Serialization.XmlRoot("AdCollection")]
    public class AdCollection
    {
        [XmlArray("Advertisements")]
        [XmlArrayItem("Ad", typeof(Ad))]
        public Ad[] Ads { get; set; }
    }
}
// /*
// ####################################################################################################################
// ##
// ## SiteMap
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
// *

using System;
using System.Collections.Generic;
using System.Xml;

namespace SnitzCore.Sitemap
{
    public interface ISitemapGenerator
    {
        XmlDocument GenerateSiteMap(IEnumerable<ISitemapItem> items);
    }

    /// <summary>
    /// An interface for sitemap items
    /// </summary>
    public interface ISitemapItem
    {
        /// <summary>
        /// URL of the page.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// The date of last modification of the file.
        /// </summary>
        DateTime? LastModified { get; }

        /// <summary>
        /// How frequently the page is likely to change.
        /// </summary>
        SitemapChangeFrequency? ChangeFrequency { get; }

        /// <summary>
        /// The priority of this URL relative to other URLs on your site. Valid values range from 0.0 to 1.0.
        /// </summary>
        double? Priority { get; }
    }


    public enum SitemapChangeFrequency
    {
        Always,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Never

    }



}
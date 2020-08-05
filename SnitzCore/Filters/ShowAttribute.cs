
// /*
// ####################################################################################################################
// ##
// ## ShowAttribute
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

namespace SnitzDataModel.Validation
{
    /// <summary>
    /// Conditionally shows and hides member profile fields based on
    /// the data in FORUM_CONFIG_NEW
    /// </summary>
    public class ShowAttribute : Attribute
    {
        public string DependentProperty { get; set; }
        public int ShowAttr { get; set; }
        private bool _showme;

        public ShowAttribute(string dependentProperty)
        {
            this.DependentProperty = dependentProperty;
            this._showme = ShowAttr == 1;
        }

        public bool ShowMe()
        {
            return _showme;
        }
    }
}

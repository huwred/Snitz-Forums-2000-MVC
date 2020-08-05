// /*
// ####################################################################################################################
// ##
// ## RequiredAttribute
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using LangResources.Utility;

namespace SnitzCore.Filters
{

    public class RequiredAttribute : System.ComponentModel.DataAnnotations.RequiredAttribute, IClientValidatable
    {
        private string _displayName;

        public RequiredAttribute()
        {
            this.ErrorMessage = ResourceManager.GetLocalisedString("Validation_Required", "ErrorMessage");
        }

        protected override ValidationResult IsValid
            (object value, ValidationContext validationContext)
        {
            _displayName = validationContext.DisplayName;
            return base.IsValid(value, validationContext);
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(this.ErrorMessage, _displayName);
        }

        IEnumerable<ModelClientValidationRule> IClientValidatable.GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {

            yield return new ModelClientValidationRule
            {
                ErrorMessage = string.Format(this.ErrorMessage, metadata.DisplayName),
                ValidationType = "required"
            };
        }
    }
}

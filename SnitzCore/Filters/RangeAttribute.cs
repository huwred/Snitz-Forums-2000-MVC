// /*
// ####################################################################################################################
// ##
// ## RangeAttribute
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using LangResources.Utility;

namespace SnitzCore.Filters
{
    public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute, System.Web.Mvc.IClientValidatable
    {
        private string _displayName;
        private string _errorMsg;


        public RangeAttribute(int minimum, int maximum) : base(minimum, maximum)
        {
            //_errorMsg = ResourceManager.GetLocalisedString(this.ErrorMessage, "ErrorMessage");
        }

        public RangeAttribute(double minimum, double maximum) : base(minimum, maximum)
        {
            //_errorMsg = ResourceManager.GetLocalisedString(this.ErrorMessage, "ErrorMessage");

        }

        public RangeAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum)
        {
            //_errorMsg = ResourceManager.GetLocalisedString(this.ErrorMessage, "ErrorMessage");
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(_errorMsg, _displayName);
        }
        protected override ValidationResult IsValid
            (object value, ValidationContext validationContext)
        {
            _displayName = validationContext.DisplayName;
            return base.IsValid(value, validationContext);
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            _errorMsg = ResourceManager.GetLocalisedString(this.ErrorMessage, "ErrorMessage");
            var rule = new System.Web.Mvc.ModelClientValidationRule
            {
                ErrorMessage = string.Format(_errorMsg, metadata.DisplayName, Minimum),
                ValidationType = "range"
            };

            rule.ValidationParameters.Add("min", Minimum);
            rule.ValidationParameters.Add("max", Maximum);

            yield return rule;
        }
    }
}

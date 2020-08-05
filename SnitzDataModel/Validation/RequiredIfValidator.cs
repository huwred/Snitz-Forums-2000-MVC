




// /*
// ####################################################################################################################
// ##
// ## RequiredIfValidator
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
using System.Web.Mvc;
using LangResources.Utility;
using SnitzConfig;

namespace SnitzDataModel.Validation
{
    /// <summary>
    /// Validator for the RequiredIf Attribute
    /// </summary>
    public class RequiredIfValidator : System.Web.Mvc.DataAnnotationsModelValidator<RequiredIfAttribute>
    {
        public RequiredIfValidator(ModelMetadata metadata, ControllerContext context, RequiredIfAttribute attribute)
            : base(metadata, context, attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules()
        {
            var rule = new ModelClientValidationRule();
            rule.ErrorMessage = String.Format(ResourceManager.GetLocalisedString("PropertyRequired", "ErrorMessage"), Metadata.GetDisplayName());// Metadata.GetDisplayName() + " is required";

            if (Attribute.DependentProperty.StartsWith("STRREQ"))
            {
                //Add a required validator if the field is flagged as required in forum config table
                rule.ValidationParameters.Add("dependentproperty", Attribute.DependentProperty);
                rule.ValidationParameters.Add("targetvalue", ClassicConfig.GetValue(Attribute.DependentProperty) == ((int)Attribute.TargetValue).ToString());
            }
            else
            {
                rule.ValidationParameters.Add("dependentproperty", Attribute.DependentProperty);
                rule.ValidationParameters.Add("targetvalue", Attribute.TargetValue);
            }
            //rule.ValidationParameters.Add("dependentproperty", Attribute.DependentProperty);

            rule.ValidationType = "conditional";
            yield return rule;
        }

        public override IEnumerable<ModelValidationResult> Validate(object container)
        {
            //if there is no dependant property, then we are doing a required field check against the snitz config
            if (Attribute.DependentProperty.StartsWith("STRREQ"))
            {

                if (ClassicConfig.GetValue(Attribute.DependentProperty) == ((int)Attribute.TargetValue).ToString())
                {
                    // match => means we should try validating this field
                    if (!Attribute.IsValid(Metadata.Model))
                        // validation failed - return an error
                        yield return new ModelValidationResult { Message = ErrorMessage };
                }
            }
            else
            {
                // get a reference to the property this validation depends upon
                var field = Metadata.ContainerType.GetProperty(Attribute.DependentProperty);

                if (field != null)
                {
                    // get the value of the dependent property
                    var value = field.GetValue(container, null);

                    // compare the value against the target value
                    if ((value == null && Attribute.TargetValue == null) ||
                        (value != null && value.Equals(Attribute.TargetValue)))
                    {
                        // match => means we should try validating this field
                        if (!Attribute.IsValid(Metadata.Model))
                            // validation failed - return an error
                            yield return new ModelValidationResult { Message = ErrorMessage };
                    }
                }
            }
        }
    }
}

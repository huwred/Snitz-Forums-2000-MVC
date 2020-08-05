

// /*
// ####################################################################################################################
// ##
// ## RequiredIfAttribute
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

using System.ComponentModel.DataAnnotations;
using System.Web.Security;
using SnitzConfig;
using WebMatrix.WebData;

namespace SnitzDataModel.Validation
{
    /// <summary>
    /// Allows a condtional validation decorator on class properties
    /// so you can decorate as follows
    /// </summary>
    /// <example>
    /// [RequiredIf("TopicId", 0)]
    /// public string Subject { get; set; }
    /// This means when binding an editor to Subject, it is only required if the objects topicid
    /// is non zero, ie a new topic. This allows us to reuse an object for different scenarios like
    /// posting topics and replys using the same form and post object
    /// It also allows us to use the STRREQ data from FORUM_CONFIG_NEW and make properties on the object required.
    /// </example>
    public class RequiredIfAttribute : ValidationAttribute
    {
        // Note: we don't inherit from RequiredAttribute as some elements of the MVC
        // framework specifically look for it and choose not to add a RequiredValidator
        // for non-nullable fields if one is found. This would be invalid if we inherited
        // from it as obviously our RequiredIf only applies if a condition is satisfied.
        // Therefore we're using a private instance of one just so we can reuse the IsValid
        // logic, and don't need to rewrite it.
        private RequiredAttribute innerAttribute = new RequiredAttribute();
        public string DependentProperty { get; set; }
        public object TargetValue { get; set; }
        public string Res { get; set; }
        public string Type { get; set; }

        public RequiredIfAttribute(string dependentProperty, object targetValue, string resource = "", string resSet = "ErrorMessage")
        {
            this.DependentProperty = dependentProperty;
            this.TargetValue = targetValue;
            this.Res = resource;
            this.Type = resSet;

        }

        public override bool IsValid(object value)
        {
            if (DependentProperty.StartsWith("STRREQ"))
            {
                
                if (Roles.IsUserInRole(WebSecurity.CurrentUserName, "Administrator"))
                {
                    return true;
                }
                if (ClassicConfig.GetValue(DependentProperty) == ((int)TargetValue).ToString())
                {
                    return innerAttribute.IsValid(value);
                }
                return true;
            }
            return innerAttribute.IsValid(value);
        }

        public override string FormatErrorMessage(string name)
        {
            if (Res != "")
                ErrorMessage = LangResources.Utility.ResourceManager.GetLocalisedString(Res, Type);
            return base.FormatErrorMessage(name);
        }

    }


}

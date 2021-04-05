// /*
// ####################################################################################################################
// ##
// ## StringLengthAttribute
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
using LangResources.Utility;

namespace SnitzCore.Filters
{
    public class StringLength : StringLengthAttribute
    {
        private string _displayName;
        private int _maxLength;

        public StringLength(int maximumLength)
            : base(maximumLength)
        {
            _maxLength = maximumLength;
            ErrorMessageResourceName = "strMaxLength";
        }
        protected override ValidationResult IsValid
            (object value, ValidationContext validationContext)
        {
            _displayName = validationContext.DisplayName;
            if (value == null || String.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;
            return base.IsValid(value, validationContext);
        }
        public override string FormatErrorMessage(string name)
        {
            var length = _maxLength;
            var msg = ResourceManager.GetLocalisedString(ErrorMessageResourceName, "ErrorMessage");
            if (this.MinimumLength > 0)
            {
                msg = ResourceManager.GetLocalisedString("strMinLength", "ErrorMessage");
                length = this.MinimumLength;
            }
            return string.Format(msg, _displayName ?? name, length);
        }

    }
}
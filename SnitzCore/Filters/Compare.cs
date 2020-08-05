using System.ComponentModel.DataAnnotations;
using LangResources.Utility;

namespace SnitzCore.Filters
{
    /// <summary>
    /// Data Annotation Filter for comparing two properties
    /// </summary>
    public class Compare : CompareAttribute
    {
        public string BasePropertyName { get; private set; }
        private string _defaultErrorMessage = "'{0}' and '{1}' do not match.";

        public Compare(string otherProperty)
            : base(otherProperty)
        {
            BasePropertyName = otherProperty;
        }

        public override string FormatErrorMessage(string name)
        {
            _defaultErrorMessage = ResourceManager.GetLocalisedString("comp" + BasePropertyName, "ErrorMessage");
            return string.Format(_defaultErrorMessage, name, BasePropertyName);
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var result = base.IsValid(value, validationContext);
            if (result != null)
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
            return null;
        }
    }
}
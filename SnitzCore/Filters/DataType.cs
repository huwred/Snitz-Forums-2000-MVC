using System.ComponentModel.DataAnnotations;
using LangResources.Utility;

namespace SnitzCore.Filters
{
    public class DataType : DataTypeAttribute
    {
        public string ResourceName { get; private set; }

        private string _defaultErrorMessage = "Invalid entry";
        public DataType(System.ComponentModel.DataAnnotations.DataType type)
            : base(type)
        {

        }

        public override string FormatErrorMessage(string name)
        {
            _defaultErrorMessage = ResourceManager.GetLocalisedString(ResourceName ?? "type" + name, "ErrorMessage");
            return string.Format(_defaultErrorMessage, name, this.DataType);
        }

    }
}
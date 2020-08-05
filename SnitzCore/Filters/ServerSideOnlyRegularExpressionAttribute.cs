using System.ComponentModel.DataAnnotations;

namespace SnitzCore.Filters
{
    public class ServerSideOnlyRegularExpressionAttribute : RegularExpressionAttribute
    {
        public ServerSideOnlyRegularExpressionAttribute(string pattern)
            : base(pattern)
        {
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using LangResources.Models;

namespace WWW.ViewModels
{
    public class ResTranslateViewModel
    {
        public int ResId { get; set; }
        public string FromLang { get; set; }
        public string ToLang { get; set; }

        public string Value { get; set; }
        public string Translated { get; set; }
    }

    public class LangResource
    {
        public IEnumerable<IGrouping<string, ResourceEntry>> Resources { get; set; }
    }
}
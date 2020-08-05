using System.Collections.Generic;
using System.Text.RegularExpressions;
using BadwordFilter = SnitzDataModel.Models.BadwordFilter;

namespace BbCodeFormatter
{
    /// <summary>
    /// Filters badwords from posts
    /// </summary>
    internal class BadWordFilter : IHtmlFormatter
    {
        static List<IHtmlFormatter> _formatters;

        /// <summary>
        /// Filter out bad words
        /// </summary>
        public BadWordFilter()
        {
            _formatters = new List<IHtmlFormatter>();
            var badwords = BadwordFilter.All();
            foreach (BadwordFilter badword in badwords)
            {
                _formatters.Add(new RegexFormatter(@"\b" + Regex.Escape(badword.BadWord) + @"\b", badword.ReplaceWord));
            }
        }
        public string Format(string data)
        {
            foreach (IHtmlFormatter formatter in _formatters)
            {
                data = formatter.Format(data);
            }

            return data;
        }
    }
}
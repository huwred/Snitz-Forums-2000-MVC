using System.Collections.Generic;
using System.Web.Mvc;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;
using Topic = SnitzDataModel.Models.Topic;


namespace WWW.ViewModels
{
    /// <summary>
    /// Home page ViewModel.
    /// contains collections required for the home page
    /// </summary>
    public class HomeViewModel
    {
        public List<ActiveTopic> RecentTopics { get; set; }
        public ForumStatistics ForumStats { get; set; }
    }

    public class TestViewModel
    {
        [RequiredIf("NewName", null, "PropertyRequired")]
        public int ModId { get; set; }

        [RequiredIf("ModId", 0, "PropertyRequired")]
        public string NewName { get; set; }

        public IEnumerable<SelectListItem> ModNames { get; set; }
    }
}
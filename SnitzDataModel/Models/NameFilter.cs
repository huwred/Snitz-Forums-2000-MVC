using System.Collections.Generic;

namespace SnitzDataModel.Models
{
    public partial class NameFilter
    {
        public static IEnumerable<Models.NameFilter> All()
        {
            return repo.Fetch<Models.NameFilter>("SELECT * FROM " + repo.FilterTablePrefix + "NAMEFILTER");
        }
        public static Models.NameFilter Fetch(int id)
        {
            return repo.GetById<Models.NameFilter>(id);
        }
    }
}
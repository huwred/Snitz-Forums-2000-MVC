using System.Collections.Generic;
using System.Linq;
using SnitzDataModel.Database;

namespace SnitzDataModel.Models
{
    public partial class BadwordFilter
    {
        public static BadwordFilter Fetch(int id)
        {
            return SnitzDataContext.Record<BadwordFilter>.repo.GetById<BadwordFilter>(id);
        }
        public static List<BadwordFilter> All()
        {
            return SnitzDataContext.Record<BadwordFilter>.repo.Fetch<BadwordFilter>("SELECT * FROM " + SnitzDataContext.Record<BadwordFilter>.repo.FilterTablePrefix + "BADWORDS");
        }

        public static bool IsBadWord(string check)
        {
            var res = SnitzDataContext.Record<BadwordFilter>.repo.Fetch<BadwordFilter>("WHERE B_BADWORD=@0",check);
            return res.Any();
        }
    }
}
using System.Collections.Generic;
using PetaPoco;

namespace SnitzDataModel.Models
{
    public partial class SpamFilter
    {
        public static Models.SpamFilter Fetch(int id)
        {
            return repo.GetById<Models.SpamFilter>(id);
        }

        public static List<Models.SpamFilter> All()
        {
            var sql = new Sql("SELECT * FROM " + repo.FilterTablePrefix + "SPAM_MAIL");
            return repo.Fetch<Models.SpamFilter>(sql);
        }
    }
}
using System.Collections.Generic;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzDataModel.Extensions;

namespace SnitzDataModel.Models
{
    public partial class ForumModerators
    {
        public static void RemoveMember(int memberid)
        {
            repo.Execute("DELETE FROM " + repo.ForumTablePrefix + "MODERATOR WHERE MEMBER_ID=@0", memberid);
        }

        public static IEnumerable<Pair<int, string>> All()
        {
            var cacheService = new InMemoryCache(600);
            var sql = new Sql();
            sql.Select("MEMBER_ID AS 'Key',M_NAME AS 'Value'");
            sql.From(repo.MemberTablePrefix + "MEMBERS");
            sql.Where("M_LEVEL > 1");
            sql.OrderBy("M_NAME");
            sql.Append(repo.dbtype == "mysql" ? "" : " COLLATE Latin1_General_BIN");
            //COLLATE utf8mb4_danish_ci
            //repo.CharSet();

            return cacheService.GetOrSet("category.forumlist",
                () => repo.Fetch<Pair<int, string>>(sql));

            //return repo.Fetch<Pair<int, string>>(sql);
        }

    }
}
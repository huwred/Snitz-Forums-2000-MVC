using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using PetaPoco;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;

namespace SnitzDataModel.Models
{
    public partial class Category
    {
        public List<Forum> Forums { get; set; }
        public bool HasForums { get; set; }

        public static Category FetchSimple(int catid)
        {

            var sql = new Sql();

            sql.Select("C.*");
            sql.From(SnitzDataContext.Record<Category>.repo.ForumTablePrefix + "CATEGORY C");
            sql.Where("C.CAT_ID = @0", catid);

            var categories = SnitzDataContext.Record<Category>.repo.Fetch<Category>(sql);

            return categories.FirstOrDefault();
        }

        public static Category Fetch(int catid)
        {

            var sql = new Sql();

            sql.Select("C.*,F.*,M.M_NAME AS LastPostAuthorName");
            sql.From(SnitzDataContext.Record<Category>.repo.ForumTablePrefix + "CATEGORY C");
            sql.LeftJoin(SnitzDataContext.Record<Category>.repo.ForumTablePrefix + "FORUM F").On("F.CAT_ID = C.CAT_ID");
            sql.LeftJoin(SnitzDataContext.Record<Category>.repo.MemberTablePrefix + "MEMBERS M").On("M.MEMBER_ID = F.F_LAST_POST_AUTHOR");
            sql.Where("C.CAT_ID = @0",catid);
            sql.OrderBy("F.F_ORDER");

            var categories = SnitzDataContext.Record<Category>.repo.Fetch<Category, Forum, Category>(
                new CategoryForumRelator().MapIt, sql);

            return categories.FirstOrDefault();
        }

        public static List<SelectListItem> List()
        {
            var convert = "CONVERT(CAT_ID,char(10))";
            if (SnitzDataContext.Record<Category>.repo.dbtype == "mssql")
            {
                convert = "CONVERT(VARCHAR,CAT_ID)";
            }
            var result =
                SnitzDataContext.Record<Category>.repo.Query<SelectListItem>(
                    "SELECT " + convert + " AS Value, CAT_NAME AS Text FROM " + SnitzDataContext.Record<Category>.repo.ForumTablePrefix + "CATEGORY ORDER BY CAT_ORDER");
            return result.ToList();
        }

        public List<Topic> SearchTopics(SearchModel model, IPrincipal user,bool archived = false)
        {
            var sql = new Sql();
            string allAllowedForums = string.Join(",", user.AllowedForumIDs().Select(n => n.ToString()).ToArray());
            if (String.IsNullOrWhiteSpace(allAllowedForums))
                allAllowedForums = "-1";

            if (archived)
            {
                sql.Select("t.*, 1 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,f.F_SUBJECT AS ForumSubject");
                sql.From(repo.ForumTablePrefix + "A_TOPICS t");
                sql.LeftJoin(repo.ForumTablePrefix + "A_REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }
            else
            {
                sql.Select("t.*, 0 AS Archived,a.M_NAME AS PostAuthorName ,a.M_PHOTO_URL AS AuthorAvatar, lpa.M_NAME AS LastPostAuthorName,f.F_SUBJECT AS ForumSubject");
                sql.From(repo.ForumTablePrefix + "TOPICS t");
                sql.LeftJoin(repo.ForumTablePrefix + "REPLY r").On("t.TOPIC_ID = r.TOPIC_ID");
            }

            sql.LeftJoin(repo.ForumTablePrefix + "FORUM f").On("t.FORUM_ID = f.FORUM_ID");

            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS a").On("a.MEMBER_ID = t.T_AUTHOR");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS lpa").On("lpa.MEMBER_ID = t.T_LAST_POST_AUTHOR");

            sql.Where("t.CAT_ID=@0", this.Id);
            if (!user.IsAdministrator())
            {
                sql.Where(" t.FORUM_ID IN (" + allAllowedForums + ")");

            }
            sql.Where("(t.T_MESSAGE LIKE @0 OR r.R_MESSAGE LIKE @1)", "%" + model.Term + "%", "%" + model.Term + "%");

            var res = repo.Query<Topic>(sql);

            return res.Distinct(new TopicComparer()).ToList();
        }

        public static IEnumerable<Category> FetchAll()
        {
            return repo.Query<Category>("SELECT * FROM " + repo.ForumTablePrefix + "CATEGORY");
        }

        // Slug generation taken from http://stackoverflow.com/questions/2920744/url-slugify-algorithm-in-c
        public string GenerateSlug()
        {
            string phrase = HttpUtility.UrlDecode(HttpUtility.HtmlDecode(Regex.Replace(Title,@"\[[^\]]*\]","")));
            if (phrase != null)
            {
                string str = phrase.ToLower();
                // invalid chars           
                str = Regex.Replace(str, @"[\\/?:;,.']", "");
                // convert multiple spaces into one space   
                str = Regex.Replace(str, @"\s+", " ").Trim();
                // cut and trim 
                str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
                str = Regex.Replace(str, @"\s", "-"); // hyphens   
                return $"{str}-{Id}";
            }

            return Title;
        }

    }
}
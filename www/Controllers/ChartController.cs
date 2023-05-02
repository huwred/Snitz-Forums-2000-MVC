using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;

using SnitzDataModel.Database;
using SnitzDataModel.Extensions;

namespace WWW.Controllers
{


    public class ChartsController : CommonController
    {
        public ChartsController()
        {
            Dbcontext = new SnitzDataContext();
        }

        public ActionResult Index()
        {
            var yearQuery = @"select min(SUBSTRING(T_DATE, 1, 4)) as MinYear, max(SUBSTRING(T_DATE, 1, 4)) as MaxYear FROM " + Dbcontext.ForumTablePrefix + "TOPICS";
            ChartModel Years = Dbcontext.Query<ChartModel>(yearQuery).FirstOrDefault();
            return PartialView(Years);
        }
        // POST: Chart
        //[HttpPost]
        public JsonResult PostsByYear(string page)
        {

            var postsByYear =
                            $@"
            SELECT 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}REPLY WHERE  SUBSTRING(R_DATE, 1, 4) = SUBSTRING(A.T_DATE, 1, 4)) + 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}A_REPLY WHERE  SUBSTRING(R_DATE, 1, 4) = SUBSTRING(A.T_DATE, 1, 4)) + 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}TOPICS WHERE  SUBSTRING(T_DATE, 1, 4) = SUBSTRING(A.T_DATE, 1, 4)) + 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}A_TOPICS WHERE  SUBSTRING(T_DATE, 1, 4) = SUBSTRING(A.T_DATE, 1, 4)) AS 'Value',
            SUBSTRING(A.T_DATE, 1, 4) AS 'Key'
            FROM {Dbcontext.ForumTablePrefix}TOPICS A
            GROUP BY SUBSTRING(A.T_DATE, 1, 4)
            ORDER BY SUBSTRING(A.T_DATE, 1, 4)
            ";


            var data = Dbcontext.Fetch<Pair<string, Int32>>(postsByYear);
            List<object> iData = new List<object>
            {
                data.Select(m => m.Key).ToList(), data.Select(m => m.Value).ToList()
            };

            return Json(iData, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        public JsonResult PostsByMonth(int id, int? page)
        {
            var postsbyMonth =
                $@"
            SELECT 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}REPLY WHERE  SUBSTRING(R_DATE, 1, 6) = SUBSTRING(A.T_DATE, 1, 6)) + 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}A_REPLY WHERE  SUBSTRING(R_DATE, 1, 6) = SUBSTRING(A.T_DATE, 1, 6)) + 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}TOPICS WHERE  SUBSTRING(T_DATE, 1, 6) = SUBSTRING(A.T_DATE, 1, 6)) + 
            (SELECT COUNT(*) FROM {Dbcontext.ForumTablePrefix}A_TOPICS WHERE  SUBSTRING(T_DATE, 1, 6) = SUBSTRING(A.T_DATE, 1, 6)) AS 'Value',
            SUBSTRING(A.T_DATE, 5, 2) AS 'Key'
            FROM {Dbcontext.ForumTablePrefix}TOPICS A
            WHERE  SUBSTRING(A.T_DATE, 1, 4) = {id}
            GROUP BY SUBSTRING(A.T_DATE, 1, 6), SUBSTRING(A.T_DATE, 5, 2)
            ORDER BY SUBSTRING(A.T_DATE, 1, 6), SUBSTRING(A.T_DATE, 5, 2)
            ";

            var data = Dbcontext.Fetch<Pair<string, int>>(postsbyMonth);
            List<object> iData = new List<object>
            {
                data.Select(m => m.Key).ToList(), 
                data.Select(m => m.Value).ToList()
            };
            try
            {
                return Json(iData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        //[HttpPost]
        public JsonResult TopicsByUser()
        {
            var sqlTop = "";
            if (Dbcontext.dbtype == "mssql")
            {
                sqlTop = "TOP 10";
            }

            var postsbyMember = $@"select {sqlTop} poster  as 'Key', CAST(sum(postcount) as INT) as 'Value' 
                  from (
                    select M.M_NAME AS poster, count(*) as postcount
                      from {Dbcontext.ForumTablePrefix}TOPICS
	                  JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON T_AUTHOR = M.MEMBER_ID
                      GROUP BY M.M_NAME
                    union all
                    select M.M_NAME AS poster, count(*) as postcount
                      from {Dbcontext.ForumTablePrefix}A_TOPICS
	                                  JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON T_AUTHOR = M.MEMBER_ID
                                GROUP BY M.M_NAME
                                
                       ) as u GROUP BY u.poster ORDER BY 'Value' Desc";

            if (Dbcontext.dbtype == "mysql")
            {
                postsbyMember = postsbyMember + " LIMIT 10";
            }
            var data = Dbcontext.Fetch<Pair<string, Int32>>(postsbyMember);

            List<object> iData = new List<object>
            {
                data.Select(m => m.Key).ToList(), data.Select(m => m.Value).ToList()
            };

            return Json(iData,JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        public JsonResult RepliesByUser()
        {
            var sqlTop = "";
            if (Dbcontext.dbtype == "mssql")
            {
                sqlTop = "TOP 10";
            }
            var postsbyMember = $@"select {sqlTop} poster as 'Key', CAST(sum(postcount) as INT) as 'Value' 
                              from (
                                select M.M_NAME AS poster,  count(*) as postcount
                                  from {Dbcontext.ForumTablePrefix}REPLY
	                                JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON R_AUTHOR = M.MEMBER_ID
                                    GROUP BY M.M_NAME
                                union all
                                select M.M_NAME AS poster, count(*) as postcount
                                  from {Dbcontext.ForumTablePrefix}A_REPLY
	                                              JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON R_AUTHOR = M.MEMBER_ID
                                            GROUP BY M.M_NAME
                                            
                                   ) as u GROUP BY u.poster ORDER BY 'Value' Desc";

            if (Dbcontext.dbtype == "mysql")
            {
                postsbyMember = postsbyMember + " LIMIT 10";
            }
            var data = Dbcontext.Fetch<Pair<string, Int32>>(String.Format(postsbyMember,Dbcontext.ForumTablePrefix,Dbcontext.MemberTablePrefix));
            List<object> iData = new List<object>
            {
                data.Select(m => m.Key).ToList(), data.Select(m => m.Value).ToList()
            };

            return Json(iData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult PostsByUser()
        {
            var sqlTop = "";
            if (Dbcontext.dbtype == "mssql")
            {
                sqlTop = "TOP 10";
            }
            var postsbyMember = $@"select {sqlTop} poster as 'Key', CAST(sum(postcount) as INT) as 'Value' 
                              from (
                                select M.M_NAME AS poster,  count(REPLY_ID) as postcount
                                  from {Dbcontext.ForumTablePrefix}REPLY
	                                JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON R_AUTHOR = M.MEMBER_ID
                                    GROUP BY M.M_NAME
                                union all
                                select M.M_NAME AS poster, count(REPLY_ID) as postcount
                                  from {Dbcontext.ForumTablePrefix}A_REPLY
	                                              JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON R_AUTHOR = M.MEMBER_ID
                                            GROUP BY M.M_NAME
union all
select M.M_NAME AS poster, count(TOPIC_ID) as postcount
                      from {Dbcontext.ForumTablePrefix}TOPICS
	                  JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON T_AUTHOR = M.MEMBER_ID
                      GROUP BY M.M_NAME
                    union all
                    select M.M_NAME AS poster, count(TOPIC_ID) as postcount
                      from {Dbcontext.ForumTablePrefix}A_TOPICS
	                                  JOIN {Dbcontext.MemberTablePrefix}MEMBERS M ON T_AUTHOR = M.MEMBER_ID
                                GROUP BY M.M_NAME
                                            
                                   ) as u GROUP BY u.poster ORDER BY 'Value' Desc";

            if (Dbcontext.dbtype == "mysql")
            {
                postsbyMember = postsbyMember + " LIMIT 10";
            }
            var data = Dbcontext.Fetch<Pair<string, Int32>>(String.Format(postsbyMember,Dbcontext.ForumTablePrefix,Dbcontext.MemberTablePrefix));
            List<object> iData = new List<object>
            {
                data.Select(m => m.Key).ToList(), data.Select(m => m.Value).ToList()
            };

            return Json(iData, JsonRequestBehavior.AllowGet);
        }
    }

    public class ChartModel
    {
        public string MinYear { get; set; }
        public string MaxYear { get; set; }
    }
    public class PostsByDate
    {
        public string y;
        public string m;
        public int tally;
    }
}
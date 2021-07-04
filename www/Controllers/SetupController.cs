using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using SnitzConfig;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel;
using SnitzDataModel.Database;
using WWW.Models;
using WWW.ViewModels;
using WWW.Helpers;

namespace WWW.Controllers
{

    public class SetupController : Controller
    {

        delegate string ProcessTask(string id);

        private readonly ILogger _logger = new Logger();

        private static DbsFileProcessor _dbsProcessor;

        public ActionResult Index()
        {
            if (!IsServerConnected())
            {
                ViewBag.Title = "Setup Error";
                ViewBag.Message = "There was aproblem connecting to the database, please check your connection strings in connectionstrings.config file.";
                return View();
            }

            if (!Config.RunSetup)
            {
                return RedirectPermanent("~/");
            }
            //does the member table exist

            try
            {
                var sqlDb = new System.Data.SqlClient.SqlConnectionStringBuilder(
                    ConfigurationManager.ConnectionStrings["SnitzConnectionString"]
                        .ConnectionString);
                var database = sqlDb.InitialCatalog;

            }
            catch (Exception ex)
            {
                return View("Error", ex);
            }

            using (SnitzDataContext db = new SnitzDataContext())
            {
                if (Config.TableExists(db.MemberTablePrefix + "MEMBERS",ControllerContext.HttpContext))
                {
                    return RedirectToAction("Upgrade");
                }
                else
                {
                    return RedirectToAction("Setup");
                }
            }
        }
        public bool IsServerConnected()
        {
            using (var l_oConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ConnectionString))
            {
                try
                {
                    l_oConnection.Open();
                    l_oConnection.Close();
                    return true;
                }
                catch (SqlException ex)
                {
                    return false;
                }
            }
        }
        public ActionResult Upgrade()
        {
            if (!Config.RunSetup)
            {
                return RedirectPermanent("~/");
            }

            ViewBag.Title = "Snitz Forums Mvc Upgrade";
            ViewBag.Message = "Your database needs to be upgraded to support the MVC version.";
            ViewBag.Redirect = false;
            UpdateViewModel vm = new UpdateViewModel {DbsFile = "~/App_Data/upgrade.xml"};
            return View(vm);
        }

        public ActionResult Setup()
        {
            if (!Config.RunSetup)
            {
                return RedirectPermanent("~/");
            }
            SetupViewModel vm = new SetupViewModel();
            ViewBag.Title = "Snitz Forums Mvc setup";
            ViewBag.Message = "You need to create all the tables in the database before you can start using the forum.";
            ViewBag.Init = true;
            vm.DbsFile = "~/App_Data/base.xml";
            vm.AdminUsername = "Administrator";
            return View(vm);
        }

        public ActionResult SetupStart(SetupViewModel vm)
        {
            ViewBag.Init = true;
            if (ModelState.IsValid)
            {
                try
                {
                    var sqlDb =
                        new System.Data.SqlClient.SqlConnectionStringBuilder(
                            ConfigurationManager.ConnectionStrings["SnitzConnectionString"]
                                .ConnectionString) {IntegratedSecurity = vm.IntegratedSecurity};
                    if (!vm.IntegratedSecurity)
                    {
                        sqlDb.UserID = vm.SqlUser;
                        sqlDb.Password = vm.SqlPwd;
                    }
                    ViewBag.ConnStr = sqlDb.ConnectionString;
                    ViewBag.Init = false;
                }
                catch (Exception ex)
                {
                    return View("Error", ex);
                }

            }
            else
            {
                ViewBag.Title = "Snitz Forum Mvc setup";
                ViewBag.Message =
                    "You need to create all the tables in the database before you can start using the forum.";
            }

            return View("Setup", vm);
        }

        public void StartCreate(string id, string sqluser, string sqlpwd, string adminuser, string adminpwd,
            string adminemail)
        {
            var path = HttpContext.Server.MapPath("~/App_Data/base.xml");
            var provider = ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ProviderName;
            var dbtype = "mssql";
            if (provider.StartsWith("MySql"))
                dbtype = "mysql";
            _dbsProcessor = new DbsFileProcessor(path)
            {
                AdminUser = adminuser,
                AdminPassword = adminpwd,
                AdminEmail = adminemail,
                Context = HttpContext,
                _dbType = dbtype
            };

            if (_dbsProcessor.Applied)
            {
                ViewBag.Message = "DBS file already applied";
                Config.RunSetup = false;
                Config.Update();
                RedirectToAction("Finished", new {message = "DBS file already applied"});
            }
            else
            {
                _dbsProcessor.Add(id);
                ProcessTask processTask = _dbsProcessor.Process;
                processTask.BeginInvoke(id, EndCreateProcess, processTask);
            }
        }

        public void StartProcess(string id, string dbsfile)
        {
            var path = HttpContext.Server.MapPath(dbsfile);
            var provider = ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ProviderName;
            var dbtype = "mssql";
            if (provider.StartsWith("MySql"))
                dbtype = "mysql";
            _dbsProcessor = new DbsFileProcessor(path) {_dbType = dbtype};
            if (_dbsProcessor.Applied)
            {
                ViewBag.Message = "DBS file already applied";
                Config.RunSetup = false;
                Config.Update();
                RedirectToAction("Finished", new {message = "DBS file already applied"});
            }
            else
            {
                _dbsProcessor.Add(id);
                ProcessTask processTask = _dbsProcessor.Process;
                processTask.BeginInvoke(id, EndProcess, processTask);
            }
        }

        public void EndProcess(IAsyncResult result)
        {
            ProcessTask processTask = (ProcessTask) result.AsyncState;
            string id = processTask.EndInvoke(result);
            _dbsProcessor.Remove(id);
        }

        public void EndCreateProcess(IAsyncResult result)
        {
            ProcessTask processTask = (ProcessTask) result.AsyncState;
            string id = processTask.EndInvoke(result);
            _dbsProcessor.Remove(id);
        }

        public ContentResult GetCurrentProgress(string id)
        {
            this.ControllerContext.HttpContext.Response.AddHeader("cache-control", "no-cache");
            var currentProgress = _dbsProcessor.GetStatus(id);
            try
            {
                _logger.Info(currentProgress);
            }
            catch (Exception)
            {
                // ignored
            }

            return Content(currentProgress);
        }


        public ActionResult Finished(string message)
        {
            Config.RunSetup = false;
            Config.Update();
            ViewBag.Message = message ?? "Setup Completed";
            OfflineFileData.SetOffline(1,Common.GetUserIP(System.Web.HttpContext.Current),"Forum is currently Offline",HttpContext.Server.MapPath);
            return View();
        }
    }
}
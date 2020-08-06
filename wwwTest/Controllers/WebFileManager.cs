using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using SnitzConfig;
using SnitzDataModel.Controllers;
using SnitzMembership;
using WWW.Models;

namespace WWW.Controllers
{
    //[Authorize]
    public class WebFileManagerController : BaseController
    {
        private string _strHideFilePattern = FileManager.GetConfigString("HideFolderPattern");
        //-- hide any folders matching pattern
        private string _strHideFolderPattern = FileManager.GetConfigString("HideFilePattern");
        //-- force user to stay on paths matching pattern
        private string _strAllowedPathPattern = FileManager.GetConfigString("AllowedPathPattern");


        public ActionResult Index(string path = "", string sort = "")
        {
            const string strPathError = "The path '{0}' {1}";
            if (!User.IsInRole("Administrator") )
            {
                _strAllowedPathPattern = Config.ContentFolder + "/Members/" + WebSecurity.CurrentUserId;
                if (FileManager.WebPath() == "~/")
                {
                    path = "~/" + _strAllowedPathPattern;
                }

                ViewBag.WebPath = FileManager.WebPath(path);
            }
            //-- make sure we're allowed to look at this web path
            if (!string.IsNullOrEmpty(_strAllowedPathPattern) && !Regex.IsMatch(FileManager.WebPath(path), _strAllowedPathPattern))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = (string.Format(strPathError, FileManager.WebPath(path), "is not allowed because it does not match the pattern '" + Server.HtmlEncode(_strAllowedPathPattern) + "'."));
                return View("Error");
            }

            ViewBag.WebPath = FileManager.WebPath(path);

            ViewBag.UpUrl = UpUrl();
            return View();

        }

        public PartialViewResult WriteFileTable(string path = "")
        {
            if (!User.IsInRole("Administrator"))
            {
                _strAllowedPathPattern = Config.ContentFolder + "/Members/" + WebSecurity.CurrentUserId;
                if (String.IsNullOrWhiteSpace(path))
                {
                    path = "~/" + _strAllowedPathPattern;
                }
                
                ViewBag.WebPath = FileManager.WebPath(path);
            }
            else { ViewBag.WebPath = FileManager.WebPath();}

            
            return PartialView();
        }

        public ActionResult WriteFileRows()
        {
            string path = "";
            const string strPathError = "The path '{0}' {1}";
            if (!User.IsInRole("Administrator"))
            {
                _strAllowedPathPattern = Config.ContentFolder + "/Members/" + WebSecurity.CurrentUserId;
                if (FileManager.WebPath() == "~/")
                {
                    path = "~/" + _strAllowedPathPattern;
                }

                ViewBag.WebPath = FileManager.WebPath(path);
            }
            //-- make sure we're allowed to look at this web path
            if (!string.IsNullOrEmpty(_strAllowedPathPattern) && !Regex.IsMatch(FileManager.WebPath(path), _strAllowedPathPattern))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = (string.Format(strPathError, FileManager.WebPath(path), "is not allowed because it does not match the pattern '" + Server.HtmlEncode(_strAllowedPathPattern) + "'."));
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath(path);
            //-- make sure this directory exists on the server
            string strLocalPath = FileManager.GetLocalPath(path);
            if (!Directory.Exists(strLocalPath))
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = (string.Format(strPathError, FileManager.WebPath(), "does not exist."));
                return View("Error");
            }

            //-- make sure we can get the files and directories for this directory
            DirectoryInfo[] da = null;
            FileInfo[] fa = null;
            try
            {
                DirectoryInfo di = new DirectoryInfo(strLocalPath);
                da = di.GetDirectories();
                fa = di.GetFiles();
            }
            catch (Exception ex)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = ex.Message;
                return View("Error");
            }

            //-- add all file/directory info to intermediate DataTable
            DataTable dt = GetFileInfoTable();
            dt.BeginLoadData();
            foreach (DirectoryInfo d in da)
            {
                AddRowToFileInfoTable(d, dt);
            }
            foreach (FileInfo f in fa)
            {
                AddRowToFileInfoTable(f, dt);
            }
            dt.EndLoadData();
            dt.AcceptChanges();


            if (dt.Rows.Count == 0)
            {
                //set error message to pass to referring controller
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = "(Folder is empty)";
                return PartialView(new DataView(dt));
            }
            DataView dv = default(DataView);

            //-- sort and render intermediate DataView from our DataTable
            if (string.IsNullOrEmpty(FileManager.SortColumn()))
            {
                dv = dt.DefaultView;
            }
            else
            {
                dv = new DataView(dt);
                if (FileManager.SortColumn().StartsWith("-", StringComparison.Ordinal))
                {
                    dv.Sort = "IsFolder, " + FileManager.SortColumn().Substring(1) + " desc";
                }
                else
                {
                    dv.Sort = "IsFolder desc, " + FileManager.SortColumn();
                }
            }
            return PartialView(dv);
        }

        public ActionResult Delete(FormCollection form)
        {
            foreach (string key in form)
            {
                if (key.StartsWith("checked_", StringComparison.Ordinal))
                {
                    var path = FileManager.WebPathCombine(FileManager.WebPath(), key.Replace("checked_", ""));
                    FileManager.DeleteFileOrFolder(key.Replace("checked_", ""));
                }
            }

            if (FileManager.FileOperationException != null)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = FileManager.FileOperationException.Message;
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        public ActionResult Rename(FormCollection form)
        {
            foreach (string key in form)
            {
                if (key.StartsWith("checked_", StringComparison.Ordinal))
                {
                    var path = FileManager.WebPathCombine(FileManager.WebPath(), key.Replace("checked_", ""));
                    FileManager.RenameFileOrFolder(key.Replace("checked_", ""));
                }
            }

            if (FileManager.FileOperationException != null)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = FileManager.FileOperationException.Message;
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        public ActionResult Zip(FormCollection form)
        {
            ArrayList fileList = new ArrayList();
            foreach (string key in form)
            {
                if (key.StartsWith("checked_", StringComparison.Ordinal))
                {
                    fileList.Add(key.Replace("checked_", ""));
                }
            }
            FileManager.ZipFileOrFolder(fileList);

            if (FileManager.FileOperationException != null)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = FileManager.FileOperationException.Message;
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        public ActionResult File(string path)
        {
            string filename = Path.GetFileName(path);
            string filepath = FileManager.GetLocalPath();
            byte[] filedata = System.IO.File.ReadAllBytes(filepath);
            string contentType = MimeMapping.GetMimeMapping(filepath);

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = true,
            };

            Response.AppendHeader("Content-Disposition", cd.ToString());

            return File(filedata, contentType);

        }

        [Authorize(Roles ="Administrator")]
        public ActionResult NewFolder(FormCollection form)
        {
            if (form.AllKeys.Contains("targetfolder"))
            {
                var path = FileManager.WebPathCombine(FileManager.WebPath(), form["targetfolder"]);
                FileManager.MakeFolder(form["targetfolder"]);
                if(FileManager.FileOperationException != null)
                {
                    ViewBag.ErrTitle = "Error";
                    ViewBag.Error = FileManager.FileOperationException.Message;
                    return View("Error");
                }
                
            }
            
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult Copy(FormCollection form)
        {
            foreach (string key in form)
            {
                if (key.StartsWith("checked_", StringComparison.Ordinal))
                {
                    var path = FileManager.WebPathCombine(FileManager.WebPath(), key.Replace("checked_", ""));
                    FileManager.CopyFileOrFolder(key.Replace("checked_", ""));
                }
            }

            if (FileManager.FileOperationException != null)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = FileManager.FileOperationException.Message;
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult Move(FormCollection form)
        {
            foreach (string key in form)
            {
                if (key.StartsWith("checked_", StringComparison.Ordinal))
                {
                    var path = FileManager.WebPathCombine(FileManager.WebPath(), key.Replace("checked_", ""));
                    FileManager.MoveFileOrFolder(key.Replace("checked_", ""));
                }
            }

            if (FileManager.FileOperationException != null)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = FileManager.FileOperationException.Message;
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult Upload(HttpPostedFileBase[] files)
        {

            if (files.Any())
            {
                FileManager.SaveUploadedFile(files);
            }
            if (FileManager.FileOperationException != null)
            {
                ViewBag.ErrTitle = "Error";
                ViewBag.Error = FileManager.FileOperationException.Message;
                return View("Error");
            }
            ViewBag.WebPath = FileManager.WebPath();
            return View("Index");
        }

        /// <summary>
        /// Returns the URL for one level "up" from our current WebPath()
        /// </summary>
        [NonAction]
        private string UpUrl()
        {
            string strUp = Regex.Replace(FileManager.WebPath(), "/[^/]+$", "");
            if (string.IsNullOrEmpty(strUp) | strUp == "/")
            {
                strUp = FileManager.GetConfigString("DefaultPath", "~/");
            }
            return FileManager.PageUrl(strUp);
        }

        /// <summary>
        /// returns intermediate DataTable of File/Directory info 
        /// to be used for sorting prior to display
        /// </summary>
        [NonAction]
        private DataTable GetFileInfoTable()
        {
            DataTable dt = new DataTable();

            var cols = dt.Columns;
            cols.Add(new DataColumn("Name", typeof(System.String)));
            cols.Add(new DataColumn("IsFolder", typeof(System.Boolean)));
            cols.Add(new DataColumn("FileExtension", typeof(System.String)));
            cols.Add(new DataColumn("Attr", typeof(System.String)));
            cols.Add(new DataColumn("Size", typeof(System.Int64)));
            cols.Add(new DataColumn("Modified", typeof(System.DateTime)));
            cols.Add(new DataColumn("Created", typeof(System.DateTime)));
            return dt;
        }
        /// <summary>
        /// translates a FileSystemInfo entry to a DataRow in our intermediate DataTable
        /// </summary>
        [NonAction]
        private void AddRowToFileInfoTable(FileSystemInfo fi, DataTable dt)
        {
            DataRow dr = dt.NewRow();
            string Attr = AttribString(fi.Attributes);

            var row = dr;
            row["Name"] = fi.Name;
            row["FileExtension"] = Path.GetExtension(fi.Name);
            row["Attr"] = Attr;
            if (Attr.IndexOf("d", StringComparison.Ordinal) > -1)
            {
                row["IsFolder"] = true;
                row["Size"] = 0;
            }
            else
            {
                row["IsFolder"] = false;
                row["Size"] = new FileInfo(fi.FullName).Length;
            }
            row["Modified"] = fi.LastWriteTime;
            row["Created"] = fi.CreationTime;

            dt.Rows.Add(dr);
        }
        /// <summary>
        /// turn numeric attribute into standard "RHSDAC" text
        /// </summary>
        [NonAction]
        private string AttribString(System.IO.FileAttributes a)
        {
            StringBuilder sb = new StringBuilder();
            if ((a & FileAttributes.ReadOnly) > 0)
                sb.Append("r");
            if ((a & FileAttributes.Hidden) > 0)
                sb.Append("h");
            if ((a & FileAttributes.System) > 0)
                sb.Append("s");
            if ((a & FileAttributes.Directory) > 0)
                sb.Append("d");
            if ((a & FileAttributes.Archive) > 0)
                sb.Append("a");
            if ((a & FileAttributes.Compressed) > 0)
                sb.Append("c");
            return sb.ToString();
        }

    }
}
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Database;
using SnitzDataModel.Models;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;


namespace WWW.Controllers
{
    public class FilesController : CommonController
    {
        public FilesController()
        {
            Dbcontext = new SnitzDataContext();

        }

        [HttpGet]
        [Authorize(Roles ="Administrator")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Index(string sort = "Downloads", string sortdir = "DESC")
        {
            if (Config.TableExists("FORUM_FILECOUNT",ControllerContext.HttpContext))
            {
                var test = SortIQueryable<DownloadFile>(SnitzDataContext.GetDownloadFiles().AsQueryable(), sort, sortdir);
                return View(SortIQueryable<DownloadFile>(SnitzDataContext.GetDownloadFiles().AsQueryable(), sort, sortdir.ToLower()).ToList());
            }
            return View();
        }

        [HttpGet]
        public FileResult Download(string filename)
        {

            string filepath = Server.MapPath("~/Downloads/" + filename);
            byte[] fileBytes = System.IO.File.ReadAllBytes(filepath);
            var stream = new MemoryStream(fileBytes);

            stream.Seek(0, SeekOrigin.Begin);
            var extension = Path.GetExtension(filename);
            if (extension != null)
            {
                var reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension.ToLower());
                string contentType = "application/unknown";

                if (reg != null)
                {
                    string registryContentType = reg.GetValue("Content Type") as string;

                    if (!String.IsNullOrWhiteSpace(registryContentType))
                    {
                        contentType = registryContentType;
                    }
                }
                Dbcontext.UpdateFileCounter(filename);
                return File(stream, contentType, filename);
            }
            // Return to client.
            throw new HttpException(404, "Not found");
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        [Authorize(Roles = "Administrator")]
        public PartialViewResult UploadFile()
        {
            return PartialView("popFileUpload");
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public override JsonResult Upload()
        {
            string uploadPath = Server.MapPath("~/Downloads");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string fileName = "";
            string mimeType = "";
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i]; //Uploaded file                                       

                if (file != null)
                {
                    mimeType = file.ContentType;
                    fileName = Path.GetFileName(file.FileName);//Use the following properties to get file's name, size and MIMEType

                    if (fileName != null)
                    {
                        file.SaveAs(Path.Combine(uploadPath, fileName)); //File will be saved in users folder
                        try
                        {
                            Dbcontext.AddFileCounter(fileName);
                        }
                        catch (Exception ex)
                        {
                            return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                }
            }
            return Json(new { success = true, responseText = "OK" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public PartialViewResult Downloads()
        {
            return PartialView("_Downloads", SnitzDataContext.GetDownloadFiles().Where(f=>f.Archive==0).OrderBy(f=>f.Order).ToList());
        }

        [HttpPost]
        public JsonResult Edit(DownloadFile downloadfile)
        {
            try
            {
                // TODO: Add update logic here
                Dbcontext.Save(downloadfile);

                return Json(new { success = true, responseText = "File details changed." }, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                return Json(new { success = true, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                // TODO: Add delete logic here
                Dbcontext.Delete<DownloadFile>(id);
                return Json(new { success = true, responseText = "File removed." }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { success = false, responseText = "Unable to remove." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveConfig(FormCollection form)
        {
            if (ModelState.IsValid)
            {

                foreach (var key in form.AllKeys)
                {
                    var upperkey = key.ToUpper();
                    if (upperkey.StartsWith("STR") || upperkey.StartsWith("INT"))
                    {
                        string[] amounts = Request.Form.GetValues(key);
                        if (amounts != null)
                            ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting(upperkey, amounts[0]);
                    }
                    ClassicConfig.Update(new string[] { upperkey });
                }
            }
            return View("Index", SnitzDataContext.GetDownloadFiles());

        }

        private static IQueryable<T> SortIQueryable<T>(IQueryable<T> data, string fieldName, string sortOrder)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return data;
            if (string.IsNullOrWhiteSpace(sortOrder)) return data;

            var param = Expression.Parameter(typeof(T), "i");
            Expression conversion = Expression.Convert(Expression.Property(param, fieldName), typeof(object));
            var mySortExpression = Expression.Lambda<Func<T, object>>(conversion, param);

            return (sortOrder == "desc") ? data.OrderByDescending(mySortExpression)
                : data.OrderBy(mySortExpression);
        }

    }
}

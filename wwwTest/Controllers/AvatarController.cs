using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using LangResources.Utility;

using SnitzConfig;
using SnitzDataModel.Extensions;
using SnitzMembership;

using WWW.Helpers;

namespace WWW.Controllers
{
    public class AvatarController : Controller
    {
        // Dimensions of the cropped image window
        private const int AvatarWidth = 100;
        private const int AvatarHeight = 100;
        // Display width of initially uploaded image (scale is preserved so height is calculated).
        private const int AvatarScreenWidth = 350;

        //private const string TempFolder = "/Content/Temp";
        private const string MapTempFolder = "~/Content/Temp";
        private static readonly string AvatarPath = "~/" + Config.ContentFolder + "/Avatar";
        private static readonly ILogger _logger = new Logger();
        private readonly string[] _imageFileExtensions = ClassicConfig.GetValue("STRIMAGETYPES").Split(',');

        [HttpGet]
        public ActionResult Upload(int id)
        {
            ViewBag.MemberId = id;
            return PartialView("popUploadAvatar");
        }

        [ValidateAntiForgeryToken]
        public ActionResult _Upload(IEnumerable<HttpPostedFileBase> files)
        {
            string webPath = "";
            try
            {
                if (files == null)
                {
                    return Json(new { success = false, errorMessage = ResourceManager.GetLocalisedString("UploadNoFileMsg", "FileManager") });
                }
                var httpPostedFileBases = files as IList<HttpPostedFileBase> ?? files.ToList();

                if (!httpPostedFileBases.Any())
                    return Json(new { success = false, errorMessage = ResourceManager.GetLocalisedString("UploadNoFileMsg", "FileManager") });

                var file = httpPostedFileBases.FirstOrDefault();  // get ONE only
                if (file == null || !IsImage(file))
                    return Json(new { success = false, errorMessage = ResourceManager.GetLocalisedString("uploadType", "FileManager") });

                if (file.ContentLength <= 0)
                    return Json(new { success = false, errorMessage = ResourceManager.GetLocalisedString("uploadEmpty", "FileManager") });

                webPath = GetTempSavedFilePath(file);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }


            return Json(new { success = true, fileName = webPath.Replace("\\", "/") }); // success
        }

        [HttpPost]
        public ActionResult Save(string t, string l, string h, string w, string fileName)
        {
            try
            {
                // Get image from temporary folder
                if (fileName != null)
                {
                    var fn = Path.Combine(Server.MapPath(MapTempFolder), Path.GetFileName(fileName));
                    // calculate new dimensions
                    var height = Convert.ToInt32(h.Replace("-", "").Replace("px", ""));
                    var width = Convert.ToInt32(w.Replace("-", "").Replace("px", ""));
                    //var img = new WebImage(fn);

                    //img.Resize(width, height);
                    //new ImageResizer.ResizeSettings(width=100; height=100; format = jpg;mode=max)
                    var settings = new ImageResizer.Instructions();
                    settings.Height = height;
                    settings.Width = width;
                    settings.Format = "jpg";
                    settings.JpegQuality = 90;




                    // crop user selection
                    var top = Convert.ToInt32(t.Replace("-", "").Replace("px", ""));
                    var left = Convert.ToInt32(l.Replace("-", "").Replace("px", ""));
                    var bottom = height - top - AvatarHeight;
                    var right = width - left - AvatarWidth;

                    // check validity of calculations
                    if (bottom < 0 || right < 0)
                    {
                        // If you reach this point, your avatar sizes in here and in the CSS file are different.
                        // Check _avatarHeight and _avatarWidth in this file
                        // and height and width for #preview-pane .preview-container in snitz.avatar.css
                        throw new ArgumentException("Dimensions for the cropping window do not match.");
                    }
                    settings.CropRectangle = new double[] { top, left, bottom, right };
                    
                    //img.Crop(top, left, bottom, right);



                    var profile = MemberManager.GetUser(WebSecurity.CurrentUserId);
                    //delete the old image
                    if (!String.IsNullOrWhiteSpace(profile.PhotoUrl))
                    {
                        var currentAvatar = HttpContext.Server.MapPath(UrlCombine(AvatarPath, profile.PhotoUrl));
                        if (System.IO.File.Exists(currentAvatar))
                            System.IO.File.Delete(currentAvatar);
                    }
                    // save the new image 
                    var timestamp = DateTime.Now.ToFileTimeUtc();
                    var newFileName = Url.Content(UrlCombine(AvatarPath, WebSecurity.CurrentUserName + timestamp + Path.GetExtension(fn))); //Path.GetFileName(fn)
                    var newFileLocation = HttpContext.Server.MapPath(newFileName);
                    if (Directory.Exists(Path.GetDirectoryName(newFileLocation)) == false)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(newFileLocation));
                    }
                    //logger.Debug("Save avatar image");
                    //img.Save(newFileLocation);
                    ImageResizer.ImageJob i = new ImageResizer.ImageJob(fn, newFileLocation, settings);
                    i.CreateParentDirectory = true;
                    i.Build();
                    // delete the temporary image
                    //logger.Debug("delete the temporary image");
                    System.IO.File.Delete(fn);
                    //Update UserProfile
                    profile.PhotoUrl = WebSecurity.CurrentUserName + timestamp + Path.GetExtension(fn);
                    MemberManager.SaveAvatar(profile,profile.PhotoUrl, User.IsAdministrator());

                    return Json(new { success = true, avatarFileLocation = newFileName });
                }
                //logger.Debug("No file uploaded");
                return Json(new { success = false, errorMessage = ResourceManager.GetLocalisedString("UploadErr", "FileManager") + ":\n" + ResourceManager.GetLocalisedString("uploadEmpty", "FileManager") });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return Json(new { success = false, errorMessage = ResourceManager.GetLocalisedString("UploadErr", "FileManager") + ":\n" + ex.Message });
            }
        }

        private bool IsImage(HttpPostedFileBase file)
        {
            if (file == null) return false;
            return file.ContentType.Contains("image") ||
                _imageFileExtensions.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private string GetTempSavedFilePath(HttpPostedFileBase file)
        {
            // Define destination
            var serverPath = HttpContext.Server.MapPath(MapTempFolder);
            if (Directory.Exists(serverPath) == false)
            {
                Directory.CreateDirectory(serverPath);
            }

            // Generate unique file name
            var fileName = Path.GetFileName(file.FileName);
            fileName = SaveTemporaryAvatarFileImage(file, serverPath, fileName);

            // Clean up old files after every save
            CleanUpTempFolder(1);
            return Url.Content(UrlCombine(MapTempFolder, fileName));
        }

        private static string SaveTemporaryAvatarFileImage(HttpPostedFileBase file, string serverPath, string fileName)
        {
            var img = new WebImage(file.InputStream);
            var ratio = img.Height / (double)img.Width;
            img.Resize(AvatarScreenWidth, (int)(AvatarScreenWidth * ratio));

            var fullFileName = Path.Combine(serverPath, fileName);
            if (System.IO.File.Exists(fullFileName))
            {
                System.IO.File.Delete(fullFileName);
            }

            img.Save(fullFileName);
            return Path.GetFileName(img.FileName);
        }

        private void CleanUpTempFolder(int hoursOld)
        {
            try
            {
                var currentUtcNow = DateTime.UtcNow;
                var serverPath = HttpContext.Server.MapPath(MapTempFolder);
                if (!Directory.Exists(serverPath)) return;
                var fileEntries = Directory.GetFiles(serverPath);
                foreach (var fileEntry in fileEntries)
                {
                    var fileCreationTime = System.IO.File.GetCreationTimeUtc(fileEntry);
                    var res = currentUtcNow - fileCreationTime;
                    if (res.TotalHours > hoursOld)
                    {
                        System.IO.File.Delete(fileEntry);
                    }
                }
            }
            catch
            {
                // Deliberately empty.
            }
        }

        private string UrlCombine(string url1, string url2)
        {
            if (url1.Length == 0)
            {
                return url2;
            }

            if (url2.Length == 0)
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return string.Format("{0}/{1}", url1, url2);
        }
    }
}
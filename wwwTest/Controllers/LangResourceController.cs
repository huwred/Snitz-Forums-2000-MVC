using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using LangResources.Models;
using LangResources.Utility;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzDataModel.Database;
using WWW.ViewModels;

namespace WWW.Controllers
{
    [Authorize(Roles = "Administrator,LanguageEditor")]
    public class LangResourceController : CommonController
    {
        //
        // GET: /LangResources/
        
        public ActionResult Index()
        {
            LangViewModel vm = new LangViewModel {ResourceSets = ResourceManager.ReadResourceSets()};
            if (vm.ResourceSets.Any())
            {
                ViewBag.ResSet = vm.ResourceSets[0];
                vm.Resources = ResourceManager.ReadResources(ViewBag.ResSet);
            }


            return View(vm);
        }

        //GET: Resource strings
        public ActionResult Resource(string set, string name)
        {
            ViewBag.ResName = name;
            ViewBag.ResSet = set;
            return View("_ResourcePanel", ResourceManager.ReadResources(set));
        }

        public ActionResult ResourceSet(string name)
        {
            ViewBag.ResSet = name;
            return View("_ResourceKeys", ResourceManager.ReadResources(name));
        }

        public ActionResult ResEdit(string set, string name, string lang = "")
        {
            string lookupLang = lang == "" ? "en" : lang;
            ResourceEntry vm = new ResourceEntry();
            if (set != null)
            {
                vm.ResourceSet = set;
                vm.Culture = lookupLang;
            }
            ViewBag.ReadOnly = "";
            if (name != null)
            {
                vm = ResourceManager.GetResource(name, lookupLang, set);
                if (lang == "")
                    vm.Culture = "";
                ViewBag.ReadOnly = "readonly";
            }
            return View("popResourceEditor", vm);
        }

        //Post
        public ActionResult AddEdit(ResourceEntry vm)
        {
            string res = ResourceManager.Get(vm.Name, vm.Culture);
            if (res.StartsWith("!*"))
            {
                ResourceManager.Add(vm.Name, vm.Value, vm.Culture, vm.ResourceSet);
                vm = ResourceManager.GetResource(vm.Name, vm.Culture, vm.ResourceSet);
            }
            else
            {
                ResourceManager.Update(vm);
            }
            ViewBag.ResName = vm.Name;
            ViewBag.ResSet = vm.ResourceSet ?? "";
            return View("_ResourcePanel", ResourceManager.ReadResources(ViewBag.ResSet));
        }

        public ActionResult ClearLanguageCache()
        {
            ResourceManager.Reset();
            return RedirectToAction("Index");
        }
        public ActionResult ResDelete(string set, string name)
        {
            ResourceManager.Delete(set, name);
            return View("_ResourcePanel", ResourceManager.ReadResources(set));
        }
        public ActionResult ResRename(string set, string name,string old)
        {
            ResourceManager.Rename(old, name,set);
            return View("_ResourcePanel", ResourceManager.ReadResources(set));
        }
        public ActionResult Translate(int id, string name, string lang)
        {
            var defaultValue = ResourceManager.Get(name, "en");
            ResTranslateViewModel vm = new ResTranslateViewModel
            {
                ResId = id,
                FromLang = "en",
                ToLang = lang,
                Value = defaultValue
            };
            return View("popResourceTranslate", vm);
        }

        public ActionResult SaveResource(ResourceEntry vm)
        {
            ResourceManager.Update(vm);
            ViewBag.ResName = vm.Name;
            ViewBag.ResSet = vm.ResourceSet ?? "";
            return View("_ResourcePanel", ResourceManager.ReadResources(ViewBag.ResSet));
        }

        public ActionResult SetDelete(string set)
        {
            ResourceManager.Delete(set, null);
            return View("_ResourcePanel", ResourceManager.ReadResources(set));
        }

        public ActionResult SetRename(string set)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public FileResult Export(FormCollection form)
        {
            string culture = form["culture"];
            string resourceset = form["resource-set"];
            List<ResourceEntry> res;

            if (!String.IsNullOrWhiteSpace(resourceset))
            {
                res = ResourceManager.ReadResources(resourceset);
                resourceset = "_" + resourceset;
            }
            else
            {
                res = ResourceManager.ReadLangResources(culture);
                resourceset = "";
            }


            var byteArray = Encoding.UTF8.GetBytes(res.ToCSV("path", "", "Id"));
            var stream = new MemoryStream(byteArray);


            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = "export_" + culture + resourceset + ".csv",
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());
            return File(stream, "txt/plain");

        }

        public PartialViewResult Import()
        {
            ViewBag.Title = ResourceManager.GetLocalisedString("resImport", "ResEditor");
            ViewBag.Hint = "";
            return PartialView("popImportCsv");
        }

        public PartialViewResult Export()
        {
            return PartialView("popExportCsv", ResourceManager.ReadResourceSets());
        }

        [HttpPost]
        public JsonResult UploadCSV()
        {
            string uploadPath = Server.MapPath("~/App_Data/");

            if (!Directory.Exists(uploadPath))
            {
                return Json("error|App_Data folder is missing");
            }

            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i]; //Uploaded file                                       
                if (file != null &&
                    file.ContentLength > Convert.ToInt32(ClassicConfig.GetValue("INTMAXFILESIZE"))*1024*1024)
                {
                    return Json("error|File too large");
                }
                if (file != null)
                {
                    var extension = Path.GetExtension(file.FileName);
                    if (extension != null)
                    {
                        string fileExt = extension.ToLower();
                        if (!fileExt.Contains("csv"))
                        {
                            return Json("error|Invalid File type");
                        }
                    }
                    var fileName = Path.GetFileName(file.FileName);
                        //Use the following properties to get file's name, size and MIMEType

                    if (fileName != null)
                    {
                        var path = Path.Combine(uploadPath, fileName);
                        file.SaveAs(path); //File will be saved in users folder
                        SnitzDataContext.ImportLangResCSV(path, Convert.ToBoolean(Request.Form["UpdateExisting"]));
                    }

                }
            }
            return Json("error|Problem uploading data");
        }

        public ActionResult Refresh()
        {

            LangViewModel vm = new LangViewModel {ResourceSets = ResourceManager.ReadResourceSets()};
            ViewBag.ResSet = vm.ResourceSets[0];
            vm.Resources = ResourceManager.ReadResources(ViewBag.ResSet);
            return View("Index", vm);
        }

        public ActionResult ResourceGrid()
        {
            return View("GridView");

            //return PartialView("_ResourceGrid");
        }

        public ActionResult EditRow(string id, string[] rowData)
        {

            if (rowData != null)
            {
                LangUpdateViewModel vm = new LangUpdateViewModel();
                
                var cultures = rowData[1].Split(',');
                vm.ResourceId = rowData[0];
                vm.ResourceSet = rowData[2];
                vm.ResourceTranslations = new Dictionary<string, string>();
                for (int i = 0; i < cultures.Length; i++)
                {
                    vm.ResourceTranslations.Add(cultures[i], WebUtility.HtmlDecode(rowData[3 + i]));
                }
                vm.rownum = id;
                return PartialView("_EditGrid", vm);
            }
            else
            {
                return Json("An Error Has occoured");
            }
        }

        [ValidateInput(false)]
        public JsonResult Update(LangUpdateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    foreach (var resource in vm.ResourceTranslations)
                    {
                        ResourceEntry res = ResourceManager.GetResource(vm.ResourceId, resource.Key, vm.ResourceSet);
                        if (res != null)
                        {
                            if (res.Value != resource.Value)
                            {
                                res.Value = resource.Value;
                                ResourceManager.Update(res);
                            }
                        }
                        else
                        {
                            ResourceManager.Add(vm.ResourceId, resource.Value, resource.Key, vm.ResourceSet);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(ex.Message, JsonRequestBehavior.AllowGet);
                }

                //return PartialView("_ResourceGrid");
                Response.StatusCode = (int)HttpStatusCode.OK;
                //Json.Encode(vm.ResourceTranslations)) 
                return Json(vm, JsonRequestBehavior.AllowGet);
                //return Json("success", JsonRequestBehavior.AllowGet);

            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json("error", JsonRequestBehavior.AllowGet);
            }
            
        }
    }
}
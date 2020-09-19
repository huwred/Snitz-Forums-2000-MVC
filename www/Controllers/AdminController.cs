using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using CreativeMinds.StopForumSpam;
using CreativeMinds.StopForumSpam.Responses;
using BbCodeFormatter;
using Hangfire;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using SnitzMembership.Models;
using SnitzMembership.Repositories;
using WWW.Models;
using WWW.ViewModels;
using BadwordFilter = SnitzDataModel.Models.BadwordFilter;
using Category = SnitzDataModel.Models.Category;
using Forum = SnitzDataModel.Models.Forum;
using ForumModerators = SnitzDataModel.Models.ForumModerators;
using ForumTotals = SnitzDataModel.Models.ForumTotals;
using Member = SnitzDataModel.Models.Member;
using NameFilter = SnitzDataModel.Models.NameFilter;
using SpamFilter = SnitzDataModel.Models.SpamFilter;
using Subscriptions = SnitzDataModel.Models.Subscriptions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WWW.Filters;

namespace WWW.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : CommonController
    {

        public AdminController()
        {
            Dbcontext = new SnitzDataContext();

        }

        #region GET: Methods

        public ActionResult Index(string id)
        {
            AdminViewModel vm = new AdminViewModel();
            ViewBag.ActivePage = id;
            return View();
            //return View("ForumConfig", vm);
        }

        [SuperAdmin][DisplayName("Main Config")]
        public ActionResult ForumConfig()
        {
            AdminViewModel vm = new AdminViewModel();
            return View(vm);
        }

        public ActionResult FeatureConfig()
        {

            AdminFeaturesViewModel vm = new AdminFeaturesViewModel {SubscriptionLevel = ClassicConfig.SubscriptionLevel};
           //vm.CaptchaOperators = Config.CaptchaOperators.Count == 0 ?
           //     new List<Enumerators.CaptchaOperator>() { Enumerators.CaptchaOperator.Plus } : Config.CaptchaOperators;

            vm.AllowedForums = new Dictionary<int, string>();

            return View(vm);
        }

        [SuperAdmin][DisplayName("Email Config")]
        public ActionResult EmailServer()
        {
            AdminEmailServer vm = new AdminEmailServer();
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration("~");

            MailSettingsSectionGroup mailSettings =
                configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;

            if (mailSettings != null)
            {
                vm.Port = mailSettings.Smtp.Network.Port;
                vm.Server = mailSettings.Smtp.Network.Host;
                vm.Password = mailSettings.Smtp.Network.Password;
                vm.Username = mailSettings.Smtp.Network.UserName;
                vm.From = mailSettings.Smtp.From;
                vm.DeliveryMethod = mailSettings.Smtp.DeliveryMethod;
                vm.PickUpFolder = mailSettings.Smtp.SpecifiedPickupDirectory.PickupDirectoryLocation;
                vm.DefaultCred = mailSettings.Smtp.Network.DefaultCredentials;
                if (vm.DefaultCred || !string.IsNullOrEmpty(vm.Username))
                    vm.Auth = true;

            }
            vm.EmailMode = ClassicConfig.GetValue("STREMAIL");
            vm.UseSpamFilter = ClassicConfig.GetValue("STRFILTEREMAILADDRESSES");
            vm.ContactEmail = ClassicConfig.GetValue("STRCONTACTEMAIL") ?? vm.From;
            vm.BannedDomains = SpamFilter.All().OrderBy(s=>s.SpamServer).ToArray();
            return View(vm);
        }

        [SuperAdmin][DisplayName("Member Config")]
        public ActionResult MemberConfig()
        {

            AdminViewModel vm = new AdminViewModel();
            return View(vm);
        }

        public ActionResult RankingConfig()
        {
            RankingViewModel vm = new RankingViewModel();
            return View(vm);
        }

        [OutputCache(Duration = 1, VaryByParam = "*")]
        public ActionResult DateConfig()
        {
            AdminViewModel vm = new AdminViewModel
            {
                ServerTime = DateTime.UtcNow.ToFormattedString(),
                ForumTime = DateTime.UtcNow.ToClientTime().ToFormattedString()
            };

            return View(vm);
        }

        public ActionResult UserFilter()
        {
            return View(NameFilter.All());
        }

        public ActionResult WordFilter()
        {
            return View(BadwordFilter.All());
        }
        public ActionResult BbCodes()
        {
            return View(CustomBBcode.All());
        }
        [SuperAdmin]
        public ActionResult PendingMembers()
        {
            PendingMemberViewModel pvm = new PendingMemberViewModel
            {
                Pending = MemberManager.PendingMembers().ToList(),
                EnabledProfileFields = UserProfileExtensions.EnabledProfileFields()
            };
            return View(pvm);
        }

        [SuperAdmin][DisplayName("Managing Roles")]
        public ActionResult ManageRoles()
        {
            var arvm = new AdminRolesViewModel {RoleList = Roles.GetAllRoles()};

            return View(arvm);
        }

        public ActionResult ManageGroups(int id = 0)
        {
            var vm = new AdminGroupsViewModel(id)
            {
                Groups = Group.Fetch("SELECT * FROM " + Dbcontext.ForumTablePrefix + "GROUP_NAMES"),
            };

            return View(vm);
        }

        [HttpGet]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult AddEditGroup(string id)
        {
            int test = id == "0" ? 0 : Convert.ToInt32(id);
            var group = Group.SingleOrDefaultById(test);
            if (group == null)
                group = new Group();

            return PartialView("_AddEditGroup", group);
        }

        [HttpPost]
        public ActionResult AddEditGroup(Group group)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Dbcontext.Save(group);
                }
                catch (Exception e)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, e.Message);
                }
                
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid data");
            }
                

            return Json("Success");
        }

        public ActionResult AddGroupCat(FormCollection form)
        {
            if (form["CurrentGroupId"] != "0")
            {
                ForumGroup g = new ForumGroup
                {
                    Id = Convert.ToInt32(form["CurrentGroupId"]),
                    CatId = Convert.ToInt32(form["CatList"])
                };
                Dbcontext.Save(g);

                return Json("success");
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid Group");
        }
        public ActionResult DelGroupCat(FormCollection form)
        {
            if (form["CurrentGroupId"] != "0")
            {
                ForumGroup g = ForumGroup.SingleOrDefault("WHERE GROUP_ID=@0 AND GROUP_CATID=@1", Convert.ToInt32(form["CurrentGroupId"]), Convert.ToInt32(form["CatList"]));
                Dbcontext.Delete(g);

                return Json("success");
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid Group");
        }
        public ActionResult ManageSubs(Enumerators.SubscriptionLevel level = Enumerators.SubscriptionLevel.Board, int page=1)
        {
            //var allsubs = Subscriptions.All();
            var svm = new AdminSubscriptionsViewModel
            {
                Subscriptions = new List<Subscriptions>(),
                SubscriptionLevel = ClassicConfig.SubscriptionLevel,
                VisibleLevel = level
            };
            switch (level)
            {
                case Enumerators.SubscriptionLevel.Board:
                    if(svm.SubscriptionLevel == Enumerators.SubscriptionLevel.Board)
                        svm.Subscriptions = Subscriptions.BoardSubs().ToList();
                    ViewBag.LevelTitle = "Board";
                    break;
                case Enumerators.SubscriptionLevel.Category:
                    svm.Subscriptions = Subscriptions.CatSubs().ToList();
                    ViewBag.LevelTitle = "Category";
                    break;
                case Enumerators.SubscriptionLevel.Forum:
                    svm.Subscriptions = Subscriptions.ForumSubs().ToList();
                    ViewBag.LevelTitle = "Forum";
                    break;
                case Enumerators.SubscriptionLevel.Topic:
                    svm.Subscriptions = Subscriptions.TopicSubs(page).Items;
                    ViewBag.LevelTitle = "Topic";
                    break;
            }

            return View("Subscriptions", svm);
        } 
        [SuperAdmin][DisplayName("Tools")]
        public ActionResult Tools()
        {
            
            return View(new AdminToolsViewModel(User));
        }

        //public ActionResult ManageDownloads()
        //{
        //    if (Config.TableExists("FORUM_FILECOUNT"))
        //    {
        //        return View(SnitzDataContext.GetFileCounts(null));
        //    }
        //    return View();
        //}
        [SuperAdmin][DisplayName("Managing Moderators")]
        public ActionResult ManageModerators()
        {
            AdminModeratorsViewModel vm = new AdminModeratorsViewModel(User);
            ViewBag.Moderators = ForumModerators.All();
            return View(vm);
        }
        public ActionResult ManageAvatars()
        {

            FilesViewModel vm = new FilesViewModel();
            string avatarPath = Server.MapPath("~/" + Config.ContentFolder + "/Avatar/");
            vm.FolderUrl = VirtualPathUtility.ToAbsolute("~/" + Config.ContentFolder + "/Avatar/");
            vm.Files = new FileInfo[] { };
            //check destination folder exists and create if not
            if (Directory.Exists(avatarPath))
            {
                var dir = new DirectoryInfo(avatarPath);
                vm.Files = dir.GetFiles().Where(s=>!s.Name.Contains("notfound") && !s.Name.Contains("default")).ToArray();
            }

            return View(vm);
        }
        [OutputCache(Location = System.Web.UI.OutputCacheLocation.None)]
        public ActionResult ManageBanners()
        {

                        var abm = new AdminBannersViewModel();

            return View(abm);
        }
        #endregion

        #region Save Methods
        [SuperAdmin]
        public ActionResult SaveForumConfig(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                Config.ForumTitle = form["forumTitleMvc"];
                Config.ForumUrl = form["urlForumMvc"];
                Config.ForumDescription = form["forumDescription"];
                Config.TitleImage = form["forumImageMvc"];
                Config.DefaultTheme = form["defaultTheme"];
                Config.ProhibitNewMembers = form.AllKeys.Contains("allowRegisterMvc");

                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRCOPYRIGHT", form["strcopyright"]);
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRHOMEURL", form["strhomeurl"]);
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRSETCOOKIETOFORUM", form.AllKeys.Contains("strsetcookietoforum") ? "0" : "1");
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRPROHIBITNEWMEMBERS", form.AllKeys.Contains("strprohibitnewmembers") ? "1" : "0");
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRREQUIREREG", form.AllKeys.Contains("strrequirereg") ? "1" : "0");
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRRESTRICTREG", form.AllKeys.Contains("strrestrictreg") ? "1" : "0");
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRUSERNAMEFILTER", form.AllKeys.Contains("strusernamefilter") ? "1" : "0");
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTTHEMECHANGE", (form.AllKeys.Contains("intthemechange") ? "1" : "0"));
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTREQUIRECONSENT", (form.AllKeys.Contains("intrequireconsent") ? "1" : "0"));
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTPAGETIMER", (form.AllKeys.Contains("intpagetimer") ? "1" : "0"));
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRADMINUSER", form["stradminuser"]);
                ClassicConfig.Update(new[]
                {
                    "STRCOPYRIGHT", "STRHOMEURL", "STRSETCOOKIETOFORUM", "STRPROHIBITNEWMEMBERS","STRREQUIREREG","STRUSERNAMEFILTER","INTREQUIRECONSENT","STRADMINUSER","STRRESTRICTREG","INTPAGETIMER","INTTHEMECHANGE"
                });

                //foreach (var key in form.AllKeys)
                //{
                //    if (key.StartsWith("str") || key.StartsWith("int"))
                //    {
                //        string[] amounts = Request.Form.GetValues(key);
                //        if (amounts != null)
                //            ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting(key.ToUpper(), amounts[0]);
                //    }
                //    ClassicConfig.Update(new string[] { key.ToUpper() });
                //}
                Config.Update();
            }
            return View("ForumConfig", new AdminViewModel());

        }
        [ValidateInput(false)] 
        public ActionResult SaveFeatureConfig(FormCollection form)
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
                    ClassicConfig.Update(new string[]{ upperkey });
                }
                if (ClassicConfig.GetIntValue("INTALLOWHIDEONLINE") == 1 && !Roles.GetAllRoles().Contains("HiddenMembers"))
                {
                    Roles.CreateRole("HiddenMembers");
                }
                if (ClassicConfig.GetIntValue("INTALLOWHIDEONLINE") == 0 && Roles.GetAllRoles().Contains("HiddenMembers"))
                {
                    var userinrole = Roles.GetUsersInRole("HiddenMembers");
                    Roles.RemoveUsersFromRole(userinrole, "HiddenMembers");
                    Roles.DeleteRole("HiddenMembers", false);
                }
                List <Enumerators.CaptchaOperator> operators = new List<Enumerators.CaptchaOperator>();
                if (form.AllKeys.Contains("CaptchaOperatorsPlus"))
                    operators.Add(Enumerators.CaptchaOperator.Plus);
                if (form.AllKeys.Contains("CaptchaOperatorsMinus"))
                    operators.Add(Enumerators.CaptchaOperator.Minus);
                if (form.AllKeys.Contains("CaptchaOperatorsMultiply"))
                    operators.Add(Enumerators.CaptchaOperator.Multiply);

                var strcap = "";
                foreach (var captchaOperator in operators)
                {
                    if (strcap != "")
                        strcap += ";";
                    strcap += captchaOperator.ToString();
                }
                //ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRLAYOUT", form["strlayout"]);
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRCAPTCHAOPERATORS", strcap);
                ClassicConfig.ConfigDictionary["STRSUBSCRIPTION"] = ((int)Enum.Parse(typeof(Enumerators.SubscriptionLevel), form["SubscriptionLevel"])).ToString();
                ClassicConfig.Update(new[]
                                     { 
                                         "STRSUBSCRIPTION","STRCAPTCHAOPERATORS" //,"STRLAYOUT"
                                     });
                
            }
            if (form.AllKeys.Contains("AllowedForums"))
            {
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRAPIFORUMS", form["AllowedForums"]);
                ClassicConfig.Update(new[]
                                     {
                                         "STRAPIFORUMS"
                                     });
            }

            return View("FeatureConfig", new AdminFeaturesViewModel { SubscriptionLevel = ClassicConfig.SubscriptionLevel, AllowedForums = new Dictionary<int, string>() });
        }

        public ActionResult SaveMemberConfig(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                #region member-profile
                ClassicConfig.ConfigDictionary["STRFULLNAME"] = form.AllKeys.Contains("fullname") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQFULLNAME"] = form.AllKeys.Contains("req-fullname") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRAGE"] = form.AllKeys.Contains("age") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQAGE"] = form.AllKeys.Contains("req-age") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRAGEDOB"] = form.AllKeys.Contains("agedob") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQAGEDOB"] = form.AllKeys.Contains("req-agedob") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRMINAGE"] = form["min-age"];
                ClassicConfig.ConfigDictionary["STRSEX"] = form.AllKeys.Contains("gender") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQSEX"] = form.AllKeys.Contains("req-gender") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRMARSTATUS"] = form.AllKeys.Contains("marstatus") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQMARSTATUS"] = form.AllKeys.Contains("req-marstatus") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRCITY"] = form.AllKeys.Contains("city") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQCITY"] = form.AllKeys.Contains("req-city") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRSTATE"] = form.AllKeys.Contains("state") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQSTATE"] = form.AllKeys.Contains("req-state") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRCOUNTRY"] = form.AllKeys.Contains("country") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQCOUNTRY"] = form.AllKeys.Contains("req-country") ? "1" : "0";
                ClassicConfig.Update(
                    new[]
                    {
                        "STRFULLNAME",
                        "STRREQFULLNAME",
                        "STRAGE",
                        "STRREQAGE",
                        "STRAGEDOB",
                        "STRREQAGEDOB",
                        "STRMINAGE",
                        "STRSEX",
                        "STRREQSEX",
                        "STRMARSTATUS",
                        "STRREQMARSTATUS",
                        "STRCITY",
                        "STRREQCITY",
                        "STRSTATE",
                        "STRREQSTATE",
                        "STRCOUNTRY",
                        "STRREQCOUNTRY"
                    });
                #endregion
                #region member-extras
                ClassicConfig.ConfigDictionary["STRPICTURE"] = form.AllKeys.Contains("picture") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQPICTURE"] = form.AllKeys.Contains("req-picture") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STROCCUPATION"] = form.AllKeys.Contains("occupation") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQOCCUPATION"] = form.AllKeys.Contains("req-occupation") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRHOMEPAGE"] = form.AllKeys.Contains("homepage") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQHOMEPAGE"] = form.AllKeys.Contains("req-homepage") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRBIO"] = form.AllKeys.Contains("bio") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQBIO"] = form.AllKeys.Contains("req-bio") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRAIM"] = form.AllKeys.Contains("aim") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQAIM"] = form.AllKeys.Contains("req-aim") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRICQ"] = form.AllKeys.Contains("icq") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQICQ"] = form.AllKeys.Contains("req-icq") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRMSN"] = form.AllKeys.Contains("msn") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQMSN"] = form.AllKeys.Contains("req-msn") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRYAHOO"] = form.AllKeys.Contains("yahoo") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQYAHOO"] = form.AllKeys.Contains("req-yahoo") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRFAVLINKS"] = form.AllKeys.Contains("favlinks") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQFAVLINKS"] = form.AllKeys.Contains("req-favlinks") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRHOBBIES"] = form.AllKeys.Contains("hobbies") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQHOBBIES"] = form.AllKeys.Contains("req-hobbies") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRLNEWS"] = form.AllKeys.Contains("lnews") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQLNEWS"] = form.AllKeys.Contains("req-lnews") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRQUOTE"] = form.AllKeys.Contains("quote") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRREQQUOTE"] = form.AllKeys.Contains("req-quote") ? "1" : "0";
                ClassicConfig.Update(
                    new[]
                    {
                        "STRPICTURE",
                        "STRREQPICTURE",
                        "STROCCUPATION",
                        "STRREQOCCUPATION",
                        "STRHOMEPAGE",
                        "STRREQHOMEPAGE",
                        "STRBIO",
                        "STRREQBIO",
                        "STRAIM",
                        "STRREQAIM",
                        "STRICQ",
                        "STRREQICQ",
                        "STRMSN",
                        "STRREQMSN",
                        "STRYAHOO",
                        "STRREQYAHOO",
                        "STRFAVLINKS",
                        "STRREQFAVLINKS",
                        "STRHOBBIES",
                        "STRREQHOBBIES",
                        "STRLNEWS",
                        "STRREQLNEWS",
                        "STRQUOTE",
                        "STRREQQUOTE"
                    });
                #endregion
                #region member-regisration

                ClassicConfig.ConfigDictionary["STRUNIQUEEMAIL"] = form.AllKeys.Contains("uniqemail") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STREMAILVAL"] = form.AllKeys.Contains("emailval") ? "1" : "0";
                //ClassicConfig.ConfigDictionary["STRRESTRICTREG"] = form.AllKeys.Contains("restrictreg") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRLOGONFORMAIL"] = form.AllKeys.Contains("logonemail") ? "1" : "0";
                ClassicConfig.ConfigDictionary["INTMAXPOSTSTOEMAIL"] = form["emailpostcount"];
                ClassicConfig.ConfigDictionary["STRNOMAXPOSTSTOEMAIL"] = form["emailpostcount-message"];
                ClassicConfig.Update(
                    new[]
                    {
                        "STRUNIQUEEMAIL",
                        "STREMAILVAL",
                        //"STRRESTRICTREG",
                        "STRLOGONFORMAIL",
                        "INTMAXPOSTSTOEMAIL",
                        "STRNOMAXPOSTSTOEMAIL"
                    });
                #endregion
                #region private messages

                ClassicConfig.ConfigDictionary["STRPMSTATUS"] = form.AllKeys.Contains("strpmstatus") ? "1" : "0";
                ClassicConfig.ConfigDictionary["INTMAXPMRECIPIENTS"] = form["intmaxrecipients"];
                ClassicConfig.ConfigDictionary["STRPMLIMIT"] = form["strpmlimit"];
                ClassicConfig.ConfigDictionary["STRPMSOUND"] = form.AllKeys.Contains("strpmsound") ? "1" : "0";
                ClassicConfig.ConfigDictionary["STRALLOWUPLOADS"] = form["strallowuploads"];
                ClassicConfig.ConfigDictionary["STRPMSIG"] = form["strpmsig"];
                ClassicConfig.ConfigDictionary["INTALLOWCHAT"] = form["intallowchat"];
                ClassicConfig.Update(
                    new[]
                    {
                        "STRPMSTATUS",
                        "INTMAXPMRECIPIENTS",
                        "STRPMLIMIT",
                        "STRPMSOUND",
                        "STRALLOWUPLOADS",
                        "STRPMSIG",
                        "INTALLOWCHAT"
                    });
                #endregion
            }
            return View("MemberConfig", new AdminViewModel());
        }

        public ActionResult SaveRankingConfig(FormCollection form)
        {
            ClassicConfig.ConfigDictionary["STRSHOWRANK"] = ((int) Enum.Parse(typeof(Enumerators.RankType), form["Type"])).ToString();
            ClassicConfig.Update(
                new[]
                {
                    "STRSHOWRANK"
                });
            var ranks = Dbcontext.Fetch<Rankings>("SELECT * FROM " + Dbcontext.ForumTablePrefix + "RANKING");
            var service = new InMemoryCache() { DoNotExpire = true };
            service.Remove("Snitz.Rankings");
            if (ModelState.IsValid)
            {
                if (ranks.Any())
                {
                    foreach (Rankings rank in ranks)
                    {
                        var id = rank.Id;
                        if (form["rankLevel_" + id] != rank.Threshold.ToString())
                        {
                            rank.Threshold = Convert.ToInt32(form["rankLevel_" + id]);
                        }
                        if (form["rankTitle_" + id] != rank.Title)
                        {
                            rank.Title = form["rankTitle_" + id];
                        }
                        if (form["rankImage_" + id] != rank.Image)
                        {
                            rank.Image = form["rankImage_" + id];
                        }
                        rank.Save();
                    }
                }
                
            }
            return View("RankingConfig", new RankingViewModel());
        }

        [OutputCache(Duration = 1, VaryByParam = "*")]
        public ActionResult SaveDateConfig(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                ClassicConfig.ConfigDictionary["STRTIMETYPE"] = form.AllKeys.Contains("time-format") ? "24" : "12"; 
                ClassicConfig.ConfigDictionary["STRDATETYPE"] = form["date-format"];
                ClassicConfig.ConfigDictionary["STRCURRENTTIMEZONE"] = form["time-zone"];
                ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTUSETIMEAGO", form.AllKeys.Contains("use-timeago") ? "1" : "0");
                ClassicConfig.Update(
                    new[]
                    {
                        "STRTIMETYPE",
                        "STRDATETYPE",
                        "STRCURRENTTIMEZONE",
                        "INTUSETIMEAGO"
                    });
                if (ClassicConfig.ConfigDictionary.ContainsKey("STRTIMEADJUST"))
                {
                    ClassicConfig.ConfigDictionary["STRTIMEADJUST"] = form["time-adjust"];
                    ClassicConfig.Update(
                        new[]
                        {
                            "STRTIMEADJUST"
                        });
                }

            }
            var vm = new AdminViewModel
            {
                ServerTime = DateTime.UtcNow.ToFormattedString(),
                ForumTime = DateTime.UtcNow.ToClientTime().ToFormattedString()
            };
            return View("DateConfig", vm);
        }
        [HttpPost]
        public ActionResult SaveBadword(FormCollection form)
        {
            if (form.AllKeys.Contains("posted-id"))
            {
                int id = Convert.ToInt32(form["posted-id"]);
                string bad = form["badword_" + id];
                string replace = form["replaceword_" + id];
                var badword = BadwordFilter.Fetch(id);
                badword.BadWord = bad;
                badword.ReplaceWord = replace;
                badword.Save();

            }
            return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveNameFilter(FormCollection form)
        {
            if (form.AllKeys.Contains("posted-id"))
            {
                int id = Convert.ToInt32(form["posted-id"]);
                string username = form["username_" + id];
                var name = NameFilter.Fetch(id);
                name.Name = username;
                name.Save();
            }
            return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult SaveSettings(ArchivesViewModel vm)
        {
            foreach (Category category in vm.Categories)
            {
                foreach (Forum forum in category.Forums)
                {
                    var testF = Forum.FetchForum(forum.Id);
                    testF.ArchiveSchedule = forum.ArchiveSchedule;
                    testF.DeleteSchedule = forum.DeleteSchedule;
                    testF.Save();
                }

            }
            var service = new InMemoryCache(60);
            service.Remove("category.forums");

            vm = new ArchivesViewModel { Categories = Dbcontext.FetchCategoryForumList(User).ToList() };
            return View("ManageArchives", vm);
        }

        #endregion

        #region Word Filters
        public ActionResult AddNameFilter()
        {
            return PartialView("popAddNameFilter");
        }

        [HttpPost]
        public JsonResult AddNameFilter(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                if (form["name-filter"] != null)
                {
                    NameFilter nf = new NameFilter { Name = form["name-filter"] };
                    nf.Save();
                    var redirectUrl = Url.Content("~/Admin/UserFilter");
                    return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);

                }
            }
            return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RemoveNameFilter(int id)
        {
            if (ModelState.IsValid)
            {
                NameFilter.Delete(id);
                //return Redirect("~/Admin/UserFilter");
                var redirectUrl = Url.Content("~/Admin/UserFilter");
                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult AddBadword()
        {
            return PartialView("popAddWordFilter");
        }
        [HttpPost]
        public JsonResult AddBadword(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                if (form["word-filter"] != null)
                {
                    BadwordFilter nf = new BadwordFilter
                    {
                        BadWord = form["word-filter"],
                        ReplaceWord = form["word-replace"]
                    };
                    nf.Save();
                    var redirectUrl = Url.Content("~/Admin/WordFilter");
                    return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeleteBadword(int id)
        {
            if (ModelState.IsValid)
            {
                BadwordFilter.Delete(id);
                var redirectUrl = Url.Content("~/Admin/WordFilter");
                return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region BB Code
        public ActionResult AddBbCode()
        {
            return PartialView("popAddBBCodeFilter", new CustomBBcode());
        }
        [HttpPost]
        public ActionResult AddBbCode(CustomBBcode code)
        {
            if (ModelState.IsValid)
            {
                code.Save();

                return Json(new { success = true, responseText = "BB filter added" });
            }
            string result = string.Join("; ", ModelState.Values
                                    .SelectMany(x => x.Errors)
                                    .Select(x => x.ErrorMessage));
            return Json(new { success = false, responseText = result });
        }
        public JsonResult DeleteBbCode(int id)
        {
            CustomBBcode.Delete(id);
            var redirectUrl = Url.Action("BbCodes");
            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);
            //return RedirectToAction("BbCodes");
        }
        [HttpPost]
        public ActionResult SaveBbCode(CustomBBcode code)
        {
            if (ModelState.IsValid)
            {
                code.Save();
                
            }
            return RedirectToAction("BbCodes");
        }
        #endregion

        #region Manage Roles

        [SuperAdmin][DisplayName("Delete Role")]
        public ActionResult DeleteRole(string rolename)
        {
            if (!String.IsNullOrWhiteSpace(rolename))
            {
                Roles.DeleteRole(rolename,false);
            }
            AdminRolesViewModel vm = new AdminRolesViewModel {RoleList = Roles.GetAllRoles()};
            return View("ManageRoles", vm);
        }
        
        [HttpParamAction]
        public ActionResult AddRole(AdminRolesViewModel vm)
        {
            if (Roles.RoleExists(vm.NewRolename))
            {
                ModelState.AddModelError("RoleName", LangResources.Utility.ResourceManager.GetLocalisedString("AdminController_AddRole_Role_already_exists", "ErrorMessage"));
            }
            if (ModelState.IsValid)
            {
                Roles.CreateRole(vm.NewRolename);
                vm = new AdminRolesViewModel();
            }

            vm.RoleList = Roles.GetAllRoles();
            return View("ManageRoles", vm);
        }
        [HttpParamActionAttribute]
        public ActionResult AddMemberToRole(AdminRolesViewModel vm)
        {
            var checkuser = WebSecurity.GetUser(vm.Username);
            if (checkuser == null)
            {
                ModelState.AddModelError("UserName", LangResources.Utility.ResourceManager.GetLocalisedString("AdminController_Username_does_not_exist", "ErrorMessage"));
                vm.RoleList = Roles.GetAllRoles();
                return View("ManageRoles", vm);
            }
            if (!Roles.RoleExists(vm.Rolename))
            {
                ModelState.AddModelError("RoleName", LangResources.Utility.ResourceManager.GetLocalisedString("AdminController_AddRole_Role_already_exists", "ErrorMessage"));
                vm.RoleList = Roles.GetAllRoles();
                return View("ManageRoles", vm);
            }
            if (ModelState.IsValid)
            {
                Roles.AddUserToRole(vm.Username, vm.Rolename);
                //compatability
                if(new[]{"Administrator","Moderator"}.Contains(vm.Rolename))
                {
                    var user = Member.GetByName(vm.Username);
                    user.UserLevel = (short)(vm.Rolename == "Administrator" ? 3 : 2);
                    //user.Save();
                    user.Update(new List<String> { "M_LEVEL" });
                }
                vm = new AdminRolesViewModel();
            }
            vm.RoleList = Roles.GetAllRoles();
            return View("ManageRoles", vm);
        }

        public ActionResult DelMemberFromRole(string user, string rolename)
        {
            if (!String.IsNullOrWhiteSpace(rolename))
            {
                Roles.RemoveUserFromRole(user,rolename);
                if (new[] {"Administrator", "Moderator"}.Contains(rolename))
                {
                    var member = Member.GetByName(user);
                    member.UserLevel = 1;
                    member.Update(new List<String> { "M_LEVEL"});
                    //Remove user from any moderated forums
                    if (rolename == "Moderator")
                    {
                        ForumModerators.RemoveMember(member.Id);
                    }
                }
            }
            AdminRolesViewModel vm = new AdminRolesViewModel {RoleList = Roles.GetAllRoles()};
            return View("ManageRoles", vm);
        }
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public PartialViewResult GetRoleView(string rolename)
        {


            AdminRolesViewModel vm = new AdminRolesViewModel
            {
                RoleList = Roles.GetAllRoles(),
                Members = new List<UserProfile>()
            };

            if (!String.IsNullOrWhiteSpace(rolename))
            {
                vm.Rolename = rolename;
                ModelState.Clear();
                string[] users = Roles.GetUsersInRole(rolename);
                using (SnitzMemberContext db = new SnitzMemberContext())
                {
                    vm.Members = (from s in db.UserProfiles
                        where users.Contains(s.UserName)
                        select s).ToList();
                }
            }
            return PartialView("_RoleView",vm);
        }
        #endregion

        #region tools
        [HttpPost]
        [SuperAdmin][DisplayName("Email Config")]
        public ActionResult EmailServer(AdminEmailServer vm)
        {
            if (ModelState.IsValid)
            {
                Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration("~");

                MailSettingsSectionGroup mailSettings =
                    configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;

                if (mailSettings != null)
                {
                    mailSettings.Smtp.Network.Port = vm.Port;
                    mailSettings.Smtp.Network.Host = vm.Server;
                    if (vm.Auth)
                    {
                        if (vm.DefaultCred)
                        {
                            mailSettings.Smtp.Network.DefaultCredentials = vm.DefaultCred;
                        }
                        else
                        {
                            mailSettings.Smtp.Network.Password = vm.Password;
                            mailSettings.Smtp.Network.UserName = vm.Username;                          
                        }
                    }

                    mailSettings.Smtp.From = vm.From;
                    mailSettings.Smtp.DeliveryMethod = vm.DeliveryMethod;
                    if (vm.DeliveryMethod.In(SmtpDeliveryMethod.SpecifiedPickupDirectory,
                        SmtpDeliveryMethod.PickupDirectoryFromIis))
                    {
                        mailSettings.Smtp.SpecifiedPickupDirectory.PickupDirectoryLocation = vm.PickUpFolder;
                    }
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRSENDER", vm.From);
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRCONTACTEMAIL",vm.ContactEmail);
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STREMAIL", vm.EmailMode == null ? "0" : "1");
                    ClassicConfig.ConfigDictionary["STRFILTEREMAILADDRESSES"] = vm.UseSpamFilter == null ? "0" : "1";
                    ClassicConfig.Update(new[]
                                         {
                                         "STRFILTEREMAILADDRESSES","STRCONTACTEMAIL","STREMAIL","STRSENDER"
                                     });

                    configurationFile.Save();
                }
            }
            vm.EmailMode = ClassicConfig.GetValue("STREMAIL");
            vm.UseSpamFilter = ClassicConfig.GetValue("STRFILTEREMAILADDRESSES");

            return View(vm);
        }

        public ActionResult LockUser(string user)
        {
            //test util
            Roles.AddUserToRole(user,"Disabled");
            AdminViewModel vm = new AdminViewModel();
            return View("ForumConfig",vm);
        }

        public ActionResult ApproveMember(int id)
        {
            using (SnitzMemberContext db = new SnitzMemberContext())
            {
                UserProfile user = db.GetUser(id);
                var confirmationToken = db.GetEmailConfirmToken(user.UserName);

                if (ClassicConfig.EmailValidation && !String.IsNullOrWhiteSpace(confirmationToken.Token))
                {
                    EmailController.SendApprovedEmailConfirmation(ControllerContext, user.Email, user.UserName, confirmationToken.Token,user.UserId);
                    user.UserLevel = 1;
                    db.SaveChanges();
                }
                else
                {
                    var member = db.GetMembership(id);
                    member.IsConfirmed = true;
                    user.UserLevel = 1;
                    user.Status = 1;
                    user.LastActivity = DateTime.UtcNow.ToSnitzDate();
                    db.SaveChanges();
                    EmailController.SendApprovalConfirmation(ControllerContext, user.Email, user.UserName, confirmationToken.Token, user.UserId);
                    ForumTotals.AddUser();
                }

                return Redirect("~/Admin/PendingMembers");
            }

        }      

        public ActionResult DeleteMemberTopics(FormCollection form)
        {
            if (!String.IsNullOrWhiteSpace(form["MemberName"]))
            {
                bool delreplies = form.AllKeys.Contains("delete-replies");
                bool deluser = form.AllKeys.Contains("delete-user");
                try
                {
                    var user = Member.GetByName(form["MemberName"]);
                    user.DeleteTopics(delreplies);
                    if (deluser)
                    {
                        Dbcontext.Delete(user);
                    }
                }
                catch (Exception e)
                {
                    TempData["errTitle"] = "Delete Member Topics";
                    TempData["errMessage"] = e.Message;                     
                }
            }
            else
            {
                TempData["errTitle"] = "Delete Member Topics";
                TempData["errMessage"] = "You must provide a Username";          
            }
            return Redirect("~/Admin/Tools");
        }

        public ActionResult UpdatePostCount()
        {
            Dbcontext.UpdatePostCount();
            return Redirect("~/Admin/Tools");
        }
        #endregion


        [OutputCache(Duration = 1, VaryByParam = "*")]
        public ActionResult GetForumModerators(int id)
        {
            Forum forum = Forum.FetchForum(id);
            AdminModeratorsViewModel vm = new AdminModeratorsViewModel(User) {ForumId = id};
            Dictionary<int, string> modList = Forum.Moderators(id);

            ViewBag.Moderators = ForumModerators.All();
            ViewBag.Moderation = forum.Moderation;
            foreach (KeyValuePair<int, string> mod in modList)
            {
                vm.ForumModerators.Add(mod.Key);
            }
            return PartialView("_Moderators",vm);
        }

        [HttpPost]
        public ActionResult ManageModerators(AdminModeratorsViewModel vm)
        {
            if (ModelState.IsValid)
            {
                Forum forum = Forum.FetchForum(vm.ForumId);
                forum.SaveModerators(vm.ForumModerators.ToList());
                
            }
            vm = new AdminModeratorsViewModel(User);
            vm.ModList = Forum.Moderators(vm.ForumId);
            ViewBag.Moderators = ForumModerators.All();
            foreach (KeyValuePair<int, string> mod in vm.ModList)
            {
                vm.ForumModerators.Add(mod.Key);
            }
            return View("ManageModerators", vm);
        }

        public ActionResult RemoveAvatar(string url)
        {
            string avatarPath = Server.MapPath("~/" + Config.ContentFolder + "/Avatar/");

            try
            {
                System.IO.File.Delete(Path.Combine(avatarPath, url));
            }
            catch(Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }

            FilesViewModel vm = new FilesViewModel
            {
                FolderUrl = VirtualPathUtility.ToAbsolute("~/" + Config.ContentFolder + "/Avatar/"),
                Files = new FileInfo[] {}
            };
            //check destination folder exists and create if not
            if (Directory.Exists(avatarPath))
            {
                var dir = new DirectoryInfo(avatarPath);
                vm.Files = dir.GetFiles();
            }

            return View("ManageAvatars", vm);
        }


        public ActionResult ManageArchives()
        {
            ArchivesViewModel vm = new ArchivesViewModel {Categories = Dbcontext.FetchCategoryForumList(User).ToList()};
            return View(vm);
        }

        public ActionResult ArchiveForum(int id)
        {
            if (ClassicConfig.ConfigDictionary["STRARCHIVESTATE"] != "1")
            {
                ViewBag.Error = "Archiving not enabled";
                return View("Error");
            }
            ArchiveViewModel vm = new ArchiveViewModel {ForumId = id};
            return PartialView("popArchiveForum", vm);
        }

        [HttpPost]
        public ActionResult ArchiveForum(ArchiveViewModel vm)
        {
            var archiveDate = DateTime.UtcNow.AddMonths(-vm.MonthsOlder).ToSnitzDate();
            BackgroundJob.Enqueue(() => Forum.ArchiveTopics(vm.ForumId, archiveDate));

            ArchivesViewModel avm = new ArchivesViewModel {Categories = Dbcontext.FetchCategoryForumList(User).ToList()};
            return View("ManageArchives", avm);
        }
        public ActionResult DeleteArchiveForum(int id)
        {
            ArchivesViewModel avm = new ArchivesViewModel {Categories = Dbcontext.FetchCategoryForumList(User).ToList()};
            return View("ManageArchives", avm);
        }

        public ActionResult UnSubscribe(int id, int forumid, int catid, int userid)
        {
            try
            {
                Subscriptions.UnSubscribe(catid, forumid, id, userid);

                return Json(new { code = 0 }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }

        }
        public JsonResult RemoveSubscription(int id)
        {
            try
            {
                Subscriptions.Remove(id);
                //return RedirectToAction("ManageSubs","Admin");
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(e.Message);
            }
            var redirectUrl = Url.Action("ManageSubs");
            return Json(new { redirectUrl }, JsonRequestBehavior.AllowGet);

        }
        public ActionResult DeleteSubs(FormCollection form)
        {
            if (form.AllKeys.Contains("del-subs"))
            {
                var subs = form["del-subs"].StringToIntList();
                foreach (var subid in subs)
                {
                    Subscriptions.Remove(subid);
                }
            }

            var svm = new AdminSubscriptionsViewModel
            {
                Subscriptions = null,
                SubscriptionLevel = ClassicConfig.SubscriptionLevel,
                VisibleLevel = (Enumerators.SubscriptionLevel) Convert.ToInt32(form["level"])
            };
            switch (svm.VisibleLevel)
            {
                case Enumerators.SubscriptionLevel.Board:
                    if (svm.SubscriptionLevel == Enumerators.SubscriptionLevel.Board)
                        svm.Subscriptions = Subscriptions.BoardSubs().ToList();
                    break;
                case Enumerators.SubscriptionLevel.Category:
                    svm.Subscriptions = Subscriptions.CatSubs().ToList();
                    break;
                case Enumerators.SubscriptionLevel.Forum:
                    svm.Subscriptions = Subscriptions.ForumSubs().ToList();
                    break;
                case Enumerators.SubscriptionLevel.Topic:
                    svm.Subscriptions = Subscriptions.TopicSubs(1).Items;
                    break;
            }

            return View("Subscriptions", svm);

        }

        public ActionResult StopForumSpamCheck(string id, string email, string userip)
        {
            Client client = new Client(); //(apiKeyTextBox.Text)
            Response response;
            if (!String.IsNullOrWhiteSpace(id) && String.IsNullOrWhiteSpace(email) && String.IsNullOrWhiteSpace(userip))
            {
                response = client.CheckUsername(id);
            }
            else if (String.IsNullOrWhiteSpace(id) && !String.IsNullOrWhiteSpace(email) && String.IsNullOrWhiteSpace(userip))
            {
                response = client.CheckEmailAddress(email);
            }
            else if (String.IsNullOrWhiteSpace(id) && String.IsNullOrWhiteSpace(email) && !String.IsNullOrWhiteSpace(userip))
            {
                response = client.CheckIPAddress(userip);
            }
            else
            {
                response = client.Check(id, email, userip);
            }

            if (response.Success)
            {
                ViewBag.Text = "Success:" + Environment.NewLine;

            }
            else
            {
                ViewBag.Text = String.Format("Error: {0}", ((FailResponse)response).Error);
            }

            return PartialView(response.ResponseParts);
        }
        public ActionResult SaveSpamDomain(int id, string domain)
        {
            var spamdomain = SpamFilter.Fetch(id);
            spamdomain.SpamServer = domain;
            spamdomain.Save();
            TempData["ActiveTab"] = "#email-spam";
            return RedirectToAction("EmailServer");
        }
        public ActionResult DeleteSpamDomain(int id)
        {
            var spamdomain = SpamFilter.Fetch(id);
            spamdomain.Delete();
            TempData["ActiveTab"] = "#email-spam";
            return RedirectToAction("EmailServer");
        }
        public ActionResult AddSpamDomain(string domain)
        {
            SpamFilter spamdomain = new SpamFilter();
            if (!domain.StartsWith("@"))
                domain = "@" + domain;
            spamdomain.SpamServer = domain;
            spamdomain.Save();
            return RedirectToAction("EmailServer");
        }
        public ActionResult DeleteSpamFilters(FormCollection form)
        {
            var domains = form["del-spam-check"].StringToIntList();
            foreach (var domain in domains)
            {
                var spamdomain = SpamFilter.Fetch(domain);
                spamdomain.Delete();

            }
            TempData["ActiveTab"] = "#email-spam";
            return RedirectToAction("EmailServer");
        }

        [SuperAdmin][DisplayName("Running DBS scripts")]
        public ActionResult DbsFile()
        {
            string appDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            var files = Directory.GetFiles(appDataPath, "dbs*.xml");
            List<DbsFile> dbsfiles = new List<DbsFile>();
            foreach (string path in files)
            {
                dbsfiles.Add(new DbsFile(path));
            }
            return View(dbsfiles.OrderBy(f=>f.Description).ToList());
        }
        public void ProcessDbs(string id, string dbsfile)
        {
            var provider = ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ProviderName;
            var dbtype = "mssql";
            if (provider.StartsWith("MySql"))
                dbtype = "mysql";
            _dbsProcessor = new DbsFileProcessor(dbsfile) {_dbType = dbtype};
            _dbsProcessor.Add(id);

            ProcessTask processTask = _dbsProcessor.Process;
            processTask.BeginInvoke(id, EndProcess, processTask);

        }
        public ContentResult GetProgress(string id)
        {
            this.ControllerContext.HttpContext.Response.AddHeader("cache-control", "no-cache");
            var currentProgress = _dbsProcessor.GetStatus(id);
            return Content(currentProgress);
        }

        public ActionResult ModConfiguration()
        {
            return View("ModConfiguration");
        }
        public ActionResult SaveModConfig(FormCollection form)
        {
            var updates = new List<string>();
            foreach (var key in form.AllKeys)
            {
                if (key.StartsWith("str") || key.StartsWith("int"))
                {
                    string[] amounts = Request.Form.GetValues(key);
                    if (amounts != null)
                        ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting(key.ToUpper(), amounts[0]);
                }
                updates.Add(key.ToUpper());
                
            }
            ClassicConfig.Update(updates.ToArray());
            //ClassicConfig.Update(form.AllKeys.Where(k => k.StartsWith("str")).Select(k => k.ToUpper()).ToArray());
            //ClassicConfig.Update(form.AllKeys.Where(k => k.StartsWith("int")).Select(k => k.ToUpper()).ToArray());
            return RedirectToAction(form["ControllerView"]);
            //return PartialView("ModConfiguration");
        }

        public ActionResult EnableDebug(FormCollection form)
        {
            ClassicConfig.ShowDebug = form["enable-debug"] == "True";
            return Redirect("~/Admin/Tools");
        }
        public ActionResult ManagePlugin(string controllername)
        {
            return PartialView("ManagePlugin",controllername);
        }
        public ActionResult Reset()
        {
            HttpRuntime.UnloadAppDomain();
            return RedirectToAction("Tools");
        }
        public ActionResult ResetApplication()
        {
            HttpRuntime.UnloadAppDomain();
            return RedirectToAction("Index","Home",null);
        }
        public ActionResult ManagePolls()
        {
            return View();
        }

        public ActionResult SavePollSettings(FormCollection form)
        {
            
            foreach (var key in form.AllKeys)
            {
                if (key.StartsWith("str") || key.StartsWith("int"))
                {
                    string[] amounts = Request.Form.GetValues(key);
                    if (amounts != null)
                    {
                        ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting(key.ToUpper(), amounts[0]);
                        ClassicConfig.Update(new[] {key.ToUpper()});
                    }
                }
            }
            //ClassicConfig.Update(form.AllKeys.Where(k => k.StartsWith("str")).Select(k => k.ToUpper()).ToArray());
            //ClassicConfig.Update(form.AllKeys.Where(k => k.StartsWith("int")).Select(k => k.ToUpper()).ToArray());
            return PartialView("ManagePolls");
        }

        //Offline code
        [Authorize(Roles = "Administrator")]
        public ActionResult GoOffline()
        {
            ViewBag.SetOffline = false;
            return View(new OfflineModel { DelayTillOfflineMinutes = 5, AllowedIP = Common.GetUserIP(System.Web.HttpContext.Current) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult GoOffline(OfflineModel dto)
        {
            OfflineFileData.SetOffline(dto.DelayTillOfflineMinutes,
                        Common.GetUserIP(System.Web.HttpContext.Current),
                        dto.Message,
                        HttpContext.Server.MapPath);
            ViewBag.SetOffline = true;
            return RedirectToAction("GoOffline");
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult GoOnline()
        {
            OfflineFileData.RemoveOffline(HttpContext.Server.MapPath);
            return RedirectToAction("Index", "Home");
        }


        [Authorize(Roles = "Administrator")]
        public ActionResult CreateUser()
        {
            var vm = new AdminCreateUserViewModel();

            return PartialView("_CreateUser",vm);
        }
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult CreateUser(AdminCreateUserViewModel vm)
        {
            bool isSuccess = false;

            if (ModelState.IsValid)
            {
                string status;
                var user = MemberManager.RegisterUser(vm.Username, vm.Password, vm.Email, null, out status,true);
                if (user != null && status == "Success")
                {
                    isSuccess = true;
                }
            }

            return Json(new { result = isSuccess, responseText = "Something wrong!" });

        }


        public ActionResult Import()
        {
            ViewBag.Title = "Import Spam Domains";
            ViewBag.Hint = "<p>File containing one domain per line</p>";
            return PartialView("popImportCsv");
        }
        [HttpPost]
        [Authorize]
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
                if (file != null && file.ContentLength > Convert.ToInt32(ClassicConfig.GetValue("INTMAXFILESIZE")) * 1024 * 1024)
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

                    if (fileName != null)
                    {
                        var path = Path.Combine(uploadPath, fileName);
                        file.SaveAs(path); //File will be saved in users folder
                        ImportSpamData(path);
                    }

                }
            }
            return Json("error|Problem uploading data");
        }

        public ActionResult Online()
        {
            return View();
        }

        public ActionResult SaveLoggingConfig(FormCollection form)
        {
            if (form.AllKeys.Contains("intlogrequests"))
            {
                string[] amounts = Request.Form.GetValues("intlogrequests");
                if (amounts != null)
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTLOGREQUESTS", amounts[0]);
                amounts = Request.Form.GetValues("intlogrequestssearch");
                if (amounts != null)
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTLOGREQUESTSSEARCH", amounts[0]);
                ClassicConfig.Update(new[]
                                     {
                                         "INTLOGREQUESTS","INTLOGREQUESTSSEARCH"
                                     });
            }
            return View("Online");
        }

        [HttpPost]
        [Authorize(Roles="Administrator")]
        public ActionResult RemoveAudience(string id)
        {
            AudiencesStore.RemoveAudience(id);

            return Json(Url.Action("FeatureConfig") + "#api");

        }
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult AddAudience(string id)
        {
            AudiencesStore.AddAudience(id);
            return Json(Url.Action("FeatureConfig") + "#api");
        }

        public ActionResult MergeMembers()
        {
            MergeMemberViewModel vm = new MergeMemberViewModel();
            return View(vm);

        }
        [HttpPost]
        public ActionResult MergeMembers(FormCollection form)
        {
            Member user = null;
            Member mergeuser = null;

            if (!String.IsNullOrWhiteSpace(form["MemberName"]) && !String.IsNullOrWhiteSpace(form["MemberToMerge"]))
            {
                try
                {
                    user = Member.GetByName(form["MemberName"]);
                    mergeuser = Member.GetByName(form["MemberToMerge"]);

                }
                catch (Exception e)
                {
                    TempData["errTitle"] = "Merge Members";
                    TempData["errMessage"] = e.Message;
                }
            }
            else
            {
                TempData["errTitle"] = "Merge Member";
                TempData["errMessage"] = "You must provide a Username and a username to merge";
            }
            MergeMemberViewModel vm = new MergeMemberViewModel();
            if (user != null || mergeuser != null)
            {
                vm.Primary = user;
                vm.ToMerge = mergeuser;
                return View(vm);
            }
            return Redirect("~/Admin/Tools");

        }
        public ActionResult Merge(FormCollection form)
        {
            List<string> merged = new List<string>();
            var excludeKeys = new List<string>() {"Primary","ToMerge","CopyTopics","CopyPM","Polls","Events","Albums","Thanks","Bookmarks","Ratings"};

            var PrimaryId = form["Primary"];
            var SecondaryId = form["ToMerge"];

            var MovePosts = form.AllKeys.Contains("CopyTopics") ? true : false;
            var MovePM = form.AllKeys.Contains("CopyPM") ? true : false;

            //var primaryuser = Member.GetById(Convert.ToInt32(PrimaryId));
            //var secondaryuser = Member.GetById(Convert.ToInt32(SecondaryId));
            UserProfile primaryuser = MemberManager.GetUser(Convert.ToInt32(PrimaryId));
            UserProfile secondaryuser = MemberManager.GetUser(Convert.ToInt32(SecondaryId));

            foreach (var prop in form.AllKeys.Where(x=> !excludeKeys.Contains(x)))
            {
                if (form[prop] == SecondaryId)
                {
                    if (prop == "PostCount")
                    {
                        primaryuser[prop] = (int)primaryuser[prop] + (int)secondaryuser[prop];
                    }
                    else
                    {
                        primaryuser[prop] = secondaryuser[prop];
                    }
                    
                    merged.Add(prop);
                }
            }

            if (merged.Any())
            {
                MemberManager.SaveMergedProfile(primaryuser,merged.ToArray());
            }

            int pId = Convert.ToInt32(PrimaryId);
            int sId = Convert.ToInt32(SecondaryId);

            Topic.ChangeOwner(pId, sId);
            PrivateMessage.ChangeOwner(pId, sId);
            if (form.AllKeys.Contains("Polls"))
            {
                PollsRepository.Merge(pId,sId);
            }
            if (form.AllKeys.Contains("Events"))
            {
                MemberManager.ChangeOwnership("CAL_EVENTS", "AUTHOR_ID", pId, sId);
                MemberManager.ChangeOwnership("EVENT_SUBSCRIPTIONS", "MEMBER_ID", pId, sId);
            }            
            if (form.AllKeys.Contains("Albums"))
            {
                MemberManager.ChangeOwnership("FORUM_IMAGES", "I_MID", pId, sId);
                if (Config.TableExists("FORUM_IMAGE_COMMENT"))
                {
                    MemberManager.ChangeOwnership("FORUM_IMAGE_COMMENT", "I_MEMBERID", pId, sId);
                }
            }
            if (form.AllKeys.Contains("Thanks"))
            {
                MemberManager.ChangeOwnership("FORUM_THANKS", "MEMBER_ID", pId, sId);
            }
            if (form.AllKeys.Contains("Bookmarks"))
            {
                MemberManager.ChangeOwnership("FORUM_BOOKMARKS", "B_MEMBERID", pId, sId);
            }
            if (form.AllKeys.Contains("Ratings"))
            {
                MemberManager.ChangeOwnership("TOPIC_RATINGS", "RATINGS_BYMEMBER_ID", pId, sId);
            }

            //forum_memberfields ??
            if (form.AllKeys.Contains("Remove"))
            {
                DeleteMember(Convert.ToInt32(SecondaryId));
            }
            

            MergeMemberViewModel vm = new MergeMemberViewModel();
            vm.Primary = Member.GetById(Convert.ToInt32(PrimaryId));
            if (!form.AllKeys.Contains("Remove"))
            {
                vm.ToMerge = Member.GetById(Convert.ToInt32(SecondaryId));
            }
            
            return View("MergeMembers",vm);

        }

        delegate string ProcessTask(string id);
        private static DbsFileProcessor _dbsProcessor;
        private void EndProcess(IAsyncResult result)
        {
            ProcessTask processTask = (ProcessTask)result.AsyncState;
            string id = processTask.EndInvoke(result);
            _dbsProcessor.Remove(id);

        }
        private void DeleteMember(int id)
        {
            string username;
            UserProfile profile;

            using (SnitzMemberContext udb = new SnitzMemberContext())
            {
                profile = udb.GetUser(id);
                username = profile.UserName;
                if (Roles.GetRolesForUser(username).Any())
                {
                    Roles.RemoveUserFromRoles(username, Roles.GetRolesForUser(username));
                }
            }

            //Remove the member from the moderator table
            ForumModerators.RemoveMember(id);
            //Remove any subscriptions this member has in the Subscriptions table
            Subscriptions.RemoveMember(id);
            //Remove the member from the Allowed Members and Roles table
            AllowedMembers.Remove(id);
            try
            {
                if (username != "zapped" && username != "n/a")
                {
                    WebSecurity.DeleteUser(username);
                }
                else
                {
                    WebSecurity.DeleteUser(profile);
                }
                ForumTotals.DeleteUser();
            }
            catch(Exception e)
            {
                throw new Exception("Problem deleting user:" + username, e.InnerException);
            }
        }
        private void ImportSpamData(string csvPath)
        {
            SnitzDataContext.ImportSpamDomainsCSV(csvPath);
        }

        public ActionResult SavePendingConfig(FormCollection form)
        {
            var updates = new List<string>();
            foreach (var key in form.AllKeys)
            {
                if (key.StartsWith("str") || key.StartsWith("int"))
                {
                    string[] amounts = Request.Form.GetValues(key);
                    if (amounts != null)
                        ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting(key.ToUpper(), amounts[0]);
                }
                updates.Add(key.ToUpper());
                
            }
            ClassicConfig.Update(updates.ToArray());

            PendingMemberViewModel pvm = new PendingMemberViewModel
            {
                Pending = MemberManager.PendingMembers().ToList(),
                EnabledProfileFields = UserProfileExtensions.EnabledProfileFields()
            };
            return View("PendingMembers",pvm);
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using LangResources.Utility;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzCore.Utility;
using SnitzDataModel.Extensions;
using SnitzMembership;
using WWW.Filters;
using WWW.ViewModels;
using FormCollection = System.Web.Mvc.FormCollection;
using Member = SnitzDataModel.Models.Member;
using PrivateMessage = SnitzDataModel.Models.PrivateMessage;


namespace WWW.Controllers
{
    [Authorize]
    public class PrivateMessageController : CommonController
    {
        public ActionResult Index()
        {
            if (ClassicConfig.GetValue("STRPMSTATUS") != "1")
            {
                ViewBag.Error = "Private messaging is not enabled";
                return View("Error");
            }
            var owner = Member.GetById(WebSecurity.CurrentUserId);
            var viewModel = new PrivateMessageViewModel
            {
                Owner = owner,
                InBox = PrivateMessage.InBox(WebSecurity.CurrentUserId),
                //OutBox = PrivateMessage.OutBox(WebSecurity.CurrentUserId)
            };
            var mailboxsize = PrivateMessage.MailboxSize(WebSecurity.CurrentUserId);
            SessionData.Clear("NewPM");
            viewModel.IsFull = mailboxsize >= ClassicConfig.GetIntValue("STRPMLIMIT");
            //viewModel.MailBoxMessage = 
            TempData["MailBoxMessage"] = GetMailboxLimitString(mailboxsize);


            viewModel.SaveSentItems = viewModel.Owner.PrivateMessageSentItems == 1;
            if (TempData["Inbox"] == null)
            {
                TempData["Inbox"] = true;
                TempData["PageTitle"] = ResourceManager.GetLocalisedString("Inbox", "PrivateMessage");
            }
            TempData["PMCount"] = PrivateMessage.Check(WebSecurity.CurrentUserId);
            return View(viewModel);
        }

        [OutputCache(Location = OutputCacheLocation.None, NoStore = true, Duration = 1)]
        public ActionResult MailboxLimitString()
        {
            var mailboxsize = PrivateMessage.MailboxSize(WebSecurity.CurrentUserId);

            return Content(GetMailboxLimitString(mailboxsize), "text/html");
        }
        public ContentResult InboxSizeString()
        {
            var mailboxsize = PrivateMessage.InboxSize(WebSecurity.CurrentUserId);

            return Content("(" + mailboxsize + ")", "text/html");
        }
        public ContentResult OutboxSizeString()
        {
            var mailboxsize = PrivateMessage.OutboxSize(WebSecurity.CurrentUserId);

            return Content("(" + mailboxsize + ")", "text/html");
        }
        public ActionResult GetMessage(int id, string inbox)
        {
            if (ClassicConfig.GetValue("STRPMSTATUS") != "1")
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("Disabled", "PrivateMessage");
                return View("Error");
            }


            PrivateMessage msg = PrivateMessage.Fetch(id);
            if (msg.Read <= 0 && (inbox.ToLower()=="outbox" || inbox.ToLower() == "false"))
            {
                var member = Member.GetById(WebSecurity.CurrentUserId);
                PrivateMessagePost vm = new PrivateMessagePost
                {
                    ToUser = msg.ToUsername,
                    Sender = member,
                    SaveToSent = member.PrivateMessageSentItems == 1,
                    Subject = msg.Subject,
                    Message = msg.Message,
                    //SaveDraft = true,
                    Read = -1,
                    IsFull = PrivateMessage.MailboxSize(member.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT"),
                    //IsMailBoxFull(member)
                };
                ViewBag.ReadOnly = "";
                return PartialView("_MessageForm", vm);
            }
            if (msg.ToMemberId == WebSecurity.CurrentUserId)
            {
                msg.Read = 1;
                msg.Save();
                SessionData.Clear("NewPM");
            }
            TempData["Inbox"] = inbox.ToLower();
            ViewBag.Subject = msg.Subject;
            ViewBag.Message = msg.Message;
            ViewBag.MessageId = msg.Id;
            ViewBag.Sender = msg.FromUsername;
            ViewBag.Date = msg.SentDate.Value.ToFormattedString();
            return PartialView("_PrivateMessageView");

        }

        public ActionResult Print(int id)
        {
            if (ClassicConfig.GetValue("STRPMSTATUS") != "1")
            {
                ViewBag.Error = ResourceManager.GetLocalisedString("Disabled", "PrivateMessage");
                return View("Error");
            }
            PrivateMessage msg = PrivateMessage.Fetch(id);

            return View(msg);
        }
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult GetFolder(int id, string message = "")
        {
            try
            {
                if (ClassicConfig.GetValue("STRPMSTATUS") != "1")
                {
                    ViewBag.Error = ResourceManager.GetLocalisedString("Disabled", "PrivateMessage");
                    return View("Error");
                }
                TempData["Inbox"] = id == 1;
                TempData["PageTitle"] = id != 1 ? ResourceManager.GetLocalisedString("OutBox", "PrivateMessage") : ResourceManager.GetLocalisedString("Inbox", "PrivateMessage");
                SessionData.Clear("NewPM");
                ViewBag.Message = message;
                var mailboxsize = PrivateMessage.MailboxSize(WebSecurity.CurrentUserId);

                TempData["MailBoxMessage"] = GetMailboxLimitString(mailboxsize); //ViewBag.MailBoxMessage;
                return PartialView("_PrivateMessages",
                    id == 1
                        ? PrivateMessage.InBox(WebSecurity.CurrentUserId)
                        : PrivateMessage.OutBox(WebSecurity.CurrentUserId));

            }
            catch (Exception)
            {

                return PartialView("_PrivateMessages", PrivateMessage.InBox(WebSecurity.CurrentUserId));

            }
        }

        public PartialViewResult NewMessage(string id)
        {
            var member = Member.GetById(WebSecurity.CurrentUserId);
            PrivateMessagePost vm = new PrivateMessagePost
            {
                Sender = member,
                ToUser = id,
                SaveToSent = member.PrivateMessageSentItems == 1,
                Read = 0,
                IsFull = PrivateMessage.MailboxSize(member.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT")
                //IsMailBoxFull(member)
            };
            ViewBag.ReadOnly = "";
            return PartialView("_MessageForm", vm);

        }

        public PartialViewResult ReplyMessage(string id)
        {
            StringBuilder header = new StringBuilder().AppendLine().AppendLine(ResourceManager.GetLocalisedString("OriginalMessage","PrivateMessage"));
            PrivateMessage msg = PrivateMessage.Fetch(Convert.ToInt32(id));
            header.AppendFormat(ResourceManager.GetLocalisedString("Sent", "PrivateMessage") +": {0}", msg.SentDate.Value.ToFormattedString()).AppendLine();
            header.AppendFormat(ResourceManager.GetLocalisedString("Message", "General")).AppendLine();
            var sender = Member.GetById(WebSecurity.CurrentUserId);
            PrivateMessagePost vm = new PrivateMessagePost
            {
                Sender = sender,
                ToUser = msg.FromUsername,
                Subject = ResourceManager.GetLocalisedString("PMRe", "PrivateMessage") + " " + msg.Subject,
                Message = header + msg.Message,
                IsFull = PrivateMessage.MailboxSize(sender.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT")
                //IsMailBoxFull(sender)
            };
            ViewBag.ReadOnly = "readonly";
            vm.SaveToSent = vm.Sender.PrivateMessageSentItems == 1;
            return PartialView("_MessageForm", vm);

        }

        public PartialViewResult ForwardMessage(string id)
        {
            StringBuilder header = new StringBuilder().AppendLine().AppendLine(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"));

            PrivateMessage msg = PrivateMessage.Fetch(Convert.ToInt32(id));
            header.AppendFormat("{0}: {1}", ResourceManager.GetLocalisedString("From", "PrivateMessage"), msg.FromUsername).AppendLine();
            header.AppendFormat("{0}: {1}", ResourceManager.GetLocalisedString("To", "PrivateMessage"), msg.ToUsername).AppendLine();
            header.AppendFormat("{0}: {1}", ResourceManager.GetLocalisedString("Sent", "PrivateMessage"), msg.SentDate.Value.ToFormattedString()).AppendLine();
            header.AppendFormat(ResourceManager.GetLocalisedString("Message", "General")).AppendLine();
            var sender = Member.GetById(WebSecurity.CurrentUserId);
            PrivateMessagePost vm = new PrivateMessagePost
            {
                Sender = sender,
                Subject = ResourceManager.GetLocalisedString("PMForward", "PrivateMessage") + " " + msg.Subject,
                Message = header + msg.Message,
                IsFull = PrivateMessage.MailboxSize(sender.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT")
                //IsMailBoxFull(sender)
            };
            ViewBag.ReadOnly = "";
            vm.SaveToSent = vm.Sender.PrivateMessageSentItems == 1;
            return PartialView("_MessageForm", vm);

        }

        /// <summary>
        /// Send a Private Message from Topic/Reply
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Popup Message Form</returns>
        public PartialViewResult SendMemberPm(string id)
        {
            var sender = Member.GetById(WebSecurity.CurrentUserId);
            PrivateMessagePost vm = new PrivateMessagePost
            {
                Sender = sender,
                ToUser = id,
                IsFull = PrivateMessage.MailboxSize(sender.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT")
                //IsMailBoxFull(sender)
            };
            vm.SaveToSent = vm.Sender.PrivateMessageSentItems == 1;
            return PartialView("popMemberPM", vm);
        }

        /// <summary>
        /// Private Message Post
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [ValidateAntiForgeryToken]
        [HttpPost]
        [PreventSpam(DelayRequest = 60, ErrorMessage = "Duplicate PM.")]
        public ActionResult SendMemberPmPost(PrivateMessagePost vm)
        {
            if (!vm.AllMembers)
            {
                if (vm.ToUser.Contains(";"))
                {
                    Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    ModelState.AddModelError("Recblock",
                     ResourceManager.GetLocalisedString("TooMany", "PrivateMessage"));
                    return Content(ResourceManager.GetLocalisedString("TooMany", "PrivateMessage"));
                }
                var recipient = Member.GetByName(vm.ToUser);
                if (PrivateMessage.BlockedBy(WebSecurity.CurrentUserId).Contains(recipient.Id))
                {
                    Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    ModelState.AddModelError("Recblock",ResourceManager.GetLocalisedString("BlockedUser", "PrivateMessage") + " " + recipient.Username);
                    return Content(ResourceManager.GetLocalisedString("BlockedUser", "PrivateMessage") + " " + recipient.Username);
                }
                if (PrivateMessage.MailboxSize(recipient.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT"))
                {
                    Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    ModelState.AddModelError("Recblock", ResourceManager.GetLocalisedString("RecipientBoxFull", "PrivateMessage"));
                    return Content(ResourceManager.GetLocalisedString("RecipientBoxFull", "PrivateMessage"));
                }
                if (ModelState.IsValid)
                {

                    var sender = Member.GetById(WebSecurity.CurrentUserId);
                    if (recipient.PrivateMessageReceive == 1 || User.IsAdministrator())
                    {
                        PrivateMessage msg = new PrivateMessage
                        {
                            ToUsername = vm.ToUser,
                            ToMemberId = recipient.Id,
                            FromMemberId = WebSecurity.CurrentUserId,
                            Subject = vm.Subject,
                            Message = vm.Message,
                            SentDate = DateTime.UtcNow,
                            Read = 0,
                            ShowOutBox = (short) (vm.SaveToSent ? 1 : 0)
                        };
                        if (vm.ShowSignature)
                        {
                            //Make sure the sig is above any quoted messages
                            if (msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal) > 0)
                            {
                                msg.Message = msg.Message.Insert(
                                    msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal),
                                    "[hr]" + sender.Signature + "[br][br]");
                            }
                            else
                            {
                                msg.Message += "[hr]" + sender.Signature;
                            }
                        }
                        msg.Save();
                        ViewBag.Message = ResourceManager.GetLocalisedString("SendPMSuccess", "PrivateMessage");
                        PrivateMessageViewModel pvm = new PrivateMessageViewModel
                        {
                            Owner = Member.GetById(WebSecurity.CurrentUserId)
                        };
                        pvm.SaveSentItems = pvm.Owner.PrivateMessageSentItems == 1;
                        pvm.InBox = PrivateMessage.InBox(WebSecurity.CurrentUserId);
                        if (recipient.PrivateMessageNotify == 1 && ClassicConfig.AllowEmail)
                        {
                            EmailController.SendPrivateMessageNotification(ControllerContext, recipient, pvm.Owner);
                        }
                        return Json(true, JsonRequestBehavior.AllowGet);

                    }
                    ModelState.AddModelError("Recblock", vm.ToUser + " " + ResourceManager.GetLocalisedString("NoPrivateMessages", "PrivateMessage"));
                }
            }
            else
            {

                var sender = Member.GetById(WebSecurity.CurrentUserId);
                PrivateMessage msg = new PrivateMessage
                {
                    FromMemberId = WebSecurity.CurrentUserId,
                    Subject = vm.Subject,
                    Message = vm.Message,
                    SentDate = DateTime.UtcNow
                };
                if (vm.ShowSignature)
                {
                    //Make sure the sig is above any quoted messages
                    if (msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal) > 0)
                    {
                        msg.Message = msg.Message.Insert(
                            msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal),
                            "[hr]" + sender.Signature + "[br][br]");
                    }
                    else
                    {
                        msg.Message += "[hr]" + sender.Signature;
                    }
                }

                msg.PmAllMembers();
                //SnitzDataContext.PmAllMembers(msg);
            }


            return PartialView("popMemberPM", vm);
        }

        [HttpPost]
        [PreventSpam(DelayRequest = 20, ErrorMessage = "Duplicate post detected.")]
        public ActionResult SendMessage(PrivateMessagePost vm)
        {
            bool toself = false;
            var sender = Member.GetById(WebSecurity.CurrentUserId);
            int sentMessages = 0;

            try
            {
            if (ModelState.IsValid)
            {
                if (vm.SaveDraft)
                {
                    PrivateMessage msg = new PrivateMessage
                    {
                        ToUsername = vm.ToUser,
                        FromMemberId = sender.Id,
                        Subject = vm.Subject,
                        Message = vm.Message,
                        Read = -1,
                        DeleteFrom = 0,
                        DeleteTo = 0,
                        ShowOutBox = 1
                    };
                    msg.Save();
                    //ViewBag.Message = "Private message saved as draft";
                       var showMessageString = new  
                       {  
                           param1 = 200,  
                           param2 = "Private message saved as draft",
                           param3 = Url.Action("Index")
                       };  
                        return Json(showMessageString, JsonRequestBehavior.AllowGet); 

                    //return RedirectToAction("GetFolder", new { id = 1, Message = "Private message saved as draft" });
                }
                if (vm.AllMembers)
                {
                    if (vm.Read < 0 && vm.Id > 0)
                    {
                        PrivateMessage.Delete(vm.Id);
                    }
                    PrivateMessage msg = new PrivateMessage
                    {
                        FromMemberId = sender.Id,
                        Subject = vm.Subject,
                        Message = vm.Message,
                        SentDate = DateTime.UtcNow,
                        ShowOutBox = 0
                    };
                    if (vm.ShowSignature)
                    {
                        //Make sure the sig is above any quoted messages
                        if (msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal) > 0)
                        {
                            msg.Message = msg.Message.Insert(
                                msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal),
                                "[hr]" + sender.Signature + "[br][br]");
                        }
                        else
                        {
                            msg.Message += "[hr]" + sender.Signature;
                        }
                    }
                    sentMessages = msg.PmAllMembers();

                }
                else
                {
                    List<string> recipients = new List<string>();
                    
                    if (vm.ToUser.Contains(";"))
                    {
                        recipients = vm.ToUser.Split(';').ToList();
                    }
                    else
                    {
                        recipients.Add(vm.ToUser);
                
                    }
                    //logger.Error("PM: Enter Sending to - " + recipients.Count + "recipients");
                    if (ClassicConfig.GetIntValue("INTMAXPMRECIPIENTS") > 0  && ClassicConfig.GetIntValue("INTMAXPMRECIPIENTS") < recipients.Count)
                    {
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        ModelState.AddModelError("Recblock",ResourceManager.GetLocalisedString("TooMany", "PrivateMessage") + " " + ClassicConfig.GetIntValue("INTMAXPMRECIPIENTS"));
                        return PartialView("_MessageForm", vm);
                    }
                    if (vm.Read < 0 && vm.Id > 0)
                    {
                        PrivateMessage.Delete(vm.Id);
                    }
                    foreach (string username in recipients)
                    {
                        if (username.ToLowerInvariant() == WebSecurity.CurrentUserName.ToLowerInvariant())
                        {
                            toself = true;
                        }
                        var recipient = Member.GetByName(username);
                        if (recipient == null)
                        {
                                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                ModelState.AddModelError("Recnouser", ResourceManager.GetLocalisedString("AdminController_Username_does_not_exist", "ErrorMessage"));
                                return PartialView("_MessageForm", vm);
                                
                        }
                        else
                        {

                            if (PrivateMessage.BlockedBy(sender.Id).Contains(recipient.Id))
                            {

                                ModelState.AddModelError("Recblock", ResourceManager.GetLocalisedString("BlockedUser", "PrivateMessage") + " " 
                                     + recipient.Username);
                                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    return PartialView("_MessageForm", vm);
                            }
                            else if (PrivateMessage.MailboxSize(recipient.Id) >= ClassicConfig.GetIntValue("STRPMLIMIT")) //IsMailBoxFull(recipient))
                            {
                                ModelState.AddModelError("Recblock", ResourceManager.GetLocalisedString("RecipientBoxFull", "PrivateMessage"));
                                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    
                                    return PartialView("_MessageForm", vm);
                            }
                            else if (recipient.PrivateMessageReceive == 1 || User.IsAdministrator())
                            {
                                sentMessages += 1;

                                PrivateMessage msg = new PrivateMessage
                                                     {
                                                         ToUsername = vm.ToUser,
                                                         ToMemberId = recipient.Id,
                                                         FromMemberId = sender.Id,
                                                         Subject = vm.Subject,
                                                         Message = vm.Message,
                                                         SentDate = DateTime.UtcNow,
                                                         Read = 0,
                                                         DeleteFrom = 0,
                                                         DeleteTo = 0,
                                                         ShowOutBox = (short) (vm.SaveToSent ? 1 : 0)
                                                     };
                                if (vm.ShowSignature)
                                {
                                    //Make sure the sig is above any quoted messages
                                    if (msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal) > 0)
                                    {
                                        msg.Message = msg.Message.Insert(
                                            msg.Message.IndexOf(ResourceManager.GetLocalisedString("OriginalMessage", "PrivateMessage"), StringComparison.Ordinal),
                                            "[hr]" + sender.Signature + "[br][br]");
                                    }
                                    else
                                    {
                                        msg.Message += "[hr]" + sender.Signature;
                                    }
                                }
                                msg.Save();

                                if (recipient.PrivateMessageNotify == 1 && ClassicConfig.AllowEmail)
                                {
                                    EmailController.SendPrivateMessageNotification(ControllerContext, recipient, sender);
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("Recblock", username + " " + ResourceManager.GetLocalisedString("NoPrivateMessages", "PrivateMessage"));
                                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    return PartialView("_MessageForm", vm);
                                }
                        
                        }
                    }
                }

                if (ModelState.IsValid && !vm.SaveDraft)
                {

                    if (sentMessages > 0)
                    {
                        var showMessageString = new
                        {
                            param1 = 200,
                            param2 = ResourceManager.GetLocalisedString("SendPMSuccess", "PrivateMessage") + (toself ? "1 " + ResourceManager.GetLocalisedString("PMSelf", "PrivateMessage") : ""),
                            param3 = Url.Action("Index")
                        };
                        return Json(showMessageString, JsonRequestBehavior.AllowGet);
                        
                    }
                    var showMessageString2 = new
                    {
                        param1 = 200,
                        param2 = toself ? ResourceManager.GetLocalisedString("PMSelf", "PrivateMessage") : ResourceManager.GetLocalisedString("SendPMSuccess", "PrivateMessage"),
                        param3 = Url.Action("Index")
                    };
                    return Json(showMessageString2, JsonRequestBehavior.AllowGet);
 
                }
            }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Recblock", ex.Message);
            }

            ViewBag.ReadOnly = "";

            return PartialView("_MessageForm", vm);

        }

        [HttpPost]
        public ActionResult DeleteMessage(FormCollection form)
        {
            bool inbox = true;
            if (form.AllKeys.Contains("inbox"))
            {
                inbox = form["inbox"] == "True";
            }
            List<int> selectedMessages = form["msg.Selected"].Split(',').Select(Int32.Parse).ToList();
            //delete messages or remove outbox flag
            PrivateMessage.DeleteMessages(selectedMessages,inbox);
            TempData["PMCount"] = PrivateMessage.Check(WebSecurity.CurrentUserId);
            SessionData.Clear("NewPM");
            TempData["Inbox"] = inbox;
            TempData["PageTitle"] = ResourceManager.GetLocalisedString("Inbox", "PrivateMessage");
            if (!inbox)
            {
                TempData["PageTitle"] = ResourceManager.GetLocalisedString("OutBox", "PrivateMessage");
            }

            return RedirectToAction("GetFolder", new { id = (inbox?1:0) });
            
        }

        /// <summary>
        /// Get Users private message settings
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [OutputCacheAttribute(VaryByParam = "*", Duration = 1)]
        public PartialViewResult Settings(int id)
        {
            var user = Member.GetById(WebSecurity.CurrentUserId);
            PrivateMessageSettings vm = new PrivateMessageSettings
            {
                Receive = user.PrivateMessageReceive == 1,
                Notify = user.PrivateMessageNotify == 1,
                SentItems = user.PrivateMessageSentItems == 1,
                Blocklist = PrivateMessage.Blocklist(WebSecurity.CurrentUserId)
            };
            return PartialView("_Settings",vm);

        }

        public PartialViewResult Blocklist(int id)
        {
            return PartialView("_Blocklist", PrivateMessage.Blocklist(WebSecurity.CurrentUserId));
        }
        public PartialViewResult SearchMessages(int id)
        {
            return PartialView("_Search", new PMSearchViewModel());
        }
        [HttpPost]
        public PartialViewResult Search(PMSearchViewModel vm)
        {
            var memberId = -1;
            if (!String.IsNullOrWhiteSpace(vm.MemberName))
                memberId = MemberManager.GetUser(vm.MemberName).UserId;

            var result = PrivateMessage.Find(WebSecurity.CurrentUserId,vm.Term,vm.SearchIn, memberId, vm.PhraseType,vm.SearchByDays);
            return PartialView("_SearchResult", result);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult SettingsSave(PrivateMessageSettings vm)
        {
            var user = Member.GetById(WebSecurity.CurrentUserId);
            if (ModelState.IsValid)
            {
                user.PrivateMessageReceive = vm.Receive ? 1 : 0;
                user.PrivateMessageNotify = vm.Notify ? 1 : 0;
                user.PrivateMessageSentItems = (short) (vm.SentItems ? 1 : 0);
                //user.Save();
                user.Update(new List<String> { "M_PMRECEIVE", "M_PMEMAIL", "M_PMSAVESENT" });
            }

            PrivateMessageViewModel pvm = new PrivateMessageViewModel
            {

                Owner = Member.GetById(WebSecurity.CurrentUserId)
            };
            pvm.SaveSentItems = pvm.Owner.PrivateMessageSentItems == 1;
            pvm.InBox = PrivateMessage.InBox(WebSecurity.CurrentUserId);

            TempData["Inbox"] = true;
            return View("Index",pvm);

        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "Block")]
        public PartialViewResult Block(string blockUser)
        {
            var blockmember = Member.GetByName(blockUser);

            PrivateMessage.BlockUser(WebSecurity.CurrentUserId, blockmember);

            return PartialView("_Blocklist", PrivateMessage.Blocklist(WebSecurity.CurrentUserId));

        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "Unblock")]
        public PartialViewResult Unblock(string blockUser)
        {
            var blockmember = Member.GetByName(blockUser);

            PrivateMessage.UnblockUser(WebSecurity.CurrentUserId, blockmember.Id);

            return PartialView("_Blocklist", PrivateMessage.Blocklist(WebSecurity.CurrentUserId));

        }

        private string GetMailboxLimitString(int mailboxsize)
        {
            var msg = String.Format(ResourceManager.GetLocalisedString("MailboxSize","PrivateMessage"), mailboxsize, ClassicConfig.GetIntValue("STRPMLIMIT"));
            if ((mailboxsize) >= (ClassicConfig.GetIntValue("STRPMLIMIT") * 0.8))
            {
                msg += "<span class='alert-warning'>" + ResourceManager.GetLocalisedString("MailboxReminder", "PrivateMessage") + "</span>";
            }
            if ((mailboxsize) >= ClassicConfig.GetIntValue("STRPMLIMIT"))
            {
                msg = "<span class='alert-danger'>" + ResourceManager.GetLocalisedString("MailboxFull", "PrivateMessage") + "</span>";
            }
            return msg;
        }

        [Authorize]
        public JsonResult BlockUsername(string term)
        {
            //get list of admins/moderator to exclude from block
            var admins = Roles.GetUsersInRole("Administrator");

            var blockeduser = PrivateMessage.Blocklist(WebSecurity.CurrentUserId).Select(b => b.BlockedMemberName);
            IEnumerable<string> result = MemberManager.CachedUsernames().Where(r => r.ToLower().Contains(term.ToLower()) && !admins.Contains(r) && !blockeduser.Contains(r));
            return Json(result, JsonRequestBehavior.AllowGet);

        }


        //[Authorize]
        //public JsonResult SearchUsername(string term)
        //{
        //    using (var db = new SnitzMemberContext())
        //    {
        //        IEnumerable<string> result = ((from r in db.UserProfiles
        //                                       where r.Status == 1 && r.UserName.ToLower().Contains(term.ToLower())
        //                                       select r.UserName).Distinct()).ToArray();
        //        return Json(result, JsonRequestBehavior.AllowGet);
        //    }
        //}
    }
}

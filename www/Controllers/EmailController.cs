using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Web.Hosting;
using System.Web.Mvc;
using BbCodeFormatter;
using PetaPoco;
using Postal;
using SnitzConfig;
using SnitzDataModel.Database;
using SnitzDataModel.Extensions;
using SnitzDataModel.Models;
using SnitzMembership;
using SnitzMembership.Repositories;
using WWW.Models;
using Forum = SnitzDataModel.Models.Forum;
using Member = SnitzDataModel.Models.Member;
using Topic = SnitzDataModel.Models.Topic;

namespace WWW.Controllers
{
    public class EmailController : Controller
    {

        /// <summary>
        /// Sends a security token to allow password reset
        /// </summary>
        /// <param name="username"></param>
        /// <param name="token"></param>
        public static void SendResetPasswordConfirmation(ControllerContext context, string username, string token)
        {
            if (!ClassicConfig.AllowEmail)
                return;

            Member user = Member.GetByName(username);

            dynamic email = new Email("ResetPassword");
            email.To = user.Email;
            email.UserName = username;
            email.ConfirmationToken = token;
            email.Send(context);
        }

        /// <summary>
        /// Sends a change email confirmation
        /// </summary>
        /// <param name="to"></param>
        /// <param name="username"></param>
        /// <param name="confirmationToken"></param>
        public static void SendChangeEmailConfirmation(ControllerContext context, string to, string username, string confirmationToken)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            Member user = Member.GetByName(username);
            if (user != null)
            {
                user.NewEmail = to;
                user.Update(new[] { "M_NEWEMAIL" });
            }

            dynamic email = new Email("ChangeEmail");
            email.To = to;
            email.UserName = username;
            email.ConfirmationToken = confirmationToken;
            email.Send(context);
        }

        /// <summary>
        /// Re-sends the email confirmation for registration
        /// </summary>
        /// <param name="username"></param>
        public static void ResendConfirmationEmail(ControllerContext context, string username)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            string email;
            string token;
            int id = WebSecurity.GetUserId(username);

            using (var db = new SnitzMemberContext())
            {
                var user = db.GetEmailConfirmToken(username);
                email = user.Email;
                token = user.Token;
            }
            SendEmailConfirmation(context, email, username, token, id);
        }
        /// <summary>
        /// Sends an email confirmation for registration
        /// </summary>
        /// <param name="to"></param>
        /// <param name="username"></param>
        /// <param name="confirmationToken"></param>
        /// <param name="id"></param>
        public static void SendApprovedEmailConfirmation(ControllerContext context, string to, string username, string confirmationToken, int id)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("ApprovedRegEmail");
            email.To = to;
            email.UserName = username;
            email.Sender = ClassicConfig.ForumEmail;
            email.ConfirmationToken = confirmationToken;
            email.UserId = id;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            email.Send(context);
        }
        public static void SendEmailConfirmation(ControllerContext context, string to, string username, string confirmationToken, int id)
        {

            dynamic email = new Email("RegEmail");
            email.To = to;
            email.UserName = username;
            email.ConfirmationToken = confirmationToken;
            email.UserId = id;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            email.Send();
        }
        public static void SendApprovalConfirmation(ControllerContext context, string to, string username, string confirmationToken, int id)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("ApprovedEmail");
            email.To = to;
            email.UserName = username;
            email.ConfirmationToken = confirmationToken;
            email.UserId = id;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            email.Send(context);
        }

        public static void SendPrivateMessageNotification(ControllerContext context, Member recipient, Member sender)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("PMEmail");
            email.To = recipient.Email;
            email.Username = recipient.Username;
            email.Sender = ClassicConfig.ForumEmail;
            email.FromUser = sender.Username;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            email.Send(context);
        }
        public static void SendTaggedUserNotification(ControllerContext context, Member recipient, Member sender, Reply reply, Topic topic = null)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("TaggedEmail");
            email.To = recipient.Email;
            email.Subject = LangResources.Utility.ResourceManager.GetLocalisedString("MentionedInPostSubject", "PrivateMessage");
            email.UserName = recipient.Username;
            email.Sender = ClassicConfig.ForumEmail;
            email.FromUser = sender.Username;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            if (reply != null)
            {
                email.Topiclink = String.Format("{0}Topic/Posts/{1}?pagenum=-1#{2}", Config.ForumUrl, reply.TopicId, reply.Id);
            }
            else if (topic != null)
            {
                email.Topiclink = String.Format("{0}Topic/Posts/{1}?pagenum=1", Config.ForumUrl, topic.Id);
            }
            
            email.Send(context);
        }
        public static void SendImageCommentNotification(ControllerContext context, Member recipient, Member sender, string imageid)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("ImageCommentEmail");
            email.To = recipient.Email;
            email.Subject = LangResources.Utility.ResourceManager.GetLocalisedString("MentionedInPostSubject", "PrivateMessage");
            email.UserName = recipient.Username;
            email.Sender = ClassicConfig.ForumEmail;
            email.FromUser = sender.Username;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            email.Imagelink = String.Format("{0}ImageComment/Comments{1}", Config.ForumUrl, imageid);
            email.Send(context);
        }
        public static void SendToFreind(ControllerContext context, EmailModel model)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("TopicSend");
            email.To = model.ToEmail;
            email.UserName = model.ToName;
            email.Sender = ClassicConfig.ForumEmail;
            email.FromEmail = model.FromEmail;
            email.FromUser = model.FromName;
            email.subject = model.Subject;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false); 
            email.message = BbCodeProcessor.Format(model.Message);
            email.Send(context);
        }

        public static void ModerationEmail(ControllerContext context, Member author, string subject, string message, Forum forum, Topic topic)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            dynamic email = new Email("TopicModerateEmail");
            email.To = author.Email;
            email.UserName = author.Username;
            email.Sender = ClassicConfig.ForumEmail;
            email.Subject = subject;
            email.Message = message;
            email.Forum = forum.Subject;
            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle,false,false);
            email.TopicSubject = topic.Subject;
            email.Send(context);
        }

        [Authorize]
        public static void TopicMoveEmail(ControllerContext context, Topic model)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            
            var author = Member.GetById(model.AuthorId);
            dynamic email = new Email("TopicMoveEmail");
            email.To = author.Email;
            email.UserName = author.Username;
            email.Sender = ClassicConfig.ForumEmail;

            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false);
            email.TopicSubject = model.Subject;

            if (!author.AllowedForumIDs().Contains(model.ForumId))
            {
                email.Message =
                    "Has been removed from public display, If you have any questions regarding this, please contact the Administrator of the forum";
            }
            else
            {
                email.Message = "Has been moved to a new forum, You can view it at " + Environment.NewLine +
                                Config.ForumUrl + "Topic/Posts/" + model.Id + "?pagenum=1";
            }
            email.Send(context);
        }
        [Authorize]
        public static void TopicMergeEmail(ControllerContext context, Topic original, Topic mainTopic)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            var author = Member.GetById(original.AuthorId);
            dynamic email = new Email("TopicMoveEmail");
            email.To = author.Email;
            email.UserName = author.Username;
            email.Sender = ClassicConfig.ForumEmail;

            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false); 
            email.TopicSubject = original.Subject;

            email.Message = "Has been merged with another topic, You can view it at " + Environment.NewLine +
                            Config.ForumUrl + "Topic/Posts/" + mainTopic.Id + "?pagenum=1";

            email.Send(context);
        }
        [Authorize]
        public static void TopicSplitEmail(ControllerContext context, Topic newTopic)
        {
            if (!ClassicConfig.AllowEmail)
                return;
            var author = Member.GetById(newTopic.AuthorId);
            dynamic email = new Email("TopicMoveEmail");
            email.To = author.Email;
            email.UserName = author.Username;
            email.Sender = ClassicConfig.ForumEmail;

            email.ForumTitle = BbCodeProcessor.Format(Config.ForumTitle, false, false); 
            email.TopicSubject = newTopic.Subject;

            email.Message = "Your reply has been used to create a new topic, You can view it at " + Environment.NewLine +
                            Config.ForumUrl + "Topic/Posts/" + newTopic.Id + "?pagenum=1";

            email.Send(context);
        }

        public static void SendMemberEmail(ControllerContext context,EmailModel model)
        {
            //If there is a file then save it
            dynamic email = new Email("DefEmail");
            if (model.Attachment != null)
            {
                string uploadPath = HostingEnvironment.MapPath("~/Content/EmailAttachments/");
                if (!Directory.Exists(uploadPath))
                {
                    if (uploadPath != null) Directory.CreateDirectory(uploadPath);
                }
                if (model.Attachment.ContentLength > Convert.ToInt32(ClassicConfig.GetValue("INTMAXFILESIZE")) * 1024 * 1024)
                {
                    throw new Exception("File too large");

                }
                var fileName = Path.GetFileName(model.Attachment.FileName);

                if (fileName != null)
                {
                    if (uploadPath != null)
                    {
                        model.Attachment.SaveAs(Path.Combine(uploadPath, fileName)); //File will be saved in banner folder
                        email.Attach(new Attachment(Path.Combine(uploadPath, fileName)));
                    }
                }
            }
            email.To = model.ToEmail;
            email.UserName = model.ToName;
            email.Sender = ClassicConfig.ForumEmail;
            email.FromUser = model.FromName;
            email.From = model.FromEmail;
            email.Subject = model.Subject;
            email.Message = model.Message;

            ((Email)email).SendCompleted += compEvent;
            email.Send(context);

        }
        private static void compEvent(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                throw new Exception("Operation Cancelled");
            }
            if (e.Error != null)
            {
                throw new ApplicationException("Send Mail",e.Error);
            }

        }
        public static void EmailMembers(ControllerContext context, bool administrator, string template, FormCollection form, string sortCol, string sortOrder)
        {

            var engines = new ViewEngineCollection();

            List<Member> recipients = MemberManager.GetAllMembers(administrator,form,sortCol,sortOrder);

            foreach (var member in recipients)
            {
                dynamic email = new Email(template);
                email.To = member.Email;
                email.Username = member.Username;
                email.Sender = ClassicConfig.ForumEmail;
                email.FromUser = "Administrator";
                email.From = ClassicConfig.ForumEmail;
                email.subject = form["EmailSubject"];
                email.message = form["EmailMessage"];

                email.Send(context);
            }
        }
    }
}

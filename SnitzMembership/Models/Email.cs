using System.ComponentModel;
using AegisImplicitMail;
using RazorEngine.Configuration;

namespace Postal
{
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using RazorEngine.Templating;
using SnitzConfig;
using Westwind.Web.Mvc;

    /// <summary>
    /// Email dynamic object (replaces Postal.Mvc which isn't working in Mono
    /// </summary>
    public class Email : DynamicObject
    {
        //private readonly string viewname;
        private MimeMailer mailClient;
        private bool IsBodyHtml;
        private string viewPath = "~/Views/Emails/";
        public string Message { get; set; }
        public List<Attachment> Attachments { get; set; }
        public string ViewName { get; }
        public ViewDataDictionary ViewData { get; set; }
        public string From { get; private set; }
        public string ContentType { get; private set; }
        public event SendCompletedEventHandler SendCompleted;

        public Email(string viewname)
        {
            if(viewname == null)
            {
                throw new ArgumentNullException("viewname");
            }
            if (string.IsNullOrWhiteSpace(viewname))
            {
                throw new ArgumentException("viewname can not be empty", "viewname");
            }

            Attachments = new List<Attachment>();
            ViewData = new ViewDataDictionary(this);
            ViewName = viewname;
            InitialiseSmtpClient();

        }
        /// <summary>
        /// Initialises the smtp client.
        /// </summary>
        private void InitialiseSmtpClient()
        {
            //Configuration configurationFile = OpenFile("mail.config");

            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration("~");

            MailSettingsSectionGroup mailSettings =
                configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;

            if (mailSettings != null)
            {
                if (mailSettings.Smtp.Network.Port == 465)
                {
                    mailClient = new MimeMailer(mailSettings.Smtp.Network.Host, mailSettings.Smtp.Network.Port)
                    {
                        User = mailSettings.Smtp.Network.UserName,
                        Password = mailSettings.Smtp.Network.Password,
                        EnableImplicitSsl = true,
                        AuthenticationMode = AuthenticationType.Base64
                    };
                    ((SmtpSocketClient)mailClient).SslType = SslMode.Ssl;
                }
                else if (mailSettings.Smtp.Network.Port == 587)
                {
                    mailClient = new MimeMailer(mailSettings.Smtp.Network.Host, mailSettings.Smtp.Network.Port)
                    {
                        User = mailSettings.Smtp.Network.UserName,
                        Password = mailSettings.Smtp.Network.Password,
                        SslType = SslMode.Tls,
                        AuthenticationMode = AuthenticationType.Base64
                    };
                }
                else
                {
                    mailClient = new MimeMailer(mailSettings.Smtp.Network.Host, mailSettings.Smtp.Network.Port)
                    {
                        User = mailSettings.Smtp.Network.UserName,
                        Password = mailSettings.Smtp.Network.Password,
                        AuthenticationMode = AuthenticationType.Base64
                    };
                }


                this.From = ClassicConfig.ForumEmail;

            }


            //Set a delegate function for call back

        }
        /// <summary>
        /// Opens <see langword="async"/> config file.
        /// </summary>
        /// <returns>WebConfiguration file.</returns>
        /// <param name="filename">Config filename</param>
        public System.Configuration.Configuration OpenFile(string filename)
        {
            var sitename = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            //var mappedpath = System.Web.HttpContext.Current.Server.MapPath("~");

            var configFile = new FileInfo(sitename + filename);
            var vdm = new VirtualDirectoryMapping(configFile.DirectoryName, true, configFile.Name);
            var wcfm = new WebConfigurationFileMap();
            wcfm.VirtualDirectories.Add("/", vdm);

            return WebConfigurationManager.OpenMappedWebConfiguration(wcfm, "/",HostingEnvironment.SiteName);
        }
        protected Email()
        {
            Attachments = new List<Attachment>();
            ViewName = DeriveViewNameFromClassName();
            ViewData = new ViewDataDictionary(this);

            InitialiseSmtpClient();
        }
        /// <summary>
        /// Attach the specified attachment.
        /// </summary>
        /// <param name="attachment">Attachment.</param>
        public void Attach(Attachment attachment)
        {
            Attachments.Add(attachment);
        }
        /// <summary>
        /// Send the specified email.
        /// </summary>
        /// <param name="context">ControllerContext.</param>
        public void Send(ControllerContext context)
        {
            if(context == null)
            {
                if(HttpContext.Current != null)
                {
                    context = CreateController<BaseController>().ControllerContext;
                }
                else
                {
                    throw new InvalidOperationException("ViewRenderer no HttpContext");
                }
            }
            string message = ViewRenderer.RenderView(viewPath + ViewName + ".cshtml", this,context);


            MimeMailMessage mail = ParseHeader(context,message);
            mail.Sender = ViewData.ContainsKey("FromUser") ? new MailAddress(From, ViewData["FromUser"].ToString()) : new MailAddress(From, "Administrator");

            //mail.To.Add(new MailAddress(ViewData["To"].ToString(), ViewData["UserName"].ToString()));

            mail.To.Add(ViewData["To"].ToString());
            mail.IsBodyHtml = IsBodyHtml;
           
            mailClient.SendCompleted += compEvent;
            mailClient.SendMailAsync(mail);
        }
        /// <summary>
        /// Send Email with no context.
        /// </summary>
        public void Send()
        {
            InitialiseSmtpClient();

            var templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views","Emails", ViewName + ".cshtml");
            var config = new TemplateServiceConfiguration
            {
                TemplateManager = new ResolvePathTemplateManager(new[] {"EmailTemplates"}),
                DisableTempFileLocking = true
            };
            var templateService = RazorEngineService.Create(config);
            //var templateService = new TemplateService();
            var message = templateService.RunCompile(templateFilePath,  null, ViewName);
            ViewData = new ViewDataDictionary(this);
            MimeMailMessage mail = ParseHeader(templateService, message);
            dynamic test = ViewData.Model;

            mail.Sender = new MailAddress(test.Sender, "Administrator");
            mail.To.Add(test.To);

            mailClient.SendCompleted += compEvent;
            mailClient.SendMailAsync(mail);
        }

        //Call back function
        private void compEvent(object sender, AsyncCompletedEventArgs e)
        {
            if (e.UserState!=null)
                Console.Out.WriteLine(e.UserState.ToString());

            Console.Out.WriteLine("is it canceled? " + e.Cancelled);

            if (e.Error != null)
            {
                if (SendCompleted != null)
                {
                    SendCompleted(this, e);
                }
            }
        }

        /// <summary>
        /// Parses the header.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="templateService">Template service.</param>
        /// <param name="message">Message.</param>
        private MimeMailMessage ParseHeader(IRazorEngineService templateService, string message)
        {
            var mailMessage = new MimeMailMessage();
            Match m = Regex.Match(message, @"([\s\S]*)(Subject:)(.*$)([\s\S]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                mailMessage.Subject = m.Groups[3].Value ?? ViewData["Subject"].ToString();

                if (m.Groups[4].Value.Contains("Views"))
                {
                    var templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "Emails", ViewName + ".Html.cshtml");
                    string newmessage = templateService.RunCompile(templateFilePath, null, ViewName);
                    Match m2 = Regex.Match(newmessage, @"([\s\S]*)(Content-Type:)(.*$)([\s\S]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    ContentType = m2.Success ? m2.Groups[3].Value : "";
                    var charsetMatch = Regex.Match(ContentType, @"\bcharset\s*=\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    
                    if (charsetMatch.Success)
                    {
                        IsBodyHtml = true;
                        //mailMessage.BodyEncoding = Encoding.GetEncoding(charsetMatch.Groups[1].Value);
                    }
                    mailMessage.Body = m2.Groups[4].Value;
                }
                else
                {

                    mailMessage.Body = m.Groups[4].Value;
                }

            }
            else
            {
                mailMessage.Body = message;
            }

            return mailMessage;
        }
        /// <summary>
        /// Parses the header using ControllerContext.
        /// </summary>
        /// <returns>The header.</returns>
        /// <param name="context">Context.</param>
        /// <param name="message">Message.</param>
        private MimeMailMessage ParseHeader(ControllerContext context, string message)
        {

            var mailMessage = new MimeMailMessage();
            Match m = Regex.Match(message, @"([\s\S]*)(Subject:)(.*$)([\s\S]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                mailMessage.Subject = m.Groups[3].Value != null ? m.Groups[3].Value.Trim() : ViewData["Subject"].ToString();

                if (m.Groups[4].Value.Contains("Views"))
                {
                    string newmessage = ViewRenderer.RenderView(viewPath + ViewName + ".Html.cshtml", this, context);
                    Match m2 = Regex.Match(newmessage, @"([\s\S]*)(Content-Type:)(.*$)([\s\S]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (m2.Success)
                    {
                        ContentType = m2.Groups[3].Value;
                    }
                    var charsetMatch = Regex.Match(ContentType, @"\bcharset\s*=\s*([a-z0-9\-]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    IsBodyHtml = true;
                    if (charsetMatch.Success)
                    {
                        mailMessage.BodyEncoding = Encoding.GetEncoding(charsetMatch.Groups[1].Value);
                    }
                    mailMessage.Body = m2.Groups[4].Value;
                }
                else
                {

                    mailMessage.Body = m.Groups[4].Value;
                }

            }
            else
            {
                mailMessage.Body = message;
            }

            return mailMessage;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = binder.Name;

            if (ViewData.ContainsKey(name))
            {
                ViewData[name] = value;
            }
            else
            {
                ViewData.Add(name, value);
            }

            return true;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return ViewData.TryGetValue(binder.Name,out result);
        }

        string DeriveViewNameFromClassName()
        {
            var viewName = GetType().Name;
            if (viewName.EndsWith("Email"))
            {
                viewName = viewName.Substring(0, viewName.Length - "Email".Length);
            }
            return viewName;

        }

        /// <summary>
        /// Creates an instance of an MVC controller from scratch 
        /// when no existing ControllerContext is present       
        /// </summary>
        /// <typeparam name="T">Type of the controller to create</typeparam>
        /// <returns>Controller Context for T</returns>
        /// <exception cref="InvalidOperationException">thrown if HttpContext not available</exception>
        public static T CreateController<T>(RouteData routeData = null) where T : Controller, new()
        {
            // create a disconnected controller instance
            T controller = new T();

            // get context wrapper from HttpContext if available
            HttpContextBase wrapper = null;
            if (HttpContext.Current != null)
                wrapper = new HttpContextWrapper(System.Web.HttpContext.Current);
            else
                throw new InvalidOperationException(
                    "Can't create Controller Context if no active HttpContext instance is available.");

            if (routeData == null)
                routeData = new RouteData();

            // add the controller routing if not existing
            if (!routeData.Values.ContainsKey("controller") && !routeData.Values.ContainsKey("Controller"))
                routeData.Values.Add("controller", controller.GetType().Name
                                                            .ToLower()
                                                            .Replace("controller", ""));

            controller.ControllerContext = new ControllerContext(wrapper, routeData, controller);
            return controller;
        }

    }
}
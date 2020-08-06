using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using LangResources.Utility;
using SnitzConfig;
using SnitzCore.Filters;
using SnitzDataModel.Validation;
using SnitzMembership.Models;
using DataType = System.ComponentModel.DataAnnotations.DataType;

namespace WWW.Models
{
    public static class UserProfileExtensions
    {
        public static Hashtable EnabledProfileFields()
        {
            var registerFields = new Hashtable();
            var props = typeof(UserProfile).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(ShowAttribute)));
            foreach (PropertyInfo info in props)
            {
                var showme = info.GetCustomAttributes(typeof(ShowAttribute));
                var required = info.GetCustomAttributes(typeof(RequiredIfAttribute)).ToList();
                var attributes = showme as IList<Attribute> ?? showme.ToList();
                if (attributes.Any())
                {
                    bool isrequired = false;
                    ShowAttribute a = (ShowAttribute)attributes.First();
                    if (ClassicConfig.GetValue(((RequiredIfAttribute)required.First()).DependentProperty) ==
                        ((int)((RequiredIfAttribute)required.First()).TargetValue).ToString())
                    {
                        isrequired = true;
                    }
                    if (a.ShowMe())
                    {
                        registerFields.Add(info.Name, isrequired);
                    }
                }
            }
            return registerFields;
        }

        public static Hashtable RequiredProfileFields()
        {
            var registerFields = new Hashtable();
            var props = typeof(UserProfile).GetProperties();
            foreach (PropertyInfo info in props)
            {
                var showme = info.GetCustomAttributes(typeof(ShowAttribute));
                var required = info.GetCustomAttributes(typeof(RequiredIfAttribute)).ToList();
                var attributes = showme as IList<Attribute> ?? showme.ToList();
                if (attributes.Any())
                {
                    if (ClassicConfig.GetValue(((RequiredIfAttribute)required.First()).DependentProperty) ==
                        ((int)((RequiredIfAttribute)required.First()).TargetValue).ToString())
                    {
                        registerFields.Add(info.Name, true);
                    }
                }
            }
            return registerFields;
        }

    }

    //public class RegisterExternalLoginModel
    //{
    //    [System.ComponentModel.DataAnnotations.Required]
    //    [LocalisedDisplayName(ResourceType: "General", Name: "UserName")]
    //    public string UserName { get; set; }

    //    public string ExternalLoginData { get; set; }
    //}

    public class LocalPasswordModel
    {
        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "CurrentPassword")]
        public string OldPassword { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.StringLength(100, MinimumLength = 6)]
        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "NewPassword")]
        public string NewPassword { get; set; }

        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "ConfirmNewPassword")]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangeEmailModel
    {
        [SnitzCore.Filters.Required]
        public string Username { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.EmailAddress)]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "Email is not valid")]
        public string CurrentEmail { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.EmailAddress, ErrorMessage = "E-mail is not valid")]
        [LocalisedDisplayName(ResourceType: "General", Name: "CurrentEmail")]
        [Compare("CurrentEmail")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "Email is not valid")]
        public string OldEmail { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.EmailAddress)]
        [LocalisedDisplayName(ResourceType: "General", Name: "NewEmail")]
        public string NewEmail { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "Password")]
        public string Password { get; set; }
        
    }
    public class ChangeUsernameModel
    {
        public string Username { get; set; }

        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(ResourceType: "General", Name: "ChangeUsername")]
        [SnitzCore.Filters.Remote("UsernameAvailable", "Account", ResourceName = "UserNameExists")]
        public string NewUserName { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "Password")]
        public string Password { get; set; }

    }    
    public class LoginModel
    {
        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(ResourceType: "General", Name: "UserName")]
        public string UserName { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "Password")]
        public string Password { get; set; }

        [LocalisedDisplayName(ResourceType: "General", Name: "RememberMe")]
        public bool RememberMe { get; set; }

        public bool IsConfirmed { get; set; }

    }

    public class ProfileEditModel
    {
        public UserProfile Profile { get; set; }
        public readonly List<SelectListItem> GenderList = new List<SelectListItem>
                                             {
                                                 new SelectListItem
                                                 {
                                                     Text = ResourceManager.GetLocalisedString("GenderList_Male", "General") ,
                                                     Value = "Male"
                                                 },
                                                 new SelectListItem
                                                 {
                                                     Text = ResourceManager.GetLocalisedString("GenderList_Female", "General") ,
                                                     Value = "Female",
                                                     Selected = true
                                                 },
                                                 new SelectListItem
                                                 {
                                                     Text = ResourceManager.GetLocalisedString("GenderList_Other", "General") ,
                                                     Value = "Other"
                                                 }
                                             };
    }
    public class RegisterModel
    {
        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(ResourceType: "General", Name: "UserName")]
        [SnitzCore.Filters.Remote("UsernameAvailable", "Account", ResourceName = "UserNameExists")]
        [ServerSideOnlyRegularExpression(@"^[\p{L}0-9 -_]{1,150}$")]
        public string UserName { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.EmailAddress)]
        [LocalisedDisplayName(ResourceType: "General", Name: "Email")]
        [SnitzCore.Filters.Remote("EmailCheck", "Account", ResourceName = "EmailExists")]
        public string Email { get; set; }

        [RequiredIf("UseCaptcha", true, "CaptchaCheck_Wrong")]
        [LocalisedDisplayName(ResourceType: "General", Name: "CaptchaLabel")]
        public string Captcha { get; set; }

        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.StringLength(100, MinimumLength = 6)]
        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "Password")]
        public string Password { get; set; }

        [SnitzCore.Filters.DataType(DataType.Password)]
        [LocalisedDisplayName(ResourceType: "General", Name: "ConfirmPassword")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public UserProfile Profile { get; set; }

        public Hashtable RegisterFields { get; set; }

        //public bool UseCaptcha
        //{
        //    get
        //    {
        //        return Config.UseCaptcha;
        //    }
        //}
    }
    
    public class EmailModel
    {

        [LocalisedDisplayName(ResourceType: "General", Name: "EmailModel_From")]
        [SnitzCore.Filters.Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string FromEmail { get; set; }
        [SnitzCore.Filters.Required]
        public string FromName { get; set; }

        [LocalisedDisplayName(ResourceType: "General", Name: "EmailModel_Name")]
        [SnitzCore.Filters.Required]

        public string ToName { get; set; }

        [LocalisedDisplayName(ResourceType: "General", Name: "Email")]
        [SnitzCore.Filters.Required]
        [SnitzCore.Filters.DataType(DataType.EmailAddress)]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "Email is not valid")]
        public string ToEmail { get; set; }

        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(ResourceType: "General", Name: "Subject")]
        public string Subject { get; set; }

        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(ResourceType: "General", Name: "Message")]
        [AllowHtml]
        public string Message { get; set; }

        public string ReturnUrl { get; set; }

        public bool AdminEmail { get; set; }

        public HttpPostedFileBase Attachment { get; set; }
        public int MemberId { get; set; }
    }
    
    public class OnlineUserViewModel
    {
        public List<KeyValuePair<string,string>> Members { get; set; }
        public List<KeyValuePair<string, string>> RecentMembers { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveGuests { get; set; }
        public List<string> Usernames { get; set; }
    }


}

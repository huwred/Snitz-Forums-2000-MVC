using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Security;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzDataModel.Models;
using SnitzDataModel.Validation;

namespace SnitzMembership.Models
{
    //note: You may need to add a schema definition of your forum tables are created using a user schema
    //[Table("FORUM_MEMBERS", Schema = "SnitzUser")]
    [Table("FORUM_MEMBERS")]
    public class UserProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("MEMBER_ID")]
        public int UserId { get; set; }

        [Column("M_NAME")]
        [LocalisedDisplayName(Name : "UserName", ResourceType : "General")]
        public string UserName { get; set; }

        [Column("M_EMAIL")]
        [LocalisedDisplayName(Name : "Email", ResourceType : "General")]
        public string Email { get; set; }

        [Column("M_PASSWORD")]
        [LocalisedDisplayName(Name : "Password", ResourceType : "General")]
        public string Password { get; set; }

        [Column("M_NEWEMAIL")]
        [LocalisedDisplayName(Name : "NewEmail", ResourceType : "General")]
        public string NewEmail { get; set; }

        [Column("M_RECEIVE_EMAIL")]
        [LocalisedDisplayName(Name : "ReceiveEmail", ResourceType : "General")]
        public short RecEmails { get; set; }

        #region Profile fields
        /// <summary>
        /// Show determines if the field is used ie STRCITY=1 in FORUM_CONFIG_NEW
        /// RequiredIf checks the STRREQCITY to decide if the field is required
        /// </summary>
        [Column("M_CITY")]
        [Show("STRCITY")]
        [LocalisedDisplayName(Name : "ProfileCity", ResourceType : "General")]
        [RequiredIf("STRREQCITY", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100)]
        public string City { get; set; }

        [Column("M_STATE")]
        [Show("STRSTATE")]
        [LocalisedDisplayName(Name : "ProfileState", ResourceType : "General")]
        [RequiredIf("STRREQSTATE", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100)]
        public string State { get; set; }

        [Column("M_COUNTRY")]
        [Show("STRCOUNTRY")]
        [LocalisedDisplayName("ProfileCountry", "General")]
        [RequiredIf("STRREQCOUNTRY", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(50)]
        public string Country { get; set; }

        [Column("M_FIRSTNAME")]
        [Show("STRFULLNAME")]
        [LocalisedDisplayName(Name : "ProfileFirstname", ResourceType : "General")]
        [RequiredIf("STRREQFULLNAME", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100)]
        public string Firstname { get; set; }

        [Column("M_LASTNAME")]
        [Show("STRFULLNAME")]
        [LocalisedDisplayName(Name : "ProfileLastname", ResourceType : "General")]
        [RequiredIf("STRREQFULLNAME", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100)]
        public string Lastname { get; set; }

        [Column("M_OCCUPATION")]
        [Show("STROCCUPATION")]
        [LocalisedDisplayName(Name : "ProfileOccupation", ResourceType : "General")]
        [RequiredIf("STRREQOCCUPATION", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(255)]
        public string Occupation { get; set; }

        [Column("M_SEX")]
        [Show("STRSEX")]
        [LocalisedDisplayName(Name : "ProfileGender", ResourceType : "General")]
        [RequiredIf("STRREQSEX", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(50)]
        public string Gender { get; set; }

        [Column("M_AGE")]
        [Show("STRAGE")]
        [LocalisedDisplayName(Name : "ProfileAge", ResourceType : "General")]
        [RequiredIf("STRREQAGE", 1, "PropertyRequired")]
        public string Age { get; set; }

        [Column("M_DOB")]
        [LocalisedDisplayName(Name : "ProfileBirthdate", ResourceType : "General")]
        [Show("STRAGEDOB")]
        [RequiredIf("STRREQAGEDOB", 1,  "PropertyRequired")]
        public string DateOfBirth { get; set; }

        [Column("M_MARSTATUS")]
        [Show("STRMARSTATUS")]
        [LocalisedDisplayName(Name : "ProfileMarStatus", ResourceType : "General")]
        [RequiredIf("STRREQMARSTATUS", 1,  "PropertyRequired")]
        [SnitzCore.Filters.StringLength(100)]
        public string MaritalStatus { get; set; }

        [Column("M_PHOTO_URL")]
        [Show("STRPICTURE")]
        [LocalisedDisplayName(Name : "ProfileAvatar", ResourceType : "General")]
        [RequiredIf("STRREQPICTURE", 1,  "PropertyRequired")]
        [SnitzCore.Filters.StringLength(255)]
        public string PhotoUrl { get; set; } //AvatarImgUrl

        [Column("M_HOMEPAGE")]
        [Show("STRHOMEPAGE")]
        [LocalisedDisplayName(Name : "ProfileHomePage", ResourceType : "General")]
        [RequiredIf("STRREQHOMEPAGE", 1,  "PropertyRequired")]
        [SnitzCore.Filters.StringLength(255)]
        [Url]
        public string Homepage { get; set; }

        [Column("M_AIM")]
        [Show("STRAIM")]
        [LocalisedDisplayName(Name : "ProfileAOL", ResourceType : "General")]
        [RequiredIf("STRREQAIM", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(150)]
        public string AIM { get; set; }

        [Column("M_ICQ")]
        [Show("STRICQ")]
        [LocalisedDisplayName(Name : "ProfileICQ", ResourceType : "General")]
        [RequiredIf("STRREQICQ", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(150)]
        public string ICQ { get; set; }

        [Column("M_MSN")] //MSN now Skype, so lets use this for Skype address
        [Show("STRMSN")]
        [LocalisedDisplayName(Name : "ProfileMSN", ResourceType : "General")]
        [RequiredIf("STRREQMSN", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(150)]
        public string MSN { get; set; }

        [Column("M_YAHOO")]
        [Show("STRYAHOO")]
        [LocalisedDisplayName(Name : "ProfileYahoo", ResourceType : "General")]
        [RequiredIf("STRREQYAHOO", 1, "PropertyRequired")]
        [SnitzCore.Filters.StringLength(150)]
        public string YAHOO { get; set; }

        [Column("M_HOBBIES")]
        [Show("STRHOBBIES")]
        [LocalisedDisplayName(Name : "ProfileHobby", ResourceType : "General")]
        [RequiredIf("STRREQHOBBIES", 1, "PropertyRequired")]
        public string Hobbies { get; set; }

        [Column("M_LNEWS")]
        [Show("STRLNEWS")]
        [LocalisedDisplayName(Name : "ProfileNews", ResourceType : "General")]
        [RequiredIf("STRREQLNEWS", 1, "PropertyRequired")]
        public string LatestNews { get; set; }

        [Column("M_QUOTE")]
        [Show("STRQUOTE")]
        [LocalisedDisplayName(Name : "ProfileQuote", ResourceType : "General")]
        [RequiredIf("STRREQQUOTE", 1, "PropertyRequired")]
        public string FavQuote { get; set; }

        [Column("M_BIO")]
        [Show("STRBIO")]
        [LocalisedDisplayName(Name : "ProfileBio", ResourceType : "General")]
        [RequiredIf("STRREQBIO", 1, "PropertyRequired")]
        public string Biography { get; set; }

        #endregion

        [Column("M_SIG")]
        [LocalisedDisplayName(Name : "ProfileSignature", ResourceType : "General")]
        public string Signature { get; set; }

        [Column("M_VIEW_SIG")]
        [LocalisedDisplayName(Name : "ProfileShowSig", ResourceType : "General")]
        public short ViewSig { get; set; }

        [Column("M_SIG_DEFAULT")]
        [LocalisedDisplayName(Name : "ProfileUseSig", ResourceType : "General")]
        public short SigDefault { get; set; }

        [Column("M_DEFAULT_VIEW")]
        [LocalisedDisplayName(Name : "ProfileDefaultView", ResourceType : "General")]
        public Enumerators.ForumDays DefaultView { get; set; }

        #region non user editable fields
        [Column("M_STATUS")]
        [LocalisedDisplayName(Name : "ProfileStatus", ResourceType : "General")]
        public short Status { get; set; }
        [Column("M_LEVEL")]
        [LocalisedDisplayName(Name : "ProfileLevel", ResourceType : "General")]
        public short UserLevel { get; set; }
        [Column("M_POSTS")]
        [LocalisedDisplayName(Name : "ProfilePosts", ResourceType : "General")]
        public int PostCount { get; set; }

        [Column("M_DATE")]
        [SnitzCore.Filters.StringLength(14)]
        [LocalisedDisplayName(Name : "ProfileDate", ResourceType : "General")]
        public string Created { get; set; }
        [Column("M_LASTHEREDATE")]
        [SnitzCore.Filters.StringLength(14)]
        [LocalisedDisplayName(Name : "ProfileLastVisit", ResourceType : "General")]
        public string LastVisit { get; set; }
        [Column("M_LASTPOSTDATE")]
        [SnitzCore.Filters.StringLength(14)]
        [LocalisedDisplayName(Name : "ProfileLastPost", ResourceType : "General")]
        public string LastPost { get; set; }
        [Column("M_LASTACTIVITY")]
        [SnitzCore.Filters.StringLength(14)]
        public string LastActivity { get; set; }

        [Column("M_TITLE")]
        [SnitzCore.Filters.StringLength(50)]
        [LocalisedDisplayName(Name : "ProfileTitle", ResourceType : "General")]
        public string ForumTitle { get; set; }
        [Column("M_SUBSCRIPTION")]
        [LocalisedDisplayName(Name : "ProfileAllowSubs", ResourceType : "General")]
        public short AllowSubs { get; set; }
        [Column("M_ALLOWEMAIL")]
        [LocalisedDisplayName(Name : "ProfileAllowEmail", ResourceType : "General")]
        public short? SendEmail { get; set; }
        [Column("M_LAST_IP")]
        [LocalisedDisplayName(Name : "ProfileLastIP", ResourceType : "General")]
        public string LastIp { get; set; }
        [Column("M_IP")]
        [LocalisedDisplayName(Name : "ProfileIP", ResourceType : "General")]
        public string IpAddress { get; set; }

        [Column("M_PMEMAIL")]
        [LocalisedDisplayName(Name : "ProfilePMNotify", ResourceType : "General")]
        public int PrivateMessageNotify { get; set; }
        [Column("M_PMRECEIVE")]
        [LocalisedDisplayName(Name : "ProfilePMReceive", ResourceType : "General")]
        public int PrivateMessageReceive { get; set; }
        [Column("M_PMSAVESENT")]
        [LocalisedDisplayName(Name : "ProfilePMSentItems", ResourceType : "General")]
        public short PrivateMessageSentItems { get; set; }
        #endregion
        [Column("M_PRIVATEPROFILE")]
        [LocalisedDisplayName(Name: "PrivateProfile", ResourceType: "General")]
        public short PrivateProfile { get; set; }
        public UserProfile()
        {

            RecEmails = 1;
            ViewSig = 0;
            SigDefault = 0;
            DefaultView = Enumerators.ForumDays.AllOpen;
            Status = 0;
            UserLevel = 0;
            PostCount = 0;
            AllowSubs = 1;
            SendEmail = 0;
            PrivateProfile = 0;
        }

        [NotMapped]
        [UIHint("YesNo")]
        public bool IsApproved
        {
            get { return (UserLevel > 0 && Status == 0); }
            set
            {
                if (!value)
                {
                    UserLevel = 0;
                    Status = 0;
                }
            }
        }
        [NotMapped]
        [UIHint("YesNo")]
        public bool Disabled
        {
            get
            {
                if (this.UserName == null)
                {
                    return false;
                }
                return (WebSecurity.IsUserInRole(this.UserName,"Disabled") || this.UserName == "n/a" || this.UserName == "zapped");
            }
            set
            {
                if (value)
                {
                    if (this.UserName != null)
                        Roles.AddUserToRole(this.UserName, "Disabled");
                }
                else
                {
                    if (this.UserName != null)
                        Roles.RemoveUserFromRole(this.UserName,"Disabled");
                }
            }
        }
        [NotMapped]
        [UIHint("YesNo")]
        [LocalisedDisplayName(Name : "ProfileShowSig", ResourceType : "General")]
        public bool ViewSignatures
        {
            get { return ViewSig == 1; }
            set
            {
                ViewSig = Convert.ToInt16(value ? 1 : 0);
            }
        }

        [NotMapped]
        [UIHint("YesNo")]
        [LocalisedDisplayName(Name : "ProfileUseSig", ResourceType : "General")]
        public bool AlwaysUseSignature
        {
            get { return SigDefault == 1; }
            set
            {
                SigDefault = Convert.ToInt16(value ? 1 : 0);
            }
        }

        [NotMapped]
        [UIHint("YesNo")]
        [LocalisedDisplayName(Name : "ProfileAllowSubs", ResourceType : "General")]
        public bool AllowSubscriptions
        {
            get { return AllowSubs == 1; }
            set
            {
                AllowSubs = Convert.ToInt16(value ? 1 : 0);
            }
        }

        [NotMapped]
        [LocalisedDisplayName(Name : "ProfileAllowEmail", ResourceType : "General")]
        [UIHint("YesNo")]
        public bool AllowSendEmail
        {
            get { return SendEmail == 1; }
            set
            {
                SendEmail = Convert.ToInt16(value ? 1 : 0);
            }
        }

        [NotMapped]
        [UIHint("YesNo")]
        [LocalisedDisplayName(Name : "ReceiveEmail", ResourceType : "General")]
        public bool ReceiveEmails
        {
            get { return RecEmails == 1; }
            set
            {
                RecEmails = Convert.ToInt16(value ? 1 : 0);
            }
        }

        [NotMapped]
        [LocalisedDisplayName(Name : "ProfileStatus", ResourceType : "General")]
        [UIHint("YesNo")]
        public bool Locked
        {
            get { return Status == 0; }
            set
            {
                Status = Convert.ToInt16(value ? 0 : 1);
            }
        }

        [NotMapped]
        [LocalisedDisplayName(Name : "ProfileLevel", ResourceType : "General")]
        public Enumerators.UserLevels MLev
        {
            get { return (Enumerators.UserLevels)UserLevel; }
            set
            {
                UserLevel = Convert.ToInt16(value);
            }
        }

        //[NotMapped]
        //public List<Subscriptions> Subscriptions { get; set; }

        [NotMapped]
        [LocalisedDisplayName(Name : "ProfileBirthdate", ResourceType : "General")]
        public DateTime? BirthDate
        {
            get { return DateOfBirth.ToDateTime(); }
            set
            {
                DateOfBirth = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [NotMapped]
        public HttpPostedFileBase AvatarPostedFileBase { get; set; }

        [NotMapped]
        [LocalisedDisplayName(Name: "ProfileDate", ResourceType: "General")]
        public DateTime? CreatedDate
        {
            get { return Created.ToDateTime(); }
            set
            {
                Created = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [NotMapped]
        [LocalisedDisplayName(Name: "ProfileLastVisit", ResourceType: "General")]
        public DateTime? LastVisitDate
        {
            get { return LastVisit.ToDateTime(); }
            set
            {
                LastVisit = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [NotMapped]
        [LocalisedDisplayName(Name: "ProfileLastPost", ResourceType: "General")]
        public DateTime? LastPostDate
        {
            get { return LastPost.ToDateTime(); }
            set
            {
                LastPost = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }

        [NotMapped]
        public DateTime? LastActivityDate
        {
            get { return LastActivity.ToDateTime(); }
            set
            {
                LastActivity = value.HasValue ? value.Value.ToSnitzDate() : null;
            }
        }
        [NotMapped]
        [UIHint("YesNo")]
        [LocalisedDisplayName(Name: "AnonymousUser", ResourceType: "General")]
        public bool AnonymousUser
        {
            get
            {
                if (String.IsNullOrEmpty(this.UserName))
                {
                    return false;
                }
                if (!Roles.RoleExists("HiddenMembers"))
                {
                    return false;
                }
                return ClassicConfig.GetIntValue("INTALLOWHIDEONLINE") == 1 && Roles.IsUserInRole(this.UserName, "HiddenMembers");
            }
            set
            {
                if (ClassicConfig.GetIntValue("INTALLOWHIDEONLINE") == 1)
                {
                    if (value)
                        Roles.AddUserToRole(this.UserName, "HiddenMembers");
                    else
                        Roles.RemoveUserFromRole(this.UserName, "HiddenMembers");
                }
            }
        }

        [NotMapped]
        public object this[string propertyName]
        {
            get
            {

                PropertyInfo myPropInfo = GetType().GetProperty(propertyName.Replace("Date",""));
                return myPropInfo.GetValue(this, null);
            }
            set
            {

                PropertyInfo myPropInfo = GetType().GetProperty(propertyName.Replace("Date",""));
                myPropInfo.SetValue(this, value, null);

            }

        }

        public bool HasSubscription(Reply reply)
        {

            var allsubs = Subscriptions.Member(WebSecurity.CurrentUserId).ToList();

            return allsubs.Any(s => s.TopicId == reply.TopicId || s.ForumId == reply.ForumId || s.CatId == reply.CatId);;
        }
        public bool HasSubscription(Topic topic)
        {

            var allsubs = Subscriptions.Member(WebSecurity.CurrentUserId).ToList();

            return allsubs.Any(s => s.TopicId == topic.Id || s.ForumId == topic.ForumId || s.CatId == topic.CatId);;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using LangResources.Utility;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Filters;
using SnitzMembership;

namespace SiteManage.Models
{
    [TableName("PRODUCTS", prefixType = Snitz.Base.Extras.TablePrefixTypes.Forum)]
    [PetaPoco.PrimaryKey("PRODUCT_ID")]
    [ExplicitColumns]
    public class PayPalProduct
    {

        [Column("PRODUCT_ID")]
        public int Id { get; set; }
        [Column("PRODUCT_NAME")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblProdName")]
        [MaxLength(255)]
        [SnitzCore.Filters.Required]
        public string Name { get; set; }
        [Column("PRODUCT_DESC")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblProdDesc")]
        public string Description { get; set; }
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblAmount")]
        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        [SnitzCore.Filters.Range(1.0, Double.PositiveInfinity, ErrorMessage = "ErrMinValue")]
        [Column("PRODUCT_AMOUNT")]

        public decimal Amount { get; set; }
        [Column("PRODUCT_TYPE")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblMode")]
        [SnitzCore.Filters.Required]
        public string Mode { get; set; }
        [Column("PRODUCT_PERIOD_LENGTH")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblPeriodLen")]
        public int RecurLength { get; set; }
        [Column("PRODUCT_PERIOD")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblPeriod")]
        public string RecurPeriod { get; set; } //"M","Y"
        [Column("PRODUCT_CURRENCY")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblCurrency")]
        public string Currency { get; set; }
    }

    public class PayPalModel
    {
        public string cmd { get; set; }
        public string business { get; set; }
        public string no_shipping { get; set; }
        public string return_url { get; set; }
        public string cancel_return { get; set; }
        public string notify_url { get; set; }
        public string currency_code { get; set; }
        public string item_name { get; set; }
        public string amount { get; set; }
        public string actionURL { get; set; }
        public string item_number { get; set; }

        public int p3 { get; set; }
        public string t3 { get; set; }

        public PayPalModel(bool useSandbox)
        {
            this.cmd = "_xclick";
            this.no_shipping = "1";
            this.business = ClassicConfig.GetValue("STRPAYPALBUSINESS"); //ConfigurationManager.AppSettings["business"];
            this.cancel_return = Config.ForumUrl + "PayPal/Cancel"; 
            this.return_url = Config.ForumUrl + "PayPal/Confirmation"; //PayPal/Confirmation;
            this.notify_url = Config.ForumUrl + "PayPal/NotifyFromPaypal"; 
            this.actionURL = ClassicConfig.GetValue("STRPAYPALTESTURL",Config.ForumUrl + "PayPal/TestPost");
            if(!useSandbox)
                this.actionURL = ClassicConfig.GetValue("STRPAYPALURL","https://www.paypal.com/cgi-bin/webscr");
        }
    }

    [TableName("DONATION", prefixType = Snitz.Base.Extras.TablePrefixTypes.Forum)]
    [PetaPoco.PrimaryKey("DONATION_ID")]
    [ExplicitColumns]
    public class PayPalDonation
    {
        [Column("DONATION_ID")]
        public int Id { get; set; }
        [Column("DONATION_MEMBER")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblMember")]
        public int? MemberId { get; set; }
        [Column("DONATION_AMOUNT")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblAmount")]
        [SnitzCore.Filters.Range(1.0,Double.PositiveInfinity,ErrorMessage = "ErrMinValue")]
        public decimal Amount { get; set; }
        [Column("DONATION_DATE")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblDonatedOn")]
        public string DonatedOn { get; set; }
        [Column("DONATION_ANON")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblAnon")]
        public int Anon { get; set; }
        [Column("DONATION_NAME")]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblDonorName")]
        [MaxLength(150)]
        public string Name { get; set; }

        [ResultColumn]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblFirstname")]
        public string Firstname { get; set; }
        [ResultColumn]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblLastname")]
        public string Lastname { get; set; }
        [ResultColumn]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblUser")]
        public string Username { get; set; }

        [Ignore]
        [LocalisedDisplayName(ResourceType: "PayPal", Name: "lblDonatedOn")]
        [SnitzCore.Filters.Required]
        public DateTime? DonatedDate {
            get
            {
                if (String.IsNullOrEmpty(DonatedOn))
                {
                    return null;
                }
                return DonatedOn.ToSnitzDateTime();
            }
            set
            {
                if (value.HasValue)
                {
                    DonatedOn = value.Value.ToSnitzDate();
                }
                else
                {
                    this.DonatedOn = null;
                }
                
            }
        }
    }

    public class PayPalAdminViewModel
    {
        public List<PayPalProduct> Products { get; set; }
    }
}
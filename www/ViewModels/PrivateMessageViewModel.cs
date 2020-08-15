using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SnitzCore.Filters;
//using SnitzCore.Filter;
//using System.ComponentModel.DataAnnotations;
using SnitzDataModel.Models;
using DataType = System.ComponentModel.DataAnnotations.DataType;
using Member = SnitzDataModel.Models.Member;
using PrivateMessage = SnitzDataModel.Models.PrivateMessage;


namespace WWW.ViewModels
{
    public class PrivateMessageViewModel
    {
        public Member Owner { get; set; }
        public List<PrivateMessage> OutBox { get; set; }
        public List<PrivateMessage> InBox { get; set; }
        public string MailBoxMessage { get; set; }
        public bool IsFull { get; set; }
        public bool SaveSentItems { get; set; }

    }
    public class PrivateMessagePost
    {
        public Member Sender { get; set; }
        [SnitzCore.Filters.Required]
        public string ToUser { get; set; }
        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(Name: "lblSubject", ResourceType: "labels")]
        public string Subject { get; set; }
        [SnitzCore.Filters.Required]
        [LocalisedDisplayName(Name: "Message", ResourceType: "General")]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; }

        public bool ReadReceipt { get; set; }
        public bool SaveToSent { get; set; }

        public bool ShowSignature { get; set; }
        public bool IsFull { get; set; }
        public bool AllMembers { get; set; }
        public bool SaveDraft { get; set; }
        public int Read { get; set; }
        public int Id { get; set; }
    }
    public class PrivateMessageSettings
    {
        public bool Receive { get; set; }
        public bool Notify { get; set; }

        public bool SentItems { get; set; }

        public IEnumerable<PrivateBlocklist> Blocklist { get; set; }
    }
}
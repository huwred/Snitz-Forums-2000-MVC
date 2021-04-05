using System;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;

namespace SnitzDataModel.Models
{
    [TableName("FILECOUNT", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("FC_ID")]
    [ExplicitColumns]
    public class DownloadFile
    {
        [Column("FC_ID")]
        public int Id { get; set; }
        [Column("LinkHits")]
        public int Downloads { get; set; }
        [Column("LinkOrder")]
        public int Order { get; set; }
        [Column("Archived")]
        public int Archive { get; set; }
        [Column("Posted")]
        public string Uploaded { get; set; }
        [Column("FileName")]
        public string FileName { get; set; }
        [Column("Title")]
        public string Name { get; set; }

        public DateTime? UploadedDate
        {
            get { return this.Uploaded.ToSnitzDateTime(); }
            set
            {
                if (value.HasValue)
                {
                    var srvOffset = ClassicConfig.ForumServerOffset;
                    this.Uploaded = value.Value.ToSnitzServerDateString(srvOffset);
                }
            }
        }
    }

}
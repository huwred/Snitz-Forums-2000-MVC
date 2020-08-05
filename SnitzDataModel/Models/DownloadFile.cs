using System;
using PetaPoco;
using Snitz.Base;

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
        public DateTime? UploadedDate { get; set; }
        [Column("FileName")]
        public string FileName { get; set; }
        [Column("Title")]
        public string Name { get; set; }

        //public DateTime? Date
        //{
        //    get { return this.Uploaded.ToSnitzDateTime(); }
        //    set
        //    {
        //        if (value.HasValue)
        //            this.Uploaded = value.Value.ToSnitzString();
        //    }
        //}
    }

}
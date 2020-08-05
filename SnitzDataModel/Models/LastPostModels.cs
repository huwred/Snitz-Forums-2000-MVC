using System;
using PetaPoco;

namespace SnitzDataModel.Models
{
    public class LastPostModel
    {
        public int LastPostId { get; set; }
        public int LastPostAuthorId { get; set; }
        public DateTime LastPostDate { get; set; }

        [ResultColumn]
        public int PostCount { get; set; }
    }
}
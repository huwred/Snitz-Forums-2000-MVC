using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using PetaPoco;
using Snitz.Base;
using SnitzDataModel.Database;

namespace SnitzDataModel.Models
{
    [TableName("BBCODE", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("Id")]
    [ExplicitColumns]
    public class CustomBBcode : SnitzDataContext.Record<CustomBBcode>
    {
        [Column("Id")]
        public int Id { get; set; }
        [Column("BB_NAME")]
        [Required]
        [SnitzCore.Filters.StringLength(50)]
        public string Name { get; set; }
        [Column("BB_PATTERN")]
        [Required]
        [AllowHtml]
        [SnitzCore.Filters.StringLength(1000)]
        public string Pattern { get; set; }
        [Column("BB_REPLACE")]
        [Required]
        [AllowHtml]
        [SnitzCore.Filters.StringLength(1000)]
        public string Replace { get; set; }
        [Column("BB_ORDER")]
        [Required]
        public int Order { get; set; }
        [Column("BB_ACTIVE")]
        public bool Active { get; set; }

        public static List<CustomBBcode> All()
        {
            var cacheService = new InMemoryCache() { DoNotExpire = true };

            using (var db = new SnitzDataContext())
            {
                //Mappers.Revoke(typeof(CustomBBcode));
                //Mappers.Register(typeof(CustomBBcode), new SnitzMapper());

                return cacheService.GetOrSet("custom.bbcode", () => db.Fetch<CustomBBcode>("SELECT * FROM " + db.ForumTablePrefix + "BBCODE ORDER BY BB_ORDER"));
            }
            
        }

        public CustomBBcode()
        {
            this.Order = 999;
            this.Active = true;
        }
    }
}

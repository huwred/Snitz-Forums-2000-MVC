using System.Web.Mvc;
using PetaPoco;

namespace LangResources.Models
{
    [TableName("LANGUAGE_RES")]
    [PrimaryKey("pk")]
    public partial class ResourceEntry
    {
        [Column("pk")]
        public int Id { get; set; }
        [Column("ResourceId")]
        public string Name { get; set; }
        [AllowHtml]
        public string Value { get; set; }
        public string Culture { get; set; }
        public string Type { get; set; }
        public string ResourceSet { get; set; }

        public ResourceEntry()
        {
            Type = "string";
        }
    }
}

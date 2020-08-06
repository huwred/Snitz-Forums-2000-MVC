
using System.IO;

using Member = SnitzDataModel.Models.Member;

namespace WWW.ViewModels
{
    public class FilesViewModel
    {
        public Member Owner { get; set; }
        public string FolderUrl { get; set; }
        public FileInfo[] Files { get; set; } 
    }
}
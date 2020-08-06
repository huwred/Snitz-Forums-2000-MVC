using System;
using System.Collections.Generic;

namespace WWW.Models
{
    public class DirModel
    {
        public string Name { get; set; }
        public string RealName { get; set; }
        public DateTime LastAccessed { get; set; }
        public long Size { get; set; }
        public string User { get; set; }
    }
    public class FileModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RealName { get; set; }
        public long Size { get; set; }
        public DateTime LastAccessed { get; set; }
        public string MimeType { get; set; }
    }

    public class ExplorerModel
    {
        public DirModel CurrentFolder { get; set; }
        public List<DirModel> dirModelList;
        public List<FileModel> fileModelList;

        public ExplorerModel(List<DirModel> _dirModelList, List<FileModel> _fileModelList)
        {
            dirModelList = _dirModelList;
            fileModelList = _fileModelList;
        }
    }

}
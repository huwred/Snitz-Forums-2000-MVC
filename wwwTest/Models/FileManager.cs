using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace WWW.Models
{
    public static class FileManager
    {
        public static Exception FileOperationException { get; private set; }

        /// <summary>
        /// path.combine works great, but is filesystem-centric; we just convert the slashes
        /// </summary>
        internal static string WebPathCombine(string path1, string path2)
        {
            string strTemp = Path.Combine(path1, path2).Replace("\\", "/");
            if (strTemp.IndexOf("~/", StringComparison.Ordinal) > -1)
            {
                strTemp = strTemp.Replace("~/", VirtualPathUtility.ToAbsolute("~/"));
            }
            return strTemp;
        }

        /// <summary>
        /// Returns true if this DataRowView represents a directory/folder
        /// </summary>
        internal static bool IsDirectory(DataRowView drv)
        {
            return Convert.ToString(drv["Attr"]).IndexOf("d", StringComparison.Ordinal) > -1;
        }

        /// <summary>
        /// returns True if the provided path is an existing directory
        /// </summary>
        internal static bool IsDirectory(string strFilepath)
        {
            return Directory.Exists(strFilepath);
        }

        /// <summary>
        /// recursively removes the read only tag from a file or folder, if it is present
        /// </summary>
        private static void RemoveReadOnly(string strPath)
        {
            if (IsDirectory(strPath))
            {
                foreach (string strFile in Directory.GetFiles(strPath))
                {
                    RemoveReadOnly(strFile);
                }
                foreach (string strFolder in Directory.GetDirectories(strPath))
                {
                    RemoveReadOnly(strFolder);
                }
            }
            else
            {
                FileInfo fi = new FileInfo(strPath);
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                {
                    fi.Attributes = fi.Attributes ^ FileAttributes.ReadOnly;
                }
            }
        }

        /// <summary>
        /// maps the current web path to a server filesystem path
        /// </summary>
        internal static string GetLocalPath(string strFilename = "")
        {
            return Path.Combine(HttpContext.Current.Server.MapPath(WebPath()), strFilename.Replace("~/",""));
        }

        /// <summary>
        /// Returns the current URL path we're browsing at the moment
        /// </summary>
        internal static string WebPath(string path="")
        {
            string strPath = null;
            var strings = HttpContext.Current.Request.QueryString.GetValues("path") ??
                          HttpContext.Current.Request.Form.GetValues("path");
            if (strings != null)
            {
                strPath = strings[0];
            }
            else
            {
                strPath = path;
            }

            if (string.IsNullOrWhiteSpace(strPath))
            {
                strPath = GetConfigString("DefaultPath", "~/");
            }
            return strPath;
        }

        /// <summary>
        /// Retrieve a value from the .config file appSettings
        /// </summary>
        internal static string GetConfigString(string strKey, string strDefaultValue = "")
        {
            strKey = "WebFileManager/" + strKey;
            if (ConfigurationManager.AppSettings[strKey] == null)
            {
                return strDefaultValue;
            }
            else
            {
                return Convert.ToString(ConfigurationManager.AppSettings[strKey]);
            }
        }

        /// <summary>
        /// maps the current web path, plus target folder, to a server filesystem path
        /// </summary>
        private static string GetTargetPath(string strFilename = "")
        {
            return Path.Combine(Path.Combine(GetLocalPath(), HttpContext.Current.Request.Form["targetfolder"]),
                strFilename);
        }

        /// <summary>
        /// deletes a file or folder
        /// </summary>
        internal static void DeleteFileOrFolder(string strName)
        {
            FileOperationException = null;
            string strLocalPath = GetLocalPath(strName);
            try
            {
                RemoveReadOnly(strLocalPath);
                if (IsDirectory(strLocalPath))
                {
                    Directory.Delete(strLocalPath, true);
                }
                else
                {
                    File.Delete(strLocalPath);
                }
            }
            catch (Exception ex)
            {
                FileOperationException = ex;
            }

        }

        /// <summary>
        /// moves a file from the current folder to the target folder
        /// </summary>
        internal static void MoveFileOrFolder(string strName)
        {
            FileOperationException = null;
            string strLocalPath = GetLocalPath(strName);
            string strTargetPath = GetTargetPath(strName);
            try
            {
                if (IsDirectory(strLocalPath))
                {
                    Directory.Move(strLocalPath, strTargetPath);
                }
                else
                {
                    File.Move(strLocalPath, strTargetPath);
                }
            }
            catch (Exception ex)
            {
                FileOperationException = ex;
            }

        }

        /// <summary>
        /// Saves the first HttpPostedFile (if there is one) to the current folder
        /// </summary>
        /// <param name="files"></param>
        internal static void SaveUploadedFile(HttpPostedFileBase[] files)
        {
            FileOperationException = null;

            if (files.Any())
            {
                foreach (HttpPostedFileBase pf in files)
                {
                    string strFilename = pf.FileName;
                    string strTargetFile = GetLocalPath(Path.GetFileName(strFilename));
                    //-- make sure we clear out any existing file before uploading
                    if (File.Exists(strTargetFile))
                    {
                        DeleteFileOrFolder(strFilename);
                    }
                    try
                    {
                        pf.SaveAs(strTargetFile);
                    }
                    catch (Exception ex)
                    {
                        FileOperationException = ex;
                    }                    
                }

            }
        }

        /// <summary>
        /// moves a file from the current folder to the target folder
        /// </summary>
        internal static void CopyFileOrFolder(string strName)
        {
            FileOperationException = null;
            string strLocalPath = GetLocalPath(strName);
            string strTargetPath = GetTargetPath(strName);
            try
            {
                if (IsDirectory(strLocalPath))
                {
                    CopyFolder(strLocalPath, strTargetPath, true);
                }
                else
                {
                    File.Copy(strLocalPath, strTargetPath);
                }
            }
            catch (Exception ex)
            {
                FileOperationException = ex;
            }

        }

        /// <summary>
        /// Compress all the selected files
        /// due to limitations of SharpZipLib, this must be done in one pass 
        /// (it cannot modify an existing zip file!)
        /// </summary>
        internal static void ZipFileOrFolder(ArrayList fileList)
        {
            var zipTargetFile = GetLocalPath(fileList.Count == 1 ? Path.ChangeExtension(Convert.ToString(fileList[0]), ".zip") : "ZipFile.zip");

            FileStream zfs = default(FileStream);
            ZipOutputStream zs = default(ZipOutputStream);
            try
            {
                zfs = File.Exists(zipTargetFile) ? File.OpenWrite(zipTargetFile) : File.Create(zipTargetFile);

                zs = new ZipOutputStream(zfs);

                //ExpandFileList(ref fileList);

                foreach (string strName in fileList)
                {
                    if (IsDirectory(GetLocalPath(strName)))
                    {
                        var path = GetLocalPath(strName);
                        int folderOffset = path.Length + (path.EndsWith("\\", StringComparison.Ordinal) ? 0 : 1);

                        CompressFolder(path, zs, folderOffset);

                    }
                    else
                    {
                        ZipEntry ze = new ZipEntry(strName) {DateTime = DateTime.UtcNow};

                        zs.PutNextEntry(ze);

                        FileStream fs = default(FileStream);
                        try
                        {
                            fs = File.OpenRead(GetLocalPath(strName));
                            byte[] buffer = new byte[2049];
                            int len = fs.Read(buffer, 0, buffer.Length);
                            while (len > 0)
                            {
                                zs.Write(buffer, 0, len);
                                len = fs.Read(buffer, 0, buffer.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            FileOperationException = ex;
                        }
                        finally
                        {
                            if (fs != null)
                                fs.Close();
                            zs.CloseEntry();
                        }                        
                    }

                }
            }
            finally
            {
                if (zs != null)
                    zs.Close();
                if (zfs != null)
                    zfs.Close();
            }
        }
        /// <summary>
        /// Recurses down the folder structure
        /// </summary>
        private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {

            string[] files = Directory.GetFiles(path);

            foreach (string filename in files)
            {

                FileInfo fi = new FileInfo(filename);

                string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName)
                {
                    DateTime = fi.LastWriteTime,
                    // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                    // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                    // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                    // but the zip will be in Zip64 format which not all utilities can understand.
                    //   zipStream.UseZip64 = UseZip64.Off;
                    Size = fi.Length
                    // Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
                    // A password on the ZipOutputStream is required if using AES.
                    // AESKeySize = 256;
                };

                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }
        /// <summary>
        /// renames a file; assumes filename is "(oldname)(renametag)(newname)"
        /// </summary>
        internal static void RenameFileOrFolder(string strName)
        {
            int intTagLoc = strName.IndexOf("rename", StringComparison.Ordinal);
            if (intTagLoc == -1)
                return;

            var strOldName = strName.Substring(0, intTagLoc);
            var strNewName = strName.Substring(intTagLoc + 6);
            if (strOldName == strNewName)
                return;

            string strOldPath = GetLocalPath(strOldName);
            string strNewPath = GetLocalPath(strNewName);
            FileOperationException = null;
            try
            {
                if (IsDirectory(strOldPath))
                {
                    Directory.Move(strOldPath, strNewPath);
                }
                else
                {
                    File.Move(strOldPath, strNewPath);
                }
            }
            catch (Exception ex)
            {
                FileOperationException = ex;
            }

        }

        /// <summary>
        /// creates a subfolder in the current folder
        /// </summary>
        internal static void MakeFolder(string strFilename)
        {
            FileOperationException = null;
            string strLocalPath = GetLocalPath(strFilename);
            try
            {
                if (!Directory.Exists(strLocalPath))
                {
                    Directory.CreateDirectory(strLocalPath);
                }
            }
            catch (Exception ex)
            {
                FileOperationException = ex;
            }

        }

        /// <summary>
        /// recursively copies a folder, and all subfolders and files, to a target path
        /// </summary>
        private static void CopyFolder(string strSourceFolderPath, string strDestinationFolderPath, bool blnOverwrite)
        {
            //-- make sure target folder exists
            if (!Directory.Exists(strDestinationFolderPath))
            {
                Directory.CreateDirectory(strDestinationFolderPath);
            }

            //-- copy all of the files in this folder to the destination folder
            foreach (string strFilePath in Directory.GetFiles(strSourceFolderPath))
            {
                string strFileName = Path.GetFileName(strFilePath);
                //-- if exception, will be caught in calling proc
                File.Copy(strFilePath, Path.Combine(strDestinationFolderPath, strFileName), blnOverwrite);
            }

            //-- copy all of the subfolders in this folder
            foreach (string strFolderPath in Directory.GetDirectories(strSourceFolderPath))
            {
                string strFolderName = Regex.Match(strFolderPath, "[^\\\\]+$").ToString();
                CopyFolder(strFolderPath, Path.Combine(strDestinationFolderPath, strFolderName), blnOverwrite);
            }
        }

        internal static string SortColumn()
        {
            if (HttpContext.Current.Request.QueryString["sort"] == null)
            {
                return "Name";
            }
            else
            {
                return HttpContext.Current.Request.QueryString["sort"];
            }
        }

        /// <summary>
        /// return partial URL to this page, optionally specifying a new target path
        /// </summary>
        public static string PageUrl(string newPath = "", string newSortColumn = "")
        {

            bool blnSortProvided = (!string.IsNullOrEmpty(newSortColumn));

            //-- if not provided, use the current values in the querystring
            if (string.IsNullOrEmpty(newPath))
                newPath = WebPath();
            if (string.IsNullOrEmpty(newSortColumn))
                newSortColumn = SortColumn();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append(VirtualPathUtility.ToAbsolute("~/WebFileManager/Index/"));
            sb.Append("?path=");
            sb.Append(newPath);
            if (!string.IsNullOrEmpty(newSortColumn))
            {
                sb.Append("&");
                sb.Append("sort");
                sb.Append("=");
                if (blnSortProvided & (newSortColumn.ToLower() == FileManager.SortColumn().ToLower()))
                {
                    sb.Append("-");
                }
                sb.Append(newSortColumn);
            }

            return sb.ToString();
        }
    }
}
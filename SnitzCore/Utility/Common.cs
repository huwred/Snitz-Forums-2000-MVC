using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SnitzCore.Extensions;
using Encoder = System.Drawing.Imaging.Encoder;


namespace SnitzCore.Utility
{
    public static class Common
    {
        /// <summary>
        /// returns the url for the forums root folder
        /// </summary>
        public static readonly string RootFolder = HttpContext.Current.Request.ApplicationPath == "/" ?
                "" : HttpContext.Current.Request.ApplicationPath;

        /// <summary>
        /// Get the users IP address from the HTTP Request
        /// </summary>
        /// <returns></returns>
        public static string GetUserIP(HttpContext context, bool getLan = false)
        {
            string visitorsIPAddr = "";
            if (context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                visitorsIPAddr = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            }
            if (string.IsNullOrEmpty(visitorsIPAddr))
                visitorsIPAddr = context.Request.ServerVariables["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(visitorsIPAddr))
                visitorsIPAddr = context.Request.UserHostAddress;

            if (string.IsNullOrEmpty(visitorsIPAddr) || visitorsIPAddr.Trim() == "::1")
            {
                getLan = true;
                visitorsIPAddr = string.Empty;
            }
            if (getLan && string.IsNullOrEmpty(visitorsIPAddr))
            {
                //This is for Local(LAN) Connected ID Address
                string stringHostName = Dns.GetHostName();
                //Get Ip Host Entry
                IPHostEntry ipHostEntries = Dns.GetHostEntry(stringHostName);
                //Get Ip Address From The Ip Host Entry Address List
                IPAddress[] arrIpAddress = ipHostEntries.AddressList;

                try
                {
                    visitorsIPAddr = arrIpAddress[arrIpAddress.Length - 1].ToString();
                }
                catch
                {
                    try
                    {
                        visitorsIPAddr = arrIpAddress[0].ToString();
                    }
                    catch
                    {
                        try
                        {
                            arrIpAddress = Dns.GetHostAddresses(stringHostName);
                            visitorsIPAddr = arrIpAddress[0].ToString();
                        }
                        catch
                        {
                            visitorsIPAddr = "127.0.0.1";
                        }
                    }
                }

            }
            return visitorsIPAddr;
        }

        public static void Trackit(int currentUserId, int topicid, int pagenum)
        {
            //throw new NotImplementedException();
        }
        /// <summary>
        /// Gets the countries supported.
        /// </summary>
        /// <returns>The countries.</returns>
        public static IEnumerable<SelectListItem> GetCountries()
        {
            //create a new Generic list to hold the country names returned
            List<SelectListItem> cultureList = new List<SelectListItem>();

            //create an array of CultureInfo to hold all the cultures found, these include the users local cluture, and all the
            //cultures installed with the .Net Framework
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            //loop through all the cultures found
            foreach (CultureInfo culture in cultures)
            {
                //pass the current culture's Locale ID (http://msdn.microsoft.com/en-us/library/0h88fahh.aspx)
                //to the RegionInfo contructor to gain access to the information for that culture
                RegionInfo region = new RegionInfo(culture.Name);
                cultureList.Add(new SelectListItem { Text = region.DisplayName, Value = region.DisplayName });
            }
            IEnumerable<SelectListItem> nameAdded = cultureList.GroupBy(x => x.Text).Select(x => x.FirstOrDefault()).ToList<SelectListItem>().OrderBy(x => x.Text);
            return nameAdded;
        }
        /// <summary>
        /// Gets the countries supported in local culture.
        /// </summary>
        /// <returns>The countries.</returns>
        /// <param name="curculture">Curculture.</param>
        public static object GetCountries(CultureInfo curculture)
        {
            RegionInfo country = new RegionInfo(curculture.LCID);
            List<SelectListItem> countryNames = new List<SelectListItem>();

            foreach (CultureInfo cul in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                country = new RegionInfo(new CultureInfo(cul.Name, false).LCID);
                countryNames.Add(new SelectListItem { Text = country.DisplayName, Value = country.DisplayName });
            }
            IEnumerable<SelectListItem> nameAdded = countryNames.GroupBy(x => x.Text).Select(x => x.FirstOrDefault()).ToList<SelectListItem>().OrderBy(x => x.Text);
            return nameAdded;
        }

        private delegate IPHostEntry GetHostEntryHandler(string ip);

        public static string GetReverseDNS(string ip, int timeout)
        {
            try
            {
                GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
                IAsyncResult result = callback.BeginInvoke(ip, null, null);
                if (result.AsyncWaitHandle.WaitOne(timeout * 1000, false))
                {
                    return callback.EndInvoke(result).HostName;
                }
                return ip;
            }
            catch (Exception)
            {
                return ip;
            }
        }

        public static RouteData GetReferrRouteData(string uri)
        {
            // Split the url to url + query string
            var fullUrl = uri;
            var questionMarkIndex = fullUrl.IndexOf('?');
            string queryString = null;
            string url = fullUrl;
            if (questionMarkIndex != -1) // There is a QueryString
            {
                url = fullUrl.Substring(0, questionMarkIndex);
                queryString = fullUrl.Substring(questionMarkIndex + 1);
            }

            // Arranges
            var request = new HttpRequest(null, url, queryString);
            var response = new HttpResponse(new StringWriter());
            var httpContext = new HttpContext(request, response);

            var routeData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(httpContext));

            return routeData;

        }

        /// <summary>
        /// SHA256s the password. Old Snitz password format
        /// </summary>
        /// <returns>The 256 hash.</returns>
        /// <param name="password">Password.</param>
        public static string SHA256Hash(string password)
        {
            SHA256 sha = new SHA256Managed();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(password));

            var stringBuilder = new StringBuilder();
            foreach (byte b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            return stringBuilder.ToString();
        }
        /// <summary>
        /// Hashes the password. New password store
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="password">Password.</param>
        /// <param name="passwordSalt">Password salt.</param>
        public static string HashPassword(string password, string passwordSalt)
        {
            const int saltSize = 128 / 8; //128 bits = 16 bytes
            const int PBKDF2SubkeyLength = 256 / 8; //256 bits = 32 bytes
            byte[] salt;
            byte[] subkey;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltSize, 1000))
            {
                salt = deriveBytes.Salt;
                subkey = deriveBytes.GetBytes(PBKDF2SubkeyLength);
            }

            byte[] outputBytes = new byte[1 + saltSize + PBKDF2SubkeyLength];
            Buffer.BlockCopy(salt, 0, outputBytes, 1, saltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 1 + saltSize, PBKDF2SubkeyLength);
            return Convert.ToBase64String(outputBytes);
        }

        /// <summary>
        /// Get the image format passed on file extension.
        /// </summary>
        /// <returns>The format.</returns>
        /// <param name="filePath">File path.</param>
        public static ImageFormat ImageFormat(string filePath)
        {
            ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;
            string ext = Path.GetExtension(filePath).Replace(".", "").ToLower();
            switch (ext)
            {
                case "bmp":
                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                case "gif":
                    format = System.Drawing.Imaging.ImageFormat.Gif;
                    break;
                case "png":
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    break;
                default:
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
            }
            return format;
        }

        private static Random random = new Random();

        /// <summary>
        /// Generates a random character string
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="length">Length.</param>
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static bool AssemblyLoaded(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = (from a in assemblies
                            where a.FullName == name
                            select a).SingleOrDefault();
            if (assembly != null)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Saves the cropped image.
        /// </summary>
        /// <returns><c>true</c>, if cropped image was saved, <c>false</c> otherwise.</returns>
        /// <param name="image">Image.</param>
        /// <param name="targetWidth">Target width.</param>
        /// <param name="targetHeight">Target height.</param>
        /// <param name="filePath">File path.</param>
        public static bool SaveCroppedImage(Image image, int targetWidth, int targetHeight, string filePath)
        {
            ImageCodecInfo pngInfo =
                ImageCodecInfo.GetImageEncoders().First(codecInfo => codecInfo.MimeType == "image/png");
            Image finalImage = image;
            ImageHelper.RotateImageByExifOrientationData(finalImage);
            System.Drawing.Bitmap bitmap = null;
            try
            {
                int left = 0;
                int top = 0;
                int srcWidth = targetWidth;
                int srcHeight = targetHeight;
                bitmap = new System.Drawing.Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
                bitmap.MakeTransparent();
                double croppedHeightToWidth = (double)targetHeight / targetWidth;
                double croppedWidthToHeight = (double)targetWidth / targetHeight;

                if (image.Width > image.Height)
                {
                    srcWidth = (int)(Math.Round(image.Height * croppedWidthToHeight));
                    if (srcWidth < image.Width)
                    {
                        srcHeight = image.Height;
                        left = (image.Width - srcWidth) / 2;
                    }
                    else
                    {
                        srcHeight = (int)Math.Round(image.Height * ((double)image.Width / srcWidth));
                        srcWidth = image.Width;
                        top = (image.Height - srcHeight) / 2;
                    }
                }
                else
                {
                    srcHeight = (int)(Math.Round(image.Width * croppedHeightToWidth));
                    if (srcHeight < image.Height)
                    {
                        srcWidth = image.Width;
                        top = (image.Height - srcHeight) / 2;
                    }
                    else
                    {
                        srcWidth = (int)Math.Round(image.Width * ((double)image.Height / srcHeight));
                        srcHeight = image.Height;
                        left = (image.Width - srcWidth) / 2;
                    }
                }
                bitmap.MakeTransparent();
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        new Rectangle(left, top, srcWidth, srcHeight), GraphicsUnit.Pixel);
                }
                bitmap.MakeTransparent();
                finalImage = bitmap;
            }
            catch
            {
            }
            try
            {
                using (EncoderParameters encParams = new EncoderParameters(1))
                {
                    encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)60);
                    //quality should be in the range [0..100] .. 100 for max, 0 for min (0 best compression)
                    finalImage.Save(filePath, pngInfo, encParams);
                    return true;
                }
            }
            catch
            {
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
                if (finalImage != null)
                {
                    finalImage.Dispose();
                }
            }
            return false;
        }

    }
}
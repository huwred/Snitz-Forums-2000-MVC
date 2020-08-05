using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace Snitz.Base{

    public static class Extras
    {
        public enum TablePrefixTypes
        {
            None,
            Forum,
            Member,
            Filter,
            Custom
        }

        /// <summary>
        /// converts stored Snitz date string to Utc DateTime
        /// </summary>
        /// <param name="forumdate"></param>
        /// <returns>forum date in UTC time</returns>
        public static DateTime ToSnitzDateTime(this string forumdate)
        {
            DateTime utcdate = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            CultureInfo ci = CultureInfo.CreateSpecificCulture("en-GB");
            if (string.IsNullOrWhiteSpace(forumdate))
            {
                return utcdate;
            }
            //pad the forumdate incase we are converting the DOB then trim incase it is an image timestamp
            try
            {
                //var origdate = DateTime.ParseExact(forumdate.PadRight(14, '0').Substring(0,14), "yyyyMMddHHmmss", ci);
                //origdate = origdate.AddHours(ClassicConfig.ForumServerOffset*-1);
                utcdate = DateTime.SpecifyKind(DateTime.ParseExact(forumdate.PadRight(14, '0').Substring(0, 14), "yyyyMMddHHmmss", ci), DateTimeKind.Utc);
                //var utcoffset = TimeZone.CurrentTimeZone.GetUtcOffset(origdate).Hours;
                //utcdate = origdate.AddHours(utcoffset * -1);
            }
            catch (Exception)
            {
                throw new Exception("error parsing date :" + forumdate);
            }

            return utcdate;


        }

        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        public static Image CropImage(Image image, int maxWidth, int maxHeight)
        {
            Bitmap bitmap = null;

            try
            {
                int left = 0;
                int top = 0;
                int targetWidth = maxWidth;
                int srcWidth;
                int targetHeight = maxHeight;
                int srcHeight;
                bitmap = new Bitmap(maxWidth, maxHeight);
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
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        new Rectangle(left, top, srcWidth, srcHeight), GraphicsUnit.Pixel);
                }
            }
            catch
            {
                // ignored
            }

            return bitmap;
        }

        public static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

    }
}


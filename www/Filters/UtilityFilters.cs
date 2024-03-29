﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace WWW.Filters
{
    #region compression Filter
    public class CompressContentAttribute : ActionFilterAttribute
    {

        /// <summary>
        /// Override to compress the content that is generated by
        /// an action method.
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            GZipEncodePage();
        }

        /// <summary>
        /// Determines if GZip is supported
        /// </summary>
        /// <returns></returns>
        public static bool IsGZipSupported()
        {
            string AcceptEncoding = HttpContext.Current.Request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(AcceptEncoding) &&
                    (AcceptEncoding.Contains("gzip") || AcceptEncoding.Contains("deflate")))
                return true;
            return false;
        }

        /// <summary>
        /// Sets up the current page or handler to use GZip through a Response.Filter
        /// IMPORTANT:  
        /// You have to call this method before any output is generated!
        /// </summary>
        public static void GZipEncodePage()
        {
            HttpResponse Response = HttpContext.Current.Response;

            if (IsGZipSupported())
            {
                string AcceptEncoding = HttpContext.Current.Request.Headers["Accept-Encoding"];

                if (AcceptEncoding.Contains("gzip"))
                {
                    Response.Filter = new System.IO.Compression.GZipStream(Response.Filter,
                                                System.IO.Compression.CompressionMode.Compress);
                    Response.Headers.Remove("Content-Encoding");
                    Response.AppendHeader("Content-Encoding", "gzip");
                }
                else
                {
                    Response.Filter = new System.IO.Compression.DeflateStream(Response.Filter,
                                                System.IO.Compression.CompressionMode.Compress);
                    Response.Headers.Remove("Content-Encoding");
                    Response.AppendHeader("Content-Encoding", "deflate");
                }


            }

            // Allow proxy servers to cache encoded and unencoded versions separately
            Response.AppendHeader("Vary", "Content-Encoding");
        }
    }
    #endregion

    #region Removes Empty lines Filter
    public class EmptyLineFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var response = filterContext.HttpContext.Response;

            // If it's a sitemap, just return.
            if (filterContext.HttpContext.Request.RawUrl == "/sitemap.xml") return;

            if (response.ContentType != "text/html" || response.Filter == null) return;

            response.Filter = new HelperClass(response.Filter);
        }

        private class HelperClass : Stream
        {
            private readonly Stream _base;
            StringBuilder _s = new StringBuilder();

            public HelperClass(Stream responseStream)
            {
                if (responseStream == null)
                    throw new ArgumentNullException("responseStream");
                _base = responseStream;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var html = Encoding.UTF8.GetString(buffer, offset, count);
                //var reg = new Regex(@"(?<=\s)\s+(?![^<>]*</pre>)");
                var reg = new Regex(@"^\s*$");
                html = reg.Replace(html, Environment.NewLine);

                buffer = Encoding.UTF8.GetBytes(html);
                _base.Write(buffer, 0, buffer.Length);
            }

            #region Other Members

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead { get { return false; } }

            public override bool CanSeek { get { return false; } }

            public override bool CanWrite { get { return true; } }

            public override long Length { get { throw new NotSupportedException(); } }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override void Flush()
            {
                _base.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            #endregion
        }
    }
    #endregion

    #region ETag Filter
    public class ETagAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Filter = new ETagFilter(filterContext.HttpContext.Response, filterContext.RequestContext.HttpContext.Request);
        }
    }

    public class ETagFilter : MemoryStream
    {
        private HttpResponseBase _response = null;
        private HttpRequestBase _request;
        private Stream _filter = null;

        public ETagFilter(HttpResponseBase response, HttpRequestBase request)
        {
            _response = response;
            _request = request;
            _filter = response.Filter;
        }

        private string GetToken(Stream stream)
        {
            var checksum = new byte[0];
            checksum = MD5.Create().ComputeHash(stream);
            return Convert.ToBase64String(checksum, 0, checksum.Length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = new byte[count];

            Buffer.BlockCopy(buffer, offset, data, 0, count);

            var token = GetToken(new MemoryStream(data));
            var clientToken = _request.Headers["If-None-Match"];

            if (token != clientToken)
            {
                _response.AddHeader("ETag", token);
                _filter.Write(data, 0, count);
            }
            else
            {
                _response.SuppressContent = true;
                _response.StatusCode = 304;
                _response.StatusDescription = "Not Modified";
                _response.AddHeader("Content-Length", "0");
            }
        }
    }
    #endregion

}
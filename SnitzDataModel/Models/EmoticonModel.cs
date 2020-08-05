using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Xml.Linq;
using SnitzCore.Utility;

namespace SnitzDataModel.Models
{

    public class Emoticon
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Image { get; set; }

        public string Display
        {
            get
            {

                return String.Format("<img data-toggle='tooltip' title='{0}' data-src='{1}/Content/images/emoticon/{2}' alt='{3}' rel='{4}' class='lazyload emote'/>", this.Name + " " + this.Code, Common.RootFolder, this.Image, this.Name, this.Code);
            }
        }

        /// <summary>
        /// Fetches a list of forum emoticons
        /// </summary>
        /// <returns></returns>
        public static List<Emoticon> Emoticons()
        {
            List<Emoticon> emoticonlist = new List<Emoticon>();
            //open the emoticon xml file from the "~/App_data folder
            string appdata = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\emoticons.xml");
            XElement emoticons = XElement.Load(appdata);
            IEnumerable<XElement> childList =
                from el in emoticons.Elements()
                select el;
            foreach (XElement el in childList)
            {
                Emoticon emote = new Emoticon
                {
                    Code = el.Attribute("code").Value,
                    Image = el.Attribute("image").Value,
                    Name = el.Attribute("name").Value
                };
                
                emoticonlist.Add(emote);
            }
            return emoticonlist.Distinct(new EmoticonComparer()).ToList();
        }

    }
    class EmoticonComparer : IEqualityComparer<Emoticon>
    {
        public bool Equals(Emoticon x, Emoticon y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(Emoticon obj)
        {
            return obj.Name.GetHashCode();
        }

    }
}
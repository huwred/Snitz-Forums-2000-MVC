// /*
// ####################################################################################################################
// ##
// ## AdRotator
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml.Serialization;
using SnitzDataModel.Models;

namespace SnitzDataModel
{
    public static class RandomGen2
    {
        private static Random _global = new Random();
        [ThreadStatic]
        private static Random _local;

        public static int Next()
        {
            Random inst = _local;
            if (inst == null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }
            return inst.Next();
        }

        public static int Next(int i, int totalWeight)
        {
            Random inst = _local;
            if (inst == null)
            {
                int seed;
                lock (_global) seed = _global.Next(i, totalWeight);
                _local = inst = new Random(seed);
            }
            return inst.Next(i, totalWeight);
        }
    }

    public static class AdRotator
    {

        static string path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data/adrotator.xml");
      
        static AdCollection _ads = null;
        private static IEnumerable<Ad> _adBanners = null;
        public static AdCollection GetAds(HttpContext context)
        {
            if (context.Cache["AdBanners"] == null)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AdCollection));
                using (var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    StreamReader reader = new StreamReader(inStream);
                    _ads = (AdCollection)serializer.Deserialize(reader);
                    reader.Close();
                    context.Cache.Insert("AdBanners", _ads,
                        new System.Web.Caching.CacheDependency(path));
                }



            }
            else
            {
                _ads = (AdCollection)context.Cache["AdBanners"];
            }

            _adBanners = _ads.Ads;
            return _ads;
        }

        public static Ad GetAd(IEnumerable<Ad> ads, int totalWeight)
        {
            // totalWeight is the sum of all brokers' weight

            int randomNumber = RandomGen2.Next(0, totalWeight);

            Ad selectedAd = null;
            foreach (Ad ad in ads)
            {
                if (randomNumber < ad.Weight)
                {
                    selectedAd = ad;
                    break;
                }

                randomNumber = randomNumber - ad.Weight;
            }

            return selectedAd;
        }

        public static void Save()
        {
            XmlSerializer x = new XmlSerializer(typeof(AdCollection));
            using (var outStream = new FileStream(path, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
            {
                TextWriter writer = new StreamWriter(outStream);
                x.Serialize(writer, _ads);
                writer.Close();
            }

        }

        public static void Add(Ad selectedAd)
        {
            _adBanners = _adBanners.Concat(new[] { selectedAd });
            _ads.Ads = _adBanners.ToArray();
            Save();
        }

        public static void Delete(string id)
        {
            var modified = _adBanners.Where(u => u.Id.ToString() != id).ToList();
            _ads.Ads = modified.ToArray();
            Save();
        }
    }
}

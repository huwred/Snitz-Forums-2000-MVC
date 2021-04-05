using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using LangResources.Abstract;
using LangResources.Concrete;
using LangResources.Models;
using Snitz.Base;

namespace LangResources.Utility
{
    public static class ResourceManager
    {
        private static bool RunningSetup
        {
            get
            {
                if (ConfigurationManager.AppSettings["boolRunSetup"] == "1")
                    return true;
                return false;

            }
        }
        private static IResourceProvider provider = new DbResourceProvider(); //new XmlResourceProvider(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\Resources.xml")); 

        /// <summary>
        /// Get the resource using the current culture
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetLocalisedString(string name)
        {
            if (RunningSetup)
                return name;

            return provider.GetResource(name, lang()) as String;
        }

        /// <summary>
        /// Get the resource using the current culture
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resourceset"></param>
        /// <returns></returns>
        public static string GetLocalisedString(string name, string resourceset)
        {
            if (RunningSetup)
                return name;
            return provider.GetResource(name, lang(), resourceset) as String;
        }

        /// <summary>
        /// Gets the string for a specific culture
        /// </summary>
        /// <param name="name"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string Get(string name, string culture)
        {
            return provider.GetResource(name, culture) as String;
        }
        public static ResourceEntry GetResource(string name, string culture, string resourceset)
        {
            return provider.GetResourceEntry(name, culture, resourceset) as ResourceEntry;
        }

        public static void Add(string name, string value)
        {
            provider.AddResource(name, value);
        }

        public static void Add(string name, string value, string culture)
        {
            provider.AddResource(name, value, culture);
        }
        public static void Add(string name, string value, string culture, string resourceset)
        {
            if (resourceset == null)
            {
                provider.AddResource(name, value, culture);
            }
            else
            {
                provider.AddResource(name, value, culture, resourceset);
            }

        }
        public static void Update(ResourceEntry res)
        {
            provider.UpdateResource(res.Id, res.Name, res.Value, res.Culture, res.ResourceSet);
        }
        public static string GetKey(string name)
        {
            if (RunningSetup)
                return name;
            return provider.GetResourceKey(name, lang()) as String;
        }

        public static List<ResourceEntry> ReadResources(string resourceset)
        {
            return provider.ReadAllResources(resourceset);
        }
        public static IEnumerable<IGrouping<string, ResourceEntry>> ReadResources()
        {
            return provider.ReadAllResources();
        }
        public static List<ResourceEntry> ReadLangResources(string culture)
        {
            return provider.ReadLangResources(culture);
        }
        public static List<string> ReadResourceSets()
        {
            return provider.GetResourceSets();
        }
        public static object GetLocalisedStrings(string name, string resourceset)
        {
            if (RunningSetup)
                return name;
            return provider.GetResource(name, lang(), resourceset) as String;
        }

        public static void Delete(string set, string name)
        {
            provider.DeleteResource(set, name);
        }
        public static void Rename(string old, string name, string set)
        {
            provider.RenameResource(old, name, set);
        }
        public static void Delete(int id)
        {
            provider.DeleteResource(id);
        }

        private static string lang()
        {
            if (RunningSetup)
                return "en";
            var langName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (langName == "nn" || langName == "nb")
            {
                langName = "no";
            }
            return IsSupported(langName);
        }

        public static string IsSupported(string langName)
        {
            return SupportLanguages().Contains(langName) ? langName : "en";
        }

        public static string[] SupportLanguages()
        {
            var cacheService = new InMemoryCache(60*24) { DoNotExpire = true };
            return cacheService.GetOrSet("lang.cultures", () => provider.GetCultures().ToArray());
            //var cultures = provider.GetCultures().ToArray();
            //Array.Sort(cultures);
            //return cultures;
        }


        public static void Reset()
        {
            provider.Reset();
        }
    }
}

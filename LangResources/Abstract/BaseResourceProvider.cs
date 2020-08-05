using System;
using System.Collections.Generic;
using System.Linq;
using LangResources.Models;
using Snitz.Base;

namespace LangResources.Abstract
{
    public abstract class BaseResourceProvider : IResourceProvider
    {
        // Cache list of resources
        private static Dictionary<string, ResourceEntry> _resources = null;
        private static object lockResources = new object();

        public BaseResourceProvider()
        {
            Cache = true; // By default, enable caching for performance
        }

        protected bool Cache { get; set; } // Cache resources ?
        protected Dictionary<string, ResourceEntry> Resources { get { return _resources; } set { _resources = value; } } // Cache resources ?

        public void Reset()
        {
            new InMemoryCache().Remove("language.strings");
            _resources = null;
        }
        /// <summary>
        /// Returns a single resource for a specific culture
        /// </summary>
        /// <param name="name">Resorce name (ie key)</param>
        /// <param name="culture">Culture code</param>
        /// <returns>Resource</returns>
        public object GetResource(string name, string culture)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(culture))
                throw new ArgumentException("Culture name cannot be null or empty.");

            // normalize
            culture = culture.ToLowerInvariant();

            if (Cache && _resources == null)
            {
                // Fetch all resources

                lock (lockResources)
                {

                    if (_resources == null)
                    {

                        _resources = new InMemoryCache(60).GetOrSet("language.strings", () => ReadResources().ToDictionary(r => string.Format("{0}.{1}", r.Culture.ToLowerInvariant(), r.Name)));

                    }
                }
            }

            if (Cache)
            {
                ResourceEntry res;
                if (_resources.TryGetValue(string.Format("{0}.{1}", culture, name), out res))
                {
                    return res.Value;
                }
                return "!*" + string.Format("{0}.{1}", culture, name) + "*!";
            }

            return ReadResource(name, culture).Value;

        }

        /// <summary>
        /// Returns a single resource for a specific culture
        /// </summary>
        /// <param name="name">Resorce name (ie key)</param>
        /// <param name="culture">Culture code</param>
        /// <param name="resourceset"></param>
        /// <returns>Resource</returns>
        public object GetResource(string name, string culture, string resourceset)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(culture))
                throw new ArgumentException("Culture name cannot be null or empty.");

            // normalize
            culture = culture.ToLowerInvariant();

            if (Cache && _resources == null)
            {
                // Fetch all resources
                lock (lockResources)
                {

                    if (_resources == null)
                    {
                        _resources = new InMemoryCache(60).GetOrSet("language.strings", () => ReadResources().ToDictionary(r => string.Format("{0}.{1}", r.Culture.ToLowerInvariant(), r.Name)));

                    }
                }
            }

            if (Cache)
            {
                ResourceEntry res;
                if (_resources.TryGetValue(string.Format("{0}.{1}", culture, name), out res))
                {
                    if (!String.IsNullOrWhiteSpace(res.Value))
                        return res.Value;
                }
                return "!*" + string.Format("{0}_{1}.{2}", culture, resourceset, name) + "*!";

            }

            return ReadResource(name, culture).Value;

        }

        public ResourceEntry GetResourceEntry(string name, string culture, string resourceset)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(culture))
                throw new ArgumentException("Culture name cannot be null or empty.");

            // normalize
            culture = culture.ToLowerInvariant();


            return ReadResource(name, culture);

        }
        public object GetResourceKey(string name, string culture)
        {
            return GetKey(name, culture);
        }


        public void AddResource(string name, string value)
        {
            AddResource(new ResourceEntry() { Name = name, Value = value, Culture = "en" });
            new InMemoryCache().Remove("language.strings");

        }

        public void AddResource(string name, string value, string culture)
        {
            AddResource(new ResourceEntry() { Name = name, Value = value, Culture = culture });
            new InMemoryCache().Remove("language.strings");
        }
        public void AddResource(string name, string value, string culture, string resourceset)
        {
            AddResource(new ResourceEntry() { Name = name, Value = value, Culture = culture, ResourceSet = resourceset });
            new InMemoryCache().Remove("language.strings");
        }

        public void UpdateResource(int id, string name, string value, string culture, string resourceset)
        {
            UpdateResource(new ResourceEntry() { Id = id, Name = name, Value = value, Culture = culture, ResourceSet = resourceset });
            new InMemoryCache().Remove("language.strings");
        }

        public void DeleteResource(int id)
        {
            DeleteResourceValue(id);
            new InMemoryCache().Remove("language.strings");
        }

        public void DeleteResource(string set, string name)
        {
            DeleteResources(set, name);
            new InMemoryCache().Remove("language.strings");
        }
        public void RenameResource(string old, string name, string set)
        {
            RenameResources(name, old, set);
            new InMemoryCache().Remove("language.strings");
        }
        public List<string> GetResourceSets()
        {
            return new List<string>(ReadResources().Select(r => r.ResourceSet).Distinct());

        }

        public List<ResourceEntry> ReadAllResources(string resourceset)
        {
            return new List<ResourceEntry>(ReadResources(resourceset));
        }

        public List<ResourceEntry> ReadLangResources(string culture)
        {
            return new List<ResourceEntry>(ReadLanguageResources(culture));
        }


        public IEnumerable<IGrouping<string, ResourceEntry>> ReadAllResources()
        {
            var test = ReadResources().GroupBy(c => c.Name);
            return test;
        }

        public abstract string GetCulture(string langName);
        public abstract IEnumerable<string> GetCultures();
        protected abstract string GetKey(string value, string culture);
        protected abstract IEnumerable<ResourceEntry> ReadLanguageResources(string culture);

        /// <summary>
        /// Returns all resources for all cultures. (Needed for caching)
        /// </summary>
        /// <returns>A list of resources</returns>
        protected abstract IEnumerable<ResourceEntry> ReadResources();

        /// <summary>
        /// Returns all resources for all cultures. (Needed for caching)
        /// </summary>
        /// <returns>A list of resources</returns>
        protected abstract IEnumerable<ResourceEntry> ReadResources(string resourceset);

        /// <summary>
        /// Returns a single resource for a specific culture
        /// </summary>
        /// <param name="name">Resorce name (ie key)</param>
        /// <param name="culture">Culture code</param>
        /// <returns>Resource</returns>
        protected abstract ResourceEntry ReadResource(string name, string culture);

        /// <summary>
        /// Add a new resource string
        /// </summary>
        /// <param name="resource"></param>
        protected abstract void AddResource(ResourceEntry resource);
        protected abstract void DeleteResources(string set, string name);
        protected abstract void DeleteResourceValue(int id);
        /// <summary>
        /// Update a resource
        /// </summary>
        /// <param name="resource"></param>
        protected abstract void UpdateResource(ResourceEntry resource);

        protected abstract void RenameResources(string newname, string oldname, string set);

    }
}


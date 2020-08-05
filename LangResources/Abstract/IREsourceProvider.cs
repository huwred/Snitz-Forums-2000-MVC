using System.Collections.Generic;
using System.Linq;
using LangResources.Models;

namespace LangResources.Abstract
{
    public interface IResourceProvider
    {
        object GetResource(string name, string culture);
        object GetResource(string name, string culture, string resourceset);
        ResourceEntry GetResourceEntry(string name, string culture, string resourceset);
        object GetResourceKey(string name, string culture);
        //object GetKey(string value, string culture);
        void AddResource(string name, string value);
        void AddResource(string name, string value, string culture);
        void AddResource(string name, string value, string culture, string resourceset);
        void UpdateResource(int id, string name, string value, string culture, string resourceset);
        void DeleteResource(int id);
        void DeleteResource(string set, string name);
        List<string> GetResourceSets();
        List<ResourceEntry> ReadAllResources(string resourceset);
        List<ResourceEntry> ReadLangResources(string culture);
        IEnumerable<IGrouping<string, ResourceEntry>> ReadAllResources();
        string GetCulture(string langName);
        IEnumerable<string> GetCultures();

        void RenameResource(string old, string name, string set);
        void Reset();
    }
}

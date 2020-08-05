using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using LangResources.Abstract;
using LangResources.Models;
using PetaPoco;
using Snitz.Base;

namespace LangResources.Concrete
{
    public class DbResourceProvider : BaseResourceProvider
    {
        // Database connection string        
        private static string _connectionString;

        public DbResourceProvider()
        {

            _connectionString = ConfigurationManager.AppSettings["LanguageConnectionString"];
        }

        public DbResourceProvider(string connection)
        {
            _connectionString = connection;
        }

        public override string GetCulture(string langName)
        {
            using (var db = new Database(_connectionString))
            {
                int num = db.ExecuteScalar<int>("select count(pk) from LANGUAGE_RES where culture=@0", langName);
                return num > 0 ? langName : "en";
            }
        }

        public override IEnumerable<string> GetCultures()
        {
            using (var db = new Database(_connectionString))
            {
                return db.Fetch<string>("SELECT DISTINCT Culture FROM LANGUAGE_RES");
            }
        }

        protected override string GetKey(string value, string culture)
        {
            using (var db = new Database(_connectionString))
            {
                var name = db.SingleOrDefault<string>(
                    "select Value from LANGUAGE_RES where culture = @0 and ResourceId = @1;", culture, value);
                return name;
            }


        }

        protected override IEnumerable<ResourceEntry> ReadResources()
        {
            using (var db = new Database(_connectionString))
            {
                var resources =
                    db.Query<ResourceEntry>("select pk,  ResourceId, Value, Culture, ResourceSet from LANGUAGE_RES ORDER BY ResourceId, Culture");

                return resources;
            }
        }
        protected override IEnumerable<ResourceEntry> ReadLanguageResources(string culture)
        {
            using (var db = new Database(_connectionString))
            {
                List<ResourceEntry> resources =
                    db.Query<ResourceEntry>(
                        "select pk, Culture, ResourceId, Value, ResourceSet from LANGUAGE_RES WHERE Culture=@0 ORDER BY ResourceSet",
                        culture).ToList();

                return resources;
            }
        }
        protected override IEnumerable<ResourceEntry> ReadResources(string resourceset)
        {
            string sql = "select pk , Culture, ResourceId , Value, ResourceSet from LANGUAGE_RES WHERE ResourceSet='" + resourceset + "';";
            if (resourceset == "")
                sql = "select pk , Culture, ResourceId, Value, ResourceSet from LANGUAGE_RES WHERE ResourceSet IS NULL;";

            using (var db = new Database(_connectionString))
            {
                List<ResourceEntry> resources = db.Fetch<ResourceEntry>(sql);


                return resources;
            }
        }

        protected override ResourceEntry ReadResource(string name, string culture)
        {
            var cacheService = new InMemoryCache();

            const string sql = "select pk,Culture, ResourceId, Value, ResourceSet from LANGUAGE_RES where culture = @0 and ResourceId = @1;";
            using (var db = new Database(_connectionString))
            {

                var resource = db.SingleOrDefault<ResourceEntry>(sql, culture, name);

                return resource;
            }

        }

        protected override void AddResource(ResourceEntry resource)
        {
            using (var db = new Database(_connectionString))
            {
                db.Save("LANGUAGE_RES", "pk", resource);
                Resources = null;
            }
        }

        protected override void DeleteResources(string set, string name)
        {
            using (var db = new Database(_connectionString))
            {
                const string sql = "DELETE FROM LANGUAGE_RES WHERE ResourceSet=@0 AND ResourceId=@1";
                db.Execute(sql, (object)set ?? DBNull.Value, name);

                Resources = null;
            }
        }

        protected override void DeleteResourceValue(int id)
        {
            using (var db = new Database(_connectionString))
            {
                string sql = "DELETE FROM LANGUAGE_RES WHERE pk=" + id;
                db.Delete(sql);

                Resources = null;
            }
        }

        protected override void UpdateResource(ResourceEntry resource)
        {
            try
            {
                using (var db = new Database(_connectionString))
                {
                    db.Save("LANGUAGE_RES", "pk", resource);
                }
            }
            finally { Resources = null; }



        }

        protected override void RenameResources(string newname, string oldname, string set)
        {
            using (var db = new Database(_connectionString))
            {
                Sql sql = new Sql("UPDATE LANGUAGE_RES SET ResourceId=@0 WHERE ResourceId=@1 AND ResourceSet=@2", newname, oldname, set);
                db.Execute(sql);

                Resources = null;
            }
        }
    }
}

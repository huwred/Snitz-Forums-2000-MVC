using System;
using System.Configuration;
using System.IO;
using System.Web.Hosting;
using System.Web.Compilation;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;
using SnitzConfig;
using SnitzDataModel;
using WWW.Helpers;


[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(WWW.PreApplicationInit), "Initialize")]
[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(WWW.PreApplicationInit), "Start")]

namespace WWW
{
    public static class PreApplicationInit
    {
        private static readonly ILogger _logger = new Logger();

        static PreApplicationInit()
        {
            #region check for db update

            //if there is an xml file that matches the assembly version then run it
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var apppath = HostingEnvironment.MapPath("~/App_Data/" + version + ".xml");
            if (File.Exists(apppath))
            {
                var id = Guid.NewGuid().ToString();
                var provider = ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ProviderName;
                var dbtype = "mssql";
                if (provider.StartsWith("MySql", StringComparison.Ordinal))
                    dbtype = "mysql";
                var dbsProcessor = new DbsFileProcessor(apppath) { _dbType = dbtype };
                try
                {
                    if (!dbsProcessor.Applied)
                    {
                        dbsProcessor.Add(id);
                        dbsProcessor.Process(id);
                    }

                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
            #endregion

            #region update plugins
            //check for plugin updates
            try
            {
                var path = HostingEnvironment.MapPath("~/plugins");
                if (path != null) PluginFolder = new DirectoryInfo(path);
                if (PluginFolder.Exists)
                {
                    var pluginUpdates = PluginFolder.GetFiles("*.new", SearchOption.AllDirectories);
                    foreach (var pluginUpdate in pluginUpdates)
                    {
                        File.Move(pluginUpdate.FullName.Replace(".new", ".dll"), pluginUpdate.FullName.Replace(".new", ".old"));
                        File.Move(pluginUpdate.FullName, pluginUpdate.FullName.Replace(".new", ".dll"));
                        try
                        {
                            File.Delete(pluginUpdate.FullName.Replace(".new", ".old"));
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }

            ////check for api update
            //path = HostingEnvironment.MapPath("~/Api");
            //if (path != null) PluginFolder = new DirectoryInfo(path);
            //pluginUpdates = PluginFolder.GetFiles("*.new", SearchOption.AllDirectories);
            //foreach (var pluginUpdate in pluginUpdates)
            //{
            //    File.Move(pluginUpdate.FullName.Replace(".new", ".dll"), pluginUpdate.FullName.Replace(".new", ".old"));
            //    File.Move(pluginUpdate.FullName, pluginUpdate.FullName.Replace(".new", ".dll"));
            //    try
            //    {
            //        File.Delete(pluginUpdate.FullName.Replace(".new", ".old"));
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.Error(ex);
            //    }

            //}
            #endregion
        }

        /// <summary>
        /// The source plugin folder from which to shadow copy from
        /// </summary>
        /// <remarks>
        /// This folder can contain sub folderst to organize plugin types
        /// </remarks>
        private static readonly DirectoryInfo PluginFolder;

        /// <summary>
        /// Lod plugin dll's
        /// </summary>
        public static void Initialize()
        {
            if (!Config.RunSetup && PluginFolder.Exists)
            {
                try
                {
                    var pluginAssemblyFiles = PluginFolder.GetFiles("*.dll", SearchOption.AllDirectories);

                    foreach (var pluginAssemblyFile in pluginAssemblyFiles)
                    {
                        var asm = Assembly.LoadFrom(pluginAssemblyFile.FullName);
                        BuildManager.AddReferencedAssembly(asm);
                        //BuildManager.AddCompilationDependency(asm.FullName);
                    }
                }
                catch (Exception)
                {

                }

            }
        }

        public static void Start()
        {
            var viewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml"
            };

            var partialViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml",
                "~/Views/Partials/{0}.cshtml"
            };

            var appath = HostingEnvironment.MapPath("~/plugins");
            if (appath != null)
            {
                //var pluginFolder = new DirectoryInfo(appath);
                if (PluginFolder.Exists && !Config.RunSetup)
                {
                    var pluginAssemblyFiles = PluginFolder.GetFiles("*.dll", SearchOption.AllDirectories);

                    foreach (var pluginAssemblyFile in pluginAssemblyFiles)
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(pluginAssemblyFile.FullName);
                            var engine = new RazorGenerator.Mvc.PrecompiledMvcEngine(asm)
                            {
                                UsePhysicalViewsIfNewer = false //HttpContext.Current.Request.IsLocal
                            };
                            engine.ViewLocationFormats = viewLocationFormats;

                            engine.PartialViewLocationFormats = partialViewLocationFormats;

                            ViewEngines.Engines.Add(engine);

                            // StartPage lookups are done by WebPages. 
                            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);

                            //If there is a setup function then lets run it
                            Type setupType = asm.GetType("Helpers.Setup");
                            if (setupType != null)
                            {
                                object setupInstance = Activator.CreateInstance(setupType);

                                setupType.InvokeMember("Start",
                                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                    null, setupInstance, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            //throw;
                        }
                    }

                }
            }
        }

    }

}



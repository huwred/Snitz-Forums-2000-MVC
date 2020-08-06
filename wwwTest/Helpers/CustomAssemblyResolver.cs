using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using System.Web.Http.Dispatcher;
using Snitz.Base;

namespace WWW.Helpers
{
    public class CustomAssemblyResolver : IAssembliesResolver
    {
        public ICollection<Assembly> GetAssemblies()
        {
            List<Assembly> baseAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            try
            {
                var path = HostingEnvironment.MapPath("~/api");
                var controllersAssembly = Assembly.LoadFrom(path + @"\SnitzApi.dll");
                baseAssemblies.Add(controllersAssembly);

            }
            catch (Exception)
            {
                //fail silently
            }
            return baseAssemblies;
        }


        public static object ExecuteMethod(string asm, string classname, string method, object[] args)
        {
            var cacheService = new InMemoryCache(600);
            if (asm.ToLower() == "snitzevents" && method.ToLower() == "forumauth")
            {
                return cacheService.GetOrSet(method + "_" + args[0],
                    () => ExecuteMethodCache(asm,classname,method,args));
            }
            if (asm.ToLower() == "snitz.postthanks" && method.ToLower() == "allowed")
            {
                return cacheService.GetOrSet(method + "_" + args[0],
                    () => ExecuteMethodCache(asm,classname,method,args));
            }
            return cacheService.GetOrSet(asm + ":" +method,
            () => ExecuteMethodCache(asm,classname,method,args));
        }
        public static object ExecuteMethodCache(string asm, string classname, string method, object[] args)
        {
            try
            {
                List<Assembly> baseAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                var asmly = baseAssemblies.Find(a => a.FullName.IndexOf(asm, StringComparison.Ordinal) >= 0 );
                Type type = asmly.GetType(classname);

               var methodInfo = type.GetMethod(method);

                if (methodInfo == null) // the method doesn't exist
                {
                    return null;
                }
                var result = type.InvokeMember(
                        method,
                        BindingFlags.InvokeMethod | BindingFlags.Public |
                            BindingFlags.Static,
                        null,
                        null,
                        args);


                return result;
            }
            catch (Exception)
            {
                if (method == "Allowed")
                    return false;
                return null;
            }

        }
    }


}
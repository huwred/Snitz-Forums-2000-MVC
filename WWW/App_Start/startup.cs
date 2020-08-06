using System;
using System.Configuration;
using System.Transactions;
using System.Web;
using Hangfire;
using Hangfire.MySql;
using Hangfire.MySql.src;
using Hangfire.SqlServer;
using log4net.Config;
using Microsoft.Owin;
using Owin;
using WWW.Filters;

[assembly: OwinStartup(typeof(WWW.Startup))]

namespace WWW
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var dbProvidor = ConfigurationManager.ConnectionStrings["SnitzConnectionString"].ProviderName;
            try
            {
                if (dbProvidor.StartsWith("MySql"))
                {
                    try
                    {
                        GlobalConfiguration.Configuration.UseMySqlStorage(
                            "SnitzConnectionString",new MySqlStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(120) }
                        );
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
                else
                {
                    GlobalConfiguration.Configuration
                        .UseSqlServerStorage("SnitzConnectionString",new SqlServerStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(120) });

                }



                app.UseHangfireDashboard("/snitzjobs", new DashboardOptions
                {
                    AppPath = VirtualPathUtility.ToAbsolute("~"),
                    Authorization = new[] { new HangfireAuthorizationFilter() }
                });

                app.UseHangfireServer(new BackgroundJobServerOptions
                {
                    WorkerCount = 1,

                    HeartbeatInterval = new System.TimeSpan(0, 5, 0),
                    ServerCheckInterval = new System.TimeSpan(0, 5, 0),
                    SchedulePollingInterval = new System.TimeSpan(0, 5, 0)
                });
                app.MapSignalR();
                XmlConfigurator.Configure();
            }
            catch (System.Exception)
            {
                
                throw;
            }
        }

    }
}
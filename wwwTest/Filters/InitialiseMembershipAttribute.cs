using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Web.Mvc;
using MySql.Web.Security;
using SnitzMembership.Repositories;
using WebMatrix.WebData;


namespace WWW.Filters 
{ 
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)] 
	public sealed class InitializeSimpleMembershipAttribute : ActionFilterAttribute 
	{ 
		private static SimpleMembershipInitializer _initializer; 
		private static object _initializerLock = new object(); 
		private static bool _isInitialized; 


		public override void OnActionExecuting(ActionExecutingContext filterContext) 
		{ 
			// Ensure ASP.NET Simple Membership is initialized only once per app start 
			LazyInitializer.EnsureInitialized(ref _initializer, ref _isInitialized, ref _initializerLock); 
		} 


		private class SimpleMembershipInitializer 
		{ 
			public SimpleMembershipInitializer() 
			{
                Database.SetInitializer<SnitzMemberContext>(null); 

				try 
				{
                    using (var context = SnitzMemberContext.CreateContext()) 
					{ 
						if (context.Database.Exists() == false) 
						{ 
							// Create the SimpleMembership database without Entity Framework migration schema 
							((IObjectContextAdapter)context).ObjectContext.CreateDatabase(); 
						}

                        if (context.Provider.StartsWith("MySql"))
                        {


                            MySqlWebSecurity.InitializeDatabaseConnection("SnitzMembership", context.MemberTablePrefix + "MEMBERS", "MEMBER_ID", "M_NAME", false);
                        }

                        WebSecurity.InitializeDatabaseConnection("SnitzMembership", context.MemberTablePrefix + "MEMBERS", "MEMBER_ID", "M_NAME", false,SimpleMembershipProviderCasingBehavior.RelyOnDatabaseCollation); 
					    
					} 


				} 
				catch (Exception ex) 
				{ 
					throw new InvalidOperationException("The ASP.NET Simple Membership database could not be initialized. For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588", ex); 
				} 
			} 
		} 
	} 
} 

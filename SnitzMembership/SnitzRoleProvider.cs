using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnitzMembership.Models;
using SnitzMembership.Repositories;

namespace SnitzMembership
{
    public class SnitzRoleProvider : System.Web.Security.RoleProvider
    {
        private string _ApplicationName = null;
        private ConnectionStringSettings pConnectionStringSettings;
        private string connectionString;


        public SnitzRoleProvider()
        {
            return;
        }

        public override string ApplicationName
        {
            get
            {
                return _ApplicationName;
            }
            set
            {
                _ApplicationName = value;
            }
        }

    public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
    {
            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "SnitzRoleProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Snitz Role provider");
            }

            // Initialize the abstract base class. 
            base.Initialize(name, config);


            if (config["applicationName"] == null || config["applicationName"].Trim() == "")
            {
                _ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
            }
            else
            {
                _ApplicationName = config["applicationName"];
            }

            pConnectionStringSettings = ConfigurationManager.ConnectionStrings[config["SnitzMembership"]];

            if (pConnectionStringSettings == null || pConnectionStringSettings.ConnectionString.Trim() == "")
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = pConnectionStringSettings.ConnectionString;
        }

        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            foreach (string rolename in rolenames)
            {
                if (!RoleExists(rolename))
                {
                    throw new ProviderException("Role name not found.");
                }
            }

            foreach (string username in usernames)
            {
                if (username.Contains(","))
                {
                    throw new ArgumentException("User names cannot contain commas.");
                }

                foreach (string rolename in rolenames)
                {
                    if (IsUserInRole(username, rolename))
                    {
                        throw new ProviderException("User is already in role.");
                    }
                }
            }

            using (var db = new SnitzMemberContext())
            {
                var newrole = new UserRole();


            }

        }

        public override void CreateRole(string roleName)
        {
            using (var db = new SnitzMemberContext())
            {
                var newrole = new DefRole();
                newrole.RoleName = roleName;
                db.Roles.Add(newrole);
                db.SaveChanges();

            }
        }

    public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
    {
        if (throwOnPopulatedRole && GetUsersInRole(roleName).Any())
        {
            throw new Exception("Role has assigned Members");
        }

        using (var db = new SnitzMemberContext())
        {
            var role = db.Roles.Single(r => r.RoleName == roleName);
            db.Roles.Remove(role);
            db.SaveChanges();
                return true;
            }
        }
    public override string[] FindUsersInRole(string roleName, string usernameToMatch)
    {
        throw new Exception("NOT IMPLEMENTED");
    }

    public override string[] GetAllRoles()
    {
        using (var db = new SnitzMemberContext())
        {
            return db.Roles.Select(r => r.RoleName).ToArray();
        }
    }

    public override string[] GetRolesForUser(string username)
    {
        var userid = WebSecurity.GetUserId(username);
        using (var db = new SnitzMemberContext())
        {
            //return db.UserRoles.Where(ur=>ur.UserId == userid).Select().
        }

                throw new Exception("NOT IMPLEMENTED");
    }

    public override string[] GetUsersInRole(string roleName)
    {
        throw new Exception("NOT IMPLEMENTED");
    }

    public override bool IsUserInRole(string username, string roleName)
    {
        throw new Exception("NOT IMPLEMENTED");
    }

    public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
    {
        throw new Exception("NOT IMPLEMENTED");
    }

    public override bool RoleExists(string roleName)
    {
        throw new Exception("NOT IMPLEMENTED");
    }
}

}

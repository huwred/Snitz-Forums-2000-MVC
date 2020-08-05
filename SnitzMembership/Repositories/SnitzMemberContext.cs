using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using SnitzMembership.Models;

namespace SnitzMembership.Repositories
{
    public class SnitzMemberContext : DbContext
    {
        public SnitzMemberContext()
            : base("SnitzMembership")
        {

        }
        public static SnitzMemberContext CreateContext() 
 		{
            return new SnitzMemberContext(); 
 		} 

        public readonly string MemberTablePrefix = ConfigurationManager.AppSettings["memberTablePrefix"];
        public bool LowercaseQuery = ConfigurationManager.AppSettings["lowercaseQuery"] == "1";
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<SnitzSecurity> Memberships { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<DefRole> Roles { get; set; }
        
        public readonly string Provider = ConfigurationManager.ConnectionStrings["SnitzMembership"].ProviderName;

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>().ToTable(ConfigurationManager.AppSettings["memberTablePrefix"] + "MEMBERS");
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = String.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = String.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }

        public EmailConfirmation GetEmailConfirmToken(string username)
        {
            UserProfile user = WebSecurity.GetUser(username);
            var res = this.Memberships.Where(m=>m.UserId==user.UserId).Select(m=>m.ConfirmationToken);

            return new EmailConfirmation {Email=user.Email,Token=res.First()};
        }

        public UserProfile GetUser(int usr)
        {
            return this.UserProfiles.SingleOrDefault(m=>m.UserId == usr);
        }

        public UserProfile GetUser(string memberName)
        {
            return this.UserProfiles.SingleOrDefault(m => m.UserName == memberName);
        }

        public SnitzSecurity GetMembership(int id)
        {
            return this.Memberships.SingleOrDefault(m => m.UserId == id);
        }
    }

}

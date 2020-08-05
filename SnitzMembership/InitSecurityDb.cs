using System.Data.Entity;
using SnitzMembership.Repositories;

namespace SnitzMembership
{
    // DropCreateDatabaseAlways

    public class InitSecurityDb : CreateDatabaseIfNotExists<SnitzMemberContext>
    {
        protected override void Seed(SnitzMemberContext context)
        {
            WebSecurity.Register();

        }

    }
}
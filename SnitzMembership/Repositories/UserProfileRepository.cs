using SnitzMembership.Models;

namespace SnitzMembership.Repositories
{
    public class UserProfileRepository: GenericRepository<UserProfile>
    {
        public UserProfileRepository(SnitzMemberContext context) : base(context) { }
    }
}

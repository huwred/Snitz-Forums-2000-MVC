using System.Collections;
using System.Collections.Generic;
using SnitzMembership.Models;

namespace WWW.ViewModels
{
    public class PendingMemberViewModel
    {
        public List<UserProfile> Pending { get; set; }
        public Hashtable EnabledProfileFields { get; set; }
    }

    public class MemberViewModel
    {
        public UserProfile User { get; set; }
        public Hashtable EnabledProfileFields { get; set; }

    }

}

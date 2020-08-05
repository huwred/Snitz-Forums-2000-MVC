using System;
using System.Collections.Generic;
using System.Linq;
using PetaPoco;
using SnitzDataModel.Database;

namespace SnitzDataModel.Models
{
    public partial class AllowedMembers
    {

        public static void Remove(int forumId, List<int> members)
        {
            if (members == null)
            {
                try
                {
                    SnitzDataContext.Record<AllowedMembers>.repo.BeginTransaction();
                    SnitzDataContext.Record<AllowedMembers>.repo.Execute("DELETE FROM " + SnitzDataContext.Record<AllowedMembers>.repo.ForumTablePrefix + "ALLOWED_MEMBERS WHERE FORUM_ID=@forumId", new { forumId });
                    SnitzDataContext.Record<AllowedMembers>.repo.CompleteTransaction();
                }
                catch (Exception)
                {
                    SnitzDataContext.Record<AllowedMembers>.repo.AbortTransaction();
                    throw;
                }
            }
            else if (members.Any())
            {
                try
                {
                    SnitzDataContext.Record<AllowedMembers>.repo.BeginTransaction();
                    SnitzDataContext.Record<AllowedMembers>.repo.Execute("DELETE FROM " + SnitzDataContext.Record<AllowedMembers>.repo.ForumTablePrefix + "ALLOWED_MEMBERS WHERE FORUM_ID=@forumId AND MEMBER_ID IN (@members)", new { forumId, members });
                    SnitzDataContext.Record<AllowedMembers>.repo.CompleteTransaction();
                }
                catch (Exception)
                {
                    SnitzDataContext.Record<AllowedMembers>.repo.AbortTransaction();
                    throw;
                }
            }
        }

        private static void Add(int forumId, List<int> members)
        {
            if (members.Any())
            {
                var sql = new Sql();
                foreach (int member in members)
                {
                    sql.Append("INSERT INTO " + SnitzDataContext.Record<AllowedMembers>.repo.ForumTablePrefix + "ALLOWED_MEMBERS (FORUM_ID,MEMBER_ID) VALUES(@0,@1)", forumId, member);
                }
                SnitzDataContext.Record<AllowedMembers>.repo.Execute(sql);
            }
        }

        public static void Remove(int memberid)
        {
            SnitzDataContext.Record<AllowedMembers>.repo.Execute("DELETE FROM " + SnitzDataContext.Record<AllowedMembers>.repo.ForumTablePrefix + "ALLOWED_MEMBERS WHERE MEMBER_ID=@0", memberid);
            SnitzDataContext.Record<AllowedMembers>.repo.Execute("DELETE FROM webpages_UsersInRoles WHERE UserId=@0", memberid);
        }

        public static void Save(int forumid, List<int> allowedMembers)
        {
            //fetch the current moderators from the database
            Dictionary<int, string> memberList = Forum.Members(forumid);
            List<int> keyList = new List<int>(memberList.Keys);

            var removedMembers = keyList.Except(allowedMembers).ToList();
            var newMembers = allowedMembers.Except(keyList).ToList();

            Remove(forumid, removedMembers);
            Add(forumid, newMembers);
            
        }
    }
}
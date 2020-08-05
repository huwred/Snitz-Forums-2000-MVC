using System;
using System.Collections.Generic;
using System.Linq;
using PetaPoco;
using Snitz.Base;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Extensions;

namespace SnitzDataModel.Models
{
    public partial class PrivateMessage
    {

        public static PrivateMessage Fetch(int id)
        {
            var sql = new Sql();
            sql.Select("PM.*,MTO.M_NAME AS ToUserName,MFROM.M_NAME AS FromUserName");
            sql.From(repo.ForumTablePrefix + "PM PM");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS MTO ON PM.M_TO=MTO.MEMBER_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS MFROM ON PM.M_FROM=MFROM.MEMBER_ID");
            sql.Where("M_ID=@0", id);

            var pm = repo.FirstOrDefault<PrivateMessage>(sql);

            return pm;
        }

        public static List<PrivateMessage> OutBox(int memberid)
        {
            var sql = new Sql();
            sql.Select("PM.*,M.M_NAME AS ToUserName");
            sql.From(repo.ForumTablePrefix + "PM PM");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M ON PM.M_TO=M.MEMBER_ID");
            sql.Where("M_FROM=@0", memberid);
            sql.Where("M_OUTBOX=1");
            sql.OrderBy("PM.M_SENT DESC");

            return repo.Fetch<PrivateMessage>(sql);
        }

        public static List<PrivateMessage> InBox(int memberid)
        {
            var sql = new Sql();
            sql.Select("PM.*,M.M_NAME AS FromUsername");
            sql.From(repo.ForumTablePrefix + "PM PM");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M ON PM.M_FROM=M.MEMBER_ID");
            sql.Where("M_TO=@0 ", memberid);
            sql.Where("PM_DEL_TO=0 ");
            sql.Where("M_READ>=0 ");
            sql.OrderBy("PM.M_SENT DESC");
            return repo.Fetch<PrivateMessage>(sql);
        }

        public static int MailboxSize(int memberid)
        {
            var sql = new Sql();
            sql.Select("Count(M_ID)");
            sql.From(repo.ForumTablePrefix + "PM ");
            sql.Where("M_READ>=0 ");
            sql.Where("(M_TO=@0 AND PM_DEL_TO=0) OR (M_FROM=@0 AND M_OUTBOX=1)", memberid,memberid);
            
            return repo.ExecuteScalar<int>(sql);
        }
        public static int Check(int memberid)
        {
            if (!SessionData.Contains("NewPM"))
            {
                var sql = new Sql();
                sql.Select(" COUNT(*)");
                sql.From(repo.ForumTablePrefix + "PM");
                sql.Where("M_READ=0 AND M_TO=@0 AND PM_DEL_TO=0", memberid);
                var pmcount = repo.ExecuteScalar<int>(sql);
                SessionData.Set("NewPM", pmcount);
                return pmcount;                
            }
            return SessionData.Get<int>("NewPM");
        }
        public static int Check(string username)
        {
            var sql = new Sql();
            sql.Select(" COUNT(p.*)");
            sql.From(repo.ForumTablePrefix + "PM p");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS").On("m.MEMBER_ID=p.M_TO");
            sql.Where("p.M_READ=0 AND m.M_NAME=@0", username);
            var pmcount = repo.ExecuteScalar<int>(sql);
            return pmcount;
        }
        public static void DeleteMessages(List<int> selectedMessages, bool inbox)
        {

            if (selectedMessages.Any())
            {
                try
                {
                    repo.BeginTransaction();
                    repo.Execute(
                        inbox
                            ? "UPDATE " + repo.ForumTablePrefix + "PM SET PM_DEL_TO=1 WHERE M_ID IN (@selectedMessages); DELETE FROM " + repo.ForumTablePrefix + "PM WHERE M_ID IN (@selectedMessages) AND PM_DEL_FROM=1;"
                            : "UPDATE " + repo.ForumTablePrefix + "PM SET M_OUTBOX=0, PM_DEL_FROM=1 WHERE M_ID IN (@selectedMessages); DELETE FROM " + repo.ForumTablePrefix + "PM WHERE M_ID IN (@selectedMessages) AND PM_DEL_TO=1;", new { selectedMessages });
                    repo.CompleteTransaction();
                }
                catch (Exception)
                {
                    repo.AbortTransaction();
                    throw;
                }
            }
        }

        /// <summary>
        /// list of members you have blocked
        /// </summary>
        /// <param name="memberid"></param>
        /// <returns></returns>
        public static IEnumerable<PrivateBlocklist> Blocklist(int memberid)
        {
            var sql = new Sql();
            sql.Select("*");
            sql.From(repo.ForumTablePrefix + "PM_BLOCKLIST");
            sql.Where("BL_MEMBER_ID=@0", memberid);
            sql.OrderBy("BL_BLOCKED_NAME");

            return repo.Fetch<PrivateBlocklist>(sql);

        }

        /// <summary>
        /// List of Members blocking you
        /// </summary>
        /// <param name="memberid"></param>
        /// <returns></returns>
        public static IEnumerable<int> BlockedBy(int memberid)
        {
            var sql = new Sql();
            sql.Select("BL_MEMBER_ID");
            sql.From(repo.ForumTablePrefix + "PM_BLOCKLIST");
            sql.Where("BL_BLOCKED_ID=@0", memberid);
            sql.OrderBy("BL_BLOCKED_NAME");

            return repo.Fetch<int>(sql);

        }

        public static void BlockUser(int currentUserId, Member blockmember)
        {
            var blockeduser = new PrivateBlocklist();
            blockeduser.MemberId = currentUserId;
            blockeduser.BlockedMemberId = blockmember.Id;
            blockeduser.BlockedMemberName = blockmember.Username;
            repo.Save(blockeduser);
            
        }

        public static void UnblockUser(int currentUserId, int id)
        {
            var blockeduser = repo.First<PrivateBlocklist>("WHERE BL_MEMBER_ID=@0 AND BL_BLOCKED_ID=@1", currentUserId,
                id);
            blockeduser.Delete();

        }


        public static List<PrivateMessage> Find(int currentuser, string vmTerm, Enumerators.SearchIn vmSearchIn, int memberId, Enumerators.SearchWordMatch vmPhraseType, Enumerators.SearchDays vmSearchByDays)
        {
            var sql = new Sql();
            sql.Select("PM.*,M1.M_NAME AS FromUsername,M2.M_NAME AS ToUsername");
            sql.From(repo.ForumTablePrefix + "PM PM");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M1 ON PM.M_FROM=M1.MEMBER_ID");
            sql.LeftJoin(repo.MemberTablePrefix + "MEMBERS M2 ON PM.M_TO=M2.MEMBER_ID");
            if (!String.IsNullOrWhiteSpace(vmTerm))
            {
                string[] terms = vmTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (vmPhraseType)
                {
                    case Enumerators.SearchWordMatch.Any:
                        string or;
                        string sqlwhere;

                        switch (vmSearchIn)
                        {
                            case Enumerators.SearchIn.Subject:
                                or = " ";
                                sqlwhere = "";
                                var args1 = new List<object>();
                                foreach (string term in terms)
                                {
                                    sqlwhere += or;
                                    sqlwhere += "PM.M_SUBJECT LIKE @0";
                                    args1.Add("%" + term.Replace("'", "''") + "%");
                                    or = " OR ";
                                }
                                sql.Where("(" + sqlwhere + ")", args1.ToArray());
                                break;
                            case Enumerators.SearchIn.Message:
                                or = " ";
                                sqlwhere = "";
                                var args2 = new List<object>();
                                foreach (string term in terms)
                                {
                                    sqlwhere += or;
                                    sqlwhere += "(PM.M_MESSAGE LIKE @0)";
                                    args2.Add("%" + term.Replace("'", "''") + "%");
                                    or = " OR ";
                                }
                                sql.Where("(" + sqlwhere + ")", args2.ToArray());
                                break;
                        }
                        break;
                    case Enumerators.SearchWordMatch.All:
                        foreach (string term in terms)
                        {
                            switch (vmSearchIn)
                            {
                                case Enumerators.SearchIn.Subject:
                                    sql.Where("(PM.M_SUBJECT LIKE @0)", "%" + term + "%");
                                    break;
                                case Enumerators.SearchIn.Message:
                                    sql.Where("(PM.M_MESSAGE LIKE @0)", "%" + term + "%");
                                    break;
                            }
                        }
                        break;
                    case Enumerators.SearchWordMatch.ExactPhrase:
                        switch (vmSearchIn)
                        {
                            case Enumerators.SearchIn.Subject:
                                sql.Where("PM.M_SUBJECT LIKE @0", "%" + vmTerm + "%");
                                break;
                            case Enumerators.SearchIn.Message:
                                sql.Where("(PM.M_SUBJECT LIKE @0 OR PM.M_MESSAGE LIKE @1 )", "%" + vmTerm + "%", "%" + vmTerm + "%");
                                break;
                        }
                        break;
                }
            }
            string datestr = "";
            switch (vmSearchByDays)
            {
                case Enumerators.SearchDays.Any:
                    break;
                case Enumerators.SearchDays.Since30Days:
                    datestr = DateTime.UtcNow.AddMonths(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.Since60Days:
                    datestr = DateTime.UtcNow.AddMonths(-2).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.Since120Days:
                    datestr = DateTime.UtcNow.AddMonths(-6).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                case Enumerators.SearchDays.SinceYear:
                    datestr = DateTime.UtcNow.AddYears(-1).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;
                default:
                    datestr = DateTime.UtcNow.AddDays(-(int)vmSearchByDays).ToSnitzServerDateString(ClassicConfig.ForumServerOffset);
                    break;

            }
            if (!String.IsNullOrWhiteSpace(datestr))
            {
                switch (vmSearchIn)
                {
                    case Enumerators.SearchIn.Subject:
                        sql.Where("PM.M_SENT > @0", datestr);
                        break;
                    case Enumerators.SearchIn.Message:
                        sql.Where("(PM.M_SENT > @0 OR r.R_DATE > @1)", datestr, datestr);
                        break;
                }
            }
            if (memberId > -1)
            {
                sql.Where("(PM.M_TO=@0 AND PM.M_FROM=@1 AND PM.M_OUTBOX=1) OR (PM.M_FROM=@2 AND PM.M_TO=@3)", memberId, currentuser,
                    memberId, currentuser);
            }
            else
            {
                sql.Where("((PM.M_TO=@0 AND PM.M_OUTBOX=1) OR PM.M_FROM=@1)", currentuser, currentuser);
            }


            sql.OrderBy("PM.M_SENT DESC");

            return repo.Fetch<PrivateMessage>(sql);
        }

        public static void SendPrivateMessage(int from,int to,string subject, string message)
        {
            PrivateMessage msg = new PrivateMessage
            {
                ToMemberId = to,
                FromMemberId = from,
                Subject = subject,
                Message = message,
                SentDate = DateTime.UtcNow,
                Read = 0,
                ShowOutBox = 0
            };
            msg.Save();
        }

        public int PmAllMembers()
        {
            string sql =
                "INSERT INTO " + repo.MemberTablePrefix + "PM (M_SUBJECT, M_FROM, M_TO, M_SENT, M_MESSAGE, M_READ, M_OUTBOX, PM_DEL_FROM, PM_DEL_TO) ";
            sql +=
                String.Format("SELECT '{0}',{1}, MEMBER_ID,'{2}','{3}',0,0,0,0 FROM " + repo.MemberTablePrefix + "MEMBERS WHERE M_STATUS=1 AND M_LEVEL=1", this.Subject, this.FromMemberId, this.Sent, this.Message);

            return repo.Execute(sql);
        }

        public static void ChangeOwner(int primaryId, int secondaryId)
        {
            repo.Execute("UPDATE " + repo.MemberTablePrefix + "PM SET M_FROM=@0  WHERE M_FROM=@1",primaryId,secondaryId);
            repo.Execute("UPDATE " + repo.MemberTablePrefix + "PM SET M_TO=@0  WHERE M_TO=@1",primaryId,secondaryId);
        }
    }
}
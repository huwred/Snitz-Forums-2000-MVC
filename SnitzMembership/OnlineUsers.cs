using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Threading;
using SnitzConfig;
using SnitzCore.Extensions;
using SnitzCore.Utility;
using SnitzDataModel.Models;
using System.IO;
using System.Web.Hosting;

namespace SnitzMembership
{
    public class OnlineUser
    {
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string HostName { get; set; }
        public DateTime Value { get; set; }
        public string CurrentPage { get; set; }
    }
    public class OnlineUsersInstance
    {
        public static OnlineUsers OnlineUsers;
        static OnlineUsersInstance()
        {
            OnlineUsers = new OnlineUsers();
        }
    }

    /// <summary>
    /// Checks users online state
    /// </summary>
    public class OnlineUsers
    {
        private string _UnknownUser = "_'?Unknown\nUser?'_";
        private string _HiddenUser = "_'?Hidden\nUser?'_";
        private Dictionary<string,OnlineUser> fOnlineUsers = new Dictionary<string, OnlineUser>();
        private int _membersCount = 0;
        private int _guestsCount = 0;
        private int _hiddenCount = 0;
        private Timer _thTimer;

        /// <summary>
        /// 300000 = 5 minutes
        /// </summary>
        private const int TIMER_PERIOD = 300000;
        //int imtoi


        public OnlineUsers()
        {
            //var ip = Common.GetUserIP(HttpContext.Current);
            _UnknownUser += DateTime.UtcNow.Ticks.ToString();
            // Timer will start after _TimerPeriod
            _thTimer = new Timer(_ThreadTimerCallback, this, TIMER_PERIOD, TIMER_PERIOD);
        }

        #region Properties
        public int RegisteredUsersCount
        {
            get { return _membersCount; }
        }
        public int GuestUsersCount
        {
            get { return _guestsCount; }
        }
        public int HiddenUsersCount
        {
            get { return _hiddenCount; }
        }
        public int UsersCount
        {
            get
            {
                Monitor.Enter(fOnlineUsers);
                try
                {
                    return fOnlineUsers.Count;
                }
                finally
                {
                    Monitor.Exit(fOnlineUsers);
                }
            }
        }
        public Dictionary<string,OnlineUser> OnlineHashtable
        {
            get
            {
                Monitor.Enter(fOnlineUsers);
                try
                {
                    return fOnlineUsers;
                }
                finally
                {
                    Monitor.Exit(fOnlineUsers);
                }
            }
        }
        //public Hashtable UserOnline => fOnlineUsers;

        #endregion


        #region public

        public void UpdateForUserLeave()
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                return;
            }

            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                SetOfflineMemberInternal(context.User.Identity.Name);
            }
            else if (context.Session != null)
            {
                SetOfflineGuestInternal(_UnknownUser + context.Session.SessionID);
            }
        }


        /// <summary>
        /// Check user online state
        /// </summary>
        public bool IsOnline(string userName)
        {
            Monitor.Enter(fOnlineUsers);
            
            try
            {
                if (String.IsNullOrWhiteSpace(userName))
                    return false;

                return fOnlineUsers.ContainsKey(userName);
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        public DateTime GetLastActivity(string userName)
        {
            DateTime state = new DateTime();
            Monitor.Enter(fOnlineUsers);
            try
            {
                if (fOnlineUsers.ContainsKey(userName))
                {
                    state = fOnlineUsers[userName].Value;
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
            return state;//new DateTime((long)state);
        }

        /// <summary>
        /// Set user state to online
        /// </summary>
        public void SetUserOnline(string userName)
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                if(!Config.AnonymousMembers.Contains(userName))
                    SetOnlineMemberInternal(userName);
                else { SetOnlineGuestInternal(_HiddenUser + userName); }
            }
            else
            {
                if (context.Session != null)
                {
                    // This user may are already in site
                    SetOfflineGuestInternal(_UnknownUser + context.Session.SessionID);
                }
                if (!Config.AnonymousMembers.Contains(userName))
                    SetOnlineMemberInternal(userName);
                else
                {
                    SetOnlineGuestInternal(_HiddenUser + userName);
                }
            }

        }

        /// <summary>
        /// Make user offline
        /// </summary>
        public void SetUserOffline(string userName)
        {
            SetOfflineInternal(userName);
            //using (var db = new SnitzDataContext())
            //{

            //    db.Execute("UPDATE " + db.MemberTablePrefix + "MEMBERS SET M_LASTHEREDATE=@0 WHERE M_NAME=@1",
            //        DateTime.UtcNow.ToSnitzDate(), userName);

            //}
        }

        /// <summary>
        /// Adds user to the list
        /// </summary>
        internal void UpdateForNewUser()
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                AddUnknownUser();
                return;
            }

            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                if (!Config.AnonymousMembers.Contains(context.User.Identity.Name))
                    SetOnlineMemberInternal(context.User.Identity.Name);
                else {SetOnlineGuestInternal(_HiddenUser + context.User.Identity.Name); }

            }
            else if (context.Session != null)
            {

                SetOnlineGuestInternal(_UnknownUser + context.Session.SessionID);
            }
            else
            {
                AddUnknownUser();
            }

            //UsersCount
            if (UsersCount > ClassicConfig.GetIntValue("INTACTIVETOTAL"))
            {
                if (!Config.RunSetup)
                {
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("INTACTIVETOTAL", UsersCount.ToString());
                    ClassicConfig.Update(new[] { "INTACTIVETOTAL" });
                    ClassicConfig.ConfigDictionary.CreateNewOrUpdateExisting("STRACTIVETOTAL", DateTime.UtcNow.ToSnitzDate());
                    ClassicConfig.Update(new[] { "STRACTIVETOTAL" }); 
                }
            }
        }

        internal void UpdateUserActivity()
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
                return;

            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                string userName = context.User.Identity.Name;
                if (!Config.AnonymousMembers.Contains(userName))
                    SetOnlineMemberInternal(userName);
                else { SetOnlineGuestInternal(_HiddenUser + userName); }
            }
            else if (context.Session != null)
            {
                string sessionID = _UnknownUser + context.Session.SessionID;
                SetOnlineGuestInternal(sessionID);
            }
        }
        #endregion

        #region private

        /// <summary>
        /// This callback should recheck all the uesrs list
        /// </summary>
        /// <param name="state"></param>
        private void _ThreadTimerCallback(Object state)
        {
            DateTime now = DateTime.UtcNow;
            ArrayList expired = new ArrayList(fOnlineUsers.Count);

            Monitor.Enter(fOnlineUsers);
            try
            {
                // Searching for expired users
                foreach (KeyValuePair<string, OnlineUser> entry in fOnlineUsers)
                {
                    if (((TimeSpan)(now - entry.Value.Value)).TotalMilliseconds > TIMER_PERIOD)
                    {
                        expired.Add(entry.Key);
                    }
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }

            // Remove expired items
            for (int i = 0; i < expired.Count; i++)
            {
                string user = expired[i].ToString();
                SetOfflineInternal(user);
            }
        }

        private void AddUnknownUser()
        {
            Monitor.Enter(fOnlineUsers);
            
            try
            {
                var host = HttpContext.Current.Request.Headers["user-agent"];
                string path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data/robots.txt");
                var bots = File.ReadAllLines(path);
                bool bot = false;
                foreach (string x in bots)
                {
                    if (host.ToLower().Contains(x))
                    {
                        bot = true;
                    }
                }

                if (!bot)
                {
                    fOnlineUsers[_UnknownUser + Guid.NewGuid().ToString()] = new OnlineUser() { UserAgent=host, Value = DateTime.UtcNow, CurrentPage = HttpContext.Current.Request.RawUrl };
                    IncreaseGuestsCount();
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        private void SetOnlineMemberInternal(string userName)
        {
            Monitor.Enter(fOnlineUsers);
            try
            {
                
                if (fOnlineUsers.ContainsKey(userName) == false)
                {
                    //SnitzCookie.ClearTracking();
                    IncreaseMembersCount();
                    var ip = Common.GetUserIP(HttpContext.Current);
                    fOnlineUsers[userName] = new OnlineUser() { Value = DateTime.UtcNow, IpAddress = ip, CurrentPage = HttpContext.Current.Request.RawUrl };
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        private void SetOnlineGuestInternal(string identity)
        {
            Monitor.Enter(fOnlineUsers);
            try
            {
                if (fOnlineUsers.ContainsKey(identity) == false)
                {
                    HttpContext context = HttpContext.Current;
                    var ip = Common.GetUserIP(context);
                    var host = Common.GetReverseDNS(ip, 5);
                    string path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data/robots.txt");
                    var bots = File.ReadAllLines(path);
                    bool bot = false;
                    foreach (string x in bots)
                    {
                        if (host.ToLower().Contains(x))
                        {
                            bot = true;
                        }
                    }

                    if (!bot)
                    {
                        fOnlineUsers[identity] = new OnlineUser() { Value = DateTime.UtcNow, IpAddress = ip, HostName = host, CurrentPage = HttpContext.Current.Request.RawUrl };
                        if (identity.StartsWith(_HiddenUser))
                        {
                            IncreaseHiddenCount();
                        }
                        else
                        {
                            IncreaseGuestsCount();
                        }
                        
                    }
                        
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        private void SetOfflineInternal(string identity)
        {
            Monitor.Enter(fOnlineUsers);
            try
            {
                if (fOnlineUsers.ContainsKey(identity))
                {
                    var host = fOnlineUsers[identity].HostName;
                    fOnlineUsers.Remove(identity);

                    if (identity.StartsWith(_UnknownUser))
                    {
                        string path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data/robots.txt");
                        var bots = File.ReadAllLines(path);
                        bool bot = false;
                        foreach (string x in bots)
                        {
                            if (host.ToLower().Contains(x))
                            {
                                bot = true;
                            }
                        }

                        if (!bot)
                        {
                            if (identity.StartsWith(_HiddenUser))
                            {
                                DecreaseHiddenCount();
                            }
                            else
                            {
                                DecreaseGuestsCount();
                            }
                        }
                    }
                    else
                        DecreaseMembersCount();
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        private void SetOfflineMemberInternal(string userName)
        {
            Monitor.Enter(fOnlineUsers);
            try
            {
                if (fOnlineUsers.ContainsKey(userName))
                {
                    fOnlineUsers.Remove(userName);
                    DecreaseMembersCount();
                }else if (Config.AnonymousMembers.Contains(userName))
                {
                    SetOfflineGuestInternal(_UnknownUser + userName);
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        private void SetOfflineGuestInternal(string identity)
        {
            Monitor.Enter(fOnlineUsers);
            try
            {
                if (fOnlineUsers.ContainsKey(identity))
                {
                    var host = fOnlineUsers[identity].HostName;
                    fOnlineUsers.Remove(identity);

                    if (identity.StartsWith(_UnknownUser))
                    {
                        string path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data/robots.txt");
                        var bots = File.ReadAllLines(path);
                        bool bot = false;
                        foreach (string x in bots)
                        {
                            if (host.ToLower().Contains(x))
                            {
                                bot = true;
                            }
                        }

                        if (!bot)
                        {
                            if (identity.StartsWith(_HiddenUser))
                            {
                                DecreaseHiddenCount();
                            }
                            else
                            {
                                DecreaseGuestsCount();
                            }
                        }
                            
                    }
                }
            }
            finally
            {
                Monitor.Exit(fOnlineUsers);
            }
        }

        private void DecreaseMembersCount()
        {
            _membersCount--;
            if (_membersCount < 0)
                _membersCount = 0;
        }

        private void DecreaseGuestsCount()
        {
            _guestsCount--;
            if (_guestsCount < 0)
                _guestsCount = 0;
        }

        private void IncreaseMembersCount()
        {
            _membersCount++;

        }

        private void IncreaseGuestsCount()
        {
            _guestsCount++;
        }

        private void IncreaseHiddenCount()
        {
            _hiddenCount++;
            if (_hiddenCount < 0)
                _hiddenCount = 0;
        }

        private void DecreaseHiddenCount()
        {
            _hiddenCount--;
            if (_hiddenCount < 0)
                _hiddenCount = 0;
        }
        #endregion

    }
}

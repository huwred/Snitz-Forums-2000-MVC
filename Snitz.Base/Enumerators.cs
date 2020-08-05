/*
####################################################################################################################
##
## SnitzConfig - Enumerators
##   
## Author:      Huw Reddick
## Copyright:   Huw Reddick
## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
## Created:     29/07/2013
## 
## The use and distribution terms for this software are covered by the 
## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
## which can be found in the file Eclipse.txt at the root of this distribution.
## By using this software in any fashion, you are agreeing to be bound by 
## the terms of this license.
##
## You must not remove this notice, or any other, from this software.  
##
#################################################################################################################### 
*/

using System.ComponentModel;
using System.Reflection;

namespace Snitz.Base
{
    public static class Enumerators
    {

        /// <summary>
        /// User level enumerators
        /// </summary>
        public enum UserLevels
        {
            NormalUser = 1,
            Moderator = 2,
            Administrator = 3
        }

        public enum Status
        {
            Closed = 0,
            Open = 1
        }

        /// <summary>
        /// Status enumerator for Posts
        /// </summary>
        public enum PostStatus
        {
            Closed = 0,
            Open = 1,
            UnModerated = 2,
            OnHold = 3,

            Draft = 99
        }

        /// <summary>
        /// Forum moderation enumerators
        /// </summary>
        public enum Moderation
        {
            UnModerated = 0,
            AllPosts,
            Topics,
            Replies
        }

        public enum ModerationLevel
        {
            NotAllowed = 0,
            Allowed
        }
        /// <summary>
        /// Allowed Subscription level
        /// </summary>
        public enum Subscription
        {
            None = 0,
            ForumSubscription,
            TopicSubscription
        }

        /// <summary>
        /// Category Subscription levels
        /// </summary>
        public enum CategorySubscription
        {
            None = 0,
            CategorySubscription,
            ForumSubscription,
            TopicSubscription
        }

        /// <summary>
        /// Forum Subscription Level
        /// </summary>
        public enum SubscriptionLevel
        {
            None = 0,
            Board,
            Category,
            Forum,
            Topic
        }

        public enum ForumAuthType
        {
            All = 0,
            AllowedMembers = 1,
            PasswordProtected,
            AllowedMemberPassword,
            Members,
            MembersHidden,
            AllowedMembersHidden,
            MembersPassword
        }
        public enum PostAuthType
        {
            Anyone = 0,
            Admins,
            Moderators
        }
        /// <summary>
        /// Forum types
        /// </summary>
        public enum ForumType
        {
            Topics = 0,
            WebLink = 1,
            //[Description("Bug Forum")]
            BugReports = 3,
            BlogPosts = 4
        }

        public enum ForumDays
        {
            AllOpen = -1,
            All = 0,
            LastDay,
            Last2Days,
            Last5Days = 5,
            Last7Days = 7,
            Last14Days = 14,
            Last30Days = 30,
            Last60Days = 60,
            Last120Days = 120,
            LastYear = 365,
            Archived = -99,
            NoReplies = -999,
            Draft = -9999,
            Hot = -88,

        }

        public enum SearchDays
        {
            Any = 0,
            Since1Day,
            Since2Days,
            Since5Days = 5,
            Since7Days = 7,
            Since14Days = 14,
            Since30Days = 30,
            Since60Days = 60,
            Since120Days = 120,
            SinceYear = 365
        }
        /// <summary>
        /// Forum Rank display type
        /// </summary>
        public enum RankType
        {
            None = 0,
            RankOnly,
            StarsOnly,
            Both
        }

        /// <summary>
        /// Page reload time enumerators
        /// </summary>
        public enum ActiveRefresh
        {
            None = 0,
            Minute = 60,
            TwoMinute = 120,
            FiveMinute = 300,
            TenMinute = 600,
            FifteenMinute = 900
        }

        /// <summary>
        /// topics since enumerator
        /// </summary>
        public enum ActiveTopicsSince
        {
            LastVisit = 0,
            LastFifteen,
            LastThirty,
            LastHour,
            Last2Hours,
            Last6Hours,
            Last12Hours,
            LastDay,
            Last2Days,
            LastWeek,
            Last2Weeks,
            LastMonth,
            Last2Months,
        }
        /// <summary>
        /// topics since enumerator
        /// </summary>
        public enum MyTopicsSince
        {
            LastVisit = 0,
            LastHour,
            Last2Hours,
            Last6Hours,
            Last12Hours,
            LastDay,
            LastWeek,
            LastMonth,
            Last6Months,
            Last12Months
        }
        /// <summary>
        /// search word match
        /// </summary>
        public enum SearchWordMatch
        {
            ExactPhrase,
            All,
            Any
        }

        public enum FullTextMatch
        {
            Loose,
            Exact
        }

        public enum SearchIn
        {
            Message,
            Subject
        }

        public enum Position
        {
            Horizontal = 0,
            Vertical = 1,
        }

        public enum PostButtonMode
        {
            Basic,
            Help,
            Prompt
        }

        public enum CaptchaOperator
        {
            Plus,
            Minus,
            Multiply
        }

        public enum PollAuth
        {
            Disallow,
            Members,
            AdminModerators
        }

        public static string GetEnumDescription<TEnum>(TEnum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if ((attributes != null) && (attributes.Length > 0))
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }
}

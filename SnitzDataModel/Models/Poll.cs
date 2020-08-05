// /*
// ####################################################################################################################
// ##
// ## Poll
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using PetaPoco;
using Snitz.Base;

namespace SnitzDataModel.Models
{

    [TableName("POLLS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("POLL_ID")]
    [ExplicitColumns]
    public class Poll
    {
        [Column("POLL_ID")]
        public int Id { get; set; }
        [Column("CAT_ID")]
        public int CatId { get; set; }
        [Column("FORUM_ID")]
        public int ForumId { get; set; }
        [Column("TOPIC_ID")]
        public int TopicId { get; set; }
        [Column("P_WHOVOTES")]
        public string AllowedRoles { get; set; }
        [Column("P_LASTVOTE")]
        public DateTime? LastVoteDate { get; set; }
        [Column("P_QUESTION")]
        public string Question { get; set; }

        [ResultColumn]
        public int CommentCount { get; set; }
        [ResultColumn]
        public short Active { get; set; }
        [ResultColumn]
        public int? Topic { get; set; }

        public List<PollAnswer> Answers { get; set; }
        public List<PollVotes> Votes { get; set; }
    }

    [TableName("POLL_ANSWERS", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("POLLANSWER_ID")]
    [ExplicitColumns]
    public class PollAnswer
    {
        [Column("POLLANSWER_ID")]
        public int Id { get; set; }
        [Column("POLL_ID")]
        public int PollId { get; set; }
        [Column("POLLANSWER_LABEL")]
        public string Answer { get; set; }
        [Column("POLLANSWER_ORDER")]
        public int Order { get; set; }
        [Column("POLLANSWER_COUNT")]
        public int Votes { get; set; }

    }

    [TableName("POLL_VOTES", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("POLLVOTES_ID")]
    [ExplicitColumns]
    public class PollVotes
    {
        [Column("POLLVOTES_ID")]
        public int Id { get; set; }
        [Column("POLL_ID")]
        public int PollId { get; set; }
        [Column("CAT_ID")]
        public int CatId { get; set; }
        [Column("FORUM_ID")]
        public int ForumId { get; set; }
        [Column("TOPIC_ID")]
        public int TopicId { get; set; }
        [Column("MEMBER_ID")]
        public int MemberId { get; set; }
        [Column("GUEST_VOTE")]
        public int GuestVote { get; set; }

    }
}


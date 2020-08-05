using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PetaPoco;
using Snitz.Base;
using SnitzDataModel.Database;

namespace SnitzDataModel.Models
{
    public partial class Topic
    {
        [Column("T_RATING_TOTAL_COUNT")]
        public int RatingCount { get; set; }

        [Column("T_RATING_TOTAL")]
        public int RatingTotal { get; set; }

        [Column("T_ALLOW_RATING")]
        public int AllowRating { get; set; }

        public bool HasVoted(int memberid)
        {
            return TopicRating.FetchRating(this.Id, memberid);
        }
    }
    [TableName("TOPIC_RATINGS", prefixType = Extras.TablePrefixTypes.None)]
    [PrimaryKey("RATING")]
    [ExplicitColumns]
    public class TopicRating : SnitzDataContext.Record<TopicRating>  
    {
        [Column("RATING")]
        public int Id { get; set; }   
        
        [Column("RATINGS_BYMEMBER_ID")]
        public int MemberId { get; set; }

        [Column("RATINGS_TOPIC_ID")]
        public int TopicId { get; set; }

        public static bool FetchRating(int topicid, int memberid)
        {

            return repo.Query<TopicRating>("WHERE RATINGS_TOPIC_ID = @0 AND RATINGS_BYMEMBER_ID=@1", topicid,memberid).Any();
        }
    }
}
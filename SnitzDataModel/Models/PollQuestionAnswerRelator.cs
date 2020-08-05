// /*
// ####################################################################################################################
// ##
// ## PollQuestionAnswerRelator
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

using System.Collections.Generic;

namespace SnitzDataModel.Models
{
    /// <summary>
    /// class to map the category->forum one to many relationship
    /// </summary>
    class PollQuestionAnswerRelator
    {
        public Poll current;
        public Poll MapIt(Poll p, PollAnswer a)
        {
            // Terminating call.  Since we can return null from this function
            // we need to be ready for PetaPoco to callback later with null
            // parameters
            if (p == null)
                return current;

            // Is this the same category as the current one we're processing
            if (current != null && current.Id == p.Id)
            {
                // Yes, just add this forum to the current categories collection of forums
                current.Answers.Add(a);

                // Return null to indicate we're not done with this category yet
                return null;
            }

            // This is a different category to the current one, or this is the 
            // first time through and we don't have a forum yet

            // Save the current category
            var prev = current;

            // Setup the new current category
            current = p;
            current.Answers = new List<PollAnswer>();
            if (a.Id > 0)
            {

                current.Answers.Add(a);
            }

            // Return the now populated previous category (or null if first time through)
            return prev;
        }
    }
    class PollQuestionVotesRelator
    {
        public Poll current;
        public Poll MapIt(Poll p, PollVotes v)
        {
            // Terminating call.  Since we can return null from this function
            // we need to be ready for PetaPoco to callback later with null
            // parameters
            if (p == null)
                return current;

            // Is this the same category as the current one we're processing
            if (current != null && current.Id == p.Id)
            {
                // Yes, just add this forum to the current categories collection of forums
                current.Votes.Add(v);

                // Return null to indicate we're not done with this category yet
                return null;
            }

            // This is a different category to the current one, or this is the 
            // first time through and we don't have a forum yet

            // Save the current category
            var prev = current;

            // Setup the new current category
            current = p;
            current.Votes = new List<PollVotes>();
            if (v.Id > 0)
            {

                current.Votes.Add(v);
            }

            // Return the now populated previous category (or null if first time through)
            return prev;
        }
    }
}

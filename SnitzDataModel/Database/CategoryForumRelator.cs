// /*
// ####################################################################################################################
// ##
// ## CategoryForumRelator
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

namespace SnitzDataModel.Database
{
    /// <summary>
    /// class to map the category->forum one to many relationship
    /// </summary>
    class CategoryForumRelator
    {
        public Models.Category current;
        public Models.Category MapIt(Models.Category a, Models.Forum p)
        {
            // Terminating call.  Since we can return null from this function
            // we need to be ready for PetaPoco to callback later with null
            // parameters
            if (a == null)
                return current;

            // Is this the same category as the current one we're processing
            if (current != null && current.Id == a.Id)
            {
                // Yes, just add this forum to the current categories collection of forums
                current.Forums.Add(p);

                // Return null to indicate we're not done with this category yet
                return null;
            }

            // This is a different category to the current one, or this is the 
            // first time through and we don't have a forum yet

            // Save the current category
            var prev = current;

            // Setup the new current category
            current = a;
            current.Forums = new List<Models.Forum>();
            if (p.Id > 0)
            {

                current.Forums.Add(p);
            }

            // Return the now populated previous category (or null if first time through)
            return prev;
        }
    }
}

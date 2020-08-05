
// /*
// ####################################################################################################################
// ##
// ## TopicComparer
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
    class TopicComparer : IEqualityComparer<Models.Topic>
    {
        public bool Equals(Models.Topic x, Models.Topic y)
        {
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Models.Topic obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}

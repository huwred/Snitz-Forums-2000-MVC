// /*
// ####################################################################################################################
// ##
// ## CSVFiles
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
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SnitzDataModel
{
    public class CSVFiles
    {
        public DataTable Table;

        public CSVFiles(string filepath, DataColumn[] columns)
        {
            Table = new DataTable();
            Table.Columns.AddRange(columns);
            string csvData = System.IO.File.ReadAllText(filepath);

            var pattern = @"(\,|\r?\n|\r|^)(?:""([^""]*(?:""""[^""] *)*)""|([^""\r\n]*))";
            MatchCollection matches = Regex.Matches(csvData, pattern);
            int i = 1;
            foreach (Match match in matches)
            {
                var matched_delimiter = match.Groups[1].Value;

                if (matched_delimiter != ",")
                {
                    i = 1;
                    // Since this is a new row of data, add an empty row to the array.
                    Table.Rows.Add();
                    Table.Rows[Table.Rows.Count - 1][i] = match.Groups[2].Value;
                    i++;
                }
                else
                {
                    Table.Rows[Table.Rows.Count - 1][i] = match.Groups[2].Value.Replace("\"\"", "\"");
                    i++;
                }

            }

        }

    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SnitzCore.Extensions
{
    public static partial class Extensions
    {
        public static string ToString<T>(this IList<T> list, string include = "", string exclude = "")
        {
            //Variables for build string
            string propStr = string.Empty;
            StringBuilder sb = new StringBuilder();

            //Get property collection and set selected property list
            PropertyInfo[] props = typeof(T).GetProperties();
            List<PropertyInfo> propList = GetSelectedProperties(props, include, exclude);

            //Add list name and total count
            string typeName = GetSimpleTypeName(list);
            sb.AppendLine(string.Format("{0} List - Total Count: {1}", typeName, list.Count.ToString()));

            //Iterate through data list collection
            foreach (var item in list)
            {
                sb.AppendLine("");
                //Iterate through property collection
                foreach (var prop in propList)
                {
                    //Construct property name and value string
                    propStr = prop.Name + ": " + prop.GetValue(item, null);
                    sb.AppendLine(propStr);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Turns a List<typeparamref name="T"/> into a csv file
        /// </summary>
        /// <typeparam name="T">Type of Object</typeparam>
        /// <param name="list">List of objects to convert</param>
        /// <param name="path"></param>
        /// <param name="include">Comma seperated string of properties to include</param>
        /// <param name="exclude">Comma seperated string of properties to exclude</param>
        /// <remarks>
        /// <paramref name="include"/> + <paramref name="exclude"/> are mutually exclusive
        /// If <paramref name="include"/> is non empty, only those properties defined will be exported
        /// OR If <paramref name="exclude"/> is non empty, all properties except those defined will be included
        /// </remarks>
        /// <returns>CSV formatted string</returns>
        public static string ToCSV<T>(this IList<T> list, string path = "", string include = "", string exclude = "")
        {
            return CreateCsvFile(list, path, include, exclude);
        }



        private static string CreateCsvFile<T>(IList<T> list, string path, string include, string exclude)
        {
            //Variables for build CSV string
            StringBuilder sb = new StringBuilder();

            //Get property collection and set selected property list
            PropertyInfo[] props = typeof(T).GetProperties();
            List<PropertyInfo> propList = GetSelectedProperties(props, include, exclude);

            //Iterate through data list collection
            foreach (var item in list)
            {
                List<string> propValues = new List<string>();

                //Iterate through property collection
                foreach (var prop in propList)
                {
                    //Construct property value string with double quotes for issue of any comma in string type data
                    var val = prop.PropertyType == typeof(string) ? "\"{0}\"" : "{0}";
                    var propval = prop.GetValue(item, null);
                    if (propval == null)
                    {
                        propValues.Add(null);
                    }
                    else
                    {
                        propValues.Add(string.Format(val, ((string)propval).Replace("\"", "'")));
                    }
                }

                string line = string.Empty;
                //Add line for the values
                line = string.Join(",", propValues);
                sb.AppendLine(line);
            }
            if (!string.IsNullOrEmpty(sb.ToString()) && path != "")
            {
                //MemoryStream memoryStream = new MemoryStream();
                //TextWriter tw = new StreamWriter(memoryStream); 

                //tw.Write(sb.ToString());
                return sb.ToString();
                //File.WriteAllText(path, sb.ToString());
            }
            return null;
        }

        private static List<PropertyInfo> GetSelectedProperties(PropertyInfo[] props, string include, string exclude)
        {
            List<PropertyInfo> propList = new List<PropertyInfo>();
            if (include != "") //Do include first
            {
                var includeProps = include.ToLower().Split(',').ToList();
                foreach (var item in props)
                {
                    var propName = includeProps.FirstOrDefault(a => a == item.Name.ToLower());
                    if (!string.IsNullOrEmpty(propName))
                        propList.Add(item);
                }
            }
            else if (exclude != "") //Then do exclude
            {
                var excludeProps = exclude.ToLower().Split(',');
                foreach (var item in props)
                {
                    var propName = excludeProps.FirstOrDefault(a => a == item.Name.ToLower());
                    if (string.IsNullOrEmpty(propName))
                        propList.Add(item);
                }
            }
            else //Default
            {
                propList.AddRange(props.ToList());
            }
            return propList;
        }

        private static string GetSimpleTypeName<T>(IList<T> list)
        {
            string typeName = list.GetType().ToString();
            int pos = typeName.IndexOf("[", System.StringComparison.CurrentCulture) + 1;
            typeName = typeName.Substring(pos, typeName.LastIndexOf("]", System.StringComparison.CurrentCulture) - pos);
            typeName = typeName.Substring(typeName.LastIndexOf(".", System.StringComparison.CurrentCulture) + 1);
            return typeName;
        }
    }
}

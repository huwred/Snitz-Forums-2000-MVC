
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace SnitzCore.Extensions
{
    public static partial class Extensions
    {
        /// <summary>
        /// Extension method to check if a list of enumerator values contains the required value
        /// </summary>
        /// <typeparam name="T">Enumerator Type</typeparam>
        /// <param name="val">Value to check</param>
        /// <param name="values">List of values to check</param>
        /// <returns></returns>
        public static bool In<T>(this T val, params T[] values) where T : struct
        {
            return values.ToList().Contains(val);
        }

        /// <summary>
        /// Converts an Emerated type into key value pairs
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumObj"></param>
        /// <returns>SelectList</returns>
        public static SelectList ToSelectList<TEnum>(this TEnum enumObj)
            where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            var values = from TEnum e in Enum.GetValues(typeof(TEnum))
                         select new { Id = e.ToInt32(CultureInfo.InvariantCulture), Name = LangResources.Utility.ResourceManager.GetLocalisedString(e.GetType().Name + "_" + e) };
            return new SelectList(values, "Id", "Name", enumObj.ToInt32(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add or update Dictionary items
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source">Dictionary to update</param>
        /// <param name="key">Key to Add/Update</param>
        /// <param name="value">New Value</param>
        public static void CreateNewOrUpdateExisting<TKey, TValue>(
                this IDictionary<TKey, TValue> source, TKey key, TValue value)
        {
            if (source.ContainsKey(key))
            {
                source[key] = value;
            }
            else
            {
                source.Add(key, value);
            }

        }

    }
}
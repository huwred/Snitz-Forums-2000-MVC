using System;
using System.Reflection;
using PetaPoco;

namespace Snitz.Base
{
    /// <summary>
    /// Maps some columns in Snitz tables to Dates, Enums etc
    /// </summary>
    public class SnitzMapper : IMapper
    {

        public TableInfo GetTableInfo(Type pocoType)
        {
            return TableInfo.FromPoco(pocoType);
        }

        public ColumnInfo GetColumnInfo(PropertyInfo pocoProperty)
        {
            return ColumnInfo.FromProperty(pocoProperty);
        }

        /// <summary>
        /// Maps forum date strings into datetime objects
        /// and forum status columns into enums
        /// </summary>
        /// <param name="pi"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public Func<object, object> GetFromDbConverter(PropertyInfo pi, Type sourceType)
        {
            
            return src =>
                   {
                       if (sourceType == typeof(string)
                           && pi.Name.ToLower().Contains("date"))
                       {
                           try
                           {
                               if (!String.IsNullOrWhiteSpace(src.ToString()))
                               {
                                   //CultureInfo ci = CultureInfo.CreateSpecificCulture("en-GB");
                                   return src.ToString().ToSnitzDateTime();
                               }
                               return DateTime.MinValue;

                           }
                           catch (Exception ex)
                           {
                               throw new Exception(String.Format("Date convert error:{0}-{1}", pi.Name, src.ToString()), ex);
                           }
                       }
                       try
                       {
                           if (sourceType == typeof(Int16) && pi.PropertyType == typeof(string))
                           {
                               return src != null ? src.ToString() : "0";
                           }
                           if (sourceType == typeof(Int64))
                           {
                               return src != null ? Convert.ToInt32(src) : 0;
                           }
                           if (sourceType == typeof(UInt64)) //mysql returns bit as uint64
                           {
                               return src != null && Convert.ToBoolean(src);
                           }
                           if (pi.PropertyType == typeof (Int64))
                           {
                               return src != null ? Convert.ToInt64(src) : 0;
                           }
                           if (pi.Name.Equals("Archived"))
                           {
                               return src != null ? Convert.ToInt32(src) : 0;
                           }
                           if (sourceType == typeof (short) && pi.Name.Contains("Status"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.PostStatus), src) : Enumerators.PostStatus.Open;
                           }
                           if (pi.Name.Equals("Type"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.ForumType), src) : Enumerators.ForumType.Topics;
                           }
                           if (pi.Name.Contains("Moderation"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.Moderation), src) : Enumerators.Moderation.UnModerated;
                           }
                           if (pi.Name.Contains("Subscription"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.Subscription), src) : Enumerators.Subscription.None;
                           }
                           if (pi.Name.Contains("DefaultDays"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.ForumDays), src) : Enumerators.ForumDays.Last30Days;
                           }
                           if (pi.Name.Contains("PrivateForums"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.ForumAuthType), src) : Enumerators.ForumAuthType.All;
                           }
                           if (pi.Name.Contains("PollsAuth"))
                           {
                               return src != null ? Enum.ToObject(typeof(Enumerators.PollAuth), src) : Enumerators.PollAuth.Disallow;
                           }

                           if (pi.PropertyType == typeof (Enum))
                           {
                                return src != null ? Enum.ToObject(pi.PropertyType, src) : 0;
                               
                           }
                       }
                       catch (Exception ex)
                       {
                           throw new Exception(String.Format("Property convert error:{0}-{1}", pi.Name, src), ex);
                       }
                       return src;
                   };

        }

        public Func<object, object> GetToDbConverter(PropertyInfo pi)
        {
            if (pi.PropertyType == typeof(DateTime) || pi.PropertyType == typeof(DateTime?))
            {
                return (x) =>
                       {
                           if (x == null)
                               return null;
                           var date = (DateTime) x;
                           if (date == DateTime.MinValue)
                               return String.Empty;

                           return date.ToString("yyyyMMddHHmmss");
                       };
            }
            if (pi.PropertyType == typeof(Enumerators))
            {
                return (x) => ((int)x);
            }

            return null;
        }
    }
}
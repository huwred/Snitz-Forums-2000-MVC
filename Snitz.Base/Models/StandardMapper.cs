using System;
using System.Globalization;
using System.Reflection;
using PetaPoco;


namespace Snitz.Base
{
    /// <summary>
    /// StandardMapper is the default implementation of IMapper used by PetaPoco
    /// </summary>
    public class StandardMapper : PetaPoco.IMapper
    {
        /// <summary>
        /// Constructs a TableInfo for a POCO by reading its attribute data
        /// </summary>
        /// <param name="pocoType">The POCO Type</param>
        /// <returns></returns>
        public TableInfo GetTableInfo(Type pocoType)
        {
            return TableInfo.FromPoco(pocoType);
        }

        /// <inheritdoc />
        /// <summary>
        /// Constructs a ColumnInfo for a POCO property by reading its attribute data
        /// </summary>
        /// <param name="pocoProperty"></param>
        /// <returns></returns>
        public ColumnInfo GetColumnInfo(PropertyInfo pocoProperty)
        {
            return ColumnInfo.FromProperty(pocoProperty);
        }

        /// <inheritdoc />
        /// <summary>
        /// Maps forum date strings into datetime objects
        /// and forum status columns into enums
        /// </summary>
        /// <remarks>Updated to Snitz specific Mapper</remarks>
        /// <param name="targetProperty"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public Func<object, object> GetFromDbConverter(PropertyInfo targetProperty, Type sourceType)
        {

            return src =>
            {
                if (sourceType == typeof(string)
                    && targetProperty.Name.ToLower().Contains("date"))
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
                        throw new Exception(String.Format("Date convert error:{0}-{1}", targetProperty.Name, src.ToString()), ex);
                    }
                }
                try
                {
                    if (sourceType == typeof(Int16) && targetProperty.PropertyType == typeof(string))
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
                    if (targetProperty.PropertyType == typeof(Int64))
                    {
                        return src != null ? Convert.ToInt64(src) : 0;
                    }
                    if (targetProperty.Name.Equals("Archived"))
                    {
                        return src != null ? Convert.ToInt32(src) : 0;
                    }
                    if (sourceType == typeof(short) && targetProperty.Name.Contains("Status"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.PostStatus), src) : Enumerators.PostStatus.Open;
                    }
                    if (targetProperty.Name.Equals("Type"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.ForumType), src) : Enumerators.ForumType.Topics;
                    }
                    if (targetProperty.Name.Contains("Moderation"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.Moderation), src) : Enumerators.Moderation.UnModerated;
                    }
                    if (targetProperty.Name.Contains("Subscription"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.Subscription), src) : Enumerators.Subscription.None;
                    }
                    if (targetProperty.Name.Contains("DefaultDays"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.ForumDays), src) : Enumerators.ForumDays.Last30Days;
                    }
                    if (targetProperty.Name.Contains("PrivateForums"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.ForumAuthType), src) : Enumerators.ForumAuthType.All;
                    }
                    if (targetProperty.Name.Contains("PollsAuth"))
                    {
                        return src != null ? Enum.ToObject(typeof(Enumerators.PollAuth), src) : Enumerators.PollAuth.Disallow;
                    }
                    if (targetProperty.PropertyType == typeof(Enum))
                    {
                        return src != null ? Enum.ToObject(targetProperty.PropertyType, src) : 0;

                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Property convert error:{0}-{1}", targetProperty.Name, src), ex);
                }
                return src;
            };

        }

        public Func<object, object> GetToDbConverter(PropertyInfo sourceProperty)
        {
            if (sourceProperty.PropertyType == typeof(DateTime) || sourceProperty.PropertyType == typeof(DateTime?))
            {
                return (x) =>
                {
                    if (x == null)
                        return null;
                    var date = (DateTime)x;
                    if (date == DateTime.MinValue)
                        return String.Empty;

                    return date.ToString("yyyyMMddHHmmss");
                };
            }
            if (sourceProperty.PropertyType == typeof(Enumerators))
            {
                return (x) => ((int)x);
            }

            return null;
        }

    }
}

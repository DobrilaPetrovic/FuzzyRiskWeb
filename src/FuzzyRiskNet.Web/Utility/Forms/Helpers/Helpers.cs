using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Text;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using MVCFormsLibrary;
using System.Reflection;
using System.Linq.Expressions;
using Nik.Linq.Dynamic;
using Nik.Expressions;
using FuzzyRiskNet.Libraries.Forms;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace FuzzyRiskNet.Libraries.Helpers
{
    public class LineNotFoundException : Exception { public LineNotFoundException(string Content) : base("Line with content '" + Content + "' not found.") { } }
    public static class Helper
    {
        public static string ReplaceNull<T>(this T Obj, Func<T, string> IfNotNull, string IfNull = "")
        {
            if (Obj == null) return IfNull;
            return IfNotNull(Obj);
        }

        public static T Do<T>(this T Obj, Action<T> Act)
        {
            Act(Obj);
            return Obj;
        }

        public static V Get<T, V>(this T Obj, Func<T, V> Func)
        {
            return Func(Obj);
        }

        public static T[] ToSingleArray<T>(this T Obj)
        {
            return new T[] { Obj };
        }

        public static T2 GetValueSafe<T, T2>(this Dictionary<T, T2> Dic, T Key) where T2 : class
        {
            T2 val;
            if (Dic.TryGetValue(Key, out val)) return val;
            return null;
        }

        public static void SetValueSafe<T, T2>(this Dictionary<T, T2> Dic, T Key, T2 Value) where T2 : class
        {
            if (Value != null)
            {
                if (Dic.ContainsKey(Key)) Dic[Key] = Value; else Dic.Add(Key, Value);
            }
            else
                if (Dic.ContainsKey(Key)) Dic.Remove(Key);
        }

        public static Nullable<T2> GetNullableValueSafe<T, T2>(this Dictionary<T, Nullable<T2>> Dic, T Key) where T2 : struct
        {
            Nullable<T2> val;
            if (Dic.TryGetValue(Key, out val)) return val;
            return null;
        }

        public static Nullable<T2> GetNullableValueSafe<T, T2>(this Dictionary<T, T2> Dic, T Key) where T2 : struct
        {
            T2 val;
            if (Dic.TryGetValue(Key, out val)) return val;
            return null;
        }

        public static void SetValueSafe<T, T2>(this Dictionary<T, Nullable<T2>> Dic, T Key, Nullable<T2> Value) where T2 : struct
        {
            if (Dic.ContainsKey(Key)) Dic[Key] = Value; else Dic.Add(Key, Value);
        }

        public static void SetValueSafe<T, T2>(this Dictionary<T, T2> Dic, T Key, Nullable<T2> Value) where T2 : struct
        {
            if (Value != null)
            {
                if (Dic.ContainsKey(Key)) Dic[Key] = Value.Value; else Dic.Add(Key, Value.Value);
            }
            else
                if (Dic.ContainsKey(Key)) Dic.Remove(Key);
        }

        public static int FindLineIndex(this string[] Lines, string Content, int AfterIndex = -1)
        {
            for (int i = AfterIndex + 1; i < Lines.Length; i++)
                if (Lines[i].Trim() == Content) return i;
            throw new LineNotFoundException(Content);
        }

        public static int FindLineStartWithIndex(this string[] Lines, string Content, int AfterIndex = -1)
        {
            for (int i = AfterIndex + 1; i < Lines.Length; i++)
                if (Lines[i].Trim().StartsWith(Content)) return i;
            throw new LineNotFoundException(Content);
        }

        public static string[] PersianMonths = new string[] 
                    { 
                        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
                        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" 
                    };

        public static string ToPersianString(this double Input)
        { return ToPersianString(Input.ToString()); }

        public static string ToPersianString(this double? Input)
        {
            if (!Input.HasValue) return "";
            return ToPersianString(Input.Value.ToString());
        }

        public static string ToPersianString(this int Input)
        { return ToPersianString(Input.ToString()); }

        public static string ToPersianString(this int? Input)
        {
            if (!Input.HasValue) return "";
            return ToPersianString(Input.Value.ToString());
        }

        public static string ToPersianString(this decimal Input)
        {
            return ToPersianString(Input.ToString());
        }

        public static string ToPersianString(this decimal? Input)
        {
            if (!Input.HasValue) return "";
            return ToPersianString(Input.Value.ToString());
        }

        public static string ToPersianString(this string InputString)
        {
            if (InputString == null) return null;
            char[] allchar = InputString.ToCharArray();
            for (int i = 0; i < allchar.Length; i++)
            {
                int chind = (int)allchar[i];
                if (chind <= 0x39 && chind >= 0x30)
                    chind += 0x630;
                allchar[i] = (char)chind;
            }
            return new string(allchar);
        }

        static PersianCalendar pc = new PersianCalendar();

        public static string GetPersianMonthFull(this DateTime date)
        {
            return PersianMonths[pc.GetMonth(date) - 1];
        }
        public static string GetPersianYear(this DateTime date)
        {
            return pc.GetYear(date).ToString().ToPersianString();
        }

        public static string GetPersianMonth(this DateTime date)
        {
            return pc.GetMonth(date).ToString().ToPersianString();
        }

        public static string GetPersianDayOfMonth(this DateTime date)
        {
            return pc.GetDayOfMonth(date).ToString().ToPersianString();
        }
        
        public static string ToPersianDateFull(this DateTime date)
        {            
            return String.Format("{0} {1} {2}", date.GetPersianDayOfMonth(), date.GetPersianMonthFull(), date.GetPersianYear());
        }
        public static string ToPersianDateTimeFull(this DateTime date)
        {
            var min = date.TimeOfDay.Minutes < 10 ? "0" + date.TimeOfDay.Minutes.ToString() : date.TimeOfDay.Minutes.ToString();
            return String.Format("{0} {1} {2}:{3}", date.ToPersianDateFull(), "ساعت", 
                date.TimeOfDay.Hours.ToString().ToPersianString(), min.ToPersianString());
        }

        public static string ToPersianDateTimeConcise(this DateTime date)
        {
            return String.Format("{2:D4}/{1:D2}/{0:D2} {3:D2}:{4:D2}", pc.GetDayOfMonth(date), pc.GetMonth(date), 
                pc.GetYear(date), date.Hour, date.Minute).ToPersianString();
        }

        public static string ToPersianDateBrief(this DateTime date)
        {
            return String.Format("{2}/{1}/{0}", date.GetPersianDayOfMonth(), date.GetPersianMonth(), date.GetPersianYear());
        }

        public static string ToPersianDateTimeStamp(this DateTime date)
        {
            return String.Format("{2:D4}{1:D2}{0:D2} {3:D2}{4:D2}{5:D2}", pc.GetDayOfMonth(date), pc.GetMonth(date), pc.GetYear(date), date.Hour, date.Minute, date.Second);
        }

        public static MvcHtmlString MyInsert(this HtmlHelper html, string ActionName, object routeValues)
        {
            return html.ActionLink(Messages.InsertTitle, ActionName ?? "Insert", routeValues);
        }
        public static MvcHtmlString MyInsert(this HtmlHelper html, string ActionName)
        {
            return html.MyInsert(ActionName, null);
        }
        public static MvcHtmlString MyInsert(this HtmlHelper html)
        {
            return html.MyInsert(null);
        }
        public static MvcHtmlString MyReturnList(this HtmlHelper html)
        {
            return html.ActionLink(Messages.ReturnToListTitle, "Index", null, new { @class = "ActionButton" });
        }
                
        public static string CreateQueryString(NameValueCollection values, List<string> SkipKeys)
        {
            var builder = new StringBuilder();

            foreach (string key in values.Keys)
                if (!SkipKeys.Contains(key))
                    foreach (var value in values.GetValues(key))
                    {
                        builder.AppendFormat("&{0}={1}", key, HttpUtility.UrlEncode(value));
                    }

            return builder.ToString();
        }

        private static string CreateQueryString(NameValueCollection values, string PageQueryName)
        {
            var builder = new StringBuilder();

            foreach (string key in values.Keys)
            {
                if (key == PageQueryName)
                //Don't re-add any existing 'page' variable to the querystring - this will be handled in CreatePageLink.
                {
                    continue;
                }

                foreach (var value in values.GetValues(key))
                {
                    builder.AppendFormat("&{0}={1}", key, HttpUtility.UrlEncode(value));
                }
            }

            return builder.ToString();
        }
        /*public static IGridWithOptions<T> RowAlternateColor<T>(this IGridWithOptions<T> grid) where T : class
        {
            grid.Model.Sections.RowStart(a => (a.IsAlternate) ? "<tr class='tr-alt-item'>" : "<tr>");
            return grid;
        }*/
        public static int? ParseIntSafe(this string Value)
        {
            int val;
            if (int.TryParse(Value, out val)) return val;
            return null;
        }
        public static IQueryable<T> OrderBy<T, TKey>(this IQueryable<T> list, System.Linq.Expressions.Expression<Func<T, TKey>> KeySelector, SortDirection Direction)
        {
            if (Direction == SortDirection.Ascending)
                return list.OrderBy(KeySelector);
            else 
                return list.OrderByDescending(KeySelector);
        }

        public static TimeSpan? GetTime(string strTime)
        {
            var timeparts = strTime.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (timeparts.Length != 2) throw new Exception("Bad Format");
            var timeofday = new TimeSpan(int.Parse(timeparts[0]), int.Parse(timeparts[1]), 0);
            if (int.Parse(timeparts[1]) > 59) throw new Exception("Bad Format");
            if (int.Parse(timeparts[0]) > 23) throw new Exception("Bad Format");
            return timeofday;
        }

        public static DateTime? GetDate(string strDate, bool HasTime)
        {
            strDate = strDate.Trim();

            if (strDate == "") return null;

            TimeSpan timeofday = TimeSpan.Zero;
            if (HasTime)
            {
                var datetimeparts = strDate.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (datetimeparts.Length > 2) throw new Exception("Bad Format");
                if (datetimeparts.Length == 2)
                {
                    strDate = datetimeparts[0];
                    var timeparts = datetimeparts[1].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (timeparts.Length != 2) throw new Exception("Bad Format");
                    timeofday = new TimeSpan(int.Parse(timeparts[0]), int.Parse(timeparts[1]), 0);
                    if (timeofday.Hours > 23) throw new Exception("Bad Format");
                }
            }

            var parts = strDate.Split(new char[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) throw new Exception("Bad Format");

            var year = int.Parse(parts[0]);
            var month = int.Parse(parts[1]);
            var day = int.Parse(parts[2]);

            System.Globalization.PersianCalendar pc = new PersianCalendar();
            return pc.ToDateTime(year, month, day, timeofday.Hours, timeofday.Minutes, 0, 0);
        }

        public static bool ValidDate(string strDate, bool HasTime, bool CheckDateInPast)
        {
            try
            {
                var d = GetDate(strDate, HasTime);
                if (CheckDateInPast && d > DateTime.Now.AddDays(1)) return false;
                return true;
            }
            catch
            { return false; }
        }

        public static DateTime? GetDateSafe(string strDate, bool HasTime)
        {
            try
            {
                return GetDate(strDate, HasTime);
            }
            catch
            { return null; }
        }

        public static bool ValidTime(string strDate)
        {
            try
            {
                var d = GetTime(strDate);
                return true;
            }
            catch
            { return false; }
        }

        public static TimeSpan? GetTimeSafe(string strTime)
        {
            try
            {
                return GetTime(strTime);
            }
            catch
            { return null; }
        }

        public static string GetPersianDate(DateTime Value, bool HasTime)
        {
            return String.Format(HasTime ? "{0}/{1}/{2} {3}:{4}" : "{0}/{1}/{2}", pc.GetYear(Value).ToString("0000"), pc.GetMonth(Value).ToString("00"), pc.GetDayOfMonth(Value).ToString("00"), Value.Hour.ToString("00"), Value.Minute.ToString("00"));
        }


        /// <summary>
        /// Search for a method by name and parameter types.  Unlike GetMethod(), does 'loose' matching on generic
        /// parameter types, and searches base interfaces.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"/>
        public static MethodInfo GetMethodExt(this Type thisType, string name, params Type[] parameterTypes)
        {
            return GetMethodExt(thisType, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, parameterTypes);
        }

        /// <summary>
        /// Search for a method by name, parameter types, and binding flags.  Unlike GetMethod(), does 'loose' matching on generic
        /// parameter types, and searches base interfaces.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"/>
        public static MethodInfo GetMethodExt(this Type thisType, string name, BindingFlags bindingFlags, params Type[] parameterTypes)
        {
            MethodInfo matchingMethod = null;

            // Check all methods with the specified name, including in base classes
            GetMethodExt(ref matchingMethod, thisType, name, bindingFlags, parameterTypes);

            // If we're searching an interface, we have to manually search base interfaces
            if (matchingMethod == null && thisType.IsInterface)
            {
                foreach (Type interfaceType in thisType.GetInterfaces())
                    GetMethodExt(ref matchingMethod, interfaceType, name, bindingFlags, parameterTypes);
            }

            return matchingMethod;
        }

        private static void GetMethodExt(ref MethodInfo matchingMethod, Type type, string name, BindingFlags bindingFlags, params Type[] parameterTypes)
        {
            // Check all methods with the specified name, including in base classes
            foreach (MethodInfo methodInfo in type.GetMember(name, MemberTypes.Method, bindingFlags))
            {
                // Check that the parameter counts and types match, with 'loose' matching on generic parameters
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                if (parameterInfos.Length == parameterTypes.Length)
                {
                    int i = 0;
                    for (; i < parameterInfos.Length; ++i)
                    {
                        if (!parameterInfos[i].ParameterType.IsSimilarType(parameterTypes[i]))
                            break;
                    }
                    if (i == parameterInfos.Length)
                    {
                        if (matchingMethod == null)
                            matchingMethod = methodInfo;
                        else
                            throw new AmbiguousMatchException("More than one matching method found!");
                    }
                }
            }
        }

        /// <summary>
        /// Special type used to match any generic parameter type in GetMethodExt().
        /// </summary>
        public class T
        { }

        /// <summary>
        /// Determines if the two types are either identical, or are both generic parameters or generic types
        /// with generic parameters in the same locations (generic parameters match any other generic paramter,
        /// but NOT concrete types).
        /// </summary>
        private static bool IsSimilarType(this Type thisType, Type type)
        {
            // Ignore any 'ref' types
            if (thisType.IsByRef)
                thisType = thisType.GetElementType();
            if (type.IsByRef)
                type = type.GetElementType();

            // Handle array types
            if (thisType.IsArray && type.IsArray)
                return thisType.GetElementType().IsSimilarType(type.GetElementType());

            // If the types are identical, or they're both generic parameters or the special 'T' type, treat as a match
            if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof(T)) && (type.IsGenericParameter || type == typeof(T))))
                return true;

            // Handle any generic arguments
            if (thisType.IsGenericType && type.IsGenericType)
            {
                Type[] thisArguments = thisType.GetGenericArguments();
                Type[] arguments = type.GetGenericArguments();
                if (thisArguments.Length == arguments.Length)
                {
                    for (int i = 0; i < thisArguments.Length; ++i)
                    {
                        if (!thisArguments[i].IsSimilarType(arguments[i]))
                            return false;
                    }
                    return true;
                }
            }

            return false;
        }

        public static T GetAttr<T>(this System.Reflection.PropertyInfo Prop, bool inherit = false) where T : Attribute
        {
            return Prop.GetCustomAttributes(typeof(T), inherit).FirstOrDefault() as T;
        }

        public static IEnumerable<T> GetAttrs<T>(this Type Type, bool inherit = false) where T : Attribute
        {
            return Type.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

    }
}
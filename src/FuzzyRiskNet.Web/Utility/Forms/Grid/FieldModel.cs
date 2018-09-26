using System;
using System.Collections.Generic;
using System.Linq;
using Nik.Helpers;
using Nik.Expressions;
using System.Linq.Expressions;
using Nik.Linq.Dynamic;
using MVCFormsLibrary;
using FuzzyRiskNet.Libraries.Forms;
using FuzzyRiskNet.Fuzzy;
using System.Web.Mvc;
using System.Web;
using System.Web.Mvc.Html;
using FuzzyRiskNet.Libraries.Helpers;

namespace FuzzyRiskNet.Libraries.Grid
{
    public interface IColumnModel
    {
        string HeaderName { get; set; }
        string ClassName { get; set; }
        bool Encode { get; }
        bool IsHidden { get; }
    }

    public interface IColumnModel<T> : IColumnModel
    {
        MvcHtmlString RenderValue(HtmlHelper html, T item);
    }

    public interface ISortableFieldModel : IColumnModel
    {
        string FieldName { get; }
        bool Sortable { get; }
    }

    public interface ITypedValueColumnModel<T2> : IColumnModel
    {
    }

    public interface ISortableFieldModel<T> : IColumnModel<T>, ISortableFieldModel
    {
        IOrderedQueryable<T> Sort(IQueryable<T> Query, bool Asc);
    }
    
    public interface IDynamicFieldModel<T> : IColumnModel<T>
    {
        Expression<Func<T, object>> GetDataExp { get; }
        MvcHtmlString RenderValueDynamic(HtmlHelper html, object dataitem);
    }
    public interface IActionColumnModel : IColumnModel { }
    public class ActionColumnModel<T> : IDynamicFieldModel<T>, IColumnModel<T>, IActionColumnModel
    {
        public string HeaderName { get; set; }
        public string ClassName { get; set; } 
        public string FormatString { get; set; }
        public bool Encode { get { return false; } }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public bool IsHidden { get; set; }

        public Expression<Func<T, object>> GetParams { get { return _GetValue; } set { _GetValue = value; Dirty = true; } }
        Expression<Func<T, object>> _GetValue = null;
        public Expression<Func<T, bool>> CellCondition { get { return _CellCondition; } set { _CellCondition = value; Dirty = true; } }
        Expression<Func<T, bool>> _CellCondition = null;
        public Expression<Func<T, string>> GetOtherParams { get { return _GetOtherParams; } set { _GetOtherParams = value; Dirty = true; } }
        Expression<Func<T, string>> _GetOtherParams = null;

        public Expression<Func<T, object>> GetDataExp { get { CompileExp(); return _GetDataExp; } }
        Expression<Func<T, object>> _GetDataExp;
        Func<T, object> CompiledFunc = null;
        bool Dirty = true;

        void CompileExp()
        {
            if (!Dirty) return; else Dirty = false;
            _GetDataExp = BaseColumnData<object>.CombineExp<T>(_GetValue, _CellCondition, _GetOtherParams);
            CompiledFunc = _GetDataExp.Compile();
        }
        public MvcHtmlString RenderValue(HtmlHelper html, T item)
        {
            if (Dirty) CompileExp();
            return RenderValueDynamic(html, (BaseColumnData<object>)CompiledFunc(item));
        }
        public virtual MvcHtmlString RenderValueDynamic(HtmlHelper html, object dataitem)
        {
            var di = (BaseColumnData<object>)dataitem;
            if (!di.Condition) return new MvcHtmlString("");
            if (di.OtherParams == null)
                return html.ActionLink(HeaderName, ActionName, ControllerName, di.Value, null);
            else
                return html.ActionLink(String.Format(FormatString, di.OtherParams), ActionName, ControllerName, di.Value, null);
        }

    }
    public class DeleteColumnModel<T> : ActionColumnModel<T>
    {
        public DeleteColumnModel()
        {
            ActionName = "Delete";
            HeaderName = Messages.DeleteTitle;
            Post = false;
        }
        public override MvcHtmlString RenderValueDynamic(HtmlHelper html, object dataitem)
        {
            var di = (BaseColumnData<object>)dataitem;
            if (!di.Condition) return new MvcHtmlString("");
            if (Post)
            {
                string ret = string.Format("<form action='{0}' method='post'>", new UrlHelper(html.ViewContext.RequestContext).Action(ActionName, di.Value));
                ret += "<a href='#' onclick=\"if (confirm('" + Messages.DeleteConfirmationMessage + "')) this.parentNode.submit(); return false; \">" + HeaderName + "</a>";
                ret += "</form>";
                return new MvcHtmlString(ret);
            }
            else
                return html.ActionLink(HeaderName, ActionName, di.Value, new { onclick = "return confirm('" + Messages.DeleteConfirmationMessage + "');" });
        }
        public bool Post { get; set; }
    }

    public class BaseColumnDataOnlyValue<T2> : BaseColumnData<T2> { }
    public class BaseColumnDataOnlyValueCondition<T2> : BaseColumnData<T2> { }

    public class BaseColumnData<T2>
    {
        public BaseColumnData() { Condition = true; }
        public T2 Value { get; set; }
        public bool Condition { get; set; }
        public string OtherParams { get; set; }

        public static Expression<Func<T, object>> CombineExp<T>(Expression<Func<T, T2>> GetValue, Expression<Func<T, bool>> CellCondition)
        {
            var exp1 = GetValue ?? ((T item) => default(T2));
            if (CellCondition == null)
            {
                return Expression.Lambda<Func<T, object>>(
                    Expression.MemberInit(
                        Expression.New(typeof(BaseColumnDataOnlyValue<T2>)),
                        Expression.Bind(typeof(BaseColumnDataOnlyValue<T2>).GetMember("Value")[0], exp1.Body)),
                    exp1.Parameters[0]);
            }
            else
            {
                var exp2 = CellCondition ?? ((T item) => true);

                return Expression.Lambda<Func<T, object>>(
                    Expression.MemberInit(
                        Expression.New(typeof(BaseColumnDataOnlyValueCondition<T2>)),
                        Expression.Bind(typeof(BaseColumnDataOnlyValueCondition<T2>).GetMember("Value")[0], exp1.Body),
                        Expression.Bind(typeof(BaseColumnDataOnlyValueCondition<T2>).GetMember("Condition")[0],
                            ParameterRebinder.ReplaceParameters(exp2.Parameters[0], exp1.Parameters[0], exp2.Body))),
                    exp1.Parameters[0]);
            }
        }
        public static Expression<Func<T, object>> CombineExp<T>(Expression<Func<T, T2>> GetValue, Expression<Func<T, bool>> CellCondition, Expression<Func<T, string>> GetOtherParams)
        {
            if (GetOtherParams == null) return CombineExp(GetValue, CellCondition);
            var exp1 = GetValue ?? ((T item) => default(T2));
            var exp2 = CellCondition ?? ((T item) => true);
            var exp3 = GetOtherParams ?? ((T item) => null);

            return Expression.Lambda<Func<T, object>>(
                Expression.MemberInit(
                    Expression.New(typeof(BaseColumnData<T2>)),
                    Expression.Bind(typeof(BaseColumnData<T2>).GetMember("Value")[0], exp1.Body),
                    Expression.Bind(typeof(BaseColumnData<T2>).GetMember("Condition")[0],
                        ParameterRebinder.ReplaceParameters(exp2.Parameters[0], exp1.Parameters[0], exp2.Body)), 
                    Expression.Bind(typeof(BaseColumnData<T2>).GetMember("OtherParams")[0],
                        ParameterRebinder.ReplaceParameters(exp3.Parameters[0], exp1.Parameters[0], exp3.Body))
                        ),
                exp1.Parameters[0]);
        }
    }

    public abstract class BaseColumnModel<T, T2> : IDynamicFieldModel<T>, ISortableFieldModel<T>, ITypedValueColumnModel<T2>
    {
        public BaseColumnModel()
        {
            Sortable = true;
            Encode = true;
            IsHidden = false;
        }
        public string HeaderName { get; set; }
        public string ClassName { get; set; }
        public bool Encode { get; protected set; }
        public string FieldName { get; set; }
        public bool IsHidden { get; set; }

        public Expression<Func<T, T2>> GetValue { get { return _GetValue; } set { _GetValue = value; Dirty = true;  } }
        Expression<Func<T, T2>> _GetValue = null;
        public Expression<Func<T, bool>> CellCondition { get { return _CellCondition; } set { _CellCondition = value; Dirty = true; } }
        Expression<Func<T, bool>> _CellCondition = null;

        public Expression<Func<T, object>> GetDataExp { get { CompileExp(); return _GetDataExp; } }
        Expression<Func<T, object>> _GetDataExp;
        Func<T, object> CompiledFunc = null;
        bool Dirty = true;

        void CompileExp()
        {
            if (!Dirty) return; else Dirty = false;
            _GetDataExp = BaseColumnData<T2>.CombineExp<T>(_GetValue, _CellCondition);
            CompiledFunc = _GetDataExp.Compile();
        }

        public bool Sortable { get; set; }

        public virtual IOrderedQueryable<T> Sort(IQueryable<T> Query, bool Asc)
        {
            if (OverrideSort == null)
                if (Asc)
                    return Query.OrderBy(GetValue);
                else
                    return Query.OrderByDescending(GetValue);
            else
                if (Asc)
                    return Query.OrderBy(OverrideSort);
                else
                    return Query.OrderByDescending(OverrideSort);
        }
        public System.Linq.Expressions.Expression<Func<T, double>> OverrideSort { get; set; }

        public abstract string ConvertString(T2 value);

        public MvcHtmlString RenderValue(HtmlHelper html, T item)
        {
            if (Dirty) CompileExp();
            return RenderValueDynamic(html, (BaseColumnData<T2>)CompiledFunc(item));
        }
        public MvcHtmlString RenderValueDynamic(HtmlHelper html, object dataitem)
        {
            var di = (BaseColumnData<T2>)dataitem;
            if (!di.Condition) return new MvcHtmlString("");
            return new MvcHtmlString(ConvertString(di.Value));
        }
    }

    public class StringColumnModel<T> : BaseColumnModel<T, string>
    {
        public override string ConvertString(string value) { return value; }
    }
    public class EnumStringColumnModel<T> : BaseColumnModel<T, IEnumerable<string>>
    {
        public EnumStringColumnModel() { this.Sortable = false; }
        public string Separator { get; set; }
        public override string ConvertString(IEnumerable<string> value) { return string.Join(Separator ?? "", value.ToArray()); }
    }
    public class CustomToStringColumnModel<T, T2> : BaseColumnModel<T, T2>
    {
        public Func<T2, string> ConvertStringFunc { get; private set; }
        public CustomToStringColumnModel(Func<T2, string> ToString)
        {
            this.ConvertStringFunc = ToString;
        }
        public override string ConvertString(T2 value)
        {
            return ConvertStringFunc(value);
        }
    }
    public class DoubleColumnModel<T> : BaseColumnModel<T, double>
    {
        public override string ConvertString(double value) 
        { 
            var str = value.ToString(FormatString ?? "0." + String.Concat(Enumerable.Range(0, Math.Min(Math.Max((int)-Math.Log10(Math.Abs(value)) + 3, 0), 12)).Select(i => "#")));
            return PersianNumber ? str.ToPersianString() : str; 
        }
        public bool PersianNumber { get; set; }
        public string FormatString { get; set; }
    }
    public class DecimalColumnModel<T> : BaseColumnModel<T, decimal>
    {
        public override string ConvertString(decimal value) { return PersianNumber ? value.ToString("g0").ToPersianString() : value.ToString(); }
        public bool PersianNumber { get; set; }
    }
    public class IntColumnModel<T> : BaseColumnModel<T, int>
    {
        public override string ConvertString(int value) 
        {
            if (DoNotShowZero && value == 0) return "";
            return PersianNumber ? value.ToPersianString() : value.ToString(); 
        }
        public bool PersianNumber { get; set; }
        public bool DoNotShowZero { get; set; }
    }

    public class IntNullColumnModel<T> : BaseColumnModel<T, int?>
    {
        public string EmptyString { get; set; }
        public override string ConvertString(int? value) {
            if (!value.HasValue) return EmptyString;
            return PersianNumber ? value.ToPersianString() : value.ToString(); 
        }
        public bool PersianNumber { get; set; }
    }

    public class DoubleNullColumnModel<T> : BaseColumnModel<T, double?>
    {
        public string EmptyString { get; set; }
        public override string ConvertString(double? value)
        {
            if (!value.HasValue) return EmptyString;
            return PersianNumber ? value.ToPersianString() : value.ToString();
        }
        public bool PersianNumber { get; set; }
    }

    public class DecimalNullColumnModel<T> : BaseColumnModel<T, decimal?>
    {
        public string EmptyString { get; set; }
        public override string ConvertString(decimal? value)
        {
            if (!value.HasValue) return EmptyString;
            return PersianNumber ? value.ToString().ToPersianString() : value.ToString();
        }
        public bool PersianNumber { get; set; }
    }

    public class DateColumnModel<T> : BaseColumnModel<T, DateTime>
    {
        public Func<DateTime, string> ConvertStringFunc { get; set; }
        public override string ConvertString(DateTime value) 
        {
            if (ConvertStringFunc == null)
                return value.ToPersianDateTimeFull();
            else
                return ConvertStringFunc(value);
        }
    }
    public class TFNColumnModel<T> : BaseColumnModel<T, TFN>
    {
        public TFNColumnModel() : base() { this.Sortable = false; }
        public string EmptyString { get; set; }
        public override string ConvertString(TFN value)
        {
            if (value == null) return EmptyString;
            return PersianNumber ? value.ToString().ToPersianString() : value.ToString();
        }
        public bool PersianNumber { get; set; }
    }
    public class DateNullColumnModel<T> : BaseColumnModel<T, DateTime?>
    {
        public Func<DateTime, string> ConvertStringFunc { get; set; }
        public string EmptyString { get; set; }
        public override string ConvertString(DateTime? value)
        {
            if (!value.HasValue || value == DateTime.MinValue) return EmptyString;
            if (ConvertStringFunc == null)
                return value.Value.ToPersianDateTimeFull();
            else
                return ConvertStringFunc(value.Value);
        }
    }
    public class TimeNullColumnModel<T> : BaseColumnModel<T, TimeSpan?>
    {
        public Func<TimeSpan, string> ConvertStringFunc { get; set; }
        public string EmptyString { get; set; }
        public override string ConvertString(TimeSpan? value)
        {
            if (!value.HasValue) return EmptyString;
            if (ConvertStringFunc == null)
                return value.Value.ToString(@"hh\:mm");
            else
                return ConvertStringFunc(value.Value);
        }
    }
    public class TimeSpanMinuteNullColumnModel<T> : BaseColumnModel<T, int?>
    {
        public bool IsSmart { get; set;  }
        public Func<int, string> ConvertStringFunc { get; set; }
        public string EmptyString { get; set; }
        public override string ConvertString(int? value)
        {
            if (!value.HasValue) return EmptyString;
            bool IsNegative = value < 0;
            if (IsNegative) value = -value;
            if (ConvertStringFunc == null)
            {
                if (IsSmart)
                {
                    if (value > 1440 * 30)
                        return String.Format("{1}{0} ماه", value / (1440 * 30), IsNegative ? "-" : "");
                    if (value > 1440)
                        return String.Format("{1}{0} روز", value / 1440, IsNegative ? "-" : "");
                    else if (value > 60)
                        return String.Format("{1}{0} ساعت", value / 60, IsNegative ? "-" : "");
                    else
                        return String.Format("{1}{0} دقیقه", value, IsNegative ? "-" : "");
                }
                else return String.Format("{2}{0}:{1}", value.Value / 60, (value.Value % 60).ToString("00"), IsNegative ? "-" : "").ToPersianString();
            }
            else
                return ConvertStringFunc(value.Value);
        }
    }

    public class FormatColumnModel<T, T2> : BaseColumnModel<T, T2> 
    {
        public FormatColumnModel() { Encode = false; }
        public string FormatString { get; set; }
        public string EmptyString { get; set; }
        public override string ConvertString(T2 value)
        {
            if (value == null) return EmptyString;
            if (FormatString == null)
                return EmptyString;
            else
                return String.Format(FormatString, value);
        }
    }
}

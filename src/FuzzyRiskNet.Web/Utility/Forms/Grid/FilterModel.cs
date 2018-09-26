using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using Nik.Expressions;
using System.Web.Mvc;

namespace FuzzyRiskNet.Libraries.Grid
{
    public class FilterModel<T> : IFilterModel
    {
        public FilterModel() { ShowPivot = ShowSubmit = true; FilterBoxID = "filterbox"; }
        public IFilterItem[] Items { get; set; }
        public bool ShowSubmit { get; set; }
        public bool ShowPivot { get; set; }
        public string FilterBoxID { get; set; }

        public IQueryable<T> Filter(IQueryable<T> Query)
        {
            foreach (var item in Items)
                if (item is IQueryFilterItem<T>)
                    Query = (item as IQueryFilterItem<T>).Filter(Query);
            return Query;
        }
        public void FillValues(NameValueCollection Dic)
        {
            foreach (var f in Items)
                if (f is IValueFilterItem) (f as IValueFilterItem).ParseValueSafe(Dic[f.Name] ?? "");
        }
        public void FillValues(Func<string, string> GetValue)
        {
            foreach (var f in Items)
                if (f is IValueFilterItem)
                    (f as IValueFilterItem).ParseValueSafe(GetValue(f.Name));
        }
    }
    public interface IQueryFilterItem<T> : IFilterItem
    {
        IQueryable<T> Filter(IQueryable<T> Query);
    }
    
/*    public class FilterModel : IFilterModel
    {
        public FilterModel() { ShowSubmit = true; }
        public IFilterItem[] Items { get; set; }
        public bool ShowSubmit { get; set; }
        public string FilterBoxID { get { return "filterbox"; } }
    }*/

    public interface IFilterModel
    {
        IFilterItem[] Items { get; }
        bool ShowSubmit { get; }
        bool ShowPivot { get; }
        string FilterBoxID { get; }
        //void FillValues(NameValueCollection Dic);
        void FillValues(Func<string, string> GetValue);
    }
    public abstract class FilterItem : IFilterItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsAutoPostBack { get; set; }
        public string ViewName { get; protected set; }
        public string CssClass { get; set; }
    }
    public interface IFilterItem
    {
        string Name { get; }
        string Description { get; }
        bool IsAutoPostBack { get; }
        string CssClass { get; }
        string ViewName { get; }
    }
    public interface IValueFilterItem
    {
        void ParseValueSafe(string Value);
        string ValueStr { get; }
    }
    public interface IValueFilterItem<T> : IValueFilterItem
    {
        T Value { get; set; }
    }
    public abstract class ValueFilterItem<T> : FilterItem, IValueFilterItem<T>
    {
        public ValueFilterItem(string Name, T Value) { this.ViewName = Name; this.Value = Value; }        
        public T Value { get; set; }

        public abstract void ParseValueSafe(string Value);
        public virtual string ValueStr { get { return Value.ToString(); } }
    }
    public abstract class SimpleFilterItem<T, T2> : FilterItem, IQueryFilterItem<T>, IValueFilterItem<T2>
    {
        public SimpleFilterItem(string Name) { this.ViewName = Name; }
        public SimpleFilterItem(string Name, T2 Value) { this.ViewName = Name; this.Value = Value; }        
        public T2 Value { get; set; }

        public abstract void ParseValueSafe(string Value);
        public virtual string ValueStr { get { return Value.ToString(); } }

        public System.Linq.Expressions.Expression<Func<T, T2, bool>> Where { get; set; }
        public IQueryable<T> Filter(IQueryable<T> Query)
        {
            return Filter(Where, Query, Value);
        }
        public static IQueryable<T> Filter(System.Linq.Expressions.Expression<Func<T, T2, bool>> Where, IQueryable<T> Query, T2 Value)
        {
            if (Where == null) return Query;
            var cons = System.Linq.Expressions.Expression.Constant(Value, typeof(T2));
            var par1 = Where.Parameters[0];
            var par2 = Where.Parameters[1];

            var cond = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>
                (ParameterRebinder.ReplaceParameters(par2, cons, Where.Body), par1);

            return Query.Where(cond);
        }
    }

    public class LabelFilterItem : ValueFilterItem<string> 
    {
        public override void ParseValueSafe(string Value) { this.Value = Value; }
        public LabelFilterItem(string Value) : base("Label", Value) { } 
    }

    public class LabelFilterItem<T> : LabelFilterItem, IQueryFilterItem<T>
    {
        public LabelFilterItem(string Value) : base(Value) { }
        public IQueryable<T> Filter(IQueryable<T> Query)
        {
            return Query;
        }
        public override void ParseValueSafe(string Value)
        {
        }
    }

    public class HiddenFilterItem<T> : SimpleFilterItem<T, string> 
    { 
        public HiddenFilterItem(string Value) : base("Hidden", Value ?? "") { }
        public HiddenFilterItem() : base("Hidden") { }
        public override void ParseValueSafe(string Value) { this.Value = Value; }
    }
    public class TextBoxFilterItem<T> : SimpleFilterItem<T, string> 
    { 
        public TextBoxFilterItem(string Value) : base("TextBox", Value ?? "") { }
        public TextBoxFilterItem() : base("TextBox") { }
        public override void ParseValueSafe(string Value) { this.Value = Value; }
    }
    public class DateTimeFilterItem<T> : SimpleFilterItem<T, DateTime?>, IValueFilterItem<string>
    {
        public DateTimeFilterItem() : base("TextBox") { }        
        public override void ParseValueSafe(string Value) 
        {
            this.Value = Helpers.Helper.GetDateSafe(Value, true);
        }
        string IValueFilterItem<string>.Value
        {
            get { return Value.HasValue ? Helpers.Helper.GetPersianDate(Value.Value, true) : ""; }
            set { throw new NotImplementedException(); }
        }
    }
    public class IntTextBoxFilterItem<T> : SimpleFilterItem<T, int?>, IValueFilterItem<string> 
    { 
        public IntTextBoxFilterItem(int? Value) : base("TextBox", Value) { }
        public IntTextBoxFilterItem() : base("TextBox") { }
        string IValueFilterItem<string>.Value
        {
            get { return Value.ToString(); }
            set { throw new NotImplementedException(); }
        }
        public override void ParseValueSafe(string Value) { this.Value = S.SafeParseInt(Value); }
    }
    public class DecimalTextBoxFilterItem<T> : SimpleFilterItem<T, decimal?>, IValueFilterItem<string>
    {
        public DecimalTextBoxFilterItem(decimal? Value) : base("TextBox", Value) { }
        public DecimalTextBoxFilterItem() : base("TextBox") { }
        string IValueFilterItem<string>.Value
        {
            get { return Value.ToString(); }
            set { throw new NotImplementedException(); }
        }
        public override void ParseValueSafe(string Value) { this.Value = (decimal?)S.SafeParseDbl(Value); }
    }
    //public class CheckBoxFilterItem<T> : SimpleFilterItem<T, bool> { public CheckBoxFilterItem(bool Value) : base("CheckBox", Value) { } }

    public class FixedFilterItem<T> : FilterItem, IQueryFilterItem<T>, IValueFilterItem<int?>, IValueFilterItem<string>
    {
        public FixedFilterItem() : base() { this.ViewName = "Label"; }

        public System.Linq.Expressions.Expression<Func<T, int?, bool>> Where { get; set; }

        public IQueryable<T> Filter(IQueryable<T> Query)
        {
            return SimpleFilterItem<T, int?>.Filter(Where, Query, Value);
        }

        public int? Value { get; set; }

        public string ValueStr { get; set; }

        public void ParseValueSafe(string Value)
        {
        }

        string IValueFilterItem<string>.Value
        {
            get { return ValueStr; }
            set { }
        }
    }

    public class FixedStringFilterItem<T> : FilterItem, IQueryFilterItem<T>, IValueFilterItem<string>
    {
        public FixedStringFilterItem() : base() { this.ViewName = "Label"; }

        public System.Linq.Expressions.Expression<Func<T, string, bool>> Where { get; set; }

        public IQueryable<T> Filter(IQueryable<T> Query)
        {
            return SimpleFilterItem<T, string>.Filter(Where, Query, Value);
        }

        public string Value { get; set; }
        public string ValueStr { get { return Value; } }

        public void ParseValueSafe(string Value) { }
    }

    public interface IDropDownFilterItem
    {
        bool HasDefault { get; set; }
        string DefaultText { get; set; }
        IEnumerable<SelectListItem> ListItems { get; set; }
    }
    public class IntDropDownFilterItem<T> : DropDownFilterItem<T, int?>
    {
        public override void ParseValueSafe(string Value)
        {
            this.Value = S.SafeParseInt(Value);
        }
    }

    public class BoolDropDownFilterItem<T> : DropDownFilterItem<T, bool?>
    {
        public BoolDropDownFilterItem()
        {
            ListItems = new SelectListItem[] 
            {
                //new SelectListItem() { Text = "", Value = "" },
                new SelectListItem() { Text = "✓", Value = "True" },
                new SelectListItem() { Text = "X", Value = "False" },
            };
        }
        public override void ParseValueSafe(string Value)
        {
            this.Value = S.SafeParseBool(Value);
        }
    }
    
    public class DropDownFilterItem<T, T2> : FilterItem, IDropDownFilterItem, IQueryFilterItem<T>, IValueFilterItem<T2>
    {
        public DropDownFilterItem(IEnumerable<SelectListItem> ListItems) : this() { this.ListItems = ListItems; DefaultText = ""; }
        public DropDownFilterItem() { HasDefault = true; ViewName = "DropDown"; }
        public T2 _Value;
        public T2 Value { get { return _Value; } set { _Value = value; SetSelected(); } }

        public virtual void ParseValueSafe(string Value) { throw new NotImplementedException(); }
        public virtual string ValueStr { get { return Value.ToString(); } }
        
        public bool HasDefault { get; set; }
        public string DefaultText { get; set; }
        public IEnumerable<SelectListItem> ListItems { get; set; }

        private void SetSelected()
        {
            string v = _Value.ToString();
            foreach (var l in ListItems)
                l.Selected = l.Value == v;
        }
        public System.Linq.Expressions.Expression<Func<T, T2, bool>> Where { get; set; }
        public IQueryable<T> Filter(IQueryable<T> Query)
        {
            return SimpleFilterItem<T, T2>.Filter(Where, Query, Value);
        }
    }

    public interface IHierarchyFilterItem : IValueFilterItem<int?>
    {
        IEnumerable<SelectListItem> GetAllLinks(UrlHelper Url);
        SelectListItem AllItemsLink(UrlHelper Url);
    }

    public class SelectListItemWithGroup
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public string Group { get; set; }
        public bool Selected { get; set; }
    }
}

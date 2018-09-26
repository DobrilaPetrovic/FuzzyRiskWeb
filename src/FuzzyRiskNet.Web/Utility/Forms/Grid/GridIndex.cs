using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nik.Helpers;
using FuzzyRiskNet.Libraries.Forms;
using MVCFormsLibrary;
using Nik.Expressions;
using Nik.Linq.Dynamic;
using FuzzyRiskNet.Fuzzy;
using System.Data.Entity;
using System.Web.Mvc;
using FuzzyRiskNet.Libraries.Helpers;

namespace FuzzyRiskNet.Libraries.Grid
{

    public interface IGridIndex
    {
        IFilterModel FilterModel { get; }
        IColumnsModel ColumnsModel { get; }
        ColumnsResult RenderGrid();
        IEnumerable<Tuple<string[], decimal?>> PivotGrid(string Value, params string[] Keys);
        bool IsHandleParameters { get; }
        IEnumerable<FormLink> CreateCustomActions(UrlHelper Url);
        string DefaultSortColumn { get; }
        bool DefaultSortIsAsc { get; }
    }

    public class GridIndexModel<T> : CompleteGridModel<T> where T : class
    {
        public GridIndexModel(ColumnsModel<T> Columns, FilterModel<T> Filters, IQueryable<T> Source) 
            : base(Columns.DefaultSort)
        {
            FilterModel = Filters;
            ColumnsModel = Columns;
            this.Source = Source;
            Init();
        }
        public override void InitFilterColumns()
        {
            if (FilterModel == null || ColumnsModel == null) throw new Exception("Parameters should be initialized.");
        }

        public override IEnumerable<IColumnModel<T>> ListAllColumns()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IQueryFilterItem<T>> ListAllFilters()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class CompleteGridModel<T> : IGridIndex where T : class
    {
        public CompleteGridModel(Expression<Func<T, int>> DefaultSort = null) 
        { 
            IsInitialized = false;
            this.DefaultSort = DefaultSort;
            PageSize = 10;
        }

        public IQueryable<T> Source { get; set; }

        public bool IsInitialized { get; private set; }
        
        public FilterModel<T> FilterModel { get; set; }
        protected ColumnsModel<T> ColumnsModel { get; set; }
        public System.Linq.Expressions.Expression<Func<T, int>> DefaultSort { get; protected set; }

        public abstract IEnumerable<IQueryFilterItem<T>> ListAllFilters();
        public abstract IEnumerable<IColumnModel<T>> ListAllColumns();

        protected virtual CompleteGridModel<T> Init()
        {
            InitFilterColumns();
            IsInitialized = true;
            return this;
        }

        public virtual void InitFilterColumns()
        {
            FilterModel = new FilterModel<T>() { Items = ListAllFilters().ToArray() };
            ColumnsModel = new ColumnsModel<T>() { PageSize = PageSize, Columns = ListAllColumns().ToArray(), DefaultSort = DefaultSort };
        }

        public int PageSize { get; set; }

        public Nullable<T2> GetValue<T2>(string FieldName) where T2 : struct
        {
            if (FilterModel == null) throw new Exception("Not initialized");
            foreach (var f in FilterModel.Items)
                if (f.Name == FieldName)
                {
                    if (f is IValueFilterItem<T2>)
                        return (f as IValueFilterItem<T2>).Value;
                    if (f is IValueFilterItem<Nullable<T2>>)
                        return (f as IValueFilterItem<Nullable<T2>>).Value;
                }
            return null;
        }

        public T2 PreEvaluate<T2>(T2 Value)
        {
            return Value;
        }

        public bool ShowInsert { get; set; }
        public string InsertActionName { get; set; }
        public object InsertParam { get; set; }
        public string InsertTitle { get; set; }

        public string DefaultSortColumn { get; set; }
        public bool DefaultSortIsAsc { get; set; }

        public virtual IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            if (ShowInsert) yield return new FormLink() { Link = Url.Action(InsertActionName ?? "Insert", InsertParam), Text = InsertTitle ?? Messages.InsertTitle };
        }

        public bool IsHandleParameters { get { return true; } }
        
        public ColumnsResult RenderGrid()
        {
            var q = Source;
            q = this.FilterModel.Filter(q);
            var ret = ColumnsModel.CreateResult(q);
            return ret;
        }

        public IEnumerable<Tuple<string[], decimal?>> PivotGrid(string Value, params string[] Keys)
        {
            var cols = this.ColumnsModel.FilteredColumns.Where(c => c is ISortableFieldModel)
                .Where(c => c is IDynamicFieldModel<T>).Cast<IDynamicFieldModel<T>>()
                .ToDictionary(c => (c as ISortableFieldModel).FieldName, c => c);
            
            List<DynamicProperty> properties = new List<DynamicProperty>();
            List<MemberAssignment> bindings = new List<MemberAssignment>();

            foreach (var k in Keys) properties.Add(new DynamicProperty(k, typeof(object)));

            Type type = Nik.Linq.Dynamic.DynamicExpression.CreateClass(properties);

            var consexp = (Expression<Func<T, decimal>>)((T item) => 1);

            foreach (var k in Keys)
                bindings.Add(Expression.Bind(type.GetMember(k)[0],
                    ParameterRebinder.ReplaceParameters(cols[k].GetDataExp.Parameters[0], consexp.Parameters[0], cols[k].GetDataExp.Body)));

            var exp = Expression.Lambda<Func<T, object>>(Expression.MemberInit(Expression.New(type), bindings.ToArray()), consexp.Parameters[0]);

            var q = Source;
            q = this.FilterModel.Filter(q);
            var grp = q.GroupBy(exp);

            
            var aggconsexp = (Expression<Func<IGrouping<object, T>, object>>)((item) => item.Key);
            var aggcountexp = (Expression<Func<IGrouping<object, T>, decimal?>>)((item) => item.Count());

            if (!String.IsNullOrEmpty(Value))
            {
                var p = Expression.Parameter(typeof(IGrouping<object, T>));
                var getv = ((dynamic)cols[Value]).GetValue;
                var getdec = Expression.Lambda<Func<T, decimal?>>(Expression.Convert(getv.Body, typeof(decimal?)), getv.Parameters[0]);
                //BaseColumnDataOnlyValue<>
                aggcountexp = Expression.Lambda<Func<IGrouping<object, T>, decimal?>>(
                    Expression.Call(typeof(Queryable).GetMethodExt("Sum", typeof(IQueryable<Helper.T>), 
                        typeof(Expression<Func<Helper.T, decimal?>>)).MakeGenericMethod(typeof(T)), 
                    Expression.Convert(p, typeof(IQueryable<T>)), 
                    getdec), p);
            }

            var aggexp = Expression.Lambda<Func<IGrouping<object, T>, aggclass>>(Expression.MemberInit(Expression.New(typeof(aggclass)), new MemberBinding[]
            {
                Expression.Bind(typeof(aggclass).GetMember("Key")[0], aggconsexp.Body),
                Expression.Bind(typeof(aggclass).GetMember("Value")[0], ParameterRebinder.ReplaceParameters(aggcountexp.Parameters[0], aggconsexp.Parameters[0], aggcountexp.Body))
            }), aggconsexp.Parameters[0]);

            Expression<Func<IGrouping<object, T>, aggclass>> exp2 = g => new aggclass() { Key = g.Key, Value = g.Count() };

            return grp.Select(aggexp)
                .ToArray().Select(v => Tuple.Create(
                Keys.Select(k => cols[k].RenderValueDynamic(null, type.GetProperty(k).GetValue(v.Key, null)).ToHtmlString()).ToArray(),
                v.Value));
        }

        public class aggclass { public object Key { get; set; } public decimal? Value { get; set; } }

        #region IGridIndex Members

        IFilterModel IGridIndex.FilterModel { get { if (!IsInitialized) throw new Exception("Not Initialized"); return this.FilterModel; } }
        IColumnsModel IGridIndex.ColumnsModel { get { if (!IsInitialized) throw new Exception("Not Initialized"); return ColumnsModel; } }

        #endregion

        public static Expression<Func<T, T2>> GetPropExpr<T2>(string PropName)
        {
            var obj = Expression.Parameter(typeof(T), "Param");
            return LambdaExpression.Lambda<Func<T, T2>>(Expression.Property(obj, typeof(T), PropName), obj);
        }

        public static StringColumnModel<T> NewStringCol(string FieldName, string HeaderName, Expression<Func<T, string>> GetValue)
        {
            return new StringColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue };
        }

        public static EnumStringColumnModel<T> NewStringArrayCol(string FieldName, string HeaderName, Expression<Func<T, IEnumerable<string>>> GetValue, string Separator = "")
        {
            return new EnumStringColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, Separator = Separator };
        }

        public static CustomToStringColumnModel<T, T2> NewCustomToStringCol<T2>(string FieldName, string HeaderName, Expression<Func<T, T2>> GetValue, Func<T2, string> ToString)
        {
            return new CustomToStringColumnModel<T, T2>(ToString) { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue };
        }

        public static IntColumnModel<T> NewIntCol(string FieldName, string HeaderName, Expression<Func<T, int>> GetValue, bool ConvertPersian = false)
        {
            return new IntColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, PersianNumber = ConvertPersian };
        }

        public static IntNullColumnModel<T> NewIntCol(string FieldName, string HeaderName, Expression<Func<T, int?>> GetValue, bool ConvertPersian = false)
        {
            return new IntNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, PersianNumber = ConvertPersian };
        }

        public static DecimalColumnModel<T> NewDecimalCol(string FieldName, string HeaderName, Expression<Func<T, decimal>> GetValue, bool ConvertPersian = false)
        {
            return new DecimalColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, PersianNumber = ConvertPersian };
        }

        public static DecimalNullColumnModel<T> NewDecimalCol(string FieldName, string HeaderName, Expression<Func<T, decimal?>> GetValue, bool ConvertPersian = false)
        {
            return new DecimalNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, PersianNumber = ConvertPersian };
        }

        public static DoubleColumnModel<T> NewDoubleCol(string FieldName, string HeaderName, Expression<Func<T, double>> GetValue, bool ConvertPersian = false)
        {
            return new DoubleColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, PersianNumber = ConvertPersian };
        }

        public static TFNColumnModel<T> NewTFNCol(string FieldName, string HeaderName, Expression<Func<T, TFN>> GetValue, bool ConvertPersian = false)
        {
            return new TFNColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, PersianNumber = ConvertPersian };
        }

        public static DateNullColumnModel<T> NewGregDateCol(string FieldName, string HeaderName, Expression<Func<T, DateTime?>> GetValue)
        {
            return new DateNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, ConvertStringFunc = d => d.ToShortDateString() };
        }

        public static DateNullColumnModel<T> NewDateCol(string FieldName, string HeaderName, Expression<Func<T, DateTime?>> GetValue)
        {
            return new DateNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, ConvertStringFunc = d => d.ToPersianDateFull() };
        }

        public static DateNullColumnModel<T> NewDateTimeCol(string FieldName, string HeaderName, Expression<Func<T, DateTime?>> GetValue)
        {
            return new DateNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue };
        }

        public static TimeNullColumnModel<T> NewTimeCol(string FieldName, string HeaderName, Expression<Func<T, TimeSpan?>> GetValue)
        {
            return new TimeNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue };
        }

        public static TimeSpanMinuteNullColumnModel<T> NewTimeSpanMinuteCol(string FieldName, string HeaderName, Expression<Func<T, int?>> GetValue, bool IsSmart = false)
        {
            return new TimeSpanMinuteNullColumnModel<T>() { FieldName = FieldName, HeaderName = HeaderName, GetValue = GetValue, IsSmart = IsSmart };
        }

        public static StringColumnModel<T> NewBoolCol(string FieldName, string HeaderName, Expression<Func<T, bool>> GetValue)
        {
            return NewStringCol(FieldName, HeaderName, ParameterRebinder.FoG(v => v ? "<div style='text-align: center;'>✓</div>" : "", GetValue));
        }

        public static StringColumnModel<T> NewBoolCol(string FieldName, string HeaderName, Expression<Func<T, bool?>> GetValue)
        {
            return NewStringCol(FieldName, HeaderName, ParameterRebinder.FoG(v => v ?? false ? "<div style='text-align: center;'>✓</div>" : "", GetValue));
        }

        public static StringColumnModel<T> NewTriStateCol(string FieldName, string HeaderName, Expression<Func<T, bool?>> GetValue)
        {
            return NewStringCol(FieldName, HeaderName, ParameterRebinder.FoG(v => "<div style='text-align: center;'>" + (v.HasValue ? v.Value ? "✓" : "X" : " ") + "</div>", GetValue));
        }

        public static ActionColumnModel<T> NewActionCol(string ActionName, string HeaderName, Expression<Func<T, object>> GetParams, Expression<Func<T, bool>> ShowCellCondition = null, string ControllerName = null)
        {
            return new ActionColumnModel<T>() { ActionName = ActionName, HeaderName = HeaderName, GetParams = GetParams, CellCondition = ShowCellCondition, ControllerName = ControllerName };
        }
        public static ActionColumnModel<T> NewActionCol(string ActionName, string HeaderName, string FormatString, Expression<Func<T, object>> GetParams, Expression<Func<T, string>> GetOtherParams)
        {
            return new ActionColumnModel<T>() { ActionName = ActionName, HeaderName = HeaderName, FormatString = FormatString, GetParams = GetParams, GetOtherParams = GetOtherParams };
        }
        public static DeleteColumnModel<T> NewDeleteCol(Expression<Func<T, object>> GetParams)
        {
            return new DeleteColumnModel<T>() { Post = true, GetParams = GetParams };
        }
        public static DeleteColumnModel<T> NewDeleteCol(string ActionName, Expression<Func<T, object>> GetParams)
        {
            return new DeleteColumnModel<T>() { Post = true, GetParams = GetParams, ActionName = ActionName };
        }

        public static DeleteColumnModel<T> NewPostActionCol(string ActionName, string HeaderName, Expression<Func<T, object>> GetParams, Expression<Func<T, bool>> ShowCellCondition = null)
        {
            return new DeleteColumnModel<T>() { Post = true, HeaderName = HeaderName, GetParams = GetParams, ActionName = ActionName, CellCondition = ShowCellCondition };
        }

        public static TextBoxFilterItem<T> NewTextFilter(string Name, string Description, Expression<Func<T, string, bool>> Where)
        {
            return new TextBoxFilterItem<T>() { Name = Name, Description = Description, Where = Where };
        }

        public static DecimalTextBoxFilterItem<T> NewDecimalFilter(string Name, string Description, Expression<Func<T, decimal?, bool>> Where)
        {
            return new DecimalTextBoxFilterItem<T>() { Name = Name, Description = Description, Where = Where };
        }

        public static DateTimeFilterItem<T> NewDateTimeFilter(string Name, string Description, Expression<Func<T, DateTime?, bool>> Where)
        {
            return new DateTimeFilterItem<T>() { Name = Name, Description = Description, Where = Where, CssClass = "filterdate" };
        }

        public static LabelFilterItem<T> NewLabelFilter(string Description, string Value)
        {
            return new LabelFilterItem<T>(Value) { Description = Description, Value = Value };
        }

        public static BoolDropDownFilterItem<T> NewBoolFilter(string Name, string Description, Expression<Func<T, bool?, bool>> Where)
        {
            return new BoolDropDownFilterItem<T>()
            {
                 Name = Name,
                 Description = Description,
                 Where = Where
            };
        }

        public static DropDownFilterItem<T, int?> NewGenericDropFilter<T, T2>(string FieldName, string Title, Expression<Func<T, int?, bool>> Where,            
            IQueryable<T2> Items, string EntityNameProperty = "Name", bool HasDefault = true)
            where T : class, new()
            where T2 : class
        {
            //if (Items == null) Items = Grid.DB.Set<T2>();
            var GetIDExpression = SimpleFormFieldsExtensions.GetPropExpr<T2, int>("ID");
            var GetTitleExpression = SimpleFormFieldsExtensions.GetPropExpr<T2, string>(EntityNameProperty);

            var query = Items.Select(Expression.Lambda<Func<T2, EFSelectItem>>(
                    Expression.MemberInit(Expression.New(typeof(EFSelectItem)), 
                    new MemberBinding[] 
                    {
                        Expression.Bind(typeof(EFSelectItem).GetMember("Title")[0], ParameterRebinder.ReplaceParameters(GetTitleExpression.Parameters[0], GetIDExpression.Parameters[0] , GetTitleExpression.Body)),
                        Expression.Bind(typeof(EFSelectItem).GetMember("ID")[0], GetIDExpression.Body)
                    }),
                    new ParameterExpression[] { GetIDExpression.Parameters[0] }));

            var list = query.ToArray().Select(efsi => new SelectListItem() { Value = efsi.ID.ToString(), Text = efsi.Title }).ToArray();

            return new IntDropDownFilterItem<T>()
            { 
                Name = FieldName,
                Description = Title,
                HasDefault = HasDefault,                
                ListItems = list,
                Where = Where
            };
        }

        public HierarchyFilterItem<T> NewHierarchyFilter(string Name, string Description, Expression<Func<T, T>> GetParent, Expression<Func<T, int>> GetID, Expression<Func<T, string>> GetTitle, DbSet<T> Set)
        {            
            return new HierarchyFilterItem<T>() { Description = Description, Name = Name, GetParent = GetParent, GetID = GetID, GetTitle = GetTitle, Set = Set };
        }
    }

}

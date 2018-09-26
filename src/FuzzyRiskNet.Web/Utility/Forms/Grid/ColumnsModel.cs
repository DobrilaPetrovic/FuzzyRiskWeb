using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nik.Helpers;
using Nik.Expressions;
using System.Linq.Expressions;
using Nik.Linq.Dynamic;
using System.Web.Mvc;

namespace FuzzyRiskNet.Libraries.Grid
{
    public interface IColumnsModel
    {
        string Sort { get; set; }
        bool IsAsc { get; set; }
        int Page { get; set; }
        int PageSize { get; set; }
        IEnumerable<IColumnModel> Columns { get;  }
        bool IncludeMainObject { get; set; }
    }
    public interface IFilterableColumnsModel
    {
        Func<IColumnModel, bool> ColumnFilter { get; set; }
    }

    public class ColumnsModel<T> : IColumnsModel, IFilterableColumnsModel where T : class
    {
        public ColumnsModel()
        {
            PageSize = 10;
        }
        public IEnumerable<IColumnModel<T>> Columns { get; set; }
        public IEnumerable<IColumnModel<T>> FilteredColumns { get { return Columns.Where(c => ColumnFilter == null || ColumnFilter(c)); } }
        IEnumerable<IColumnModel> IColumnsModel.Columns { get { return Columns.Cast<IColumnModel>().Where(c => ColumnFilter == null || ColumnFilter(c)); } }
        public System.Linq.Expressions.Expression<Func<T, int>> DefaultSort { get; set; }
        public Func<T, string> RowClass { get; set; }
        public string Sort { get; set; }
        public bool IsAsc { get; set; }

        public bool ShowRowNumber { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool IncludeMainObject { get; set; }

        static object GetVal(object obj, string Property)
        {
            return obj.GetType().GetProperty(Property).GetValue(obj, null);
        }

        public IOrderedQueryable<T> SortSource(IQueryable<T> Source) 
        {
            foreach (var col in FilteredColumns)
                if (col is ISortableFieldModel<T> && (col as ISortableFieldModel<T>).Sortable)
                {
                    var col2 = (col as ISortableFieldModel<T>);
                    if (col2.FieldName == Sort)
                        return col2.Sort(Source, IsAsc);
                }
            if (DefaultSort != null) return Source.OrderBy(DefaultSort);
            foreach (var col in FilteredColumns)
                if (col is ISortableFieldModel<T> && (col as ISortableFieldModel<T>).Sortable)
                {
                    var col2 = (col as ISortableFieldModel<T>);
                    return col2.Sort(Source, IsAsc);
                }
            return Source.OrderBy(t => 1);
        }

        public ColumnsResult CreateResult(IQueryable<T> Source)
        {
            var sort = SortSource(Source);

            var consexp = (Expression<Func<T, T>>)((T item) => item);
            var allcols = FilteredColumns.ToList();
            
            if (allcols.Any(d => !(d is IDynamicFieldModel<T>))) throw new Exception("Only Dynamic Fields are Accepted.");

            List<DynamicProperty> properties = new List<DynamicProperty>();
            List<MemberAssignment> bindings = new List<MemberAssignment>();

            if (IncludeMainObject) properties.Add(new DynamicProperty("Obj", typeof(T)));
            foreach (var col in allcols)
                if (col is IDynamicFieldModel<T>)
                    properties.Add(new DynamicProperty("Col" + allcols.IndexOf(col).ToString(), typeof(object)));
            
            properties.Add(new DynamicProperty("RowNumber", typeof(int)));

            Type type = Nik.Linq.Dynamic.DynamicExpression.CreateClass(properties);
            
            if (IncludeMainObject) bindings.Add(Expression.Bind(type.GetMember("Obj")[0], consexp.Body));
            
            foreach (var col in allcols)
                if (col is IDynamicFieldModel<T>)
                    bindings.Add(Expression.Bind(type.GetMember("Col" + allcols.IndexOf(col).ToString())[0],
                        ParameterRebinder.ReplaceParameters(
                            (col as IDynamicFieldModel<T>).GetDataExp.Parameters[0],
                            consexp.Parameters[0],
                            (col as IDynamicFieldModel<T>).GetDataExp.Body)));


            var exp = ColumnsExpVisitor.Correct(Expression.Lambda<Func<T, object>>(Expression.MemberInit(Expression.New(type), bindings.ToArray()), consexp.Parameters[0]));
            var valfuncs = new List<Func<object, HtmlHelper, MvcHtmlString>>();
            

            foreach (var col in allcols)
            {
                int ind = allcols.IndexOf(col);

                if (col is IDynamicFieldModel<T>)
                    valfuncs.Add((item, html) => (allcols[ind] as IDynamicFieldModel<T>).RenderValueDynamic(html, type.GetProperty("Col" + ind.ToString()).GetValue(item, null)));
                else
                    valfuncs.Add((item, html) => allcols[ind].RenderValue(html, type.GetProperty("Obj").GetValue(item, null) as T));
            }
            
            var res = new ColumnsResult() { ValueFuncs = valfuncs.ToArray(), OutputQuery = sort.Select(exp),
                                            Columns = this.FilteredColumns.Select(c => (IColumnModel)c),
                                            Page = this.Page,
                                            PageSize = this.PageSize,
                RowCssClass = RowClass != null ? obj => RowClass(GetVal(obj, "Obj") as T) : (Func<object, string>)null,
                Sort = Sort, IsAsc = IsAsc, ShowRowNumber = ShowRowNumber };
            
            if (res.Count < (Page - 1) * PageSize) res.Page = 1;
            return res;
        }
        protected class ColumnsExpVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            public static T2 Correct<T2>(T2 exp) where T2 : Expression
            {
                return new ColumnsExpVisitor().Visit(exp) as T2;
            }
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "GetValue" && (node.Object is ConstantExpression) && ((node.Object as ConstantExpression).Value is CompleteGridModel<T>))
                    if (node.Arguments[0] is ConstantExpression)
                    {
                        var l = Expression.Lambda<Func<object>>(Expression.Convert(node, typeof(object))).Compile();
                        return Expression.Constant(l.Invoke(), node.Type);
                    }
                if (node.Method.Name == "PreEvaluate" && (node.Object is ConstantExpression) && ((node.Object as ConstantExpression).Value is CompleteGridModel<T>))
                {
                    var l = Expression.Lambda<Func<object>>(Expression.Convert(node, typeof(object))).Compile();
                    return Expression.Constant(l.Invoke(), node.Type);
                } 
                return base.VisitMethodCall(node);
            }
        }

        public Func<IColumnModel, bool> ColumnFilter { get; set; }
    }
}

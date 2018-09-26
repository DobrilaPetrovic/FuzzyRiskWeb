using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using Nik.Linq.Dynamic;
using System.Text;
using System.Web.UI;
using Nik.Helpers;
using MVCFormsLibrary;
using System.Web.Mvc;

namespace FuzzyRiskNet.Libraries.Grid
{
    public class ColumnsResult 
    {
        public ColumnsResult()
        {
            GridID = "list2";
        }
        public string GridID { get; set; }
        public IEnumerable<IColumnModel> Columns { get; set; }
        public IQueryable<object> OutputQuery { get; set; }
        public Func<object, HtmlHelper, MvcHtmlString>[] ValueFuncs { get; set; }
        public int Page { get; set; }
        public string Sort { get; set; }
        public bool IsAsc { get; set; }
        public Func<object, string> RowCssClass { get; set; }
        public int PageSize { get; set; }

        public bool ShowRowNumber { get; set; }

        private int? _Count = null;
        public int Count { get { if (!_Count.HasValue) _Count = OutputQuery.Count(); return _Count.Value; } }
        public int CountPage { get { return (int)Math.Ceiling((double)Count / PageSize); } }

        public int FirstItem { get { return 1 + (Page - 1) * PageSize; } }
        public bool HasNextPage { get { return CountPage > Page; } }
        public bool HasPreviousPage { get { return Page > 1; } }
        public int LastItem { get { return Math.Min(Page * PageSize, Count); } }
        public int PageNumber { get { return Page; } }
        public int TotalItems { get { return Count; } }
        public int TotalPages { get { return CountPage; } }
        
        IEnumerable<object> _CachedItems = null;
        private IEnumerable<object> GetEnumerable() 
        {
            if (_CachedItems == null)
            {
                _CachedItems = OutputQuery.Skip(PageSize * (Page - 1)).Take(PageSize).ToList();
                if (ShowRowNumber)
                {
                    int row = PageSize * (Page - 1) + 1;
                    foreach (var o in _CachedItems)
                        o.GetType().GetProperty("RowNumber").SetValue(o, row++, null);
                }
            }
            return _CachedItems; 
        }       
        public System.Collections.IEnumerator GetEnumerator() { return GetEnumerable().GetEnumerator(); }
    }
}

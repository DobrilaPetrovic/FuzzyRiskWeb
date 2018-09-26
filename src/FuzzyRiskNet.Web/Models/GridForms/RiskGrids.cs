using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FuzzyRiskNet.Models.GridForms
{

    public interface IRiskContext
    {
        RiskDbContext DB { get; }
    }

    public abstract class RiskGrids<T> : FuzzyRiskNet.Libraries.Grid.CompleteGridModel<T> where T : class
    {
        public IRiskContext Context { get; private set; }
        public RiskDbContext DB { get { return Context.DB; } }

        public Func<RiskDbContext, IQueryable<T>> DefaultSelect { get; protected set; }

        public void InitNoReturn(IRiskContext Context) { this.Init(Context); }

        public RiskGrids(System.Linq.Expressions.Expression<Func<T, int>> DefaultSort = null,
            Func<RiskDbContext, IQueryable<T>> DefaultSelect = null)
            : base(DefaultSort) 
        { 
            PageSize = 20;
            this.DefaultSelect = DefaultSelect;
        }

        public virtual RiskGrids<T> Init(IRiskContext Context)
        {
            if (Context == null) throw new ArgumentNullException("Context");
            this.Context = Context;
            if (this.Source == null && DefaultSelect != null) this.Source = DefaultSelect(DB);
            if (Source == null) throw new Exception("Source (or DefaultSelect) should be initialized.");
            var grid = base.Init();
            if (grid != this) throw new Exception("Returned grid is not expected.");
            this.ColumnsModel.ShowRowNumber = true;
            return this;
        }

    }
}
using FuzzyRiskNet.Models;
using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FuzzyRiskNet.Models.GridForms
{
    public interface IHasSettableContext
    {
        ApplicationUser CurrentUser { get; set; }
    }


    public abstract class RiskForms<T> : CRUDForm<T>, IHasSettableContext where T : class, new()
    {
        public RiskForms(int? ID) { if (ID.HasValue) this.SetEditID(ID.Value); }

        public ApplicationUser CurrentUser { get; set; }
    }

    public abstract class RiskFlexForms<T> : FlexForm<T>, IHasSettableContext where T : RiskFlexForms<T>
    {
        public ApplicationUser CurrentUser { get; set; }
    }
}
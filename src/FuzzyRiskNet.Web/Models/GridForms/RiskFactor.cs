using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MVCFormsLibrary.Form;
using System.Linq.Expressions;
using FuzzyRiskNet.Models;
using System.Web.Mvc;

namespace FuzzyRiskNet.Models.GridForms
{
    public class RiskFactorForm : RiskForms<RiskFactor>
    {
        public int? EditID { get; private set; }
        public RiskFactorForm(int? ID) : base(ID) { this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<RiskFactor>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    if (pi.Name == "Description" || pi.Name == "Category") builder.Add(null);
                    return false;
                }).ToList();
           
            return list;
        }
    }

    public class RiskFactorGrid : RiskGrids<RiskFactor>
    {
        public RiskFactorGrid() : base(n => n.ID, db => db.RiskFactors) {  }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<RiskFactor>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<RiskFactor>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewCustomToStringCol("Category", "Category", v => v.Category, v => v.ToString());
            yield return NewActionCol("Edit", "Edit", v => new { ID = v.ID });
            yield return NewDeleteCol("Delete", v => new { ID = v.ID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
        }
    }
}
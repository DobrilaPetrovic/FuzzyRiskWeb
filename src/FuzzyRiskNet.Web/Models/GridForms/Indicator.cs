using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using MVCFormsLibrary.Form;
using System.Web.Mvc;

namespace FuzzyRiskNet.Models.GridForms
{
    public class IndicatorForm : RiskForms<Indicator>
    {
        public int? EditID { get; private set; }
        public IndicatorForm(int? ID) : base(ID) { this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<Indicator>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    return false;
                }).ToList();
           
            return list;
        }

        public override void BeforeSave(Indicator Obj)
        {
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("Index"));
        }
    }

    public class ViewIndTuple
    {
        public Country Country { get; set; }
        public decimal? Value { get; set; }
        public string Date { get; set; }
    }

    public class ViewIndicatorGrid : RiskGrids<ViewIndTuple>
    {
        public ViewIndicatorGrid() : base(n => n.Country.ID) { }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<ViewIndTuple>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Country.Name.Contains(v));
            yield return NewTextFilter("Date", "Date", (n, v) => v == null || v == "" || n.Date == v);
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<ViewIndTuple>> ListAllColumns()
        {
            yield return NewStringCol("Country", "Country", v => v.Country.Name);
            yield return NewStringCol("Code", "Code", v => v.Country.Code);
            yield return NewDecimalCol("Value", "Value", v => v.Value);
            yield return NewStringCol("Date", "Date", v => v.Date);
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
            yield return new FormLink("Back to the list", Url.Action("IndexIndicators"));
        }
    }

    public class IndicatorsGrid : RiskGrids<Indicator>
    {
        public IndicatorsGrid() : base(n => n.ID, db => db.Indicators) {  }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<Indicator>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
            yield return NewTextFilter("Code", "Code", (n, v) => v == null || v == "" || n.Code.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<Indicator>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewStringCol("Code", "Code", v => v.Code);
            yield return NewActionCol("ViewIndicator", "View", v => new { ID = v.ID });
            yield return NewActionCol("InsertIndicator", "Update Data", v => new { Code = v.Code });
            yield return NewActionCol("EditIndicator", "Edit", v => new { ID = v.ID });
            yield return NewDeleteCol("DeleteIndicator", v => new { ID = v.ID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
        }
    }
}
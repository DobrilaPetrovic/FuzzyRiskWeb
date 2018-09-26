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
    public class CountryForm : RiskForms<Country>
    {
        public int? EditID { get; private set; }
        public CountryForm(int? ID) : base(ID) { this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<Country>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    return false;
                }).ToList();
           
            return list;
        }

        public override void BeforeSave(Country Obj)
        {
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("Index"));
        }
    }

    public class CountrysGrid : RiskGrids<Country>
    {
        public CountrysGrid() : base(n => n.ID, db => db.Countries) {  }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<Country>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
            yield return NewTextFilter("Code", "Code", (n, v) => v == null || v == "" || n.Code.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<Country>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewStringCol("Code", "Code", v => v.Code);
            yield return NewActionCol("EditCountry", "Edit", v => new { ID = v.ID });
            yield return NewDeleteCol("DeleteCountry", v => new { ID = v.ID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
            yield return new FormLink("Update Countries", Url.Action("UpdateCountries"));
        }
    }
}
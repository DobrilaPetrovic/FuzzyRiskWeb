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
    public class RegionForm : RiskForms<Region>
    {
        public int ProjectID { get; private set; }
        public int? EditID { get; private set; }
        public RegionForm(int? ID, int ProjectID) : base(ID) { this.ProjectID = ProjectID; this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<Region>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    if (pi.Name == "ParentID") { builder.Add(this.CreateGenericDrop<Region, Region>("ParentID", "Parent", "Name", true, DB.Set<Region>().Where(r => r.ProjectID == ProjectID))); return true; }
                    return false;
                }).ToList();
           
            return list;
        }

        public override void BeforeSave(Region Obj)
        {
            if (IsInsert) Obj.ProjectID = ProjectID;
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexRegions", new { ProjectID = ProjectID }));
        }
    }

    public class RegionsGrid : RiskGrids<Region>
    {
        public int ProjectID { get; private set; }
        public RegionsGrid(int ProjectID) : base(n => n.ID, db => db.Regions.Where(p => p.ProjectID == ProjectID)) { this.ProjectID = ProjectID; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<Region>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<Region>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewStringCol("Parent", "Parent", v => v.Parent.Name);
            yield return NewActionCol("EditRegion", "Edit", v => new { ID = v.ID, ProjectID = ProjectID });
            yield return NewDeleteCol("DeleteRegion", v => new { ID = v.ID, ProjectID = ProjectID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
        }
    }
}
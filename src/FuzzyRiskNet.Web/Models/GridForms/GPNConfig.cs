using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MVCFormsLibrary.Form;
using System.Linq.Expressions;
using FuzzyRiskNet.Models;
using FuzzyRiskNet.Fuzzy;
using System.Web.Mvc;

namespace FuzzyRiskNet.Models.GridForms
{
    public class GPNConfigForm : RiskForms<GPNConfiguration>
    {
        public int ProjectID { get; private set; }
        public int? EditID { get; private set; }
        public GPNConfigForm(int? ID, int ProjectID) : base(ID) { this.ProjectID = ProjectID; this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<GPNConfiguration>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    return false;
                }).ToList();

            if (!IsInsert)
            {
                foreach (var n2 in DB.Set<Node>().Where(p => p.ProjectID == ProjectID).ToArray())
                {
                    list.Add(this.CreateHeaderLine(n2.Name + "'s Dependencies"));
                    foreach (var n in DB.Set<Node>().Where(p => p.ProjectID == ProjectID))
                        if (n2.ID != n.ID)
                        {
                            var id = n.ID;
                            var depid = n2.ID;
                            list.Add(new FuzzyNumberFormField<GPNConfiguration>()
                            {
                                IsVisible = true,
                                IsOptional = true,
                                FieldName = "Dep" + n2.ID + "$" + n.ID,
                                Title = n.Name,
                                CustomGetObject = (node, f) =>
                                {
                                    var d = DB.Set<Dependency>().FirstOrDefault(d2 => d2.FromID == depid && d2.ToID == id && d2.GPNConfigurationID == EditID);
                                    return d == null ? "" : d.Rate.ToString();
                                },
                                CustomSetObject = (node, f, v) =>
                                {
                                    var d = DB.Set<Dependency>().FirstOrDefault(d2 => d2.FromID == depid && d2.ToID == id && d2.GPNConfigurationID == EditID);
                                    TFN tfn = null;
                                    TFN.TryParse(v, out tfn);

                                    if (d == null && tfn != null)
                                    {
                                        d = new Dependency() { FromID = depid, ToID = id, Rate = tfn, GPNConfigurationID = EditID };
                                        DB.Set<Dependency>().Add(d);
                                    }
                                    else if (d != null && tfn == null)
                                        DB.Set<Dependency>().Remove(d);
                                    else if (d != null)
                                        d.Rate = tfn;
                                },
                                ViewName = "TFNDependencyField"
                            });
                        }
                }
            }

            return list;
        }

        public override void BeforeSave(GPNConfiguration Obj)
        {
            if (IsInsert) Obj.ProjectID = ProjectID;
            if (IsInsert)
            {
                var defaults = DB.Set<Dependency>().Where(dep => !dep.GPNConfigurationID.HasValue && dep.From.ProjectID == ProjectID).ToArray()
                    .Select(dep => new Dependency() { FromID = dep.FromID, ToID = dep.ToID, Rate = dep.Rate }).ToArray();
                foreach (var d in defaults) d.GPNConfiguration = Obj;
                DB.Set<Dependency>().AddRange(defaults);
            }
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexGPNConfigs", new { ProjectID = ProjectID }));
        }
    }

    public class GPNConfigsGrid : RiskGrids<GPNConfiguration>
    {
        public int ProjectID { get; private set; }
        public GPNConfigsGrid(int ProjectID) : base(n => n.ID, db => db.GPNConfigurations.Where(p => p.ProjectID == ProjectID)) { this.ProjectID = ProjectID; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<GPNConfiguration>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<GPNConfiguration>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewActionCol("Analysis", "Analysis", v => new { GPNConfigID = v.ID, ProjectID = ProjectID });
            yield return NewActionCol("EditGPNConfig", "Edit", v => new { ID = v.ID, ProjectID = ProjectID });
            yield return NewActionCol("ViewGPNConfig", "View", v => new { ID = v.ID, ProjectID = ProjectID });
            yield return NewDeleteCol("DeleteGPNConfig", v => new { ID = v.ID, ProjectID = ProjectID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
            yield return new FormLink("Analysis All", Url.Action("AnalysisAll", new { ProjectID = ProjectID }));
        }
    }
}
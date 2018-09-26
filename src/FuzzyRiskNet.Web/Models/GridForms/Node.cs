using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MVCFormsLibrary.Form;
using System.Linq.Expressions;
using FuzzyRiskNet.Fuzzy;
using System.Web.Mvc;

namespace FuzzyRiskNet.Models.GridForms
{
    public class ProjectForm : RiskForms<Project>
    {
        public ProjectForm(int? ID) : base(ID) {  }

        public override IEnumerable<IFormField> ListMainFields()
        {
            return new FormFieldsBuilder<Project>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
            {
                if (pi.Name == "UserID") return true;
                return false;
            });
        }

        public override void BeforeSave(Project Obj)
        {
            if (IsInsert) Obj.UserID = CurrentUser.Id;
            base.BeforeSave(Obj);
        }
    }

    public class ProjectGrid : RiskGrids<Project>
    {
        public ProjectGrid(string UserID) : base(n => n.ID, db => db.Projects.Where(p => p.UserID == UserID)) { }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<Project>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<Project>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewIntCol("NumNodes", "# Nodes", v => v.Nodes.Count());
            yield return NewActionCol("IndexNodes", "Nodes", v => new { ProjectID = v.ID });
            yield return NewActionCol("Analysis", "Analysis", v => new { ProjectID = v.ID });
            yield return NewActionCol("EditProject", "Edit", v => new { ID = v.ID });
            yield return NewActionCol("CopyProject", "Copy", v => new { ID = v.ID });
            yield return NewDeleteCol("DeleteProject", v => new { ID = v.ID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
            yield return new FormLink("Insert Examples", Url.Action("InsertExamples"));
        }
    }

    public class NodeForm : RiskForms<Node>
    {
        public int ProjectID { get; private set; }
        public int? EditID { get; private set; }
        public NodeForm(int? ID, int ProjectID) : base(ID) { this.ProjectID = ProjectID; this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<Node>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    if (pi.Name == "ProjectID") { builder.Add(this.CreateHiddenForeignKeyField("ProjectID", ProjectID)); return true; }
                    if (pi.Name == "RegionID") { builder.Add(this.CreateGenericDrop("RegionID", "Region", (Expression<Func<Region, string>>)(r => r.Name), true, Items: DB.Set<Project>().Find(ProjectID).Regions.AsQueryable())); return true; }
                    if (pi.Name == "RoleID") { builder.Add(this.CreateGenericDrop("RoleID", "Role", (Expression<Func<Criteria, string>>)(r => r.Name), true, Items: DB.Set<Project>().Find(ProjectID).Criteria.Where(c => c.ParentID == null).AsQueryable())); return true; }
                    return false;
                }).ToList();

            foreach (var f in list) if (f is FuzzyNumberFormField<Node>) (f as FuzzyNumberFormField<Node>).ViewName = f.FieldName == "CostPetUnitInoperability" ? "NumericTFNField" : "TFNField";

            list.Add(this.CreateHeaderLine("Node's Default Dependencies"));            
            foreach (var n in DB.Set<Node>().Where(p => p.ProjectID == ProjectID))
                if (!IsInsert && this.EditID != n.ID)
                {
                    var id = n.ID;
                    list.Add(new FuzzyNumberFormField<Node>()
                    {
                        IsVisible = true, IsOptional = true, FieldName = "Dep" + n.ID, Title = n.Name, 
                        CustomGetObject = (node, f) =>
                            {
                                var d = DB.Set<Dependency>().FirstOrDefault(d2 => d2.FromID == node.ID && d2.ToID == id && !d2.GPNConfigurationID.HasValue);
                                return d == null ? "" : d.Rate.ToString();
                            },
                        CustomSetObject = (node, f, v) =>
                        {
                            var d = DB.Set<Dependency>().FirstOrDefault(d2 => d2.FromID == node.ID && d2.ToID == id && !d2.GPNConfigurationID.HasValue);
                            TFN tfn = null;
                            TFN.TryParse(v, out tfn);

                            if (d == null && tfn != null)
                            {
                                d = new Dependency() { From = node, ToID = id, Rate = tfn };
                                node.Dependencies.Add(d);
                            }
                            else if (d != null && tfn == null)
                                DB.Set<Dependency>().Remove(d);
                            else if (d != null)
                                d.Rate = tfn;
                        },
                        ViewName = "TFNDependencyField"
                    });
                }
            return list;
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexNodes", new { ProjectID = ProjectID }));
        }
    }

    public class NodesGrid : RiskGrids<Node>
    {
        public int ProjectID { get; private set; }
        public NodesGrid(int ProjectID) : base(n => n.ID, db => db.Nodes.Where(p => p.ProjectID == ProjectID)) { this.ProjectID = ProjectID; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<Node>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<Node>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewTFNCol("DefPerturbation", "Perturbation", v => v.DefaultPurturbation);
            yield return NewTFNCol("Resilience", "Resilience", v => v.Resilience);
            yield return NewTFNCol("UnitLossOfRisk", "Unit Loss of Risk", v => v.CostPetUnitInoperability);
            yield return NewStringCol("Location", "Location", v => "[" + v.LocationX.ToString() + ", " + v.LocationY.ToString() + "]");
            yield return NewStringCol("Region", "Region", v => v.Region.Name);
            yield return NewStringCol("Role", "Role", v => v.Role.Name);
            //yield return NewActionCol("IndexDependency", "Dependencies", v => new { FromID = v.ID });
            yield return NewActionCol("EditNode", "Edit", v => new { ID = v.ID, ProjectID = ProjectID });
            yield return NewDeleteCol("DeleteNode", v => new { ID = v.ID, ProjectID = ProjectID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
            yield return new FormLink("Analysis", Url.Action("Analysis", new { ProjectID = ProjectID }));
            yield return new FormLink("Analysis All", Url.Action("AnalysisAll", new { ProjectID = ProjectID }));
            yield return new FormLink("Analysis Uncertainty", Url.Action("AnalysisUncertainty", new { ProjectID = ProjectID }));
            yield return new FormLink("Optimise Uncertainty", Url.Action("OptimiseUncertainty", new { ProjectID = ProjectID }));
            yield return new FormLink("Sensitivity", Url.Action("AnalysisSensitivity", new { ProjectID = ProjectID }));
            yield return new FormLink("Manage Regions", Url.Action("IndexRegions", new { ProjectID = ProjectID }));
            yield return new FormLink("Manage GPN Configurations", Url.Action("IndexGPNConfigs", new { ProjectID = ProjectID }));
            yield return new FormLink("Manage Scenarios", Url.Action("IndexPerturbationScenarios", new { ProjectID = ProjectID }));
            yield return new FormLink("Manage Criteria", Url.Action("IndexCriteria", new { ProjectID = ProjectID }));
        }
    }
}
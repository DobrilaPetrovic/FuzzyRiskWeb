using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MVCFormsLibrary.Form;
using System.Linq.Expressions;
using FuzzyRiskNet.Models;
using FuzzyRiskNet.Helpers;
using System.Web.Mvc;

namespace FuzzyRiskNet.Models.GridForms
{
    public class PerturbationScenarioForm : RiskForms<PerturbationScenario>
    {
        public int ProjectID { get; private set; }
        public int? EditID { get; private set; }
        public PerturbationScenarioForm(int? ID, int ProjectID) : base(ID) { this.ProjectID = ProjectID; this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<PerturbationScenario>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    return false;
                }).ToList();
           
            return list;
        }

        public override void BeforeSave(PerturbationScenario Obj)
        {
            if (IsInsert) Obj.ProjectID = ProjectID;
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexPerturbationScenarios", new { ProjectID = ProjectID }));
        }
    }

    public class PerturbationScenariosGrid : RiskGrids<PerturbationScenario>
    {
        public int ProjectID { get; private set; }
        public PerturbationScenariosGrid(int ProjectID) : base(n => n.ID, db => db.PerturbationScenarios.Where(p => p.ProjectID == ProjectID)) { this.ProjectID = ProjectID; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<PerturbationScenario>> ListAllFilters()
        {
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<PerturbationScenario>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewTFNCol("Likelihood", "Likelihood", v => v.Likelihood);
            yield return NewActionCol("IndexPerturbationScenarioItems", "Perturbations", v => new { ScenarioID = v.ID, ProjectID = ProjectID });
            yield return NewActionCol("Analysis", "Analysis", v => new { ScenarioID = v.ID, ProjectID = ProjectID });
            yield return NewActionCol("AnalysisUncertainty", "Uncertainty", v => new { ScenarioID = v.ID, ProjectID = ProjectID });
            yield return NewActionCol("AnalysisSensitivity", "Sensitivity", v => new { ScenarioID = v.ID, ProjectID = ProjectID });
            yield return NewActionCol("EditPerturbationScenario", "Edit", v => new { ID = v.ID, ProjectID = ProjectID });
            yield return NewDeleteCol("DeletePerturbationScenario", v => new { ID = v.ID, ProjectID = ProjectID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
            yield return new FormLink("Analysis All", Url.Action("AnalysisAll", new { ProjectID = ProjectID }));
        }
    }

    public class PerturbationScenarioItemForm : RiskForms<PerturbationScenarioItem>
    {
        public int ScenarioID { get; private set; }
        public int ProjectID { get; private set; }
        public int? EditID { get; private set; }
        public PerturbationScenarioItemForm(int? ID, int ProjectID, int ScenarioID) : base(ID) { this.ProjectID = ProjectID; this.ScenarioID = ScenarioID; this.EditID = ID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<PerturbationScenarioItem>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
            {
                if (pi.Name == "RegionID") { builder.Add(this.CreateGenericDrop("RegionID", "Region", (Expression<Func<Region, string>>)(r => r.Name), true, Items: DB.Set<Project>().Find(ProjectID).Regions.AsQueryable())); return true; }
                if (pi.Name == "NodeID") { builder.Add(this.CreateGenericDrop("NodeID", "Node", (Expression<Func<Node, string>>)(r => r.Name), true, Items: DB.Set<Project>().Find(ProjectID).Nodes.AsQueryable())); return true; }
                if (pi.Name == "RiskFactorID") { builder.Add(this.CreateGenericDrop("RiskFactorID", "Risk Factor", (Expression<Func<RiskFactor, string>>)(r => r.Name), true, Items: DB.Set<RiskFactor>())); return true; }
                return false;
            }).ToList();

            return list;
        }

        public override void BeforeSave(PerturbationScenarioItem Obj)
        {
            if (IsInsert) Obj.PerturbationScenarioID = ScenarioID;
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexPerturbationScenarioItems", new { ProjectID = ProjectID, ScenarioID = ScenarioID }));
        }
    }

    public class PerturbationScenarioItemsGrid : RiskGrids<PerturbationScenarioItem>
    {
        public int ProjectID { get; private set; }
        public int ScenarioID { get; private set; }
        public PerturbationScenarioItemsGrid(int ProjectID, int ScenarioID) : base(n => n.ID, db => db.PerturbationScenarioItems.Where(p => p.PerturbationScenario.ProjectID == ProjectID && p.PerturbationScenarioID == ScenarioID)) { this.ProjectID = ProjectID; this.ScenarioID = ScenarioID; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<PerturbationScenarioItem>> ListAllFilters()
        {
            yield break;
            //yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.PerturbationScenario.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<PerturbationScenarioItem>> ListAllColumns()
        {
            yield return NewStringCol("RiskFactor", "Risk Factor", v => v.RiskFactor.Name);
            yield return NewTFNCol("Impact", "Impact", v => v.Purturbation);
            yield return NewStringCol("Node", "Node", v => v.Node.Name);
            yield return NewStringCol("Region", "Region", v => v.Region.Name);
            yield return NewIntCol("Start", "StartPeriod", v => v.StartPeriod);
            yield return NewIntCol("Duration", "Duration", v => v.Duration);
            yield return NewActionCol("EditPerturbationScenarioItem", "Edit", v => new { ID = v.ID, ProjectID = ProjectID, ScenarioID = ScenarioID });
            yield return NewDeleteCol("DeletePerturbationScenarioItem", v => new { ID = v.ID, ProjectID = ProjectID, ScenarioID = ScenarioID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
        }
    }
}
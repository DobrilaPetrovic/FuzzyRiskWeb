using FuzzyRiskNet.Libraries.Forms;
using Nik.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using MVCFormsLibrary.Form;
using FuzzyRiskNet.Libraries.Grid;
using FuzzyRiskNet.Fuzzy;
using System.Web.Mvc;
using FuzzyRiskNet.Libraries.Helpers;

namespace FuzzyRiskNet.Models.GridForms
{
    public class CriteriaForm : RiskForms<Criteria>
    {
        public int ProjectID { get; private set; }
        public int? EditID { get; private set; }
        public int? ParentID { get; set; }
        public CriteriaForm(int? ID, int ProjectID, int? ParentID = null) : base(ID) { this.ProjectID = ProjectID; this.EditID = ID; this.ParentID = ParentID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var list = new FormFieldsBuilder<Criteria>(this).AutoGenerateEntityFields(CustomPropHandler: (pi, builder) =>
                {
                    if (pi.Name == "ParentID") { builder.Add(this.CreateGenericDrop<Criteria, Criteria>("ParentID", "Parent", "Name", true, DB.Set<Criteria>().Where(r => r.ProjectID == ProjectID)).Do(f => f.Value = ParentID)); return true; }
                    if (pi.Name == "Min" || pi.Name == "Max" || pi.Name == "Level") { return true; }
                    return false;
                }).ToList();
           
            return list;
        }

        public override void BeforeSave(Criteria Obj)
        {
            if (IsInsert) Obj.ProjectID = ProjectID;
            base.BeforeSave(Obj);
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexCriteria", new { ProjectID = ProjectID }));
        }
    }

    public class CriteriaGrid : RiskGrids<Criteria>
    {
        public int ProjectID { get; private set; }
        public int? ParentID { get; set; }
        public CriteriaGrid(int ProjectID) 
            : base(n => n.ID, db => db.Criteria.Where(p => p.ProjectID == ProjectID)) { this.ProjectID = ProjectID; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IQueryFilterItem<Criteria>> ListAllFilters()
        {
            yield return NewHierarchyFilter("ParentID", "Location", c => c.Parent, c => c.ID, c => c.Name, DB.Criteria);
            yield return NewTextFilter("Name", "Name", (n, v) => v == null || v == "" || n.Name.Contains(v));
        }

        public override IEnumerable<FuzzyRiskNet.Libraries.Grid.IColumnModel<Criteria>> ListAllColumns()
        {
            yield return NewStringCol("Name", "Name", v => v.Name);
            yield return NewStringCol("Parent", "Parent", v => v.Parent.Name);
            yield return new ActionColumnModel<Criteria>()
            {
                ActionName = "IndexCriteria",
                HeaderName = "Sub-criteria",
                FormatString = "View {0}",
                GetParams = i => new { ParentID = i.ID, ProjectID = ProjectID },
                CellCondition = i => true,
                GetOtherParams = i => "(" + ((double)i.Childs.Count).ToString().Trim() + ")"
            };
            yield return NewActionCol("EditCriteria", "Edit", v => new { ID = v.ID, ProjectID = ProjectID });
            yield return NewDeleteCol("DeleteCriteria", v => new { ID = v.ID, ProjectID = ProjectID });
        }

        public override IEnumerable<FormLink> CreateCustomActions(UrlHelper Url)
        {
            yield return new FormLink("Edit Weights", Url.Action("EditWeights", new { ProjectID = ProjectID }));
            yield return new FormLink("Edit Values", Url.Action("EditValues", new { ProjectID = ProjectID }));
            yield return new FormLink("View Results", Url.Action("BSCResults", new { ProjectID = ProjectID }));
            foreach (var y in base.CreateCustomActions(Url)) yield return y;
        }
    }

    public class CriteriaWeights : RiskFlexForms<CriteriaWeights>
    {
        public int ProjectID { get; private set; }
        public CriteriaWeights(int ProjectID) { this.ProjectID = ProjectID; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var proj = DB.Set<Project>().Find(ProjectID);
            var nodes = proj.Nodes.ToArray();
            var criteria = proj.Criteria.ToArray();

            yield return this.CreateHeaderLine("Levels 1 to 3");

            foreach (var c in criteria)
                if (c.Childs.Any())
                {
                    yield return this.CreateHeaderLine("Weights for " + c.Name);
                    foreach (var c2 in c.Childs)
                    {
                        var c2local = c2;
                        yield return this.CreateDoubleField("L1Weight" + c.ID + "-" + c2.ID, c2.Name).Do(field =>
                        {
                            field.CustomGetObject = (cw, f2) => { var v = c2local.Weights.FirstOrDefault(); return v == null ? "" : v.Weight.ToString(); };
                            field.CustomSetObject = (cw, f2, v) => 
                            {
                                var dv = c2local.Weights.FirstOrDefault();
                                if (dv == null) DB.Set<CriteraWeight>().Add(dv = new CriteraWeight() { CriteriaID = c2local.ID });
                                double d;
                                if (double.TryParse(v, out d)) dv.Weight = d;
                            };
                        });
                    }
                }

            var roles = nodes.Select(n => n.Role).Where(n => n != null).Distinct().ToArray();
            var gpnconfigs = proj.GPNConfigurations.ToArray();

            yield return this.CreateHeaderLine("Level 4");

            foreach (var gpn in gpnconfigs)
            {
                yield return this.CreateHeaderLine("GPN:" + gpn.Name);

                foreach (var c2 in roles)
                {
                    var c2local = c2;
                    var gpnid = gpn.ID;
                    yield return this.CreateHeaderLine("Role:" + c2.Name);

                    foreach (var n in nodes.Where(n2 => n2.RoleID == c2.ID))
                    {
                        var nodeid = n.ID;
                        yield return this.CreateDoubleField("L4Weight" + gpn.ID + "-" + c2.ID + "-" + n.ID, n.FullName).Do(field =>
                        {
                            field.CustomGetObject = (cw, f2) => { var v = c2local.Weights.FirstOrDefault(w => w.GPNConfigurationID == gpnid && w.NodeID == nodeid); return v == null ? "" : v.Weight.ToString(); };
                            field.CustomSetObject = (cw, f2, v) =>
                            {
                                var dv = c2local.Weights.FirstOrDefault(w => w.GPNConfigurationID == gpnid && w.NodeID == nodeid);
                                if (dv == null) DB.Set<CriteraWeight>().Add(dv = new CriteraWeight() { CriteriaID = c2local.ID, GPNConfigurationID = gpnid, NodeID = nodeid });
                                double d;
                                if (double.TryParse(v, out d)) dv.Weight = d;
                            };
                        });
                    }
                }

            }

            yield return this.CreateHeaderLine("Level 5");

            foreach (var gpn in gpnconfigs)
            {
                yield return this.CreateHeaderLine("GPN:" + gpn.Name);

                foreach (var c2 in roles)
                {
                    var c2local = c2;
                    var gpnid = gpn.ID;
                    yield return this.CreateDoubleField("L5Weight" + gpn.ID + "-" + c2.ID, c2.Name).Do(field =>
                    {
                        field.CustomGetObject = (cw, f2) => { var v = c2local.Weights.FirstOrDefault(w => w.GPNConfigurationID == gpnid && w.NodeID == null); return v == null ? "" : v.Weight.ToString(); };
                        field.CustomSetObject = (cw, f2, v) =>
                        {
                            var dv = c2local.Weights.FirstOrDefault(w => w.GPNConfigurationID == gpnid && w.NodeID == null);
                            if (dv == null) DB.Set<CriteraWeight>().Add(dv = new CriteraWeight() { CriteriaID = c2local.ID, GPNConfigurationID = gpnid, NodeID = null });
                            double d;
                            if (double.TryParse(v, out d)) dv.Weight = d;
                        };
                    });
                }

            }

        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexCriteria", new { ProjectID = ProjectID }));
        }
    }
    public class CriteriaValues : RiskFlexForms<CriteriaValues>
    {
        public int ProjectID { get; private set; }
        public CriteriaValues(int ProjectID) { this.ProjectID = ProjectID; }

        private bool IsAChildOf(Criteria Candidate, Criteria Parent)
        {
            if (Candidate.ParentID == Parent.ID) return true;
            if (Candidate.ParentID == null) return false;
            return IsAChildOf(Candidate.Parent, Parent);
        }

        public override IEnumerable<IFormField> ListMainFields()
        {
            var proj = DB.Set<Project>().Find(ProjectID);
            var nodes = proj.Nodes.ToArray();
            var criteria = proj.Criteria.ToArray();


            foreach (var c in criteria)
                if (!c.Childs.Any())
                {
                    var relnodes = nodes.Where(n => n.Role != null && IsAChildOf(c, n.Role)).ToArray();
                    if (relnodes.Count() > 0)
                    {
                        yield return this.CreateHeaderLine("Values for " + c.Name);

                        var clocal = c;

                        yield return this.CreateNumberField("Min" + c.ID, "Worst", false, "0").Do(field =>
                        {
                            field.CustomGetObject = (cw, f2) => clocal.Min.ToString();
                            field.CustomSetObject = (cw, f2, v) => { double d; if (double.TryParse(v, out d)) clocal.Min = d; };
                        });

                        yield return this.CreateNumberField("Max" + c.ID, "Best", false, "1").Do(field =>
                        {
                            field.CustomGetObject = (cw, f2) => clocal.Max.ToString();
                            field.CustomSetObject = (cw, f2, v) => { double d; if (double.TryParse(v, out d)) clocal.Max = d; };
                        });
                    
                        foreach (var node in relnodes)
                        {
                            var nodelocal = node;
                            yield return this.CreateTFNField("Value" + c.ID + "-" + node.ID, node.FullName).Do(field =>
                            {
                                field.CustomGetObject = (cw, f2) => { var v = clocal.Values.FirstOrDefault(w => w.NodeID == nodelocal.ID); return v == null ? "" : v.Value.ToString(); };
                                field.CustomSetObject = (cw, f2, v) =>
                                {
                                    var dv = clocal.Values.FirstOrDefault(w => w.NodeID == nodelocal.ID);
                                    if (dv == null) DB.Set<CriteriaValue>().Add(dv = new CriteriaValue() { CriteriaID = clocal.ID, NodeID = nodelocal.ID });
                                    TFN d;
                                    if (TFN.TryParse(v, out d)) dv.Value = d;
                                };
                            });
                        }
                    }
                }
        }
        

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Return to list", Url.Action("IndexCriteria", new { ProjectID = ProjectID }));
        }
    }
}
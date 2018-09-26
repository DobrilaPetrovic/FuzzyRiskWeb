using FuzzyRiskNet.Models;
using FuzzyRiskNet.Models.GridForms;
using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Text;
using FuzzyRiskNet.Fuzzy;
using FuzzyRiskNet.FuzzyRisk;
using FuzzyRiskNet.MetaHeuristics.GA;
using FuzzyRiskNet.MetaHeuristics.Core;

namespace FuzzyRiskNet.Controllers
{
    [Authorize]
    public class ProjectController : AppControllers
    {
        public ActionResult Index()
        {
            SetTitle("Projects");
            return ViewGrid(new ProjectGrid(CurrentUser.Id) { InsertActionName = "InsertProject", ShowInsert = true }.Init(this));
        }

        public ActionResult InsertProject()
        {
            SetTitle("Insert Project");
            return EditInsertForm(new ProjectForm(null));
        }

        public ActionResult EditProject(int ID)
        {
            SetTitle("Edit Project");
            SetProject(ID);
            return EditInsertForm(new ProjectForm(ID));
        }

        public ActionResult DeleteProject(int ID)
        {
            SetProject(ID);
            return DeleteForm(new ProjectForm(ID));
        }

        // Nodes

        public ActionResult IndexNodes(int ProjectID)
        {
            SetTitle("Nodes");
            SetProject(ProjectID);
            return ViewGrid(new NodesGrid(ProjectID) { InsertActionName = "InsertNode", ShowInsert = true, InsertParam = new { ProjectID = ProjectID } }.Init(this));
        }

        public ActionResult InsertNode(int ProjectID)
        {
            SetTitle("Insert Node");
            ViewBag.SavePath = "Dependency/NewNode";
            SetProject(ProjectID);
            return EditInsertForm(new NodeForm(null, ProjectID));
        }

        public ActionResult EditNode(int ID, int ProjectID)
        {
            SetTitle("Edit Node");
            ViewBag.SavePath = "Dependency/Node" + ID;
            SetProject(ProjectID);
            return EditInsertForm(new NodeForm(ID, ProjectID));
        }

        public ActionResult DeleteNode(int ID, int ProjectID)
        {
            SetProject(ProjectID);
            return DeleteForm(new NodeForm(ID, ProjectID), RedirectToAction("IndexNodes", new { ProjectID = ProjectID }));
        }

        // Regions

        public ActionResult IndexRegions(int ProjectID)
        {
            SetTitle("Regions");
            SetProject(ProjectID);
            return ViewGrid(new RegionsGrid(ProjectID) { InsertActionName = "InsertRegion", ShowInsert = true, InsertParam = new { ProjectID = ProjectID } }.Init(this));
        }

        public ActionResult InsertRegion(int ProjectID)
        {
            SetTitle("Insert Region");
            SetProject(ProjectID);
            return EditInsertForm(new RegionForm(null, ProjectID));
        }

        public ActionResult EditRegion(int ID, int ProjectID)
        {
            SetTitle("Edit Region");
            SetProject(ProjectID);
            return EditInsertForm(new RegionForm(ID, ProjectID));
        }

        public ActionResult DeleteRegion(int ID, int ProjectID)
        {
            SetProject(ProjectID);
            return DeleteForm(new RegionForm(ID, ProjectID), RedirectToAction("IndexRegions", new { ProjectID = ProjectID }));
        }

        // Criterias

        public ActionResult IndexCriteria(int ProjectID, string ParentID)
        {
            SetTitle("Criteria");
            SetProject(ProjectID);
            return ViewGrid(new CriteriaGrid(ProjectID) { InsertActionName = "InsertCriteria", ShowInsert = true, InsertParam = new { ProjectID = ProjectID, ParentID = ParentID } }.Init(this));
        }

        public ActionResult InsertCriteria(int ProjectID, int? ParentID)
        {
            SetTitle("Insert Criteria");
            SetProject(ProjectID);
            return EditInsertForm(new CriteriaForm(null, ProjectID, ParentID));
        }

        public ActionResult EditCriteria(int ID, int ProjectID)
        {
            SetTitle("Edit Criteria");
            SetProject(ProjectID);
            return EditInsertForm(new CriteriaForm(ID, ProjectID));
        }

        public ActionResult DeleteCriteria(int ID, int ProjectID)
        {
            SetProject(ProjectID);
            return DeleteForm(new CriteriaForm(ID, ProjectID), RedirectToAction("IndexCriteria", new { ProjectID = ProjectID }));
        }

        public ActionResult EditWeights(int ProjectID)
        {
            SetTitle("Edit Weights");
            return FlexForm(new CriteriaWeights(ProjectID), (v) =>
            {
                v.SetObject();
                DB.SaveChanges();
                return RedirectToAction("IndexCriteria", new { ProjectID = ProjectID });
            });
        }

        public ActionResult EditValues(int ProjectID)
        {
            SetTitle("Edit Values");
            return FlexForm(new CriteriaValues(ProjectID), (v) =>
            {
                v.SetObject();
                DB.SaveChanges();
                return RedirectToAction("IndexCriteria", new { ProjectID = ProjectID });
            });
        }

        public ActionResult BSCResults(int ProjectID)
        {
            SetTitle("BSC Results");
            var bscres = new FuzzyBSC().GenerateResults(DB, ProjectID);
            return View(bscres);
        }

        // Perturbation Scenarios

        public ActionResult IndexPerturbationScenarios(int ProjectID)
        {
            SetTitle("Perturbation Scenarios");
            SetProject(ProjectID);
            return ViewGrid(new PerturbationScenariosGrid(ProjectID) { InsertActionName = "InsertPerturbationScenario", ShowInsert = true, InsertParam = new { ProjectID = ProjectID } }.Init(this));
        }

        public ActionResult InsertPerturbationScenario(int ProjectID)
        {
            SetTitle("Insert Perturbation Scenario");
            SetProject(ProjectID);
            return EditInsertForm(new PerturbationScenarioForm(null, ProjectID));
        }

        public ActionResult EditPerturbationScenario(int ID, int ProjectID)
        {
            SetTitle("Edit Perturbation Scenario");
            SetProject(ProjectID);
            return EditInsertForm(new PerturbationScenarioForm(ID, ProjectID));
        }

        public ActionResult DeletePerturbationScenario(int ID, int ProjectID)
        {
            SetProject(ProjectID);
            return DeleteForm(new PerturbationScenarioForm(ID, ProjectID), RedirectToAction("IndexPerturbationScenarios", new { ProjectID = ProjectID }));
        }

        // Perturbation Scenario Item

        public ActionResult IndexPerturbationScenarioItems(int ProjectID, int ScenarioID)
        {
            SetProject(ProjectID);
            return ViewGrid(new PerturbationScenarioItemsGrid(ProjectID, ScenarioID) { InsertActionName = "InsertPerturbationScenarioItem", ShowInsert = true, InsertParam = new { ProjectID = ProjectID, ScenarioID = ScenarioID } }.Init(this));
        }

        public ActionResult InsertPerturbationScenarioItem(int ProjectID, int ScenarioID)
        {
            SetProject(ProjectID);
            return EditInsertForm(new PerturbationScenarioItemForm(null, ProjectID, ScenarioID));
        }

        public ActionResult EditPerturbationScenarioItem(int ID, int ProjectID, int ScenarioID)
        {
            SetProject(ProjectID);
            return EditInsertForm(new PerturbationScenarioItemForm(ID, ProjectID, ScenarioID));
        }

        public ActionResult DeletePerturbationScenarioItem(int ID, int ProjectID, int ScenarioID)
        {
            SetProject(ProjectID);
            return DeleteForm(new PerturbationScenarioItemForm(ID, ProjectID, ScenarioID), RedirectToAction("IndexPerturbationScenarioItems", new { ProjectID = ProjectID, ScenarioID = ScenarioID }));
        }


        // GPNConfigs

        public ActionResult IndexGPNConfigs(int ProjectID)
        {
            SetTitle("GPN Configurations");
            SetProject(ProjectID);
            return ViewGrid(new GPNConfigsGrid(ProjectID) { InsertActionName = "InsertGPNConfig", ShowInsert = true, InsertParam = new { ProjectID = ProjectID } }.Init(this));
        }

        public ActionResult InsertGPNConfig(int ProjectID)
        {
            SetTitle("Insert GPN Configuration");
            SetProject(ProjectID);
            return EditInsertForm(new GPNConfigForm(null, ProjectID));
        }

        public ActionResult EditGPNConfig(int ID, int ProjectID)
        {
            SetTitle("Edit GPN Configuration");
            ViewBag.SavePath = "Dependency/GPN" + ID;
            SetProject(ProjectID);
            return EditInsertForm(new GPNConfigForm(ID, ProjectID));
        }

        public ActionResult ViewGPNConfig(int ID, int ProjectID)
        {
            SetTitle("View GPN Configuration");
            ViewBag.ProjectID = ProjectID;
            ViewBag.GPNID = ID;
            SetProject(ProjectID);
            return View();
        }

        public ActionResult DeleteGPNConfig(int ID, int ProjectID)
        {
            SetProject(ProjectID);
            return DeleteForm(new GPNConfigForm(ID, ProjectID), RedirectToAction("IndexGPNConfigs", new { ProjectID = ProjectID }));
        }


        public void SetProject(int ID)
        {
            var p = DB.Projects.Find(ID);
            if (p.UserID != null && p.UserID != CurrentUser.Id && !CurrentUser.IsAdmin())
                throw new SecurityException("You do not have access to this project.");
        }

        public ActionResult InsertExamples()
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore, PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects };

            foreach (var json in new[] { Examples.Example1, Examples.Example2, Examples.Example3, Examples.Example4 })
            {
                var proj = Newtonsoft.Json.JsonConvert.DeserializeObject<Project>(json, settings);
                proj.User = CurrentUser;
                DB.Projects.Add(proj);
                DB.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public ActionResult AnalysisUncertainty(int ProjectID, int? ScenarioID)
        {
            SetProject(ProjectID);
            var p = DB.Projects.Find(ProjectID);

            if (ScenarioID.HasValue)
                ViewBag.Scenario = DB.PerturbationScenarios.Find(ScenarioID);

            return View(new[] { Tuple.Create("(Default)", new ScenarioAnalysis(DB, ProjectID)) }
                .Concat(p.GPNConfigurations.Select(gpn => Tuple.Create(gpn.Name, new ScenarioAnalysis(DB, ProjectID, gpn.ID)))).ToArray());
        }

        public ActionResult AnalysisSensitivity(int ProjectID, int? ScenarioID)
        {
            SetProject(ProjectID);
            var p = DB.Projects.Find(ProjectID);

            if (ScenarioID.HasValue)
                ViewBag.Scenario = DB.PerturbationScenarios.Find(ScenarioID);

            return View(new[] { Tuple.Create("(Default)", new ScenarioAnalysis(DB, ProjectID)) }
                .Concat(p.GPNConfigurations.Select(gpn => Tuple.Create(gpn.Name, new ScenarioAnalysis(DB, ProjectID, gpn.ID)))).ToArray());
        }

        public ActionResult OptimiseUncertainty(int ProjectID)
        {
            SetProject(ProjectID);
            var p = DB.Projects.Find(ProjectID);

            // Use default configuration.
            var analysis = new ScenarioAnalysis(DB, ProjectID);
            var defu = analysis.GetLossUncertainty();

            var listparam = analysis.Parameters
                .Where(p2 => p2.Value.InitValue.A < p2.Value.InitValue.C).Select(p2 => p2.Value).ToArray();

            var numparam = listparam.Length;

            var arg = new BasicSODiscreteDecisionParams((dic) => 
                {
                    var red = dic["Reductions"];
                    return analysis.ExpectedLossUncertaintyExcept(0.5D, red.Select(ind => listparam[ind]).ToArray());
                },
                new DiscreteDecisionParamDef("Reductions", Enumerable.Range(0, 4).Select(u => numparam - 1).ToArray()));

            var sb = new StringBuilder();

            var ga = new SOGA<ArrayChromosome>(arg) { PopulationSize = 50, MaximumGeneration = 50 };
            int rep = 0;
            ga.OnNewPopulation = (pop, time) =>
                {
                    sb.AppendFormat("Gen: {0} Best: {1} ({2:F2}%) <br/>", rep++, pop.BestChromosome.Objectives[0], 100D * (defu - pop.BestChromosome.Objectives[0]) / defu);
                    var dic = arg.FillIntDic(pop.BestChromosome as ArrayChromosome);
                    sb.AppendFormat("{0} <br/>", string.Join(",", dic["Reductions"].Select(ind => listparam[ind].Title)));
                    return true;
                };
            ga.Run();
            return ViewHtml(sb.ToString());
        }

        public ActionResult AnalysisAll(int ProjectID, bool HideDefault = false)
        {
            SetProject(ProjectID);
            var p = DB.Projects.Find(ProjectID);

            var list = new List<Tuple<string, ScenarioAnalysis>>();

            if (!HideDefault)
                list.Add(Tuple.Create("(Default)", new ScenarioAnalysis(DB, ProjectID)));

            list.AddRange(p.GPNConfigurations.Select(gpn => Tuple.Create(gpn.Name, new ScenarioAnalysis(DB, ProjectID, gpn.ID))));

            return View(list.ToArray());
        }

        public ActionResult Analysis(int ProjectID, int? ScenarioID, int? GPNConfigID)
        {
            SetProject(ProjectID);

            var p = DB.Projects.Find(ProjectID);
            var nodes = p.Nodes.ToArray();
            var model = new FuzzyIIMViewModel() { Form = new FuzzyIIMForm(nodes.ToList(), !ScenarioID.HasValue) };

            var dic = new Dictionary<int, double>();
            if (!IsGet) model.Form.SetForm(Request.Form, null);
            else
            {
                foreach (var n in nodes)
                    dic[n.ID] = n.DefaultPurturbation.B;
                model.Form.GetObject(dic);
            }

            model.Form.SetObject(dic);

            model.Scenarios = new SelectListItem[] { new SelectListItem() { Text = "(Default)", Value = "" }}.Concat(p.PerturbationScenarios.Select(s => new SelectListItem() { Text = s.Name, Value = s.ID.ToString(), Selected = s.ID == ScenarioID }));
            model.GPNConfigs = new SelectListItem[] { new SelectListItem() { Text = "(Default)", Value = "" }}.Concat(p.GPNConfigurations.Select(s => new SelectListItem() { Text = s.Name, Value = s.ID.ToString(), Selected = s.ID == ScenarioID }));
            model.GPNConfigID = GPNConfigID;

            model.IIM = new ScenarioAnalysis(DB, p.ID, GPNConfigID, false).CreateIIMModel(ScenarioID, dic);
            
                //Enumerable.Range(0, c.Length).Select(i => new TriMF(0.4 * multi, 0.5 * multi, 0.6 * multi, "")).ToArray();
                //var multi = 2D;

            return View(model);
        }

        public ActionResult ShowInopChart(int ProjectID, int? ScenarioID, int? GPNConfigID, int NodePos)
        {
            SetProject(ProjectID);

            var p = DB.Projects.Find(ProjectID);
            var nodes = p.Nodes.ToArray();
            var model = new FuzzyIIMViewModel() { Form = new FuzzyIIMForm(nodes.ToList(), !ScenarioID.HasValue) };

            var dic = new Dictionary<int, double>();
            model.Form.SetForm(Request.QueryString, null);

            model.Form.SetObject(dic);


            model.IIM = new ScenarioAnalysis(DB, ProjectID, GPNConfigID, false).CreateIIMModel(ScenarioID, dic);

            return File(model.IIM.GenerateChart(NodePos, 1000, 400, false, true), "image/png");
        }



        public ActionResult DependencyCalc(string SavePath)
        {
            var form = new TFNDepCalcForm();
            //form.Init();
            if (!string.IsNullOrEmpty(SavePath))
            {
                if (IsGet)
                { 
                    var values = CurrentUser.Settings.Where(s => s.Path.StartsWith(SavePath + "/")).ToArray();
                    var dic = new NameValueCollection();
                    foreach (var v in values)
                        dic.Add(v.Path.Substring(SavePath.Length + 1), v.Value);
                    form.SetForm(dic, null);
                    form.SetObject();
                }
            }
            return FlexForm(form, (dcf) =>
                {
                    foreach (var f in form.AllFields.Where(f2 => f2 is IOrdinaryFormField).Cast<IOrdinaryFormField>())
                    {
                        var path = SavePath + "/" + f.FieldName;
                        var st = CurrentUser.Settings.FirstOrDefault(s => s.Path == path);
                        if (st == null)
                        {
                            st = new UserSetting() { UserID = CurrentUser.Id, Path = path, Value = f.ReadOnlyValue };
                            DB.UserSettings.Add(st);
                        }
                        st.Value = f.ReadOnlyValue;
                    }
                    DB.SaveChanges();
                    return ViewHtml("<div id='returnresult'>" + dcf.Calculate().ToString("g3") + "</div>");
                });

        }

        public ActionResult TFNCalc(bool IsNumeric = false)
        {
            return FlexForm(new TFNCalcForm(IsNumeric), (dcf) =>
            {
                return ViewHtml("<div id='returnresult'>" + dcf.Calculate().ToString(d => Math.Round(d, 4).ToString("g")) + "</div>");
            });

        }

        public ActionResult CopyProject(int ID)
        {
            SetProject(ID);
            var settings = new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore, PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(DB.Projects.Find(ID), settings);

            var newproj = Newtonsoft.Json.JsonConvert.DeserializeObject<Project>(json, settings);
            newproj.Name = "Copy of " + newproj.Name;
            newproj.User = CurrentUser;
            DB.Projects.Add(newproj);
            var numrec = DB.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}

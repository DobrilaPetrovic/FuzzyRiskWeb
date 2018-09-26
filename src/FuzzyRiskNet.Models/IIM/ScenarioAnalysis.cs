using FuzzyRiskNet.Models;
using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.DataVisualization.Charting;
using FuzzyRiskNet.Helpers;

namespace FuzzyRiskNet.FuzzyRisk
{
    public class ScenarioAnalysis
    {
        RiskDbContext DB;
        Project Project;

        int[] DepCnt;

        public int? GPNConfigID { get; private set; }
        public IMF[,] A { get; private set; }
        public Node[] Nodes { get; private set; }
        public int TimeHorizon { get; private set; }


        public Dictionary<string, UncertainParameter> Parameters = new Dictionary<string, UncertainParameter>();
        public Dictionary<PerturbationScenario, FuzzyIIM> Analysis { get; private set; }
        public ScenarioAnalysis(RiskDbContext DB, int ProjectID, int? GPNConfig = null, bool InitAnalysis = true)
        {
            this.DB = DB;
            this.Project = DB.Projects.Find(ProjectID);
            Nodes = Project.Nodes.ToArray();
            this.GPNConfigID = GPNConfig;

            if (Project.DivideByNumberOfDependencies)
            {
                DepCnt = new int[Nodes.Length];
                for (int i = 0; i < Nodes.Length; i++)
                {
                    var id = Nodes[i].ID;
                    DepCnt[i] = DB.Dependencies.Count(d2 => d2.FromID == id && GPNConfigID == d2.GPNConfigurationID);
                }
            }

            if (InitAnalysis) UpdateAnalysis();
        }

        private void UpdateAnalysis()
        {
            var emptydic = Nodes.ToDictionary(n => n.ID, n => 0D);

            A = CreateDependencyMatrix();

            Analysis = Project.PerturbationScenarios.ToArray()
                .ToDictionary(s => s, s => CreateIIMModel(s.ID, A, emptydic));

            TimeHorizon = Analysis.First().Value.TimeHorizon;
        }

        public TFN AverageInop(int NodeNo)
        {
            TFN total = new TFN();
            int cnt = 0;
            foreach (var s in Analysis.Keys)
            {
                total += Analysis[s].AverageInop(NodeNo);
                cnt++;
            }
            total *= 1D / cnt;
            return total;
        }

        public double ExpectedLossUncertaintyExcept(double Ratio = 0, params UncertainParameter[] Params)
        {
            return SensitivityCombined(UncertaintyMultiplier: Ratio, UncertaintyParams: Params).GetLossUncertainty();
        }

        public double ParamAmbSensitivity(UncertainParameter Param, double UncertaintyMultiplier = 0)
        {
            Param.Reset();

            Param.Value = new TFN(UncertaintyMultiplier * Param.Value.A + (1 - UncertaintyMultiplier) * Param.Value.B, Param.Value.B, UncertaintyMultiplier * Param.Value.C + (1 - UncertaintyMultiplier) * Param.Value.B);

            return Param.ToTriMF().CalcAmbiguity();
        }

        public double ExpectedLossUncertaintyExcept(int? ScenarioID, double Ratio = 0, params UncertainParameter[] Params)
        {
            return SensitivityCombined(UncertaintyMultiplier: Ratio, UncertaintyParams: Params).GetLossUncertainty(ScenarioID);
        }

        public TFN ExpectedLossSensitivity(double Ratio = 1.1, params UncertainParameter[] Params)
        {
            return SensitivityCombined(Ratio, Params).GetLoss();
        }

        public ScenarioAnalysis SensitivityCombined(double ParamMultiplier = 1.1, UncertainParameter[] Params = null, double UncertaintyMultiplier = 0, UncertainParameter[] UncertaintyParams = null, double ShiftRate = 0, UncertainParameter[] ShiftParams = null)
        {
            foreach (var k in Parameters) k.Value.Reset();
            
            if (Params != null)
                foreach (var Param in Params)
                {
                    if (!Param.Key.StartsWith("Intended") && Param.Max != 1) throw new Exception("Bad!");
                    Param.Value = Param.Value * ParamMultiplier; // TFN.Normalize(Param.Value * ParamMultiplier, Max: Param.Max ?? double.MaxValue);
                }
            
            if (UncertaintyParams != null)
                foreach (var Param in UncertaintyParams)
                    Param.Value = new TFN(UncertaintyMultiplier * Param.Value.A + (1 - UncertaintyMultiplier) * Param.Value.B, Param.Value.B, UncertaintyMultiplier * Param.Value.C + (1 - UncertaintyMultiplier) * Param.Value.B);

            if (ShiftParams != null)
                foreach (var Param in ShiftParams)
                    Param.Value = new TFN(Param.Value.A + ShiftRate * Param.Value.B, (1 + ShiftRate) * Param.Value.B, Param.Value.C + ShiftRate * Param.Value.B);
            
            UpdateAnalysis();
            return this;
        }

        public ScenarioAnalysis RemoveUncertainty()
        {
            foreach (var k in Parameters.Values) 
                k.Value = new TFN(k.InitValue.B, k.InitValue.B, k.InitValue.B);
            UpdateAnalysis();
            return this;
        }

        public ScenarioAnalysis Reset()
        {
            foreach (var k in Parameters) k.Value.Reset();
            UpdateAnalysis();
            return this;
        }


        public bool UseMF = true;

        double? Space = null;

        public double GetLossUncertainty(int? ScenarioID = null)
        {
            if (UseMF)
            {
                if (!Space.HasValue)
                {
                    var alphas = Analysis.Values.First().Alphas.OrderBy(v => v);
                    Space = alphas.ElementAt(1) - alphas.ElementAt(0);
                }
                return GetLossMF(ScenarioID).CalcAmbiguity(Space.Value);
            }
            else
            {
                var loss = GetLoss(ScenarioID);
                return loss.C - loss.A;
            }
        }


        public TFN GetLoss(int? ScenarioID = null)
        {
            return ScenarioID.HasValue ? InopLoss(ScenarioID.Value) : ExpectedInopLoss();
        }

        public CustomIntervalMF GetLossMF(int? ScenarioID = null)
        {
            return ScenarioID.HasValue ? InopLossMF(ScenarioID.Value) : ExpectedInopLossMF();
        }

        public TFN ExpectedInopLoss()
        {
            var val = new TFN();

            foreach (var s in Analysis.Keys)
            {
                var loss = Analysis[s].TotalInopLoss();
                val = val + new TFN(Analysis[s].Likelihood.A * loss.A, Analysis[s].Likelihood.B * loss.B, Analysis[s].Likelihood.C * loss.C);
            }

            return val;
        }

        public CustomIntervalMF ExpectedInopLossMF()
        {
            var alphas = Analysis.Values.First().Alphas;
            return new CustomIntervalMF(alphas.Select(a => new FuzzyInterval(a, Analysis.Values.Sum(l => l.TotalInopLossMF().GetInterval(a).Min * new TriMF(l.Likelihood).GetInterval(a).Min),
                                                                                Analysis.Values.Sum(l => l.TotalInopLossMF().GetInterval(a).Max * new TriMF(l.Likelihood).GetInterval(a).Max))));
        }

        public TFN InopLoss(int ScenarioID)
        {
            var scenario = Analysis.Keys.First(s => s.ID == ScenarioID);
            return Analysis[scenario].TotalInopLoss();
        }

        public CustomIntervalMF InopLossMF(int ScenarioID)
        {
            var scenario = Analysis.Keys.First(s => s.ID == ScenarioID);
            return Analysis[scenario].TotalInopLossMF();
        }

        public byte[] DrawBoard(int Width = 1000, int Height = 2000)
        {
            var chart = new Chart();
            chart.Width = Width;
            chart.Height = Height;
           
            bool First = true;

            var elwidth = 98F / (Analysis.Keys.Count);
            var elheight = 93F / Nodes.Length;

            var ScenarioNo = 0;
            var font = new System.Drawing.Font("Arial", 12);
            chart.Legends.Add(new Legend() {  Position = new ElementPosition(0, 93, 100, 7), Font = font  });
            chart.CustomizeLegend += chart_CustomizeLegend;
            foreach (var scenario in Analysis.Keys)
            {
                chart.Annotations.Add(new TextAnnotation() { X = ScenarioNo * elwidth + 2, Y = 0, Height = 2, Width = elwidth, Text = scenario.Name, Alignment = System.Drawing.ContentAlignment.MiddleCenter, Font = font });

                var NodeNo = 0;
                foreach (var n in Nodes)
                {
                    var scope = NodeNo + "-" + ScenarioNo;
                    /*var pertarea = chart.ChartAreas.Add("Perturbation-" + scope);
                    
                    pertarea.AxisY.Maximum = 1;
                    pertarea.AxisY.Minimum = 0;
                    pertarea.AxisX.Minimum = 0;
                    pertarea.AxisX.Maximum = TimeHorizon - 1;
                    pertarea.AxisX.Title = n.Name;
                    pertarea.AxisY.Title = "Perturbation";*/

                    var inoparea = chart.ChartAreas.Add("Inoperability-" + scope);

                    if (ScenarioNo > 0)
                    {
                        inoparea.AlignWithChartArea = "Inoperability-" + NodeNo + "-0";
                        //inoparea.AlignmentStyle = AreaAlignmentStyles.Position;
                        inoparea.AlignmentOrientation = AreaAlignmentOrientations.Horizontal;
                    }

                    inoparea.AxisY.Maximum = 1;
                    inoparea.AxisY.Minimum = 0;
                    inoparea.AxisX.Minimum = 0;
                    inoparea.AxisX.Maximum = TimeHorizon - 1;
                    inoparea.AxisX.Interval = 10;
                    
                    inoparea.Position = new ElementPosition() { X = ScenarioNo * elwidth + (ScenarioNo > 0 ? 2 : 0), Width = elwidth + (ScenarioNo > 0 ? 0 : 2), Y = NodeNo * elheight + 2, Height = elheight };
                    if (ScenarioNo == 0)
                    {
                        if (n.Name.StartsWith("Ferment")) inoparea.AxisY.Title = "Ferment.";
                        else inoparea.AxisY.Title = n.Name;
                    }
                    inoparea.AxisY.TitleFont = new System.Drawing.Font("Arial", 11);

                    int cnt = 0;
                    var dashes = new ChartDashStyle[] { ChartDashStyle.Solid, ChartDashStyle.Dash, ChartDashStyle.DashDot, ChartDashStyle.Dot };
                    var markerstyles = new MarkerStyle[] { MarkerStyle.Square, MarkerStyle.Circle, MarkerStyle.Diamond, MarkerStyle.Cross, MarkerStyle.Triangle, MarkerStyle.Cross, MarkerStyle.Diamond, MarkerStyle.Circle, MarkerStyle.Square };

                    foreach (var t in Analysis[scenario].qt.OrderBy(t2 => t2.Item1.Contains("Min") ? -1 + t2.Item2 : 1 - t2.Item2) /*.Where(v => (int)Math.Round((v.Item2 * 10)) % 2 == 0)*/)
                    {
                        var s = new Series(t.Item1 + scope) { ChartType = SeriesChartType.Line, ChartArea = inoparea.Name, LegendText = t.Item1.Replace("Alpha", "ɑ"), IsVisibleInLegend = First };
                        if (t.Item2 == 0 || t.Item2 == 1) s.BorderWidth = 3;
                        
                        int BorderWidth = (t.Item2 == 0 || t.Item2 == 1) ? 3 : 1;
                        System.Drawing.Color? Color = null;
                        if (t.Item2 == 0 && t.Item1.Contains("Min")) Color = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF);
                        if (t.Item2 == 0.25 && t.Item1.Contains("Min")) Color = System.Drawing.Color.FromArgb(0x00, 0x20, 0xAA);
                        if (t.Item2 == 0.5 && t.Item1.Contains("Min")) Color = System.Drawing.Color.FromArgb(0x00, 0x40, 0x80);
                        if (t.Item2 == 0.75 && t.Item1.Contains("Min")) Color = System.Drawing.Color.FromArgb(0x00, 0x60, 0x40);
                        if (t.Item2 == 1) Color = System.Drawing.Color.FromArgb(0x00, 0x80, 0x00);
                        if (t.Item2 == 0.75 && t.Item1.Contains("Max")) Color = System.Drawing.Color.FromArgb(0x40, 0x60, 0x00);
                        if (t.Item2 == 0.5 && t.Item1.Contains("Max")) Color = System.Drawing.Color.FromArgb(0x80, 0x40, 0x00);
                        if (t.Item2 == 0.25 && t.Item1.Contains("Max")) Color = System.Drawing.Color.FromArgb(0xAA, 0x20, 0x00);
                        if (t.Item2 == 0 && t.Item1.Contains("Max")) Color = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00);
                        ChartDashStyle DashStyle = dashes[cnt % dashes.Length];

                        s.BorderWidth = BorderWidth; s.BorderDashStyle = DashStyle; if (Color.HasValue) s.Color = Color.Value;
                        chart.Series.Add(s); s.MarkerStyle = markerstyles[cnt]; s.MarkerSize = 100;
                        
                        for (int i = 0; i < TimeHorizon; i++)
                        {
                            var ind = s.Points.AddXY(i, t.Item3[i][NodeNo]);
                            s.Points[ind].MarkerSize = i % 10 != 5 ? 0 : (BorderWidth > 1 ? 8 : 4);
                        }

                        /*
                        // Create corresponding perturbation series
                        var ps = new Series(t.Item1.ToString() + "Perturb" + scope) { ChartType = SeriesChartType.Line, ChartArea = pertarea.Name, IsVisibleInLegend = false };
                        ps.BorderWidth = s.BorderWidth; ps.Color = s.Color;
                        chart.Series.Add(ps);

                        for (int i = 0; i < TimeHorizon; i++)
                            ps.Points.AddXY(i, t.Item1.Contains("Min") ? Analysis[scenario].ct[i][NodeNo].GetInterval(t.Item2).Min : Analysis[scenario].ct[i][NodeNo].GetInterval(t.Item2).Max);
                         */
                        cnt++;
                    }
                    First = false;
                    NodeNo++;
                }
                ScenarioNo++;
            }
            //chart.Annotations.Add(new TextAnnotation() { X = 0, Y = 0, Height = 100, Width = 5, Text = "ttgdfsgdfsgdsfgf", AxisX = chart.ChartAreas[0].AxisY, AxisY = chart.ChartAreas[0].AxisX });
            return chart.ToPngByteArray();
        }

        void chart_CustomizeLegend(object sender, CustomizeLegendEventArgs e)
        {
            var leg6 = e.LegendItems[6];
            var leg7 = e.LegendItems[7];
            var leg8 = e.LegendItems[8];
            e.LegendItems.RemoveAt(8);
            e.LegendItems.RemoveAt(7);
            e.LegendItems.RemoveAt(6);
            e.LegendItems.Insert(1, leg6);
            e.LegendItems.Insert(3, leg7);
            e.LegendItems.Insert(5, leg8);
        }

        public FuzzyIIM CreateIIMModel(int? ScenarioID, Dictionary<int, double> Adjustments)
        {
            return CreateIIMModel(ScenarioID, CreateDependencyMatrix(), Adjustments);
        }
        Dictionary<int, Tuple<PerturbationScenario, PerturbationScenarioItem[]>> scenariodata = new Dictionary<int, Tuple<PerturbationScenario, PerturbationScenarioItem[]>>();
        public FuzzyIIM CreateIIMModel(int? ScenarioID, IMF[,] A, Dictionary<int, double> Adjustments)
        {
            var ProjectID = Project.ID;

            var exp = new FuzzyIIM(A);
            exp.Nodes = Nodes.ToList();
            exp.Project = Project;

            exp.K = Nodes.Select(n => SetParameter("Resilience" + n.ID, "Resilience of " + n.Name, n.Resilience, 0, 1).ToTriMF()).ToArray();
            exp.IntendedRevenue = Nodes.Select(n => SetParameter("IntendedRevenue" + n.ID, "Intended revenue of " + n.Name, n.CostPetUnitInoperability, 0).NormlizedValue).ToArray();

            IMF[][] c;
            if (!ScenarioID.HasValue)
            {
                var defpert = Nodes.Select(n => SetParameter("DefPert" + n.ID, "Default Perturbation of " + n.Name, n.DefaultPurturbation, 0, 1).NormlizedValue).ToArray();
                //TODO: Remove adjustments (use the default parameter senstivity mechnasim) + use triangular normalisation provided by the UncertaintyParameter
                c = Enumerable.Range(0, exp.TimeHorizon).Select(t => Nodes.Select((n, nind) => new TriMF(Math.Max(0, defpert[nind].A - defpert[nind].B + Adjustments[n.ID]), Adjustments[n.ID], Math.Min(1, defpert[nind].C - defpert[nind].B + Adjustments[n.ID]), "")).ToArray()).ToArray();
            }
            else
            {
                if (!scenariodata.ContainsKey(ScenarioID.Value))
                {
                    scenariodata.Add(ScenarioID.Value, 
                        Tuple.Create(DB.PerturbationScenarios.Find(ScenarioID),
                        DB.PerturbationScenarioItems.Where(i => i.PerturbationScenarioID == ScenarioID && i.PerturbationScenario.ProjectID == ProjectID).ToArray()));
                }
                var scenario = scenariodata[ScenarioID.Value].Item1;
                var ct = Enumerable.Range(0, exp.TimeHorizon).Select(t => Nodes.Select(n => new TFN()).ToArray()).ToArray();
                var items = scenariodata[ScenarioID.Value].Item2;
                var nodeindices = Nodes.Select((n, ind) => new { ID = n.ID, Ind = ind }).ToDictionary(n => n.ID, n => n.Ind);
                var pitemsind = 1;
                foreach (var pitem in items)
                {
                    var param = SetParameter("Impact" + scenario.ID + "-" + pitem.ID, "Impact of " + scenario.Name + " Item: " + pitemsind, pitem.Purturbation, 0, 1);
                    for (int t = pitem.StartPeriod; t < pitem.StartPeriod + pitem.Duration && t < exp.TimeHorizon; t++)
                    {
                        var value = param.NormlizedValue;

                        if (pitem.NodeID.HasValue)
                            ct[t][nodeindices[pitem.NodeID.Value]] += value;
                        else if (pitem.RegionID.HasValue)
                            foreach (var n in Nodes.Where(n2 => n2.RegionID == pitem.RegionID.Value ||
                                (pitem.Region != null && n2.RegionID.HasValue && n2.Region.Parent != null && n2.Region.Parent.ID == pitem.Region.ID) ||
                                (pitem.Region != null && n2.RegionID.HasValue && n2.Region.Parent != null && n2.Region.Parent.Parent != null && n2.Region.Parent.Parent.ID == pitem.Region.ID)))
                                ct[t][nodeindices[n.ID]] += value;
                    }
                    pitemsind++;
                }
                c = Enumerable.Range(0, exp.TimeHorizon).Select(t => Nodes.Select((node, n) => ConvertTriMF(ct[t][n])).ToArray()).ToArray();
                exp.Likelihood = SetParameter("Likelihood" + ScenarioID, "Likelihood of " + scenario.Name, scenario.Likelihood, 0, 1).NormlizedValue;
            }

            exp.Calculate(null, c);

            return exp;
        }

        private IMF[,] CreateDependencyMatrix()
        {
            var configname = GPNConfigID.HasValue ? DB.GPNConfigurations.Find(GPNConfigID).Name : "";
            var len = Nodes.Length;
            var A = new IMF[len, len];
            for (int i = 0; i < len; i++)
                for (int j = 0; j < len; j++)
                {
                    var dep = Nodes[i].Dependencies.FirstOrDefault(d => d.ToID == Nodes[j].ID && GPNConfigID == d.GPNConfigurationID);

                    if (dep != null)
                    {
                        var param = SetParameter("Dep" + dep.ID, configname + (string.IsNullOrEmpty(configname) ? "" : ": ") + "Dependency of " + Nodes[j].Name + " on " + Nodes[i].Name, dep.Rate, 0, 1);
                        if (Project.DivideByNumberOfDependencies)
                        {
                            A[i, j] = ConvertTriMF(param.NormlizedValue * (1D / DepCnt[i]));
                        }
                        else
                        {
                            A[i, j] = ConvertTriMF(param.NormlizedValue);
                        }
                    }
                    else
                        A[i, j] = TriMF.Zero;
                }
            return A;
        }
        private static IMF ConvertTriMF(TFN TFN)
        {
            return new TriMF(TFN.A, TFN.B, TFN.C, "");
        }

        private UncertainParameter SetParameter(string Key, string Title, TFN InitValue, double? Min = null, double? Max = null)
        {
            if (Parameters.ContainsKey(Key)) return Parameters[Key];
            var p = new UncertainParameter() { Key = Key, Title = Title, Value = InitValue, InitValue = InitValue, Min = Min, Max = Max };
            Parameters.Add(Key, p);
            return p;
        }
    }

    public class UncertainParameter
    {
        public string Key { get; set; }
        public string Title { get; set; }

        public TFN InitValue { get; set; }

        public TFN Value { get; set; }

        public TFN NormlizedValue { get { return new TFN(FilterMinMax(Value.A), FilterMinMax(Value.B), FilterMinMax(Value.C)); } }

        public IMF ToTriMF()
        {
            return new TriMF(FilterMinMax(Value.A), FilterMinMax(Value.B), FilterMinMax(Value.C), "");
        }

        public void Reset() { this.Value = InitValue; }

        public double FilterMinMax(double Val)
        {
            return Math.Max(Min ?? double.MinValue, Math.Min(Max ?? double.MaxValue, Val));
        }

        public double? Min { get; set; }
        public double? Max { get; set; }
    }
}
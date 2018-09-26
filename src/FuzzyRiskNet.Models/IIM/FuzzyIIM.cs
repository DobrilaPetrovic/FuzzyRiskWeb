using FuzzyRiskNet.Models;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web.UI.DataVisualization.Charting;

namespace FuzzyRiskNet.FuzzyRisk
{
    public class FuzzyIIM
    {
        public FuzzyIIM(IMF[,] A) { this.A = A; TimeHorizon = 50; r = new Random(); Alphas = Enumerable.Range(0, 5).Select(i => i * 0.25D).ToArray(); }

        public double[] Alphas { get; private set; }

        Random r;

        public IMF[,] A { get; private set; }

        double GetRandomInAlpha(double Alpha, IMF mf)
        {
            var interval = mf.GetInterval(Alpha);
            return r.NextDouble() * (interval.Max - interval.Min) + interval.Min;
        }

        public int TimeHorizon { get; set; }

        public string GetName(int Ind)
        {
            if (Nodes == null || Nodes[Ind] == null) return "Node " + Ind;
            return Nodes[Ind].Name;
        }

        /// <summary>
        /// Resilience of nodes
        /// </summary>
        public IMF[] K { get; set; }

        /// <summary>
        /// Intended revenue of nodes
        /// </summary>
        public TFN[] IntendedRevenue { get; set; }

        /// <summary>
        /// Likelihood of the scenario. Zero if no scenario is defined.
        /// </summary>
        public TFN Likelihood { get; set; }
        public IMF[] q { get; set; }
        public IMF[][] ct { get; set; }

        public List<Node> Nodes { get; set; }

        public Project Project { get; set; }

        public void Calculate(IMF[] q, IMF[][] ct)
        {
            if (ct == null && q != null) ct = Enumerable.Range(0, TimeHorizon).Select(t => Enumerable.Range(0, q.Length).Select(i => TriMF.Zero).ToArray()).ToArray();
            if (q == null && ct != null) q = Enumerable.Range(0, ct[0].Length).Select(i => TriMF.Zero).ToArray();

            if (q.Length != ct[0].Length || q.Length != A.GetLength(0) || q.Length != A.GetLength(1))
                throw new Exception("Dimension mistmatch.");

            this.q = q;
            this.ct = ct;

            var len = q.Length;


            var res = DIIMCalc(TimeHorizon, A, q, ct, K, Alphas);
            qtIntervals = res;

            // This is used for chart drawing
            this.qt = new List<Tuple<string, double, double[][]>>();

            qt.Add(Tuple.Create("Alpha = 0, Min", 0D, Aqt = res.Select(rest => rest.Select(restn => restn.GetInterval(0).Min).ToArray()).ToArray()));
            qt.Add(Tuple.Create("Alpha = 1", 1D, Bqt = res.Select(rest => rest.Select(restn => restn.GetInterval(1).Min).ToArray()).ToArray()));
            qt.Add(Tuple.Create("Alpha = 0, Max", 0D, Cqt = res.Select(rest => rest.Select(restn => restn.GetInterval(0).Max).ToArray()).ToArray()));

            foreach (var alpha in Alphas.Where(a => a > 0 && a < 1))
            {
                qt.Add(Tuple.Create("Alpha = " + alpha + ", Min", alpha, res.Select(rest => rest.Select(restn => restn.GetInterval(alpha).Min).ToArray()).ToArray()));
                qt.Add(Tuple.Create("Alpha = " + alpha + ", Max", alpha, res.Select(rest => rest.Select(restn => restn.GetInterval(alpha).Max).ToArray()).ToArray()));
            }
        }
        public void Calculate(IMF[] q, IMF[] c = null)
        {
            if (c == null) Calculate(q, (IMF[][])null);
            Calculate(q, Enumerable.Range(0, TimeHorizon).Select(t => c).ToArray());
        }

        private void SimulateAtAlpha(IMF[] q, IMF[] c, double alpha)
        {
            Simulate(q, c, mf => GetRandomInAlpha(alpha, mf), alpha, alpha.ToString());
        }

        private double[][] Simulate(IMF[] q, IMF[] c, Func<IMF, double> GetValue, double Alpha, string AddTitle = null)
        {
            var len = q.Length;
            var ACrisp = DenseMatrix.OfIndexed(len, len, Enumerable.Range(0, len)
                .SelectMany(i => Enumerable.Range(0, len).Select(j => Tuple.Create(i, j, GetValue(A[i, j])))));
            var ccrisp = DenseVector.OfEnumerable(c.Select(mf => GetValue(mf)));
            Vector<double> qnext = DenseVector.OfEnumerable(q.Select(mf => GetValue(mf)));

            var kmat = K != null ? DiagonalMatrix.OfDiagonal(len, len, K.Select(mf => GetValue(mf))) : null;

            var qt = new double[TimeHorizon][];
            for (int i = 0; i < TimeHorizon; i++)
            {
                qt[i] = qnext.ToArray();
                if (kmat == null) qnext = ACrisp * qnext + ccrisp;
                else qnext = kmat * ACrisp * qnext + kmat * ccrisp + qnext - kmat * qnext;
            }

            if (AddTitle != null) this.qt.Add(Tuple.Create(AddTitle, Alpha, qt));

            return qt;
        }

        private static CustomIntervalMF[][] DIIMCalc(int TimeHorizon, IMF[,] A, IMF[] q, IMF[][] ct, IMF[] K, double[] Alphas)
        {
            var qt = new FuzzyInterval[TimeHorizon][][];
            var len = q.Length;

            var AInt = new FuzzyInterval[len, len][];
            for (int i = 0; i < len; i++)
                for (int j = 0; j < len; j++)
                    AInt[i, j] = Alphas.Select(alpha => A[i, j].GetInterval(alpha)).ToArray();

            qt[0] = Enumerable.Range(0, len).Select(n => Alphas.Select(alpha => q[n].GetInterval(alpha)).ToArray()).ToArray();

            var cInt = Enumerable.Range(0, TimeHorizon).Select(t => Enumerable.Range(0, len).Select(i => Alphas.Select(alpha => ct[t][i].GetInterval(alpha)).ToArray()).ToArray()).ToArray();

            var KInt = Enumerable.Range(0, len).Select(i => Alphas.Select(alpha => K[i].GetInterval(alpha)).ToArray()).ToArray();

            for (int t = 1; t < TimeHorizon; t++)
            {
                qt[t] = new FuzzyInterval[len][];
                for (int n = 0; n < len; n++)
                {
                    qt[t][n] = new FuzzyInterval[Alphas.Length];

                    for (int i = Alphas.Length - 1; i >= 0; i--)
                    {
                        var prevq = qt[t - 1][n][i];
                        var qmax = prevq.Max;
                        var qmin = prevq.Min;
                        var qpmin = cInt[t][n][i].Min; var qpmax = cInt[t][n][i].Max;
                        for (int j = 0; j < len; j++)
                        {
                            var a = AInt[n, j][i]; var myqt = qt[t - 1][j][i];
                            qpmin += a.Min * myqt.Min;
                            qpmax += a.Max * myqt.Max;
                        }
                        //var qpmin = Enumerable.Range(0, len).Sum(j => AInt[n, j][i].Min * qt[t - 1][j][i].Min) + cInt[t][n][i].Min;
                        //var qpmax = Enumerable.Range(0, len).Sum(j => AInt[n, j][i].Max * qt[t - 1][j][i].Max) + cInt[t][n][i].Max;
                        var kpmin = qpmin > qmin ? KInt[n][i].Min : KInt[n][i].Max;
                        var kpmax = qpmax > qmax ? KInt[n][i].Max : KInt[n][i].Min;
                        var val = new FuzzyInterval(Alphas[i], kpmin * qpmin + (1 - kpmin) * qmin, kpmax * qpmax + (1 - kpmax) * qmax);
                        //var kmin = KInt[n][i].Min;
                        //var kmax = KInt[n][i].Max;
                        //var val = new FuzzyInterval(alphas[i], Math.Min(kmax * qpmin + (1 - kmax) * prevq.Min, kmin * qpmin + (1 - kmin) * prevq.Min), Math.Max(kmin * qpmax + (1 - kmin) * prevq.Max, kmax * qpmax + (1 - kmax) * prevq.Max));


                        var minfunc = Math.Max(qpmin, qmin);
                        var maxfunc = Math.Min(qpmax, qmax);

                        if (minfunc <= maxfunc)
                        {
                            //if (val.Min > minfunc) throw new Exception(); // To check if this actually happens
                            //if (val.Max < maxfunc) throw new Exception();
                            val.Min = Math.Min(val.Min, minfunc);
                            val.Max = Math.Max(val.Max, maxfunc); 
                        }

                        val.Min = Math.Min(1, val.Min);
                        val.Max = Math.Min(1, val.Max);

                        if (i < Alphas.Length - 1 && val.Min > epsilon + qt[t][n][i + 1].Min) throw new Exception("This should not happen.");
                        if (i < Alphas.Length - 1 && val.Max + epsilon < qt[t][n][i + 1].Max) throw new Exception("This should not happen.");

                        if (val.Min > epsilon + val.Max) throw new Exception("This definitely should not happen!");

                        qt[t][n][i] = val;
                    }
                }
            }

            return qt.Select(qtt => qtt.Select(qttn => new CustomIntervalMF(qttn)).ToArray()).ToArray();
        }

        const double epsilon = 1e-6;

        public List<Tuple<string, double, double[][]>> qt { get; private set; }
        public CustomIntervalMF[][] qtIntervals { get; private set; }

        public double[][] Aqt { get; private set; }
        public double[][] Bqt { get; private set; }
        public double[][] Cqt { get; private set; }

        public CustomIntervalMF TotalInopLossMF(int IndID)
        {
            var unitcost = new TriMF(IntendedRevenue[IndID]);
            return new CustomIntervalMF(Alphas.Select(a => new FuzzyInterval(a, qtIntervals.Sum(it => it[IndID].GetInterval(a).Min * unitcost.GetInterval(a).Min), 
                                                                                qtIntervals.Sum(it => it[IndID].GetInterval(a).Max * unitcost.GetInterval(a).Max))));
        }

        public TFN TotalInopLoss(int IndID)
        {
            var avg = AverageInop(IndID);
            var unitcost = IntendedRevenue[IndID];
            return new TFN(avg.A * TimeHorizon * unitcost.A, avg.B * TimeHorizon * unitcost.B, avg.C * TimeHorizon * unitcost.C);
        }

        public CustomIntervalMF TotalInopLossMF()
        {
            var unitcost = IntendedRevenue.Select(r => new TriMF(r)).ToArray();
            return new CustomIntervalMF(Alphas.Select(a => new FuzzyInterval(a, Enumerable.Range(0, qtIntervals[0].Length).Sum(IndID => qtIntervals.Sum(it => it[IndID].GetInterval(a).Min * unitcost[IndID].GetInterval(a).Min)), 
                                                                                Enumerable.Range(0, qtIntervals[0].Length).Sum(IndID => qtIntervals.Sum(it => it[IndID].GetInterval(a).Max * unitcost[IndID].GetInterval(a).Max)))));
        }
                
        public TFN TotalInopLoss()
        {
            var val = new TFN();
            for (int i = 0; i < Nodes.Count; i++)
                val += TotalInopLoss(i);
            return val;
        }

        public CustomIntervalMF AverageInopMF(int IndID)
        {
            return new CustomIntervalMF(Alphas.Select(a => new FuzzyInterval(a, qtIntervals.Average(it => it[IndID].GetInterval(a).Min), qtIntervals.Average(it => it[IndID].GetInterval(a).Max))));
        }
        public TFN AverageInop(int IndID)
        {
            return new TFN() { A = Aqt.Average(a => a[IndID]), B = Bqt.Average(a => a[IndID]), C = Cqt.Average(a => a[IndID]), };
        }

        public CustomIntervalMF AverageInopMF()
        {
            return new CustomIntervalMF(Alphas.Select(a => new FuzzyInterval(a, qtIntervals.SelectMany(it => it).Average(it => it.GetInterval(a).Min), qtIntervals.SelectMany(it => it).Average(it => it.GetInterval(a).Max))));
        }
        public TFN AverageInop()
        {
            var val = new TFN();
            for (int i = 0; i < Nodes.Count; i++)
                val += AverageInop (i);
            return val * (1D / Nodes.Count);
        }

        public CustomIntervalMF SteadyInopMF(int IndID)
        {
            return qtIntervals[TimeHorizon - 1][IndID];
        }

        public TFN SteadyInop(int IndID)
        {
            return new TFN() { A = Aqt[TimeHorizon - 1][IndID], B = Bqt[TimeHorizon - 1][IndID], C = Cqt[TimeHorizon - 1][IndID] };
        }
        
        public byte[] GenerateChart(int NodeNo, int Width = 300, int Height = 150, bool HavePerturbationArea = true, bool HaveInoperabilityArea = true)
        {
            var titlefont = new Font("Times New Roman", 16);
            var chart = new Chart();
            chart.Width = Width;
            chart.Height = Height;
            
            ChartArea pertarea = null;

            if (HavePerturbationArea)
            {
                pertarea = chart.ChartAreas.Add("Perturbation");

                pertarea.AxisY.Maximum = 1;
                pertarea.AxisY.Minimum = 0;
                pertarea.AxisX.Minimum = 0;
                pertarea.AxisX.Maximum = TimeHorizon - 1;
                pertarea.AxisX.Interval = 10;
                pertarea.AxisX.Title = "Time\r\n" + GetName(NodeNo);
                pertarea.AxisY.Title = "Perturbation";
            }

            ChartArea inoparea = null;

            if (HaveInoperabilityArea)
            {
                inoparea = chart.ChartAreas.Add("Inoperability");

                inoparea.AxisY.Maximum = 1;
                inoparea.AxisY.Minimum = 0;
                inoparea.AxisX.Minimum = 0;
                inoparea.AxisX.Maximum = TimeHorizon - 1;
                inoparea.AxisX.Interval = 10;
                inoparea.AxisX.Title = "Time\r\n" + GetName(NodeNo);
                inoparea.AxisY.Title = "Inoperability";
                inoparea.AxisX.TitleFont = titlefont;
                inoparea.AxisY.TitleFont = titlefont;
            }

            chart.Legends.Add(new Legend() { });
            chart.Legends[0].Font = new Font("Times New Roman", 11);
            chart.CustomizeLegend += chart_CustomizeLegend;
            int cnt = 0;
            var dashes = new ChartDashStyle[] { ChartDashStyle.Solid, ChartDashStyle.Dash, ChartDashStyle.DashDot, ChartDashStyle.Dot };
            var markerstyles = new MarkerStyle[] { MarkerStyle.Square, MarkerStyle.Circle, MarkerStyle.Diamond, MarkerStyle.Cross, MarkerStyle.Triangle, MarkerStyle.Cross, MarkerStyle.Diamond, MarkerStyle.Circle, MarkerStyle.Square };
            foreach (var t in qt.OrderByDescending(t2 => t2.Item1.Contains("Min") ? -1 + t2.Item2 : 1 - t2.Item2) /*.Where(v => (int)Math.Round((v.Item2 * 10)) % 2 == 0)*/)
            {
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

                if (HaveInoperabilityArea)
                {
                    // Create inoperability series
                    var s = new Series(t.Item1.ToString().Replace("Alpha", "ɑ")) { ChartType = SeriesChartType.Line, ChartArea = inoparea.Name };
                    s.BorderWidth = BorderWidth; s.BorderDashStyle = DashStyle; if (Color.HasValue) s.Color = Color.Value;
                    chart.Series.Add(s); s.MarkerStyle = markerstyles[cnt]; s.MarkerSize = 100; 

                    for (int i = 0; i < TimeHorizon; i++)
                    {
                        var ind = s.Points.AddXY(i, t.Item3[i][NodeNo]);
                        s.Points[ind].MarkerSize = i % 10 != 5 ? 0 : 8;
                    }
                }

                if (HavePerturbationArea)
                {
                    // Create corresponding perturbation series
                    var ps = new Series(t.Item1.ToString() + "Perturb") { ChartType = SeriesChartType.Line, ChartArea = pertarea.Name, IsVisibleInLegend = false };
                    ps.BorderWidth = BorderWidth; ps.BorderDashStyle = DashStyle; if (Color.HasValue) ps.Color = Color.Value;
                    ps.MarkerStyle = markerstyles[cnt]; ps.MarkerSize = 100;

                    chart.Series.Add(ps);

                    for (int i = 0; i < TimeHorizon; i++)
                    {
                        var ind = ps.Points.AddXY(i, t.Item1.Contains("Min") ? ct[i][NodeNo].GetInterval(t.Item2).Min : ct[i][NodeNo].GetInterval(t.Item2).Max);
                        ps.Points[ind].MarkerSize = i % 10 != 5 ? 0 : 8;
                    }
                }
                cnt++;
            }

            return chart.ToPngByteArray();
        }

        void chart_CustomizeLegend(object sender, CustomizeLegendEventArgs e)
        {
            foreach (var li in e.LegendItems)
                foreach (var c in li.Cells)
                {
                    if (c.CellType == LegendCellType.SeriesSymbol) c.SeriesSymbolSize = new Size(500, 100);                    
                }
        }        
    }
}
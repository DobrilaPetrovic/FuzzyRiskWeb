using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web.UI.DataVisualization.Charting;

namespace FuzzyRiskNet.Fuzzy
{
    public static class MFChart
    {
        private static IEnumerable<bool[]> GenIsPositives(int NumVars)
        {
            if (NumVars == 1) return new bool[][] { new bool[] { true }, new bool[] { false } };
            return new bool[][] { new bool[] { true }, new bool[] { false } }.SelectMany(m => GenIsPositives(NumVars - 1).Select(m2 => m.Concat(m2).ToArray()));
        }

        private static IEnumerable<int[]> GenCombinations(int NumVars, int NumOptions)
        {
            var comb = new int[NumVars];

            while (comb[NumVars - 1] < NumOptions)
            {
                yield return comb;

                comb[0]++;
                for (int i = 0; i < NumVars - 1; i++)
                    if (comb[i] == NumOptions) { comb[i] = 0; comb[i + 1]++; }
            }
        }

        private static IEnumerable<int[]> GenCombinations(int NumVars, int[] NumOptions)
        {
            var comb = new int[NumVars];

            while (comb[NumVars - 1] < NumOptions[NumVars - 1])
            {
                yield return comb;

                comb[0]++;
                for (int i = 0; i < NumVars - 1; i++)
                    if (comb[i] == NumOptions[i]) { comb[i] = 0; comb[i + 1]++; }
            }
        }

        public static byte[] GenerateChart(IMF[] FuzzyVariables, Func<double[], double> Function, bool Simulate = true,
            string XTitle = "", int Width = 1000, int Height = 300)
        {
            var chart = new Chart();
            chart.Width = Width;
            chart.Height = Height;
            chart.ChartAreas.Add("Main");

            chart.ChartAreas[0].AxisY.Maximum = 1;
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisX.Title = XTitle;
            chart.ChartAreas[0].AxisY.Title = "Membership Degree";
            chart.FormatNumber += chart_FormatNumber;

            if (Simulate) AddSimulation(FuzzyVariables, Function, chart);

            //IntervalEstimateOld(FuzzyVariables, Function, chart);
            
            ReducedTransformation(FuzzyVariables, Function, chart);
            
            //if (Simulate) GeneralTransformation(FuzzyVariables, Function, chart);
            if (Simulate) ModifiedGeneralTransformation(FuzzyVariables, Function, chart);

            if (Simulate) chart.Legends.Add("L1");

            chart.ChartAreas[0].AxisX.RoundAxisValues();

            return chart.ToPngByteArray();
        }

        private static void ModifiedGeneralTransformation(IMF[] FuzzyVariables, Func<double[], double> Function, Chart chart)
        {
            bool[][] ispositives = GenIsPositives(FuzzyVariables.Length).ToArray();
            var s1 = chart.Series.Add("Mod. General transformation"); s1.Color = Color.Blue;
            s1.ChartType = SeriesChartType.Line;

            var step = 0.015D;
            var vals = new double[FuzzyVariables.Length];

            var prevmin = new double[FuzzyVariables.Length];
            var prevmax = new double[FuzzyVariables.Length];

            var ranges = new double[4][];
            for (int i = 0; i < 4; i++) ranges[i] = new double[FuzzyVariables.Length];

            double? prevavalue = null, prevbvalue = null;
            for (double alpha = 1; alpha >= 0 - step / 2; alpha -= step)
            {
                alpha = Math.Min(1, Math.Max(0, alpha));

                for (int i = 0; i < FuzzyVariables.Length; i++)
                {
                    var interval = FuzzyVariables[i].GetInterval(alpha);
                    
                    ranges[0][i] = interval.Min;
                    if (alpha == 1) { ranges[1][i] = ranges[2][i] = interval.Min; }
                    else { ranges[1][i] = prevmin[i]; ranges[2][i] = prevmax[i]; }
                    ranges[3][i] = interval.Max;
                }

                double bvalue = double.MinValue; double avalue = double.MaxValue;
                foreach (var comb in GenCombinations(FuzzyVariables.Length, 4))                    
                {
                    for (int j = 0; j < vals.Length; j++) vals[j] = ranges[comb[j]][j];
                    var v1 = Function(vals);
                    if (v1 < avalue) { avalue = v1; Array.Copy(vals, prevmin, vals.Length); }
                    if (v1 > bvalue) { bvalue = v1; Array.Copy(vals, prevmax, vals.Length); }
                }

                if (prevavalue.HasValue && prevavalue < avalue) { avalue = prevavalue.Value; Array.Copy(ranges[1], prevmin, vals.Length); }
                if (prevbvalue.HasValue && prevbvalue > bvalue) { bvalue = prevbvalue.Value; Array.Copy(ranges[2], prevmax, vals.Length); }

                if (double.IsNaN(avalue) || double.IsInfinity(avalue) || double.IsNaN(bvalue) || double.IsInfinity(bvalue)) throw new Exception("Mathmatical error.");

                s1.Points.Insert(0, new DataPoint(avalue, alpha)); prevavalue = avalue;
                s1.Points.Add(new DataPoint(bvalue, alpha)); prevbvalue = bvalue;
            }
        }

        private static void GeneralTransformation(IMF[] FuzzyVariables, Func<double[], double> Function, Chart chart)
        {
            bool[][] ispositives = GenIsPositives(FuzzyVariables.Length).ToArray();
            var s1 = chart.Series.Add("General transformation"); s1.Color = Color.Blue;
            s1.ChartType = SeriesChartType.Line;

            var step = 0.015D;
            var vals = new double[FuzzyVariables.Length];

            var ranges = new List<double>[FuzzyVariables.Length];
            for (int i = 0; i < FuzzyVariables.Length; i++) ranges[i] = new List<double>();

            double? prevavalue = null, prevbvalue = null;
            for (double alpha = 1; alpha >= 0 - step / 2; alpha -= step)
            {
                alpha = Math.Min(1, Math.Max(0, alpha));

                int cnt = 0;
                foreach (var range in ranges)
                {
                    for (int i = 0; i < range.Count - 1; i++)
                        range[i] = (range[i] + range[i + 1]) / 2;
                    if (range.Count > 0) range.RemoveAt(range.Count - 1);

                    range.Insert(0, FuzzyVariables[cnt].GetInterval(alpha).Min);
                    range.Add(FuzzyVariables[cnt].GetInterval(alpha).Max);

                    cnt++;
                }

                double bvalue = double.MinValue; double avalue = double.MaxValue;
                foreach (var comb in GenCombinations(FuzzyVariables.Length, ranges[0].Count))
                {
                    for (int j = 0; j < vals.Length; j++) vals[j] = ranges[j][comb[j]];
                    var v1 = Function(vals);
                    if (v1 < avalue) avalue = v1;
                    if (v1 > bvalue) bvalue = v1;
                }

                if (prevavalue.HasValue && prevavalue < avalue) avalue = prevavalue.Value;
                if (prevbvalue.HasValue && prevbvalue > bvalue) bvalue = prevbvalue.Value;

                if (double.IsNaN(avalue) || double.IsInfinity(avalue) || double.IsNaN(bvalue) || double.IsInfinity(bvalue)) throw new Exception("Mathmatical error.");

                s1.Points.Insert(0, new DataPoint(avalue, alpha)); prevavalue = avalue;
                s1.Points.Add(new DataPoint(bvalue, alpha)); prevbvalue = bvalue;
            }
        }

        private static void ReducedTransformation(IMF[] FuzzyVariables, Func<double[], double> Function, Chart chart)
        {
            var s1 = chart.Series.Add("Reduced transformation"); s1.Color = Color.Red;
            s1.ChartType = SeriesChartType.Line;

            var step = 0.015D;
            var vals = new double[FuzzyVariables.Length];

            double? prevavalue = null, prevbvalue = null;
            for (double alpha = 1; alpha >= 0 - step / 2; alpha -= step)
            {
                alpha = Math.Min(1, Math.Max(0, alpha));
                var minrange = FuzzyVariables.Select(v => v.GetInterval(alpha).Min).ToArray();
                var maxrange = FuzzyVariables.Select(v => v.GetInterval(alpha).Max).ToArray();

                double bvalue = double.MinValue; double avalue = double.MaxValue;
                foreach (var ispositive in GenIsPositives(FuzzyVariables.Length))
                {
                    for (int j = 0; j < vals.Length; j++)
                        vals[j] = ispositive[j] ? minrange[j] : maxrange[j];
                    var v1 = Function(vals);
                    if (v1 < avalue) avalue = v1;

                    for (int j = 0; j < vals.Length; j++)
                        vals[j] = ispositive[j] ? maxrange[j] : minrange[j];
                    var v2 = Function(vals);
                    if (v2 > bvalue) bvalue = v2;
                }

                if (prevbvalue.HasValue && prevbvalue > bvalue) bvalue = prevbvalue.Value;
                
                if (prevavalue.HasValue && prevavalue < avalue) avalue = prevavalue.Value;

                if (double.IsNaN(avalue) || double.IsInfinity(avalue) || double.IsNaN(bvalue) || double.IsInfinity(bvalue)) throw new Exception("Mathmatical error.");

                s1.Points.Insert(0, new DataPoint(avalue, alpha)); prevavalue = avalue;
                s1.Points.Add(new DataPoint(bvalue, alpha)); prevbvalue = bvalue;
            }
        }

        private static void AddSimulation(IMF[] FuzzyVariables, Func<double[], double> Function, Chart chart)
        {
            var vals = new double[FuzzyVariables.Length];

            var s2 = chart.Series.Add("Simulated"); s2.Color = Color.YellowGreen;
            s2.ChartType = SeriesChartType.Point;

            var r = new Random();
            for (int i = 1; i < 5000; i++)
            {
                var alpha = 1D;
                for (int j = 0; j < vals.Length; j++)
                {
                    var x = r.NextDouble() * (FuzzyVariables[j].End - FuzzyVariables[j].Start) + FuzzyVariables[j].Start;
                    vals[j] = x;
                    var a = FuzzyVariables[j].GetValueAt(x);
                    if (alpha > a) alpha = a;
                }
                var res = Function(vals);
                if (double.IsNaN(res) || double.IsInfinity(res)) throw new Exception("Mathmatical error.");
                s2.Points.AddXY(res, alpha);
            }
        }

        static void chart_FormatNumber(object sender, FormatNumberEventArgs e)
        {
            e.LocalizedValue = Math.Round(e.Value, 6).ToString();
        }
    }
}
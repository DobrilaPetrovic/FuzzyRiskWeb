using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI.DataVisualization.Charting;

namespace FuzzyRiskNet.Fuzzy
{
    public static class FuzzyChartingHelpers
    {
        public static TFN EstimateTFN(this IMF MF)
        {
            var zeroalpha = MF.GetInterval(0);
            var onealpha = MF.GetInterval(1);
            return new TFN(zeroalpha.Min, (onealpha.Min + onealpha.Max) / 2, zeroalpha.Max);
        }

        public static byte[] Draw(this IMF MF, string XTitle, int Width = 1000, int Height = 300)
        {
            var chart = new Chart();
            chart.Width = Width;
            chart.Height = Height;
            chart.ChartAreas.Add("Main");

            chart.ChartAreas[0].AxisY.Maximum = 1;
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisX.Title = XTitle;
            chart.ChartAreas[0].AxisY.Title = "Membership Degree";

            var s = chart.Series.Add("MF");
            s.ChartType = SeriesChartType.Line;

            var step = 0.015D;            
            for (double alpha = 1; alpha >= 0 - step / 2; alpha -= step)
            {
                alpha = Math.Min(1, Math.Max(0, alpha));

                var range = MF.GetInterval(alpha);

                s.Points.Insert(0, new DataPoint(range.Min, alpha)); 
                s.Points.Add(new DataPoint(range.Max, alpha));

                if (alpha == 0)
                {
                    chart.ChartAreas[0].AxisX.Minimum = range.Min;
                    chart.ChartAreas[0].AxisX.Maximum = range.Max;
                }
            }

            return chart.ToPngByteArray();
        }

        public static byte[] Draw(this Tuple<double, double, double>[] MF, string XTitle, int Width = 1000, int Height = 300, double? FixAxisXMin = null, double? FixAxisXMax = null)
        {
            var chart = new Chart();
            chart.Width = Width;
            chart.Height = Height;
            chart.ChartAreas.Add("Main");

            chart.ChartAreas[0].AxisY.Maximum = 1;
            chart.ChartAreas[0].AxisY.Minimum = 0;
            if (FixAxisXMax.HasValue) chart.ChartAreas[0].AxisX.Maximum = FixAxisXMax.Value;
            if (FixAxisXMin.HasValue) chart.ChartAreas[0].AxisX.Minimum = FixAxisXMin.Value; 
            chart.ChartAreas[0].AxisX.Title = XTitle;
            chart.ChartAreas[0].AxisY.Title = "Membership Degree";

            var s = chart.Series.Add("MF");
            s.ChartType = SeriesChartType.Line;

            foreach (var val in MF.OrderByDescending(v => v.Item1))
            {
                var alpha = val.Item1;

                s.Points.Insert(0, new DataPoint(val.Item2, alpha));
                s.Points.Add(new DataPoint(val.Item3, alpha));

                if (alpha == 0)
                {
                    if (!FixAxisXMin.HasValue) chart.ChartAreas[0].AxisX.Minimum = val.Item2;
                    if (!FixAxisXMax.HasValue) chart.ChartAreas[0].AxisX.Maximum = val.Item3;
                }
            }

           
            return chart.ToPngByteArray();
        }

        public static byte[] ToPngByteArray(this Chart Chart)
        {
            using (var ms = new MemoryStream())
            {
                Chart.SaveImage(ms, ChartImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        public static byte[] ToPngByteArray(this Bitmap BMP)
        {
            using (var ms = new MemoryStream())
            {
                BMP.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        public static byte[] DrawBoard(string[] Rows, string[] Columns, int TimeHorizon, Func<int, int, IEnumerable<Tuple<string, double, double[][]>>> GetSeries, int Width = 1000, int Height = 2000)
        {
            var chart = new Chart();
            chart.Width = Width;
            chart.Height = Height;

            bool First = true;

            var elwidth = 98F / Columns.Length;
            var elheight = 93F / Rows.Length;

            var font = new System.Drawing.Font("Arial", 12);
            chart.Legends.Add(new Legend() { Position = new ElementPosition(0, 93, 100, 7), Font = font });
            //chart.CustomizeLegend += chart_CustomizeLegend;

            for (var c = 0; c < Columns.Length; c++)
            {
                chart.Titles.Add(new Title() { Name = "MainTitle" + c, Text = Columns[c], Position = new ElementPosition(c * elwidth + 7, 0, elwidth - 5, 2), TextOrientation = TextOrientation.Horizontal, Font = font, ForeColor = Color.Black });

                for (var r = 0; r < Rows.Length; r++)
                {
                    var scope = r + "-" + c;

                    var inoparea = chart.ChartAreas.Add("Inoperability-" + scope);

                    if (c > 0)
                    {
                        inoparea.AlignWithChartArea = "Inoperability-" + r + "-0";
                        //inoparea.AlignmentStyle = AreaAlignmentStyles.Position;
                        inoparea.AlignmentOrientation = AreaAlignmentOrientations.Horizontal;
                    }

                    inoparea.AxisY.Maximum = 1;
                    inoparea.AxisY.Minimum = 0;
                    inoparea.AxisX.Minimum = 0;
                    inoparea.AxisX.Maximum = TimeHorizon - 1;
                    inoparea.AxisX.Interval = 10;
                    inoparea.AxisY.Title = "Inoperability";
                    //inoparea.AxisX.Title = "Time";
                    inoparea.AxisX.TitleFont = new System.Drawing.Font("Arial", 11);
                    chart.Titles.Add(new Title() { Name = "XTitle" + r + "-" + c, Text = "Time", Position = new ElementPosition(c * elwidth + 7, (r + 1) * elheight, elwidth - 5, 2), TextOrientation = TextOrientation.Horizontal, Font = font, ForeColor = Color.Black });                    

                    inoparea.Position = new ElementPosition() { X = c * elwidth + 2, Width = elwidth, Y = r * elheight + 2, Height = elheight };
                    if (c == 0)
                    {
                        var title = Rows[r]; // Rows[r].StartsWith("Ferment") ? "Ferment." : Rows[r];
                        //inoparea.AxisY.Title = title;
                        var ttl = new Title() { Name = "Title" + r, Text = title, Position = new ElementPosition(0, r * elheight, 2, 2 + elheight), TextOrientation = TextOrientation.Rotated270, Font = font, ForeColor = Color.Black };
                        chart.Titles.Add(ttl);
                    }
                    inoparea.AxisY.TitleFont = new System.Drawing.Font("Arial", 11);

                    int cnt = 0;
                    var dashes = new ChartDashStyle[] { ChartDashStyle.Solid, ChartDashStyle.Dash, ChartDashStyle.DashDot, ChartDashStyle.Dot };
                    var markerstyles = new MarkerStyle[] { MarkerStyle.Square, MarkerStyle.Circle, MarkerStyle.Diamond, MarkerStyle.Cross, MarkerStyle.Triangle, MarkerStyle.Cross, MarkerStyle.Diamond, MarkerStyle.Circle, MarkerStyle.Square };

                    foreach (var t in GetSeries(r, c).OrderBy(t2 => t2.Item1.Contains("Min") ? -1 + t2.Item2 : 1 - t2.Item2))
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
                            var ind = s.Points.AddXY(i, t.Item3[i][r]);
                            s.Points[ind].MarkerSize = i % 10 != 5 ? 0 : (BorderWidth > 1 ? 8 : 4);
                        }

                        cnt++;
                    }
                    First = false;
                }
            }
            return chart.ToPngByteArray();
        }
        static void chart_CustomizeLegend(object sender, CustomizeLegendEventArgs e)
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
    }
}
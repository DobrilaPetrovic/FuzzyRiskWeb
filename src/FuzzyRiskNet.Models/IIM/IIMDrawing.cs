using FuzzyRiskNet.Models;
using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace FuzzyRiskNet.FuzzyRisk
{
    /// <summary>
    /// This class provides methods to draw the network diagram.
    /// </summary>
    public static class IIMDrawing
    {
        static Color[] FullInoperabilityColors = new Color[] 
        {
            Color.Green, Color.LimeGreen, Color.Lime, 
            Color.GreenYellow, Color.Yellow, Color.Orange,
            Color.DarkOrange, Color.OrangeRed, Color.Red, Color.DarkRed, Color.DarkViolet
        };

        static Color[] InoperabilityColors = new Color[] 
        {
            Color.FromArgb(146, 208, 80),
            Color.FromArgb(255, 255, 100),
            Color.FromArgb(255, 204, 0),
            Color.FromArgb(255, 31, 31),
            Color.DarkViolet
        };

        static Color GetInopColor(double InOp)
        {
            if (InOp < 0.005) return InoperabilityColors[0];
            if (InOp < 0.35) return InoperabilityColors[1];
            if (InOp < 0.7) return InoperabilityColors[2];
            if (InOp <= 1) return InoperabilityColors[3];
            return InoperabilityColors[4];
        }

        public static int NodeHeight = 55;
        public static int NodeVertDist = 100;
        public static int NodeWidth = 220;
        public static int NodeHorizDist = 300;

        public static byte[] Draw(List<Node> Nodes, Func<Node, double> CalcPerturb, Func<Node, double?> CalcInop, Func<Dependency, double?> CalcWeight
            , out Dictionary<Node, Rectangle> rectdic, int? GPNConfigID)
        {
            var image = new Bitmap((Nodes.Max(n => n.LocationX)) * NodeHorizDist + NodeWidth + 60, 
                Nodes.Max(n => n.LocationY + 1) * NodeVertDist + NodeHeight + 60);
            var font = new Font(FontFamily.GenericSansSerif, 11);
            var valuefont = new Font(FontFamily.GenericSansSerif, 10);
            using (var graph = Graphics.FromImage(image))
            {
                graph.FillRectangle(Brushes.White, graph.VisibleClipBounds);

                //for (int i = 0; i < 11; i++) graph.FillRectangle(new SolidBrush(InoperabilityColors[i]), 30 + 20 * i, 0, 20, 20);

                rectdic = new Dictionary<Node, Rectangle>();
                var fillbrush = new SolidBrush(Color.FromArgb(157, 195, 230));
                foreach (var n in Nodes)
                {
                    //var size = graph.MeasureString(n.Name, font);
                    //var rect = Rectangle.Round(new RectangleF(n.Location, size));
                    var location = new Point(50 + (n.LocationX) * NodeHorizDist, 50 + (n.LocationY) * NodeVertDist);
                    var rect = Rectangle.Round(new RectangleF(location, new Size(NodeWidth, NodeHeight)));
                    rectdic.Add(n, rect);
                    var inop = CalcInop != null ? CalcInop(n) : null;
                    if (inop.HasValue)
                    {
                        //int inop = Math.Max(-1, Math.Min((int)(n.Inoperability.Value * 10), 10));
                        graph.FillRectangle(new SolidBrush(inop.Value < 0 ? Color.White : GetInopColor(inop.Value)), rect);
                    }
                    else
                        graph.FillRectangle(fillbrush, rect);
                    graph.DrawRectangle(Pens.Black, rect);
                    graph.DrawString(n.Name, font, Brushes.Black, rect, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                    if (CalcPerturb != null)
                        graph.DrawString(Math.Round(CalcPerturb(n), 2).ToString(), valuefont, Brushes.Black, rect, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near });
                    if (n.Region != null)
                        graph.DrawString(n.Region.Name, valuefont, Brushes.Black, rect, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
                    if (inop.HasValue)
                        graph.DrawString(Math.Round(inop.Value, 2).ToString(), valuefont, Brushes.Black, rect, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                }

                foreach (var n in Nodes)
                {
                    var centrepoint = new Point((rectdic[n].Left + rectdic[n].Right) / 2, (rectdic[n].Top + rectdic[n].Bottom) / 2);
                    foreach (var d in n.Dependencies.Where(d2 => d2.GPNConfigurationID == GPNConfigID))
                    {
                        var dnode = d.To;
                        var frompoint = FindClosestPoint(rectdic[dnode], centrepoint);
                        var topoint = FindClosestPoint(rectdic[n], frompoint);
                        DrawArrow(graph, frompoint, topoint, Color.Blue, 3, 1);

                        var w = CalcWeight(d);
                        if (w.HasValue)
                        {
                            graph.TranslateTransform(topoint.X, topoint.Y);

                            if (frompoint.X != topoint.X)
                            {
                                var degree = (((float)Math.Atan2(frompoint.Y - topoint.Y, frompoint.X - topoint.X) * 180 / (float)Math.PI));
                                graph.RotateTransform(Math.Abs(degree) > 90 ? 180 + degree : degree);
                                //graph.RotateTransform(degree < 0 ? degree < -90 ? 180 + degree : degree : degree <= 90 ? degree : 180 + degree);
                            }

                            graph.DrawString(Math.Round(w.Value, 2).ToString(), font, Brushes.Black, 0, 0,
                                new StringFormat()
                                {
                                    Alignment = topoint.X > frompoint.X ? StringAlignment.Far : StringAlignment.Near,
                                    LineAlignment = topoint.Y >= frompoint.Y ? StringAlignment.Far : StringAlignment.Near
                                });
                            graph.ResetTransform();
                        }
                    }
                }
            }
            return image.ToPngByteArray();
        }

        private static Point FindClosestPoint(this Rectangle Rect, Point RefPoint)
        {
            return FindClosestPoint(ListRectConnections(Rect), RefPoint);
        }

        private static Point FindClosestPoint(this IEnumerable<Point> Points, Point RefPoint)
        {
            var closest = Points.First();
            Func<Point, double> distance = (p) => Math.Pow(p.X - RefPoint.X, 2) + Math.Pow(p.Y - RefPoint.Y, 2);
            foreach (var p in Points)
            {               
                if (distance(p) < distance(closest)) closest = p;
            }
            return closest;
        }

        private static IEnumerable<Point> ListRectConnections(Rectangle Rect)
        {
            yield return new Point(Rect.Left, Rect.Top);
            yield return new Point(Rect.Right, Rect.Top);
            yield return new Point(Rect.Left, Rect.Bottom);
            yield return new Point(Rect.Right, Rect.Bottom);
            yield return new Point((Rect.Left + Rect.Right) / 2, Rect.Top);
            yield return new Point((Rect.Left + Rect.Right) / 2, Rect.Bottom);
            yield return new Point(Rect.Left, (Rect.Top + Rect.Bottom) / 2);
            yield return new Point(Rect.Right, (Rect.Top + Rect.Bottom) / 2);
        }

        // Borrowed from CodeProject
        private static void DrawArrow(Graphics g, PointF ArrowStart, PointF ArrowEnd, Color ArrowColor, int LineWidth, int ArrowMultiplier)
        {
            //create the pen
            Pen p = new Pen(ArrowColor, LineWidth);

            //draw the line
            g.DrawLine(p, ArrowStart, ArrowEnd);

            //determine the coords for the arrow point

            //tip of the arrow
            PointF arrowPoint = ArrowEnd;

            //determine arrow length
            double arrowLength = Math.Sqrt(Math.Pow(Math.Abs(ArrowStart.X - ArrowEnd.X), 2) +
                                           Math.Pow(Math.Abs(ArrowStart.Y - ArrowEnd.Y), 2));

            //determine arrow angle
            double arrowAngle = Math.Atan2(Math.Abs(ArrowStart.Y - ArrowEnd.Y), Math.Abs(ArrowStart.X - ArrowEnd.X));

            //get the x,y of the back of the point

            //to change from an arrow to a diamond, change the 3
            //in the next if/else blocks to 6

            double pointX, pointY;
            if (ArrowStart.X > ArrowEnd.X)
            {
                pointX = ArrowStart.X - (Math.Cos(arrowAngle) * (arrowLength - (3 * ArrowMultiplier)));
            }
            else
            {
                pointX = Math.Cos(arrowAngle) * (arrowLength - (3 * ArrowMultiplier)) + ArrowStart.X;
            }

            if (ArrowStart.Y > ArrowEnd.Y)
            {
                pointY = ArrowStart.Y - (Math.Sin(arrowAngle) * (arrowLength - (3 * ArrowMultiplier)));
            }
            else
            {
                pointY = (Math.Sin(arrowAngle) * (arrowLength - (3 * ArrowMultiplier))) + ArrowStart.Y;
            }

            PointF arrowPointBack = new PointF((float)pointX, (float)pointY);

            //get the secondary angle of the left tip
            double angleB = Math.Atan2((3 * ArrowMultiplier), (arrowLength - (3 * ArrowMultiplier)));

            double angleC = Math.PI * (90 - (arrowAngle * (180 / Math.PI)) - (angleB * (180 / Math.PI))) / 180;

            //get the secondary length
            double secondaryLength = (3 * ArrowMultiplier) / Math.Sin(angleB);

            if (ArrowStart.X > ArrowEnd.X)
            {
                pointX = ArrowStart.X - (Math.Sin(angleC) * secondaryLength);
            }
            else
            {
                pointX = (Math.Sin(angleC) * secondaryLength) + ArrowStart.X;
            }

            if (ArrowStart.Y > ArrowEnd.Y)
            {
                pointY = ArrowStart.Y - (Math.Cos(angleC) * secondaryLength);
            }
            else
            {
                pointY = (Math.Cos(angleC) * secondaryLength) + ArrowStart.Y;
            }

            //get the left point
            PointF arrowPointLeft = new PointF((float)pointX, (float)pointY);

            //move to the right point
            angleC = arrowAngle - angleB;

            if (ArrowStart.X > ArrowEnd.X)
            {
                pointX = ArrowStart.X - (Math.Cos(angleC) * secondaryLength);
            }
            else
            {
                pointX = (Math.Cos(angleC) * secondaryLength) + ArrowStart.X;
            }

            if (ArrowStart.Y > ArrowEnd.Y)
            {
                pointY = ArrowStart.Y - (Math.Sin(angleC) * secondaryLength);
            }
            else
            {
                pointY = (Math.Sin(angleC) * secondaryLength) + ArrowStart.Y;
            }

            PointF arrowPointRight = new PointF((float)pointX, (float)pointY);

            //create the point list
            PointF[] arrowPoints = new PointF[4];
            arrowPoints[0] = arrowPoint;
            arrowPoints[1] = arrowPointLeft;
            arrowPoints[2] = arrowPointBack;
            arrowPoints[3] = arrowPointRight;

            //draw the outline
            g.DrawPolygon(p, arrowPoints);

            //fill the polygon
            g.FillPolygon(new SolidBrush(ArrowColor), arrowPoints);
        }
    }
}

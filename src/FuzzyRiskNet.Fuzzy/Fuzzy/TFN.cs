using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace FuzzyRiskNet.Fuzzy
{
    /// <summary>
    /// This class represents a Triangular Fuzzy Number (TFN). It has been carefully adapted to work with EF Code First.
    /// </summary>
    [ComplexType]
    [Serializable]
    public class TFN
    {
        public TFN() { }
        public TFN(double A, double B, double C) { this.A = A; this.B = B; this.C = C; }

        /// <summary>
        /// The lowest possible value (minimum)
        /// </summary>
        public double A { get; set; }
        /// <summary>
        /// The most likely value (peak)
        /// </summary>
        public double B { get; set; }
        /// <summary>
        /// The highest possible value (maximum)
        /// </summary>
        public double C { get; set; }

        public bool IsZero { get { return A == 0 && B == 0 && C == 0; } }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}]", A, B, C);
        }

        public string ToString(string NumberFormat)
        {
            return string.Format("[{0}, {1}, {2}]", A.ToString(NumberFormat), B.ToString(NumberFormat), C.ToString(NumberFormat));
        }

        public string ToString(Func<double, string> Format)
        {
            return string.Format("[{0}, {1}, {2}]", Format(A), Format(B), Format(C));
        }

        public string ToString(Func<double, int, string> Format)
        {
            return string.Format("[{0}, {1}, {2}]", Format(A, 0), Format(B, 1), Format(C, 2));
        }

        public static TFN operator *(TFN v1, double v2)
        {
            return v2 > 0 ? new TFN(v1.A * v2, v1.B * v2, v1.C * v2) : new TFN(v1.C * v2, v1.B * v2, v1.A * v2);
        }

        public static TFN operator +(TFN v1, TFN v2)
        {
            return new TFN(v1.A + v2.A, v1.B + v2.B, v1.C + v2.C);
        }

        public static TFN operator +(TFN v1, double v2)
        {
            return new TFN(v1.A + v2, v1.B + v2, v1.C + v2);
        }

        public static TFN Normalize(TFN v, double Min = 0, double Max = 1)
        {
            return new TFN(Math.Max(Min, Math.Min(Max, v.A)), Math.Max(Min, Math.Min(Max, v.B)), Math.Max(Min, Math.Min(Max, v.C)));
        }

        public static bool TryParse(string StrValue, out TFN Value)
        {
            var reg = new Regex(tfnregex);

            var m = reg.Match(StrValue);
            if (m.Success)
            {
                Value = new TFN() { A = double.Parse(m.Groups["v1"].Value), B = double.Parse(m.Groups["v2"].Value), C = double.Parse(m.Groups["v3"].Value) };
                if (Value.A <= Value.B && Value.B <= Value.C) return true;
            }
            double d;
            if (double.TryParse(StrValue, out d))
            {
                Value = new TFN(d, d, d);
                return true;
            }
            Value = null;
            return false;
        }

        static string numregex = @"[-+]?([0-9]*\.[0-9]+|[0-9]+)";
        static string tfnregex = @"\s*\[\s*(?<v1>" + numregex + @")\s*,\s*(?<v2>" + numregex + @")\s*,\s*(?<v3>" + numregex + @")\s*\]\s*\Z";
    }
}

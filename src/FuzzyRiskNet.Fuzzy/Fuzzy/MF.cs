using System;
using System.Collections.Generic;
using System.Linq;

// These classes are for general type fuzzy membership functions.

namespace FuzzyRiskNet.Fuzzy
{
    /// <summary>
    /// Represents a single line in a membership function.
    /// </summary>
    public struct MembershipLine
    {
        public MembershipXYPair From;
        public MembershipXYPair To;

        public static MembershipLine CreateAlphaCutLine(double Start, double End, double Alpha)
        {
            return new MembershipLine()
            {
                From = new MembershipXYPair() { X = Start, MembershipDegree = Alpha },
                To = new MembershipXYPair() { X = End, MembershipDegree = Alpha },
                _Offset = Alpha,
                _Steep = 0
            };
        }

        private double? _Steep, _Offset;
        public double Steep
        {
            get { if (!_Steep.HasValue) _Steep = To.X == From.X ? 0 : (To.MembershipDegree - From.MembershipDegree) / (To.X - From.X); return _Steep.Value; }
        }
        public double Offset
        {
            get { if (!_Offset.HasValue) _Offset = (To.MembershipDegree - From.MembershipDegree) * (0 - From.X) / (To.X - From.X) + From.MembershipDegree; return _Offset.Value; }
        }

        public double ValueAtLine(double X)
        {
            //return (To.MembershipDegree - From.MembershipDegree) * (X - From.X) / (To.X - From.X) + From.MembershipDegree;
            return X * Steep + Offset;
        }

        public MembershipLine CutAlphaNoCheck(double Alpha)
        {
            var steep = Steep;
            var end = FindAlphaCut(Alpha, Offset, Steep);
            return steep > 0 ? new MembershipLine() { From = From, To = end, _Offset = Offset, _Steep = Steep } :
                new MembershipLine() { From = end, To = To, _Offset = Offset, _Steep = Steep };
        }

        public static MembershipXYPair FindAlphaCut(double Alpha, double Offset, double Steep)
        {
            var x = (Alpha - Offset) / Steep;
            return new MembershipXYPair() { X = x, MembershipDegree = Alpha };
        }

        public static MembershipXYPair? FindCrossPoint(MembershipLine Line1, MembershipLine Line2)
        {
            if ((Line1.From.X >= Line2.To.X) || (Line2.From.X >= Line1.To.X)) return null;

            double offset1 = Line1.Offset, offset2 = Line2.Offset;
            double steep1 = Line1.Steep, steep2 = Line2.Steep;

            if (steep1 == steep2)
                if (offset1 == offset2) return Line1.From.X < Line2.From.X ? Line1.From : Line2.From; else return null;

            var x = (offset2 - offset1) / (steep1 - steep2);

            if (x >= Line1.From.X && x <= Line1.To.X && x >= Line2.From.X && x <= Line2.To.X)
                return new MembershipXYPair() { X = x, MembershipDegree = Line1.ValueAtLine(x) };
            return null;
        }

        public MembershipLine CutLineStart(MembershipLine SubjectTo)
        {
            var crosspoint = FindCrossPoint(this, SubjectTo);
            if (crosspoint.HasValue) return new MembershipLine() { From = this.From, To = crosspoint.Value, _Offset = _Offset, _Steep = _Steep };
            else return this;
        }
        public MembershipLine CutLineEnd(MembershipLine SubjectTo)
        {
            var crosspoint = FindCrossPoint(this, SubjectTo);
            if (crosspoint.HasValue) return new MembershipLine() { From = crosspoint.Value, To = this.To, _Offset = _Offset, _Steep = _Steep };
            else return this;
        }
        public double CalcSpace()
        {
            if (To.X == From.X) return 0;
            return 0.5 * (From.MembershipDegree + To.MembershipDegree) * Math.Abs(To.X - From.X);
        }
        public double CalcIntegralX()
        {
            if (To.X == From.X) return 0;
            var steep = this.Steep;
            var offset = this.Offset;
            var ToX2 = Pow2(To.X); var FromX2 = Pow2(From.X);
            return (steep * ToX2 * To.X / 3 + offset * ToX2 / 2) - (steep * FromX2 * From.X / 3 + offset * FromX2 / 2);
        }
        private static double Pow2(double Val)
        {
            return Val * Val;
        }
        public override string ToString()
        {
            return String.Format("{0} -> {1}", From.ToString(), To.ToString());
        }
    }
    /// <summary>
    /// Represnets a single point in a fuzzy membership function (a pair of value and associated membership degree).
    /// </summary>
    public struct MembershipXYPair
    {
        public double X;
        public double MembershipDegree;
        public override string ToString()
        {
            return String.Format("({0},{1})", X, MembershipDegree);
        }
    }

    /// <summary>
    /// The general interface for all membership functions.
    /// </summary>
    public interface IMF
    {
        double Start { get; }
        double End { get; }
        double GetValueAt(double Point);
        string Name { get; }
        void ApplyAlphaCut(double Alpha, ICollection<MembershipLine> List);
        FuzzyInterval GetInterval(double Alpha);
        IEnumerable<double> GetInflectionPoints();
    }
    /// <summary>
    /// Different types of membership functions (used for fuzzy control)
    /// </summary>
    public enum MFType { FirstMF, MidMF, LastMF }

    /// <summary>
    /// A trinagular membership function, implemented as a general MF.
    /// </summary>
    public class TriMF : IMF
    {
        public TriMF(TFN TFN) : this(TFN.A, TFN.B, TFN.C) { }
        public TriMF(double Start, double MaxHeight, double End, string Name = null)
        {
            this.Start = Start; this.MaxHeight = MaxHeight; this.End = End; this.Name = Name;
            StartLine = new MembershipLine() { From = new MembershipXYPair() { X = Start, MembershipDegree = 0 }, To = new MembershipXYPair() { X = MaxHeight, MembershipDegree = 1 } };
            EndLine = new MembershipLine() { To = new MembershipXYPair() { X = End, MembershipDegree = 0 }, From = new MembershipXYPair() { X = MaxHeight, MembershipDegree = 1 } };
            MaxHeightMinusStart = MaxHeight - Start;
            EndMinusMaxHeight = End - MaxHeight;
        }
        public double Start { get; private set; }
        public double MaxHeight { get; private set; }
        public double End { get; private set; }
        public double MaxHeightMinusStart, EndMinusMaxHeight;
        MembershipLine StartLine, EndLine;
        public double GetValueAt(double Point)
        {
            if (Point < Start || Point > End) return 0;
            if (Point <= MaxHeight) return (Point - Start) / MaxHeightMinusStart;
            return (End - Point) / EndMinusMaxHeight;
        }
        public string Name { get; private set; }
        public void ApplyAlphaCut(double Alpha, ICollection<MembershipLine> List)
        {
            var startalphaline = StartLine.CutAlphaNoCheck(Alpha);
            var endalphaline = EndLine.CutAlphaNoCheck(Alpha);

            List.Add(startalphaline);
            if (Alpha < 1) List.Add(MembershipLine.CreateAlphaCutLine(startalphaline.To.X, endalphaline.From.X, Alpha));
            List.Add(endalphaline);
        }
        public IEnumerable<double> GetInflectionPoints()
        {
            yield return Start;
            yield return MaxHeight;
            yield return End;
        }


        public FuzzyInterval GetInterval(double Alpha)
        {
            return new FuzzyInterval(Alpha, Start == MaxHeight ? MaxHeight : StartLine.CutAlphaNoCheck(Alpha).To.X, End == MaxHeight ? MaxHeight : EndLine.CutAlphaNoCheck(Alpha).From.X);
        }

        public override string ToString()
        {
            return string.Format("Tri. MF [{0}, {1}, {2}]", Start, MaxHeight, End);
        }

        public static IMF Zero { get { return new TriMF(0, 0, 0, ""); } }

        public TFN ConvertTFN()
        {
            return new TFN() { A = Start, B = MaxHeight, C = End };
        }
    }

    /// <summary>
    /// A trapezoidal membership function.
    /// </summary>
    public class TrapMF : IMF
    {
        public MFType Type { get; private set; }
        public TrapMF(double Start, double MaxHeightStart, double MaxHeightEnd, double End, string Name, MFType Type)
        {
            this.Start = Start; this.MaxHeightStart = MaxHeightStart; this.MaxHeightEnd = MaxHeightEnd; this.End = End;
            this.Name = Name;
            StartLine = new MembershipLine() { From = new MembershipXYPair() { X = Start, MembershipDegree = 0 }, To = new MembershipXYPair() { X = MaxHeightStart, MembershipDegree = 1 } };
            EndLine = new MembershipLine() { To = new MembershipXYPair() { X = End, MembershipDegree = 0 }, From = new MembershipXYPair() { X = MaxHeightEnd, MembershipDegree = 1 } };
            this.Type = Type;
        }
        MembershipLine StartLine, EndLine;
        public double Start { get; private set; }
        public double MaxHeightStart { get; private set; }
        public double MaxHeightEnd { get; private set; }
        public double End { get; private set; }
        public double GetValueAt(double Point)
        {
            if (Point < MaxHeightStart && Type == MFType.FirstMF) return 1;
            if (Point > MaxHeightEnd && Type == MFType.LastMF) return 1;
            if (Point < Start || Point > End) return 0;
            if (Point >= MaxHeightStart && Point <= MaxHeightEnd) return 1;
            if (Point <= MaxHeightStart) return (Point - Start) / (MaxHeightStart - Start);
            return (End - Point) / (End - MaxHeightEnd);
        }
        public string Name { get; private set; }
        public void ApplyAlphaCut(double Alpha, ICollection<MembershipLine> List)
        {
            var startalphaline =StartLine.CutAlphaNoCheck(Alpha);
            var endalphaline = EndLine.CutAlphaNoCheck(Alpha);

            List.Add(startalphaline);
            if (startalphaline.To.X < endalphaline.From.X) 
                List.Add(MembershipLine.CreateAlphaCutLine(startalphaline.To.X, endalphaline.From.X, Alpha));
            List.Add(endalphaline);
        }

        public IEnumerable<double> GetInflectionPoints()
        {
            yield return Start;
            yield return MaxHeightStart;
            yield return MaxHeightEnd;
            yield return End;
        }

        public FuzzyInterval GetInterval(double Alpha)
        {
            return new FuzzyInterval(Alpha, Start == MaxHeightStart ? MaxHeightStart : StartLine.CutAlphaNoCheck(Alpha).To.X, End == MaxHeightEnd ? MaxHeightEnd : EndLine.CutAlphaNoCheck(Alpha).From.X);
        }


        public override string ToString()
        {
            return string.Format("Trap. MF [{0}, {1}, {2}, {3}]", Start, MaxHeightStart, MaxHeightEnd, End);
        }
    }

    /// <summary>
    /// A fuzzy alpha-cut.
    /// </summary>
    public struct FuzzyInterval
    {
        public FuzzyInterval(double Alpha, double Min, double Max) : this() { this.Alpha = Alpha; this.Min = Min; this.Max = Max; if (Min - Max > 0.000001) throw new ArgumentOutOfRangeException("Min"); }
        public double Alpha { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }

    /// <summary>
    /// Represents a general membership function defined by a set of intervals.
    /// </summary>
    public class CustomIntervalMF : IMF
    {

        public CustomIntervalMF(IEnumerable<FuzzyInterval> Intervals) 
        { 
            this.Intervals = Intervals.OrderBy(i => i.Alpha).ToArray();
            if (this.Intervals[0].Alpha != 0 || this.Intervals.Last().Alpha != 1)
                throw new Exception("Intervals at alpha = 0 and alpha = 1 should be provided.");
            Start = this.Intervals[0].Min;
            End = this.Intervals[0].Max;
        }

        public FuzzyInterval[] Intervals { get; private set; }

        public double Start { get; private set; }

        public double End { get; private set; }

        public double GetValueAt(double Point)
        {
            for (int i = Intervals.Length - 1; i >= 0; i--)
                if (Point <= Intervals[i].Max && Point >= Intervals[i].Min) return Intervals[i].Alpha;
            return 0D;
        }

        public string Name { get; set; }

        public void ApplyAlphaCut(double Alpha, ICollection<MembershipLine> List)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<double> GetInflectionPoints()
        {
            for (int i = 0; i < Intervals.Length; i++) yield return Intervals[i].Min;
            for (int i = Intervals.Length - 1; i >= 0; i--) yield return Intervals[i].Max;
        }


        public FuzzyInterval GetInterval(double Alpha)
        {
            for (int i = 0; i < Intervals.Length; i++)
                if (Intervals[i].Alpha >= Alpha)
                {
                    if (Intervals[i].Alpha == Alpha || Intervals.Length == i + 1)
                        return Intervals[i];
                    else
                    {
                        var ratio = (Alpha - Intervals[i].Alpha) / (Intervals[i + 1].Alpha - Intervals[i].Alpha);
                        return new FuzzyInterval(Alpha, (1 - ratio) * Intervals[i].Min + ratio * Intervals[i + 1].Min, (1 - ratio) * Intervals[i].Max + ratio * Intervals[i + 1].Max);
                    }
                }
            throw new Exception("Alpha is out of range.");
        }
    }
}

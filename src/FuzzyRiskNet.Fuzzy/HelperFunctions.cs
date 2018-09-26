using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.Helpers
{
    public static class HelperFunctions
    {
        public static string ArrayToString<T>(this T[] Array)
        {
            return "[" + string.Join(", ", Array.Select(i => i.ToString())) + "]";
        }

        public static double Scale(double Value, double Mean, double Range)
        {
            var ret = (Value - Mean + (Range / 2)) / Range;
            if (ret > 1.5) return 1.5;
            if (ret < -0.5) return -0.5;
            return ret;
        }

        public static IEnumerable<int> CountTo(this int To)
        {
            return Enumerable.Range(0, To);
        }


        public static double[] StepArray(int size, double coeff, double disposition)
        {
            var dbl = new double[size];
            for (int i = 0; i < dbl.Length; i++) dbl[i] = disposition + i * coeff;
            return dbl;
        }

        public static double[] StepArray(int size, double coeff, double disposition, double min)
        {
            var dbl = new double[size];
            for (int i = 0; i < dbl.Length; i++) dbl[i] = Math.Max(min, disposition + i * coeff);
            return dbl;
        }

        public static double SumOverArray(double[] arr, double[] mult, int from, int to)
        {
            double sum = 0;
            for (int i = from; i < to; i++) sum += arr[i] * mult[i];
            return sum;
        }

        public static double SumOverArray(double[] arr, int from, int to)
        {
            double sum = 0;
            for (int i = from; i < to; i++) sum += arr[i];
            return sum;
        }

        public static int[] DistributeByProbabilities(Random r, double[] Probabilities, int Count)
        {
            double[] Return = new double[Probabilities.Length];
            for (int i = 0; i < Probabilities.Length; i++) Return[i] = 0;
            DistributeByProbWithoutClear(r, Probabilities, Count, Return);
            return Return.Select(v => (int)v).ToArray();
        }

        public static void DistributeByProbabilities(Random r, double[] Probabilities, int Count, ref double[] Return)
        {
            for (int i = 0; i < Probabilities.Length; i++) Return[i] = 0;
            DistributeByProbWithoutClear(r, Probabilities, Count, Return);
        }

        public static void DistributeByProbWithoutClear(Random r, double[] Probabilities, int Count, double[] Return)
        {
            for (int i = 0; i < Count; i++)
            {
                var rnd = r.NextDouble();
                double sum = 0;
                for (int j = 0; j < Probabilities.Length; j++)
                {
                    sum += Probabilities[j];
                    if (sum >= rnd) { Return[j]++; break; }
                }
            }
        }
        public static IEnumerable<int> ListRandomResults(Random r, double[] Probabilities, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                var rnd = r.NextDouble();
                double sum = 0;
                for (int j = 0; j < Probabilities.Length; j++)
                {
                    sum += Probabilities[j];
                    if (sum >= rnd) { yield return j; break; }
                }
            }
        }

        public static double CalcSpecificity(this IMF FuzzyNumber, double Space = 0.1D)
        {
            if (Math.Round(1D / Space, 5) != Math.Round(1D / Space, 0))
                throw new Exception("The number steps from zero to one should be an integer.");

            var cnt = (int)Math.Round(1D / Space, 0);
            var sum = 0D;
            for (int i = 0; i < cnt; i++)
            {
                var xt = Space * i;
                var xt1 = Space * (i + 1);
                sum += (FuzzyNumber.GetInterval(xt).Max - FuzzyNumber.GetInterval(xt).Min);
                sum += (FuzzyNumber.GetInterval(xt1).Max - FuzzyNumber.GetInterval(xt1).Min);
            }

            var range = FuzzyNumber.GetInterval(0).Max - FuzzyNumber.GetInterval(0).Min;

            return 1D - (sum / (2 * cnt)) / range;
        }

        public static double CalcAmbiguity(this IMF FuzzyNumber, double Space = 0.1D)
        {
            if (Math.Round(1D / Space, 5) != Math.Round(1D / Space, 0))
                throw new Exception("The number steps from zero to one should be an integer.");

            var cnt = (int)Math.Round(1D / Space, 0);
            var sum = 0D;
            for (int i = 0; i < cnt; i++)
            {
                var xt = Space * i;
                var xt1 = Space * (i + 1);
                sum += xt * (FuzzyNumber.GetInterval(xt).Max - FuzzyNumber.GetInterval(xt).Min);
                sum += (xt1) * (FuzzyNumber.GetInterval(xt1).Max - FuzzyNumber.GetInterval(xt1).Min);
            }

            return sum / (2 * cnt);
        }

        public static double CalcFuzziness(this IMF FuzzyNumber, double Space = 0.1D)
        {
            if (Math.Round(0.5D / Space, 5) != Math.Round(0.5D / Space, 0))
                throw new Exception("The number steps from zero to 0.5 should be an integer.");

            var cnt = (int)Math.Round(1D / Space, 0);

            var sum = 0D;
            for (int k = 0; k < cnt / 2; k++)
            {
                var alpha = Space * k;
                sum += (FuzzyNumber.GetInterval(alpha).Max - FuzzyNumber.GetInterval(alpha).Min);
                sum += (FuzzyNumber.GetInterval(alpha + Space).Max - FuzzyNumber.GetInterval(alpha + Space).Min);

            }
            for (int k = cnt / 2; k < cnt; k++)
            {
                var alpha = Space * k;
                sum += (FuzzyNumber.GetInterval(alpha).Min - FuzzyNumber.GetInterval(alpha).Max);
                sum += (FuzzyNumber.GetInterval(alpha + Space).Min - FuzzyNumber.GetInterval(alpha + Space).Max);
            } 

            return 1D / (2 * cnt) * sum;
        }
    }
}

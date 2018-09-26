using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.MetaHeuristics.Core
{
    public interface IArrayChParam : IMoveManager<ArrayChromosome, ArrayChromosomeMove>
    {
        ushort[] Ubound { get; }
        int ChromosomeLength { get; }
        double[] Evaluate(ArrayChromosome Ch, out double InferError);
        double GetMutationPriority(ArrayChromosome Ch, int Pos);
        bool IsStochasticEvaluation { get; }
        void Rearrange(ArrayChromosome Ch);
        void CrossOver(ArrayChromosome Ch1, ArrayChromosome Ch2);
        bool Equals(ArrayChromosome Ch1, ArrayChromosome Ch2);
    }

    public interface IArrayChParamWithOutput : IArrayChParam
    {
        IEnumerable<string> OutputColumnNames();
        IEnumerable<object> GetOutputColumnValues(ArrayChromosome Ch);
    }

    public static class ArrayChParamHelper
    {
        [ThreadStatic]
        static Random _rand;

        private static Random _globalrand = new Random();

        [ThreadStatic]
        static ushort[] crossovertemp = null;

        public static Random GetRandom(this IArrayChParam Param)
        {
            if (_rand == null) lock (_globalrand) { _rand = new Random(_globalrand.Next()); }
            return _rand;
        }

        public static void CrossOver(IArrayChParam Param, ArrayChromosome Ch1, ArrayChromosome Ch2)
        {
            var rand = Param.GetRandom();
            int DiffInd = -1;
            for (int i = 0; i < Ch1.Chromosome.Length; i++)
                if (Ch1.Chromosome[i] != Ch2.Chromosome[i]) { DiffInd = i; break; }
            if (DiffInd == -1 || DiffInd == Ch1.Chromosome.Length - 1) return;

            if (crossovertemp == null || crossovertemp.Length != Ch1.Chromosome.Length)
                crossovertemp = new ushort[Ch1.Chromosome.Length];

            var rnd1 = rand.Next(Ch1.Chromosome.Length - 1);
            var rnd2 = rand.Next(Ch1.Chromosome.Length - 1);

            if (rnd1 == rnd2) return;

            var rndmin = Math.Min(rnd1, rnd2);
            var rndmax = Math.Max(rnd1, rnd2);
            var len = rndmax - rndmin;

            Array.Copy(Ch1.Chromosome, rndmin, crossovertemp, rndmin, len);
            Array.Copy(Ch2.Chromosome, rndmin, Ch1.Chromosome, rndmin, len);
            Array.Copy(crossovertemp, rndmin, Ch2.Chromosome, rndmin, len);

        }

        public static int CalcRuleValue(ushort[] Chromosome, int Start, int Length, int MaxUBound)
        {
            int val = 0; int Coeff = 1;
            for (int i = Length - 1; i >= 0; i--)
            {
                val += Coeff * Chromosome[i + Start];
                Coeff *= MaxUBound;
            }
            return val;
        }

        public static List<int> GetSortedRuleValues(ushort[] Chromosome, IDiscreteDecisionParamDef[] ParamDef, List<int> Indices, int[] StartIndices, int MaxUBound = 6)
        {
            var list = ParamDef.Select((d, ind) => !Indices.Contains(ind) ? -1 : ArrayChParamHelper.CalcRuleValue(Chromosome, StartIndices[ind], d.UBound.Length, MaxUBound)).Where(v => v > -1).ToList();
            list.Sort();
            return list;
        }
    }
}

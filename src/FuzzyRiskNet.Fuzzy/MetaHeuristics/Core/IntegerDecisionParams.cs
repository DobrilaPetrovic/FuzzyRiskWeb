using System;
using System.Collections.Generic;
using System.Linq;
using FuzzyRiskNet.Helpers;

namespace FuzzyRiskNet.MetaHeuristics.Core
{
    public interface IDiscreteDecisionParamDef
    {
        string Name { get; set; }
        ushort[] UBound { get; }
        double CalcValueAt(int Pos, ushort ValuePos);
    }

    public class DiscreteDecisionParamDef : IDiscreteDecisionParamDef
    {
        public DiscreteDecisionParamDef(string Name = null, ushort[] UBound = null) { this.Name = Name; this.UBound = UBound; }
        public DiscreteDecisionParamDef(string Name = null, int[] UBound = null) { this.Name = Name; this.UBound = UBound.Select(u => (ushort)u).ToArray(); }
        public string Name { get; set; }
        public ushort[] UBound { get; set; }
        public double CalcValueAt(int Pos, ushort ValuePos) { return ValuePos; }
    }

    public class SingleStepDecisionParamDef : IDiscreteDecisionParamDef
    {
        public SingleStepDecisionParamDef(string Name, double Min, double Step, double Max)
        {
            this.Name = Name;
            this.Min = Min; this.Max = Max; this.Step = Step;
            UBound = new ushort[] { (ushort)Math.Floor((Max - Min) / Step) };           
        }

        public double Min { get; private set; }
        public double Max { get; private set; }
        public double Step { get; private set; }
        public string Name { get; set; }
        public ushort[] UBound { get; private set; }

        public double CalcValueAt(int Pos, ushort ValuePos) { return Min + ValuePos * Step; }
    }

    public class CustomValuesDecisionParamDef : IDiscreteDecisionParamDef
    {
        public CustomValuesDecisionParamDef(string Name, double[] Values)
        {
            this.Name = Name;
            this.Values = Values;
            UBound = new ushort[] { (ushort)(Values.Length - 1) };
        }

        public double[] Values { get; private set; }
        public string Name { get; set; }
        public ushort[] UBound { get; private set; }

        public double CalcValueAt(int Pos, ushort ValuePos) { return Values[ValuePos]; }
    }

    public abstract class DiscreteDecisionParams : IArrayChParam 
    {
        public DiscreteDecisionParams(IEnumerable<IDiscreteDecisionParamDef> ParamDef)
        {
            this.ParamDef = ParamDef.ToArray();
            RefreshGAParam();
        }

        public bool IsStochasticEvaluation { get { return false; } }

        public abstract bool CanEvaluateInParallel { get; }

        public IDiscreteDecisionParamDef[] ParamDef { get; private set; }

        public ushort[] Ubound { get; private set; }
        public int ChromosomeLength { get { return Ubound.Length; } }

        public int[] MutationWeight { get; private set; }
        public int _TotalMutationWeight = -1;
        public int TotalMutationWeight { get { if (_TotalMutationWeight < 0) _TotalMutationWeight = MutationWeight.Sum(); return _TotalMutationWeight; } }

        public double GetMutationPriority(ArrayChromosome Ch, int Pos)
        {
            var currentval = Ch.Chromosome[Pos];
            var sumch = Ch.Chromosome.Sum(c => c) + (currentval == 0 ? 1 : -1);

            return Math.Min(1e-5, Math.Exp(Math.Min(0, 20 * (8 - sumch))) * Math.Exp(Math.Min(0, 20 * (sumch - 6))) * MutationWeight[Pos]);
        }

        public void RefreshGAParam()
        {
            var listcount = ParamDef.Length;
            StartIndices = new int[listcount];
            int Start = 0;
            int[] TotalMW = new int[0];
            ushort[] TotalUB = new ushort[0];
            for (int i = 0; i < listcount; i++)
            {
                var g = ParamDef[i].UBound.Length;
                StartIndices[i] = Start;
                Start += g;
                TotalMW = TotalMW.Concat(g.CountTo().Select(d => 1)).ToArray();
                TotalUB = TotalUB.Concat(ParamDef[i].UBound).ToArray();
            }
            this.Ubound = TotalUB;
            this.MutationWeight = TotalMW;
            this._TotalMutationWeight = -1;
        }

        public int[] StartIndices { get; private set; }

        public ArrayChromosome GetAncestor()
        {
            return new ArrayChromosome(this);
        }

        public string[] ObjectiveNames { get { return new string[] { "Obj" }; } }

        public abstract double[] Evaluate(ArrayChromosome Ch, out double InferError);

        public Dictionary<string, double[]> FillDblDic(ArrayChromosome Ch, Dictionary<string, double[]> Dic = null) { return FillDictionary(Ch, Dic, u => (double)u); }
        public Dictionary<string, int[]> FillIntDic(ArrayChromosome Ch, Dictionary<string, int[]> Dic = null) { return FillDictionary(Ch, Dic, u => (int)u); }
        //public Dictionary<string, ushort[]> FillUShortDic(ArrayChromosome Ch, Dictionary<string, ushort[]> Dic = null) { return FillDictionary(Ch, Dic, u => (ushort)u); }

        Dictionary<string, T[]> FillDictionary<T>(ArrayChromosome Ch, Dictionary<string, T[]> Dic, Func<double, T> ConvertMethod)
        {
            if (Dic == null) Dic = new Dictionary<string, T[]>();
            var listcount = ParamDef.Length;

            for (int j = 0; j < listcount; j++)
            {
                var e = ParamDef[j];
                var n = e.Name;              
                var len = e.UBound.Length;
                if (Dic.ContainsKey(n)) { if (Dic[n].Length != len) Dic[n] = new T[len]; } else Dic.Add(n, new T[len]);
                var newarr = Dic[n];
                for (int i = 0; i < len; i++)
                    newarr[i] = ConvertMethod(ParamDef[j].CalcValueAt(i, Ch.Chromosome[StartIndices[j] + i]));
            }
            return Dic;
        }

        public ArrayChromosome GetNeighbour(ArrayChromosome Value, Random Rnd)
        {
            var n = Value.Clone() as ArrayChromosome;
            n.InvalidateObjectives();

            if (!n.Chromosome.Any(c => c != n.Chromosome.Average(u => u)) || Rnd.NextDouble() < 1)
            {
                int ind = Rnd.Next(n.Chromosome.Length);
                var newval = (ushort)Rnd.Next(Ubound[ind] - 1);
                var oldval = n.Chromosome[ind];
                n.Chromosome[ind] = newval >= oldval ? (ushort)(newval + 1) : newval;
                return n;
            }
            else
            {
                int ind;
                do
                {
                    ind = Rnd.Next(n.Chromosome.Length - 1);
                } while (n.Chromosome[ind] == n.Chromosome[ind + 1]);
                var tmp = n.Chromosome[ind];
                n.Chromosome[ind] = n.Chromosome[ind + 1];
                n.Chromosome[ind + 1] = tmp;
                return n;
            }
        }

        public virtual IEnumerable<ArrayChromosomeMove> ListAllMoves(ArrayChromosome Ch)
        {
            var chlen = Ch.Chromosome.Length;
            for (int i = 0; i < chlen; i++)
                for (ushort j = 0; j < Ubound[i]; j++)
                    if (Ch.Chromosome[i] != j)
                        yield return new ArrayChromosomeMove() { MoveType = ArrayChromosomeMove.MoveTypes.ChangeValue, Pos1 = i, NewValue = j, Pos2 = -1 };

            for (int i = 0; i < chlen - 1; i++)
            {
                var j = i + 1;
                if (Ch.Chromosome[i] != Ch.Chromosome[j])
                    yield return new ArrayChromosomeMove() { MoveType = ArrayChromosomeMove.MoveTypes.Exchange, Pos1 = i, Pos2 = j };
            }
        }

        public ArrayChromosome MakeMove(ArrayChromosome Chromosome, ArrayChromosomeMove Move)
        {
            var n = Chromosome.Clone() as ArrayChromosome;
            n.InvalidateObjectives();

            if (Move.MoveType == ArrayChromosomeMove.MoveTypes.ChangeValue)
                n.Chromosome[Move.Pos1] = Move.NewValue;
            else
            {
                var tmp = n.Chromosome[Move.Pos1];
                n.Chromosome[Move.Pos1] = n.Chromosome[Move.Pos2];
                n.Chromosome[Move.Pos2] = tmp;
            }

            return n;
        }

        public virtual void Rearrange(ArrayChromosome Ch)
        {
        }

        public virtual void CrossOver(ArrayChromosome Ch1, ArrayChromosome Ch2)
        {
            ArrayChParamHelper.CrossOver(this, Ch1, Ch2);
        }

        public virtual bool Equals(ArrayChromosome Ch1, ArrayChromosome Ch2)
        {
            var len = Ch1.Chromosome.Length;
            for (int i = 0; i < len; i++)
                if (Ch1.Chromosome[i] != Ch2.Chromosome[i]) return false;

            if (Ch1.FeasibilityError != Ch2.FeasibilityError) return false;

            if (Ch1.IsEvaluated && Ch2.IsEvaluated)
            {
                for (int i = 0; i < Ch1.Objectives.Length; i++)
                    if (Ch1.Objectives[i].CompareTo(Ch2.Objectives[i]) != 0) return false;
            }
            else
                if (Ch1.IsEvaluated || Ch2.IsEvaluated)
                    return false;

            return true;
        }
    }

    public class BasicSODiscreteDecisionParams : DiscreteDecisionParams
    {
        Func<Dictionary<string, int[]>, double> EvaluateFunc;
        public BasicSODiscreteDecisionParams(Func<Dictionary<string, int[]>, double> Evaluate, params IDiscreteDecisionParamDef[] ParamDef)
            : base(ParamDef)
        {
            EvaluateFunc = Evaluate;
        }
        

        public override bool CanEvaluateInParallel
        {
	        get { return false; }
        }

        Dictionary<string, int[]> dic = new Dictionary<string, int[]>();
        public override double[] Evaluate(ArrayChromosome Ch, out double InferError)
        {
            InferError = 0;
            var dic = FillIntDic(Ch, this.dic);
            return new double[] { EvaluateFunc(dic) };
        }
    }
}

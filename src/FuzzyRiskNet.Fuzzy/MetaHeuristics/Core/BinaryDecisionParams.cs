using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuzzyRiskNet.Helpers;
using System.Runtime.Remoting.Contexts;

namespace FuzzyRiskNet.MetaHeuristics.Core
{
    public abstract class BinaryDecisionParams : IArrayChParam 
    {
        public BinaryDecisionParams(Dictionary<string, int> BinaryVariablesLengths)
        {
            this.BinaryVariablesLengths = BinaryVariablesLengths;
            RefreshGAParam();
        }

        public bool CanEvaluateInParallel { get { return false; } }

        public bool IsStochasticEvaluation { get { return false; } }

        public Dictionary<string, int> BinaryVariablesLengths { get; private set; }

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
            var listcount = BinaryVariablesLengths.Keys.Count;
            StartIndices = new int[listcount];
            int Start = 0;
            int[] TotalMW = new int[0];
            ushort[] TotalUB = new ushort[0];
            for (int i = 0; i < listcount; i++)
            {
                var g = BinaryVariablesLengths.ElementAt(i).Value;
                StartIndices[i] = Start;
                Start += g;
                TotalMW = TotalMW.Concat(g.CountTo().Select(d => 1)).ToArray();
                TotalUB = TotalUB.Concat(g.CountTo().Select(d => (ushort)1)).ToArray();
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

        public void FillDictionary(ArrayChromosome Ch, Dictionary<string, double[]> Dic)
        {
            var listcount = BinaryVariablesLengths.Keys.Count;

            for (int j = 0; j < listcount; j++)
            {
                var e = BinaryVariablesLengths.ElementAt(j);
                var n = e.Key;              
                var len = e.Value;
                if (Dic.ContainsKey(n)) { if (Dic[n].Length != len) Dic[n] = new double[len]; } else Dic.Add(n, new double[len]);
                var newarr = Dic[n];
                for (int i = 0; i < len; i++)
                    newarr[i] = Ch.Chromosome[StartIndices[j] + i];
            }
        }

        public ArrayChromosome GetNeighbour(ArrayChromosome Value, Random Rnd)
        {
            var n = Value.Clone() as ArrayChromosome;
            n.InvalidateObjectives();

            if ((!n.Chromosome.Any(c => c == 1) || !n.Chromosome.Any(c => c == 0)) || Rnd.NextDouble() < 1)
            {
                int ind = Rnd.Next(n.Chromosome.Length);
                n.Chromosome[ind] = (ushort)(1 - n.Chromosome[ind]);
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
                yield return new ArrayChromosomeMove() { MoveType = ArrayChromosomeMove.MoveTypes.Flip, Pos1 = i, Pos2 = -1 };

            for (int i = 0; i < chlen - 1; i++)
            {
                var j = i + 1;
                //for (int j = i + 1; j < chlen; j++)
                if (Ch.Chromosome[i] != Ch.Chromosome[j])
                    yield return new ArrayChromosomeMove() { MoveType = ArrayChromosomeMove.MoveTypes.Exchange, Pos1 = i, Pos2 = j };
            }
        }

        public ArrayChromosome MakeMove(ArrayChromosome Chromosome, ArrayChromosomeMove Move)
        {
            var n = Chromosome.Clone() as ArrayChromosome;
            n.InvalidateObjectives();

            if (Move.MoveType == ArrayChromosomeMove.MoveTypes.Flip)
                n.Chromosome[Move.Pos1] = (ushort)(1 - n.Chromosome[Move.Pos1]);
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
}

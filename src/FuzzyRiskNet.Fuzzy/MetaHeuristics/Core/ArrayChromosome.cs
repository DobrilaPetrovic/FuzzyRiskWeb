using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzyRiskNet.MetaHeuristics.Core
{
    public struct ObjectiveDef
    {       
        public double[] Objectives;
        public int NumTryEval;
        public int NumEval;
    }
    public class ArrayChromosome : IChromosome
    {
        ObjectiveDef _Objectives = new ObjectiveDef();
        ushort[] _Chromosome = null;
        IArrayChParam _Parameters = null;

        [ThreadStatic] 
        static Random rand;

        public bool IsEvaluated { get { return _Objectives.Objectives != null; } }

        public double[] Objectives
        {
            get { if (IsEvaluated) return _Objectives.Objectives; else throw new Exception("Not yet evaluated."); }
        }

        public int NumEvaluated { get { return _Objectives.NumEval; } }

        public IArrayChParam Parameters { get { return _Parameters; } }
        public double FeasibilityError { get; private set; }
        public ushort[] Chromosome { get { return _Chromosome; } }

        private ArrayChromosome() { if (rand == null) rand = Parameters.GetRandom(); }
        public ArrayChromosome(IArrayChParam Parameters) : this()
        {
            this._Parameters = Parameters;
            this._Chromosome = new ushort[Parameters.ChromosomeLength];
        }

        public IChromosome Clone()
        {
            var n = new ArrayChromosome();
            n._Parameters = _Parameters;
            n._Objectives = _Objectives;
            n._Chromosome = _Chromosome.ToArray();
            n.FeasibilityError = FeasibilityError;
            return n;
        }

        public void InvalidateObjectives()
        {
            _Objectives = new ObjectiveDef();
        }

        public virtual IChromosome[] NewRandom(int Count)
        {
            var Chromosomes = new IChromosome[Count];
            for (int i = 0; i < Count; i++) 
            {
                Chromosomes[i] = new ArrayChromosome()
                {
                    _Parameters = _Parameters,
                    _Chromosome = new ushort[_Chromosome.Length]
                };
            }

            for (int i = 0; i < Parameters.ChromosomeLength; i++)
            {
                List<int> AllValues = Enumerable.Range(0, Count).OrderBy(r => rand.NextDouble()).ToList();
                double ubound = Parameters.Ubound[i] + 1; 
                for (int j = 0; j < Count; j++)
                    (Chromosomes[AllValues[j]] as ArrayChromosome)._Chromosome[i] = (ushort)(ubound * j / Count);
            }

            foreach (var ch in Chromosomes) (ch as ArrayChromosome).Rearrange();

            return Chromosomes;
        }

        public virtual void SetRandom()
        {
            for (int i = 0; i < _Chromosome.Length; i++)
                _Chromosome[i] = (ushort)rand.Next(_Parameters.Ubound[i] + 1);
            _Objectives = new ObjectiveDef();
            Rearrange();
        }

        public virtual void Mutate()
        {
            var i = rand.NextDouble() * Enumerable.Range(0, Parameters.ChromosomeLength).Sum(j => Parameters.GetMutationPriority(this, j));
            var sum = 0D;
            for (int j = 0; j < Parameters.ChromosomeLength; j++)
            {
                sum += Parameters.GetMutationPriority(this, j);
                if (sum >= i)
                {
                    ushort newval;
                    while ((newval = (ushort)rand.Next(_Parameters.Ubound[j] + 1)) == _Chromosome[j]) ;
                    _Chromosome[j] = newval;
                    _Objectives = new ObjectiveDef();
                    Rearrange();
                    return;
                }
            }

            throw new Exception("Unknown error");
        }

        public void Rearrange()
        {
            Parameters.Rearrange(this);
        }

        public virtual void CrossOver(IChromosome pair)
        {
            if (pair is ArrayChromosome)
            {
                var p = pair as ArrayChromosome;

                Parameters.CrossOver(this, p);

                p._Objectives = new ObjectiveDef();
                _Objectives = new ObjectiveDef();
                p.Rearrange();
                Rearrange();
            }
            else
                throw new ArgumentException("pair");
        }


        public void Evaluate()
        {
            _Objectives.NumTryEval++;
            if (IsEvaluated && (!Parameters.IsStochasticEvaluation || _Objectives.NumTryEval % 5 != 0)) return;
            
            double InfErr = 0;
            var newevalres = Parameters.Evaluate(this, out InfErr);
            
            if (_Objectives.NumEval > 0)
                _Objectives.Objectives = newevalres.Select((d, i) => (d + _Objectives.Objectives[i] * _Objectives.NumEval) / (_Objectives.NumEval + 1)).ToArray();
            else
                _Objectives.Objectives = newevalres;

            _Objectives.NumEval++;
            FeasibilityError = InfErr;            
        }

        public bool Equals(IChromosome other)
        {            
            if (!(other is ArrayChromosome)) return false;
            if (this == other) return true;
            var oth = other as ArrayChromosome;

            return Parameters.Equals(this, oth);
        }

        public override int GetHashCode()
        {
            int hashcode = 0;
            for (int i = 0; i < _Chromosome.Length; i++)
                hashcode ^= _Chromosome[i].GetHashCode();
            return hashcode;
        }
    }

    public struct ArrayChromosomeMove : IMove
    {
        public enum MoveTypes { Flip, Exchange, ChangeValue }
        public MoveTypes MoveType { get; set; }
        public int Pos1 { get; set; }
        public int Pos2 { get; set; }
        public ushort NewValue { get; set; }
        
        public bool Equals(IMove other)
        {
            if (other is ArrayChromosomeMove)
            {
                var o = (ArrayChromosomeMove)other;
                if (o.MoveType == MoveType && o.Pos1 == Pos1 && o.Pos2 == Pos2 && o.NewValue == NewValue) return true;
            }
            return false;
        }
    }
}

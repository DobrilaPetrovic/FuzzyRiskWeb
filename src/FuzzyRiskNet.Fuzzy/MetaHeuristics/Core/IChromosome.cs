using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.MetaHeuristics.Core
{
    public interface IChromosome : IEquatable<IChromosome>
    {
        IChromosome Clone();
        IChromosome[] NewRandom(int Count);
        void SetRandom();
        void Mutate();
        void CrossOver(IChromosome pair);
        void Evaluate();
        bool IsEvaluated { get; }
        double[] Objectives { get; }
        double FeasibilityError { get; }
    }

    public interface IGADef<T> : IGADef where T : IChromosome
    {
        T GetAncestor();
        T GetNeighbour(T Value, Random Rnd);
        bool CanEvaluateInParallel { get; }
    }

    public interface IGADef
    {
        string[] ObjectiveNames { get; }
    }

    public interface IMove : IEquatable<IMove>
    {
    }

    public interface IMoveManager<T, T2> : IGADef<T> where T : IChromosome where T2 : IMove
    {
        IEnumerable<T2> ListAllMoves(T Chromosome);
        T MakeMove(T Chromosome, T2 Move);        
    }
}

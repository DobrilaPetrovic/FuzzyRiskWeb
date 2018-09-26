using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using FuzzyRiskNet.MetaHeuristics.Core;
using System.Threading.Tasks;
//using Microsoft.SolverFoundation.Common;

namespace FuzzyRiskNet.MetaHeuristics.GA
{
    public class SOGAPopulation 
    {
        IChromosome[] _Chromosomes = null;
        int _PopulationSize;
        private SOGAPopulation() 
        {
        }


        public bool CanEvaluateInParallel { get; private set; }

        public SOGAPopulation(int PopulationSize, IChromosome Ancestor, bool CanEvaluateInParallel)
        {
            _PopulationSize = PopulationSize;
            _Chromosomes = Ancestor.NewRandom(_PopulationSize);
            this.CanEvaluateInParallel = CanEvaluateInParallel;
            Evaluate();
        }

        private SOGAPopulation(SOGAPopulation p)
        {
            this.CanEvaluateInParallel = p.CanEvaluateInParallel;
            _PopulationSize = p._PopulationSize;
            _Chromosomes = new IChromosome[p._Chromosomes.Length];
        }

        public SOGAPopulation Clone()
        {
            var n = new SOGAPopulation(this);
            Dictionary<IChromosome, IChromosome> clones = new Dictionary<IChromosome, IChromosome>();
            for (int i = 0; i < _Chromosomes.Length; i++) clones.Add(_Chromosomes[i], _Chromosomes[i].Clone());
            for (int i = 0; i < _Chromosomes.Length; i++) n._Chromosomes[i] = clones[_Chromosomes[i]];
            return n;
        }

        public static bool EqualsTo(IChromosome ch1, IChromosome ch2)
        {
            return Enumerable.Range(0, ch1.Objectives.Length).All(i => ch1.Objectives[i] == ch2.Objectives[i]) && ch1.FeasibilityError == ch2.FeasibilityError;
        }

        public class IChromosomeEqComparer : IEqualityComparer<IChromosome>
        {
            public bool Equals(IChromosome x, IChromosome y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(IChromosome obj)
            {
                return obj.GetHashCode();
            }
        }

        public SOGAPopulation FindChildPopulation(double CrossOverProb, double MutationProb)
        {
            var rand = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);

            Evaluate();

            var list = _Chromosomes.Select(c => c.Clone()).OrderBy(c => rand.NextDouble()).ToList();

            var numitems = Math.Min(PopulationSize, list.Count);
            for (int i = 1; i < numitems; i += 2)
                if (rand.NextDouble() < CrossOverProb)
                    list[i].CrossOver(list[i - 1]);

            for (int i = 0; i < numitems; i++)
                while (rand.NextDouble() < MutationProb) // Allows for multiple mutations in a single chromosome
                    list[i].Mutate();

            Evaluate(list);

            var Union = list.Concat(_Chromosomes).Distinct().ToList();

            /*for (int i = 0; i < Union.Count; i++)
                if (Union.Select((ch, j) => j != i && (Union[i] as ArrayChromosome).Chromosome.Select((v, k) => (ch as ArrayChromosome).Chromosome[k] == v).All(b => b)).Any(b => b))
                    throw new Exception("Distinct doesn't work!");*/

            if (Union.Where(l => l.FeasibilityError < 1).Count() >= PopulationSize)
                Union = Union.Where(l => l.FeasibilityError < 1).ToList();
            else
                Union = Union.OrderBy(l => l.FeasibilityError)/*.Take(PopulationSize)*/.ToList();

            List<IChromosome> pop;

            var maxobj = Union.Max(ch => ch.Objectives[0]);
            var minobj = Union.Min(ch => ch.Objectives[0]);

            if (Union.Count > PopulationSize)
            {
                pop = new List<IChromosome>();
                pop.AddRange(Union.OrderBy(ch => ch.Objectives[0]).Take(Math.Max(1, PopulationSize / 5)));
                foreach (var p in pop) Union.Remove(p);

                var arr = Union.Select(ch => new { prob = maxobj == minobj ? 1 : /*rand.NextDouble() **/ (maxobj - ch.Objectives[0]) / (maxobj - minobj), ch = ch }).ToList();

                while (pop.Count < PopulationSize)                
                {
                    var total = arr.Sum(ch => ch.prob);
                    var pos = total * rand.NextDouble();
                    var current = 0D;
                    for (int i = 0; i < arr.Count; i++)
                    {
                        current += arr[i].prob;
                        if (current >= pos) { if (!pop.Any(p => p.Equals(arr[i].ch))) { pop.Add(arr[i].ch); arr.RemoveAt(i); } else throw new Exception("Should not happen"); break; }
                    }
                    //if (!arr.Any(a => pop.Any(p => p.Equals(a.ch)))) break;
                    if (arr.Count == 0) break;
                }
            }
            else
                pop = Union;

            var n = new SOGAPopulation(this);
            n._Chromosomes = pop.ToArray();
            var bestobj = n.Chromosomes.Min(ch2 => ch2.Objectives[0]);
            n.BestChromosome = n.Chromosomes.First(ch => ch.Objectives[0] == bestobj);
            //n.DominatedByCnt = DominatedByCnt;
            //n.FindExtremes(this.Extremes == null ? Union : this.Extremes.Union(n._Chromosomes).ToList());
            
            return n;
        }

        public void Evaluate()
        {
            Evaluate(_Chromosomes);
        }

        public void Evaluate(IEnumerable<IChromosome> list)
        {
            if (CanEvaluateInParallel)
                Parallel.ForEach(list, p => p.Evaluate());
            else
                foreach (var p in list) p.Evaluate();
        }

        /*public void FindExtremes(IList<IChromosome> Items)
        {
            var numobjectives = Items.First().Objectives.Length;
            Extremes = new IChromosome[numobjectives];
            for (int i = 0; i < numobjectives; i++)
                Extremes[i] = PopulationAnalysis.FindExtreme(Items, i);
        }*/

        //public IChromosome[] Extremes; 
        //public Dictionary<IChromosome, int> DominatedByCnt;
        public IChromosome[] Chromosomes { get { return _Chromosomes; } }
        public int PopulationSize { get { return _PopulationSize; } }

        public IChromosome BestChromosome { get; private set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuzzyRiskNet.MetaHeuristics.Core;
using System.Threading.Tasks; 

namespace FuzzyRiskNet.MetaHeuristics.GA
{
    public class MPSOGA<T> where T : IChromosome
    {
        /// <summary>
        /// Constructor for Single-Objective-GA class.
        /// </summary>
        /// <param name="Definition">Problem definition</param>
        public MPSOGA(IGADef<T> Definition)
        {
            this.Definition = Definition;
            PopulationSize = 200;
            OnNewPopulations = (p, t) => true;
            OnNewPopulation = (p, t) => true;
            MaximumGeneration = 200;
            StallGenerations = 50;
            StallThreshold = 1e-7;
            MutationProb = 0.5;
            CrossOverProb = 0.5;
        }

        public IGADef<T> Definition { get; private set; }
        public Func<Population[], TimeSpan, bool> OnNewPopulations { get; set; }
        
        public Func<Population, TimeSpan, bool> OnNewPopulation { get; set; }
        public int PopulationSize { get; set; }
        public int NumPopulations { get; set; }
        public int MaximumGeneration { get; set; }
        public int StallGenerations { get; set; }
        public double StallThreshold { get; set; }
        public double MutationProb { get; set; }
        public double CrossOverProb { get; set; }
        
        /// <summary>
        /// Runs GA algorithm from a randomly generated initial population.
        /// </summary>
        public void Run()
        {
            Run(Enumerable.Range(0, NumPopulations).Select(p => new Population(PopulationSize, Definition.GetAncestor(), Definition.CanEvaluateInParallel)).ToArray());
        }

        /// <summary>
        /// Runs GA algorithm from the supplied population.
        /// </summary>
        /// <param name="pop">Populations to start the algorithm from.</param>
        public void Run(Population[] pop)
        {
            if (Definition.ObjectiveNames.Length != 1) throw new ArgumentException("SOGA is a single objective meta-heuristic.");

            double LastBestObj = 0;
            var AllChanges = new List<double>(StallGenerations);

            var EmptyChArray = new IChromosome[0];
            var rnd = new Random();

            for (int i = 0; i < MaximumGeneration; i++)
            {
                DateTime start = DateTime.Now;
                for (int j = 0; j < pop.Length; j++)
                    pop[j] = pop[j].FindChildPopulation(CrossOverProb, MutationProb, 
                        i > 0 && i % 20 == 0 
                            ? Enumerable.Range(0, 20).Select(ind => LastPopulation.Chromosomes[rnd.Next(LastPopulation.Chromosomes.Length)]).ToArray() 
                            : EmptyChArray);

                var totaltime = DateTime.Now.Subtract(start);
                LastPopulations = pop;
                LastPopulation = new Population(pop);
                LastGeneration = i;
                if (!OnNewPopulations(pop, totaltime)) break;
                if (!OnNewPopulation(LastPopulation, totaltime)) break;
                
                var newbest = pop.Min(p => p.Chromosomes.Min(ch => ch.Objectives[0]));

                if (LastBestObj != 0) AllChanges.Add(LastBestObj - newbest);

                while (AllChanges.Count > StallGenerations) AllChanges.RemoveAt(0);                
                if (AllChanges.Count == StallGenerations && AllChanges.Average() < StallThreshold)
                    break;

                LastBestObj = newbest;
            }
        }
        
        public Population[] LastPopulations { get; private set; }
        public Population LastPopulation { get; private set; }
        public int LastGeneration { get; private set; }

        public class Population 
        {
            IChromosome[] _Chromosomes = null;
            int _PopulationSize;
            private Population() 
            {
            }


            public bool CanEvaluateInParallel { get; private set; }

            public Population(int PopulationSize, IChromosome Ancestor, bool CanEvaluateInParallel)
            {
                _PopulationSize = PopulationSize;
                _Chromosomes = Ancestor.NewRandom(_PopulationSize);
                this.CanEvaluateInParallel = CanEvaluateInParallel;
                Evaluate();
            }

            public Population(Population[] pops)
            {
                this.CanEvaluateInParallel = pops.First().CanEvaluateInParallel;
                _PopulationSize = pops.Sum(p => p._PopulationSize);
                _Chromosomes = pops.SelectMany(p => p._Chromosomes).Select(p => p.Clone()).ToArray();
                var bestobj = _Chromosomes.Min(ch2 => ch2.Objectives[0]);
                BestChromosome = _Chromosomes.First(ch => ch.Objectives[0] == bestobj);
            }


            private Population(Population p)
            {
                this.CanEvaluateInParallel = p.CanEvaluateInParallel;
                _PopulationSize = p._PopulationSize;
                _Chromosomes = new IChromosome[p._Chromosomes.Length];
            }

            public Population Clone()
            {
                var n = new Population(this);
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

            public Population FindChildPopulation(double CrossOverProb, double MutationProb, IChromosome[] Immigrants)
            {
                var rand = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);

                Evaluate();

                var list = _Chromosomes.Concat(Immigrants).Select(c => c.Clone()).OrderBy(c => rand.NextDouble()).ToList();

                var numitems = Math.Min(PopulationSize, list.Count);
                for (int i = 1; i < numitems; i += 2)
                    if (rand.NextDouble() < CrossOverProb)
                        list[i].CrossOver(list[i - 1]);

                for (int i = 0; i < numitems; i++)
                    if (rand.NextDouble() < MutationProb)
                        list[i].Mutate();

                Evaluate(list);

                var Union = list.Concat(_Chromosomes).Concat(Immigrants).Distinct().ToList();

                if (Union.Where(l => l.FeasibilityError < 1).Count() >= PopulationSize)
                    Union = Union.Where(l => l.FeasibilityError < 1).ToList();
                else
                    Union = Union.OrderBy(l => l.FeasibilityError).ToList();

                List<IChromosome> pop;

                var maxobj = Union.Max(ch => ch.Objectives[0]);
                var minobj = Union.Min(ch => ch.Objectives[0]);

                if (Union.Count > PopulationSize)
                {
                    pop = new List<IChromosome>();
                    pop.AddRange(Union.OrderBy(ch => ch.Objectives[0]).Take(Math.Max(1, PopulationSize / 5)));
                    foreach (var p in pop) Union.Remove(p);

                    var arr = Union.Select(ch => new { prob = maxobj == minobj ? 1 : (maxobj - ch.Objectives[0]) / (maxobj - minobj), ch = ch }).ToList();

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

                        if (arr.Count == 0) break;
                    }
                }
                else
                    pop = Union;

                var n = new Population(this);
                n._Chromosomes = pop.ToArray();
                var bestobj = n.Chromosomes.Min(ch2 => ch2.Objectives[0]);
                n.BestChromosome = n.Chromosomes.First(ch => ch.Objectives[0] == bestobj);
            
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

            public IChromosome[] Chromosomes { get { return _Chromosomes; } }
            public int PopulationSize { get { return _PopulationSize; } }

            public IChromosome BestChromosome { get; private set; }
        }
    }
        
}

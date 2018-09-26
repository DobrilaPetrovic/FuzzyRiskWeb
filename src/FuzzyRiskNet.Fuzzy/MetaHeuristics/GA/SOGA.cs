using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuzzyRiskNet.MetaHeuristics.Core; 

namespace FuzzyRiskNet.MetaHeuristics.GA
{
    public class SOGA<T> where T : IChromosome
    {
        /// <summary>
        /// Constructor for Single-Objective-GA class.
        /// </summary>
        /// <param name="Definition">Problem definition</param>
        public SOGA(IGADef<T> Definition)
        {
            this.Definition = Definition;
            PopulationSize = 200;
            OnNewPopulation = (p, t) => true;
            MaximumGeneration = 200;
            StallGenerations = 50;
            StallThreshold = 1e-7;
            MutationProb = 0.5;
            CrossOverProb = 0.5;
        }

        public IGADef<T> Definition { get; private set; }
        public Func<SOGAPopulation, TimeSpan, bool> OnNewPopulation { get; set; }
        public int PopulationSize { get; set; }
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
            SOGAPopulation pop = new SOGAPopulation(PopulationSize, Definition.GetAncestor(), Definition.CanEvaluateInParallel);
            Run(pop);
        }

        /// <summary>
        /// Runs GA algorithm from the supplied population.
        /// </summary>
        /// <param name="pop">Population to start the algorithm from.</param>
        public void Run(SOGAPopulation pop)
        {
            if (Definition.ObjectiveNames.Length != 1) throw new ArgumentException("SOGA is a single objective meta-heuristic.");

            double LastBestObj = 0;
            var AllChanges = new List<double>(StallGenerations);

            for (int i = 0; i < MaximumGeneration; i++)
            {
                DateTime start = DateTime.Now;
                pop = pop.FindChildPopulation(CrossOverProb, MutationProb);
                var totaltime = DateTime.Now.Subtract(start);
                LastPopulation = pop;
                LastGeneration = i;
                if (!OnNewPopulation(pop, totaltime)) break;
                
                var newbest = pop.Chromosomes.Min(ch => ch.Objectives[0]);

                if (LastBestObj != 0) AllChanges.Add(LastBestObj - newbest);

                while (AllChanges.Count > StallGenerations) AllChanges.RemoveAt(0);                
                if (AllChanges.Count == StallGenerations && AllChanges.Average() < StallThreshold)
                    break;

                LastBestObj = newbest;
            }
        }
        
        public SOGAPopulation LastPopulation { get; private set; }
        public int LastGeneration { get; private set; }
    }
}

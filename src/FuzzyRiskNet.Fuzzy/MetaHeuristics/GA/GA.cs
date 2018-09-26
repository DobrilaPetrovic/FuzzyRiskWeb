using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuzzyRiskNet.MetaHeuristics.Core; 

namespace FuzzyRiskNet.MetaHeuristics.GA
{
    public class GA<T> where T : IChromosome
    {
        public GA(IGADef<T> Definition)
        {
            this.Definition = Definition;
            PopulationSize = 200;
            OnNewPopulation = (p, an, t) => true;
            MaximumGeneration = 200;
            StallGenerations = 50;
            StallThreshold = 1e-6;
            MutationProb = 0.5;
            CrossOverProb = 0.5;
        }
        public IGADef<T> Definition { get; private set; }
        public Func<Population, PopulationAnalysis, TimeSpan, bool> OnNewPopulation { get; set; }
        public int PopulationSize { get; set; }
        public int MaximumGeneration { get; set; }
        public int StallGenerations { get; set; }
        public double StallThreshold { get; set; }
        public double MutationProb { get; set; }
        public double CrossOverProb { get; set; }
        public void Run()
        {
            Population pop = new Population(PopulationSize, Definition.GetAncestor());
            Run(pop);
        }
        public void Run(Population pop)
        {            
            double LastSpread = 0;
            var AllSpreadChanges = new List<double>(StallGenerations);

            for (int i = 0; i < MaximumGeneration; i++)
            {
                DateTime start = DateTime.Now;
                pop = pop.FindChildPopulation(CrossOverProb, MutationProb);
                var totaltime = DateTime.Now.Subtract(start);
                LastPopulation = pop;
                LastPopulationAnalysis = pop.GetAnalysis();
                LastGeneration = i;
                if (!OnNewPopulation(pop, LastPopulationAnalysis, totaltime)) break;
                
                AllSpreadChanges.Add(Math.Abs(LastPopulationAnalysis.ParetoSpread - LastSpread));
                LastSpread = LastPopulationAnalysis.ParetoSpread;
                while (AllSpreadChanges.Count > StallGenerations) AllSpreadChanges.RemoveAt(0);                
                if (AllSpreadChanges.Count == StallGenerations && AllSpreadChanges.Average() < StallThreshold)
                    break;
            }
        }
        
        public Population LastPopulation { get; private set; }
        public PopulationAnalysis LastPopulationAnalysis { get; private set; }
        public int LastGeneration { get; private set; }
    }
}

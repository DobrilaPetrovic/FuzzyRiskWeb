using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using FuzzyRiskNet.MetaHeuristics.Core; 

namespace FuzzyRiskNet.MetaHeuristics.GA
{
    public class Population 
    {
        IChromosome[] _Chromosomes = null;
        int _PopulationSize;
        private Population() 
        { 
        }

        public Population(int PopulationSize, IChromosome Ancestor)
        {
            _PopulationSize = PopulationSize;
            _Chromosomes = Ancestor.NewRandom(_PopulationSize);
            Evaluate();
            var DominatedByCnt = FindDominatedByCount(_Chromosomes);
        }

        private Population(Population p)
        {
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

        public int CountDominations(Population Reference)
        {
            var refpareto = Reference.ParetoFront;
            var newcount = FindDominatedByCount(refpareto.Union(Chromosomes));
            int count = 0;
            foreach (var ch in Chromosomes)
                foreach (var refch in refpareto)
                    if (IsDominatedBy(ch, refch)) { count++; break; }

            return count;
        }
        public static Dictionary<IChromosome, int> FindDominatedByCount(IEnumerable<IChromosome> List)
        {
            return FindDominatedByCount(List, (ch, pos) => ch.Objectives[pos] * (1 + ch.FeasibilityError));
        }
        public static Dictionary<IChromosome, int> FindDominatedByCount(IEnumerable<IChromosome> List, Func<IChromosome, int, double> GetEval)
        {
            var RemainingList = List.OrderBy(l => GetEval(l, 0)).ToList();
            var Dic = new Dictionary<IChromosome, int>();

            int Level = 0;
            while (RemainingList.Count > 0)
            {
                var front = FindFrontier(RemainingList, 0, RemainingList.Count, GetEval).ToList();
                foreach (var i in front) { if (Dic.Keys.Contains(i)) throw new Exception("Test"); Dic.Add(i, Level); RemainingList.Remove(i); }
                Level++;
            }
            return Dic;
        }
        private static IEnumerable<IChromosome> FindFrontier(List<IChromosome> List, int From, int Count, Func<IChromosome, int, double> GetEval)
        {
            if (Count == 1) return List.Skip(From).Take(1); 

            var T = FindFrontier(List, From, Count / 2, GetEval);
            var B = FindFrontier(List, From + Count / 2, Count - (Count / 2), GetEval);

            var M = T.ToList();

            foreach (var i in B)
            {
                bool dominated = false;
                foreach (var j in T)
                    if (IsDominatedBy(i, j, GetEval)) { dominated = true; break; }
                if (!dominated) M.Add(i);
            }
            return M;
        }

        public static bool EqualsTo(IChromosome ch1, IChromosome ch2)
        {
            return Enumerable.Range(0, ch1.Objectives.Length).All(i => ch1.Objectives[i] == ch2.Objectives[i]) && ch1.FeasibilityError == ch2.FeasibilityError;
        }
        public static bool IsDominatedBy(IChromosome subject, IChromosome reference)
        {
            return IsDominatedBy(subject, reference, (ch, pos) => ch.Objectives[pos]);
        }
        private static bool IsDominatedBy(IChromosome subject, IChromosome reference, Func<IChromosome, int, double> GetEval)
        {
            int eq = 0;
            for (int i = 0; i < subject.Objectives.Length; i++)
                if (GetEval(subject, i) < GetEval(reference, i)) return false;
                else if (GetEval(subject, i) == GetEval(reference, i)) eq++;
            if (eq == subject.Objectives.Length) return false;
            return true;
        }

        public static Dictionary<IChromosome, double> CalcCrowdingDistance(IEnumerable<IChromosome> List)
        {
            var dic = new Dictionary<IChromosome, double>();
            foreach (var l in List) dic.Add(l, 0);

            int CountObjectives = List.First().Objectives.Length;

            for (int i = 0; i < CountObjectives; i++)
            {
                var orderedlist = List.OrderBy(l => l.Objectives[i]).ToList();
                var min = orderedlist.First().Objectives[i];
                var max = orderedlist.Last().Objectives[i];
                if (min == max) continue;
                dic[orderedlist.First()] += double.PositiveInfinity;
                dic[orderedlist.Last()] += double.PositiveInfinity;

                for (int j = 1; j < orderedlist.Count - 1; j++)
                    dic[orderedlist[j]] += (orderedlist[j + 1].Objectives[i] - orderedlist[j - 1].Objectives[i]) / (max - min);
            }
            return dic;
        }

        public static void CheckDominatedByCount(Dictionary<IChromosome, int> DomByCnt)
        {
            for (int level = 0; ; level++)
            {
                var currentlevel = DomByCnt.Where(kvp => kvp.Value == level).Select(kvp => kvp.Key).ToList();
                if (currentlevel.Count == 0)
                {
                    if (DomByCnt.Where(kvp => kvp.Value >= level).Select(kvp => kvp.Key).Count() > 0)
                        throw new Exception("Incorrect level type.");
                    break;
                }
                var loewrlevels = DomByCnt.Where(kvp => kvp.Value > level).Select(kvp => kvp.Key).ToList();
                foreach (var c in currentlevel)
                    if (loewrlevels.Any(l => IsDominatedBy(c, l, (ch, pos) => ch.Objectives[pos] * (1 + ch.FeasibilityError)))) 
                        throw new Exception("Dominated by lower levels.");
            }            
        }

        public Population FindChildPopulation(double CrossOverProb, double MutationProb)
        {
            var rand = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds);

            Evaluate();

            var list = _Chromosomes.Select(c => c.Clone()).OrderBy(c => rand.NextDouble()).ToList();

            var numitems = Math.Min(PopulationSize, list.Count);
            for (int i = 1; i < numitems; i += 2)
                if (rand.NextDouble() < CrossOverProb * 2)
                    list[i].CrossOver(list[i - 1]);

            for (int i = 0; i < numitems; i += 2)
                if (rand.NextDouble() < MutationProb * 2)
                    list[i].Mutate();

            Evaluate(list);

            var Union = list.Concat(_Chromosomes).Distinct().ToList();
            if (Union.Where(l => l.FeasibilityError < 1).Count() >= PopulationSize)
                Union = Union.Where(l => l.FeasibilityError < 1).ToList();
            else
                Union = Union.OrderBy(l => l.FeasibilityError).Take(PopulationSize).ToList();

            var DominatedByCnt = FindDominatedByCount(Union);

            List<IChromosome> pop;

            if (Union.Count > PopulationSize)
            {
                pop = new List<IChromosome>();
                int level = 0;
                while (pop.Count < PopulationSize)
                {
                    var currentlevel = DominatedByCnt.Where(kvp => kvp.Value == level).Select(kvp => kvp.Key).ToList();
                    if (currentlevel.Count == 0)
                    {
                        if (pop.Count == 0) throw new Exception("No feasible solution was found.");
                        while (pop.Count < PopulationSize)
                        {
                            var ch = pop[rand.Next(pop.Count)];
                            var clone = ch.Clone();
                            DominatedByCnt.Add(clone, DominatedByCnt[ch]);
                            pop.Add(clone);
                        }
                        break;
                    }
                    if (pop.Count + currentlevel.Count <= PopulationSize)
                        pop.AddRange(currentlevel);
                    else
                    {
                        if (currentlevel.Where(c => c.FeasibilityError <= 0).Count() + pop.Count >= PopulationSize)
                            currentlevel = currentlevel.Where(c => c.FeasibilityError <= 0).ToList();

                        var dis = CalcCrowdingDistance(currentlevel);
                        pop.AddRange(currentlevel.OrderByDescending(d => dis[d]).Take(PopulationSize - pop.Count));
                    }
                    level++;
                }
            }
            else
                pop = Union;

            /*Console.WriteLine("Non-Dominated: {0}" + ", Newly dominated: {1}", 
                pop.Count(p => DominatedByCnt[p] == 0),
                //_Chromosomes.Count(c => c.FeasibilityError < 1 && !pop.Any(p => EqualsTo(p, c) || IsDominatedBy(c, p, PopulationAnalysis.GetVal))),
                //_Chromosomes.Count(c => c.FeasibilityError < 1 && !pop.Any(p => IsDominatedBy(c, p, PopulationAnalysis.GetVal)) && DominatedByCnt[c] > 0),
                this.DominatedByCnt != null ? _Chromosomes.Count(c => this.DominatedByCnt[c] == 0 && pop.Any(p => IsDominatedBy(c, p, PopulationAnalysis.GetVal))) : -1
                );*/

            var n = new Population(this);
            n._Chromosomes = pop.ToArray();
            n.DominatedByCnt = DominatedByCnt;
            n.FindExtremes(this.Extremes == null ? Union : this.Extremes.Union(n._Chromosomes).ToList());
            
            return n;
        }

        public void Evaluate()
        {
            Evaluate(_Chromosomes);
        }
        public void Evaluate(IEnumerable<IChromosome> list)
        {
            //System.Threading.Tasks.Parallel.ForEach<IChromosome>(list,  ch => ch.Evaluate());
            foreach (var p in list) p.Evaluate();
        }

        public void FindExtremes(IList<IChromosome> Items)
        {
            var numobjectives = Items.First().Objectives.Length;
            Extremes = new IChromosome[numobjectives];
            for (int i = 0; i < numobjectives; i++)
                Extremes[i] = PopulationAnalysis.FindExtreme(Items, i);
        }

        public PopulationAnalysis GetAnalysis()
        {
            return new PopulationAnalysis(this, DominatedByCnt, Extremes);
        }

        public IChromosome[] Extremes; 
        public Dictionary<IChromosome, int> DominatedByCnt;
        public IChromosome[] Chromosomes { get { return _Chromosomes; } }
        public int PopulationSize { get { return _PopulationSize; } }
        public IChromosome[] ParetoFront { get { return Chromosomes.Where(p => DominatedByCnt[p] == 0).ToArray(); } }
    }

    public class PopulationAnalysis
    {
        public PopulationAnalysis(Population Pop, Dictionary<IChromosome, int> DominatedByCnt,
            IEnumerable<IChromosome> OldExtremes)
        {
            this.Population = Pop;
            ParetoFront = Pop.Chromosomes.Where(p => DominatedByCnt[p] == 0).ToArray();

            var numobjectives = ParetoFront[0].Objectives.Length;
            double ExtremeDistance = 0;
            var union = OldExtremes.Union(ParetoFront).ToList();
            //var union = ParetoFront;

            var range = CalcRange(union);

            for (int i = 0; i < numobjectives; i++)
            {
                var newext = FindExtreme(ParetoFront, i);
                var oldext = FindExtreme(union, i);
                var popext = FindExtreme(Pop.Chromosomes, i);
                ExtremeDistance += Math.Sqrt(Enumerable.Range(0, numobjectives).Select((ind) => Math.Abs(GetVal(newext, ind) - GetVal(oldext, ind)) / range[ind]).Sum());
            }
            var crowddis = Population.CalcCrowdingDistance(ParetoFront).Where(keypair => !double.IsInfinity(keypair.Value))
                .ToDictionary(keypair => keypair.Key, keypair => keypair.Value);

            var avgdist = crowddis.Count > 0 ? crowddis.Average(pair => pair.Value) : 0;
            AverageDistance = avgdist;

            if (crowddis.Count > 1 && (avgdist > 0 || ExtremeDistance > 0))
            {
                var avgdiff = Math.Sqrt(crowddis.Sum(pair => Math.Pow(pair.Value - avgdist, 2))) / Math.Sqrt(crowddis.Count);
                ParetoSpread = (ExtremeDistance + avgdiff) / (ExtremeDistance + crowddis.Count * avgdist);
            }
            else
                ParetoSpread = ExtremeDistance;

            AverageObjectives = Pop.Chromosomes[0].Objectives.Select((v, i) => Pop.Chromosomes.Average(c => c.Objectives[i])).ToArray();
        }
        public Population Population { get; private set; }
        public IChromosome[] ParetoFront { get; private set; }
        public double ParetoSpread { get; private set; }
        public double AverageDistance { get; private set; }
        public double[] AverageObjectives { get; private set; }

        public static IChromosome FindExtreme(IList<IChromosome> List, int ObjIndex)
        {
            int minpos = 0;
            for (int i = 1; i < List.Count; i++)
                if (GetVal(List[i], ObjIndex) < GetVal(List[minpos], ObjIndex)) minpos = i;
            return List[minpos];
        }
        public static double[] CalcRange(IEnumerable<IChromosome> List)
        {
            double[] Range = new double[List.First().Objectives.Length];
            for (int i = 0; i < Range.Length; i++)
            {
                double Min = List.Min(ch => GetVal(ch, i));
                double Max = List.Max(ch => GetVal(ch, i));
                Range[i] = Max - Min;
            }
            return Range;
        }

        public static Func<IChromosome, int, double> GetVal = (ch, ObjIndex) => ch.Objectives[ObjIndex] * (1 + ch.FeasibilityError);
    }
}

using FuzzyRiskNet.Fuzzy;
using FuzzyRiskNet.FuzzyRisk;
using FuzzyRiskNet.MetaHeuristics.Core;
using FuzzyRiskNet.MetaHeuristics.GA;
using FuzzyRiskNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyRiskNet.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var ProjectID = SelectProjectID();

            var DB = new RiskDbContext();
            var p = DB.Projects.Find(ProjectID);

            var Log = Console.Out;

            Optimise(ProjectID, DB, Log);

            Console.ReadLine();
        }

        private static void Optimise(int ProjectID, RiskDbContext DB, System.IO.TextWriter Log)
        {
            // Use default configuration.
            var analysis = new ScenarioAnalysis(DB, ProjectID);
            var defu = analysis.ExpectedInopLoss();
            Log.Write("Starting loss: {0}\r\n", defu.ToString("F2"));

            var listuparam = analysis.Parameters
                .Where(p2 => p2.Value.InitValue.A < p2.Value.InitValue.C).Select(p2 => p2.Value).ToArray();

            var listsparam = analysis.Parameters.Where(p => p.Value.Key.Contains("Dep"))
                .Select(p2 => p2.Value).ToArray();

            Func<int[], int[], TFN, double> CalcCost = (red, ured, loss) =>
                {
                    var error = (loss.C - loss.A) / loss.B;
                    var errorcost = (Math.Max(Math.Min(error, 2.5), 0.5) - 0.5) / 2 * (defu.B / 100);
                    return errorcost + red.Count(r => r > 0) * (defu.B / 200) + ured.Count(r => r > 0) * (defu.B / 1000) + loss.B;
                };

            var startcost = CalcCost(new int[0], new int[0], defu);

            double ParamMultiplier = 0.5D;

            var arg = new BasicSODiscreteDecisionParams((dic) =>
            {
                var red = dic["Reductions"];
                var ured = dic["UReductions"];
                var loss = analysis.SensitivityCombined(ParamMultiplier, red.Where(r => r > 0).Select(ind => listsparam[ind - 1]).ToArray(), 0.5D, ured.Where(r => r > 0).Select(ind => listuparam[ind - 1]).ToArray()).GetLoss();
                return CalcCost(red, ured, loss);
            },
                new DiscreteDecisionParamDef("UReductions", Enumerable.Range(0, 5).Select(u => listuparam.Length).ToArray()),
                new DiscreteDecisionParamDef("Reductions", Enumerable.Range(0, 5).Select(u => listsparam.Length).ToArray()));

            var sb = new StringBuilder();

            var ga = new SOGA<ArrayChromosome>(arg) { PopulationSize = 200, MaximumGeneration = 200 };
            int rep = 0;
            ga.OnNewPopulation = (pop, time) =>
            {
                var dic = arg.FillIntDic(pop.BestChromosome as ArrayChromosome);
                var red = dic["Reductions"];
                var ured = dic["UReductions"];
                var loss = analysis.SensitivityCombined(ParamMultiplier, red.Where(r => r > 0).Select(ind => listsparam[ind - 1]).ToArray(), 0.5D, ured.Where(r => r > 0).Select(ind => listuparam[ind - 1]).ToArray()).GetLoss();
                Log.Write("Gen: {0} Best: {1} ({5:F2}% V {3:F2}% U {2:F2}%) TFN: {4} \r\n", rep++, pop.BestChromosome.Objectives[0], 100D * (defu.C - defu.A - loss.C + loss.A) / (defu.C - defu.A), 100D * (defu.B - loss.B) / defu.B, loss.ToString("F2"), 100D * (startcost - pop.BestChromosome.Objectives[0]) / startcost);
                Log.Write("Reductions: \r\n {0} \r\n", string.Join("\r\n", red.Where(r => r > 0).Select(ind => "\t" + listsparam[ind - 1].Title)));
                Log.Write("Uncertainty Reductions: \r\n {0} \r\n", string.Join("\r\n", ured.Where(r => r > 0).Select(ind => "\t" + listuparam[ind - 1].Title)));
                return true;
            };
            ga.Run();
        }

        static int SelectProjectID()
        {
            var db = new RiskDbContext();

            var projects = db.Projects.ToArray();

            int val;
            string valstr;
            do 
            {
                Console.WriteLine("Please select the project: ");
                for (int i = 0; i < projects.Length; i++)
                    Console.WriteLine(string.Format("{0}) {1}", i + 1, projects[i].Name));
                valstr = Console.ReadLine();
            } while(!int.TryParse(valstr, out val) || val < 1 || val > projects.Length);

            return projects[val - 1].ID;
        }
    }
}

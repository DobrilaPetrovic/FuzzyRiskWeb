using FuzzyRiskNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyRiskNet.Fuzzy
{
    public class FuzzyBSC
    {
        public FuzzyBSC()
        {

        }

        public Result[] GenerateResults(RiskDbContext DB, int ProjectID)
        {
            var proj = DB.Set<Project>().Find(ProjectID);
            var nodes = proj.Nodes.ToArray();
            var criteria = proj.Criteria.ToArray();
            var roles = nodes.Select(n => n.Role).Where(n => n != null).Distinct().ToArray();
            var gpnconfigs = proj.GPNConfigurations.ToArray();

            return gpnconfigs.Select(gpn => 
                CreateResultByChilds(gpn.Name, 1D, 
                    roles.Select(r => CreateResultByChilds(r.Name, GetCriteriaWeight(r, gpn.ID), 
                        nodes.Where(n => n.RoleID == r.ID).Select(n => CreateResultByChilds(n.Name, GetCriteriaWeight(r, gpn.ID, n.ID), r.Childs.Select(c => GetNodeResults(c, n)).ToArray())).ToArray()))
                    .ToArray())
                ).ToArray();
        }

        private static Result GetNodeResults(Criteria ParentCriteria, Node Node)
        {
            if (ParentCriteria.Childs.Any())
            {
                return CreateResultByChilds(ParentCriteria.Name, GetCriteriaWeight(ParentCriteria), ParentCriteria.Childs.Select(c => GetNodeResults(c, Node)).ToArray());
            }
            else
            {
                return new Result() { Name = ParentCriteria.Name, Weight = GetCriteriaWeight(ParentCriteria), Value = Normalise(GetCriteriaValue(ParentCriteria, Node.ID), ParentCriteria.Min, ParentCriteria.Max), Childs = new Result[0] };
            }
        }

        private static double GetCriteriaWeight(Criteria Criteria, int? GPNID = null, int? NodeID = null)
        {
            var w2 = Criteria.Weights.FirstOrDefault(w => w.GPNConfigurationID == GPNID && w.NodeID == NodeID);
            return w2 == null ? 0D : w2.Weight;
        }

        private static TFN GetCriteriaValue(Criteria Criteria, int NodeID)
        {
            var w2 = Criteria.Values.FirstOrDefault(w => w.NodeID == NodeID);
            return w2 == null ? new TFN() : w2.Value;
        }

        private static Result CreateResultByChilds(string Name, double Weight, Result[] Childs)
        {
            return new Result() { Name = Name, Weight = Weight, Childs = Childs, Value = WeightedSum(Childs.Select(c => Tuple.Create(c.Weight, c.Value))) };
        }

        public static TFN WeightedSum(IEnumerable<Tuple<double, TFN>> Values)
        {
            var Sum = new TFN();
            foreach (var v in Values)
                Sum += v.Item2 * v.Item1;
            return Sum;
        }

        public static TFN Normalise(TFN Value, double Min, double Max)
        {
            var range = Max - Min;
            return new TFN(((range > 0 ? Value.A : Value.C) - Min) / range, (Value.B - Min) / range, ((range > 0 ? Value.C : Value.A) - Min) / range);
        }

        public class Result
        {
            public string Name { get; set; }
            public TFN Value { get; set; }
            public double Weight { get; set; }
            public ICollection<Result> Childs { get; set; }

            public string MakeHTMLString()
            {
                return string.Format("Name: {0} Value: {1} Weight: {2} <br/> \r\n {3}\r\n", Name, Value.ToString("F3"), Weight, Childs.Any() ? ("<Blockquote>" + string.Join("\r\n", Childs.Select(c => c.MakeHTMLString())) + "</Blockquote>") : "");
            }
        }
    }
}

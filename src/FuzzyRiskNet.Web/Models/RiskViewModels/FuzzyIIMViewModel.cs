using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using Nik.Helpers;
using FuzzyRiskNet.FuzzyRisk;
using System.Web.Mvc;
using FuzzyRiskNet.Libraries.Helpers;

namespace FuzzyRiskNet.Models
{
    public class FuzzyIIMViewModel
    {
        public FuzzyIIM IIM { get; set; }
        public FuzzyIIMForm Form { get; set; }

        public IEnumerable<SelectListItem> Scenarios { get; set; }
        public IEnumerable<SelectListItem> GPNConfigs { get; set; }

        public int? GPNConfigID { get; set; }
    }

    public class FuzzyIIMForm : CRUDForm<Dictionary<int, double>>
    {
        public List<Node> Nodes { get; private set; }

        public bool ShowSliders { get; set; }

        public FuzzyIIMForm(List<Node> Nodes, bool ShowSliders = true) { this.Nodes = Nodes; this.ShowSliders = ShowSliders; }

        public override IEnumerable<IFormField> ListMainFields()
        {
            foreach (var n in Nodes)
            {
                var node = n;
                yield return this.CreateNumberField("Node" + n.ID, "Change in Perturbation for " + n.Name, DefaultValue: "0").Do(f =>
                    {
                        f.CustomGetObject = (dic, f2) => dic.ContainsKey(node.ID) ? dic[node.ID].ToString() : "0";
                        f.CustomSetObject = (dic, f2, v) => { if (dic.ContainsKey(node.ID)) dic[node.ID] = string.IsNullOrEmpty(v) ? 0D : double.Parse(v); else dic.Add(node.ID, string.IsNullOrEmpty(v) ? 0D : double.Parse(v)); };
                        f.IsVisible = ShowSliders;
                    });
            }

        }
    }
}
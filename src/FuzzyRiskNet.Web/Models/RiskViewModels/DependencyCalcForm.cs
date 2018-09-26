using FuzzyRiskNet.Models.GridForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nik.Helpers;
using FuzzyRiskNet.Libraries.Forms;
using FuzzyRiskNet.Fuzzy;
using System.Web.Mvc;
using FuzzyRiskNet.Libraries.Helpers;

namespace FuzzyRiskNet.Models
{
    public enum FuzzyLabels { VeryLow = 1, Low = 2, MildlyLow = 3, Medium = 4, MildlyHigh = 5, High = 6, VeryHigh = 7 }

    public class TFNCalcForm : RiskFlexForms<TFNCalcForm>
    {
        public TFNCalcForm(bool IsNumeric = false) { this.IsNumeric = IsNumeric; }
        public bool IsNumeric { get; private set; }

        public int? Value { get; set; }
        public int? Uncertainty { get; set; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Forms.IFormField> ListMainFields()
        {
            if (!IsNumeric) yield return this.CreateEnumDropDownField("Value", "Value", typeof(FuzzyLabels));
            else yield return this.CreateNumberField("Value", "Value");
            yield return this.CreateEnumDropDownField("Uncertainty", "Uncertainty", typeof(FuzzyLabels));
        }

        public TFN Calculate()
        {
            if (!IsNumeric)
            {
                var val = ((double)Value - 1) / 6;

                var unc = ((double)Uncertainty - 1) / 6;

                return new TFN(Math.Max(0, val - unc), val, Math.Min(1, val + unc));
            }
            else
            {
                var val = (double)Value;

                var unc = ((double)Uncertainty - 1) / 6;

                unc = val * unc;

                return new TFN(Math.Max(0, val - unc), val, val + unc);
            }
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield break;
        }
    }

    public class TFNDepCalcForm : RiskFlexForms<TFNDepCalcForm>
    {
        public TFNDepCalcForm()
        {
            Value = new Dictionary<string, int>();
            Uncertainty = new Dictionary<string, int>();
            IsCost = new Dictionary<string, bool>();
        }

        public Dictionary<string, int> Value { get; set; }
        public Dictionary<string, int> Uncertainty { get; set; }
        public Dictionary<string, bool> IsCost { get; set; }

        public override IEnumerable<FuzzyRiskNet.Libraries.Forms.IFormField> ListMainFields()
        {
            var criteria = new Tuple<string, bool>[] 
            { 
                Tuple.Create("Trade Volume", false),
                Tuple.Create("Inventory", true),
                Tuple.Create("Substitutability of the Product", true),
                Tuple.Create("Substitutability of the Supplier/Customer", true),
                Tuple.Create("Lead-time", false),
                Tuple.Create("Distance", false),
                Tuple.Create("Information Transparency", true),
                Tuple.Create("Collaboration Agreement", true),
                Tuple.Create("Compatibility of IT Systems", true),
                //Tuple.Create("Security of Information Flow", true),
            };

            var list = Enum.GetValues(typeof(FuzzyLabels)).Cast<int>().Select(v => new SelectListItem() { Text = Enum.GetName(typeof(FuzzyLabels), v), Value = v.ToString() }).ToArray();


            foreach (var criterion in criteria)
            {
                IsCost.Add(criterion.Item1, criterion.Item2);

                yield return this.CreateSimpleDropDownField(criterion.Item1.Replace(" ", "$"), (criterion.Item2 ? "↓" : "↑") + " " + criterion.Item1, list, true).Do(f => 
                    {
                        f.CustomGetObject = (form, f2) => (Value.GetNullableValueSafe(criterion.Item1) ?? 0).ToString();
                        f.CustomSetObject = (form, f2, v) => Value.SetValueSafe(criterion.Item1, v.ParseIntSafe());
                    });
                yield return this.CreateSimpleDropDownField(criterion.Item1.Replace(" ", "$") + "Uncertainty", "Uncertainty" /* of " + criterion.Item1*/,  list, true).Do(f =>
                    {
                        f.CustomGetObject = (form, f2) => (Uncertainty.GetNullableValueSafe(criterion.Item1) ?? 0).ToString();
                        f.CustomSetObject = (form, f2, v) => Uncertainty.SetValueSafe(criterion.Item1, v.ParseIntSafe());
                    });
            }
        }

        public TFN Calculate()
        {
            var list = new List<TFN>();

            foreach (var key in IsCost.Keys)
                if (Value.ContainsKey(key) && Uncertainty.ContainsKey(key))
                {
                    var val = ((double)Value[key] - 1) / 6;

                    if (IsCost[key]) val = 1 - val;

                    var unc = ((double)Uncertainty[key] - 1) / 6;

                    list.Add(new TFN(Math.Max(0, val - unc), val, Math.Min(1, val + unc)));
                }

            list.Sort((l, l2) => l.B - l2.B > 0 ? -1 : l.B == l2.B ? 0 : 1);

            var agg = new TFN();

            for (int i = 0; i < list.Count; i++)
                agg = agg + list[i] * (double)(list.Count - i);

            agg = agg * (2D / (list.Count * (list.Count + 1)));

            return agg;
        }

        public override IEnumerable<Tuple<IFormField, string>> GetCustomValidations()
        {
            foreach (var v in base.GetCustomValidations()) yield return v;

            if (HasObjectSet)
            {
                foreach (var key in IsCost.Keys)
                    if (Value.ContainsKey(key) ^ Uncertainty.ContainsKey(key))
                        yield return Tuple.Create(FindField(key.Replace(" ", "$")) as IFormField, "Both value and uncertainty of " + key + " should be either filled or empty.");

                if (Value.Keys.Count == 0)
                    yield return Tuple.Create(FindField(IsCost.Keys.First().Replace(" ", "$")) as IFormField, "At least one criterion should be entered.");
            }
        }

        public override IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink("Generic Estimation", Url.Action("TFNCalc"));
        }
    }
}
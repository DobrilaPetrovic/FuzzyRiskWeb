using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FuzzyRiskNet.Models
{
    public static class Helpers
    {
        static CultureInfo culture = CultureInfo.GetCultureInfo("en-GB"); 
        public static IHtmlString FormatLoss(this HtmlHelper Html, TFN d)
        { 
            return Html.Raw(d.ToString((v, ind) => string.Format(ind == 1 ? "<b>{0}</b>" : "{0}", v.ToString("C0", culture))));
        }

        public static IHtmlString FormatInop(this HtmlHelper Html, TFN d)
        {
            return Html.Raw(d.ToString((v, ind) => string.Format(ind == 1 ? "<b>{0}</b>" : "{0}", v.ToString("0.###"))));
        }

        public static IHtmlString FormatDep(this HtmlHelper Html, TFN d)
        {
            if (d.IsZero) return Html.Raw("---");
            return Html.Raw(d.ToString((v, ind) => string.Format(ind == 1 ? "<b>{0}</b>" : "{0}", v.ToString("0.###"))));
        }

        public static IHtmlString FormatUncertainty(this HtmlHelper Html, double Value)
        {
            return Html.Raw(string.Format("{0}", Value.ToString("0")));
        }

        public static IHtmlString FormatUncertaintyReduction(this HtmlHelper Html, double Value, double Base, bool PositiveReduction = true)
        {
            return Html.Raw(string.Format("{0}%", ((PositiveReduction ? 1 : - 1) * 100D * (Base - Value) / Base).ToString("0")));
        }

    }
}
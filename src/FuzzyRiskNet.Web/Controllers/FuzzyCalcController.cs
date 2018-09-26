using FuzzyRiskNet.Fuzzy;
using FuzzyRiskNet.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace FuzzyRiskNet.Controllers
{
    public class FuzzyCalcController : AppControllers
    {
        public ActionResult Index(string Parameters, string Formula)
        {
            SetTitle("Fuzzy Calculator");
            var firstexp = MFFuncExamples.Examples[0];
            Parameters = Parameters ?? firstexp.Parameters;
            Formula = Formula ?? firstexp.Formula;

            ViewBag.Parameters = Parameters;
                ViewBag.Formula = Formula;
            try
            {
                var model = MFFuncParser.Parse(Parameters);
                try
                {
                    model.ParseFunction(Formula);
                    ViewBag.MFFunc = model;
                }
                catch (Exception exp2)
                {
                    this.ModelState.AddModelError("Formula", exp2.Message);
                }
            }
            catch (Exception exp) 
            {
                this.ModelState.AddModelError("Parameters", exp.Message);
            }
            ViewBag.Examples = MFFuncExamples.Examples;
            return View(ViewBag);
        }

        public ActionResult MFImage(string Parameters, string Formula, bool Simulate = true, int Width = 1000, int Height = 300)
        {
            /*try
            {*/
                var model = MFFuncParser.Parse(Parameters);
                model.ParseFunction(Formula);
                var file = MFChart.GenerateChart(model.MFs.ToArray(), model.Calculate, Simulate, Formula, Width, Height);
                return File(file, "image/png");
            /*}
            catch
            { return File(new byte[0], "image/png"); }*/
        }
    }
}

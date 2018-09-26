using FuzzyRiskNet.Models;
using FuzzyRiskNet.Models.GridForms;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Net;

namespace FuzzyRiskNet.Controllers
{
    [Authorize]
    public class IndicatorsController : AppControllers
    {
        public ActionResult IndexCountries()
        {
            return ViewGrid(new CountrysGrid().Init(this));
        }

        dynamic DownloadJson(string Url)
        {
            string value = new WebClient().DownloadString(Url);

            return new JavaScriptSerializer().DeserializeObject(value);
        }

        public ActionResult UpdateCountries()
        {
            dynamic obj = DownloadJson("http://api.worldbank.org/country?per_page=1000&format=json");

            foreach (var c in obj[1])
            {
                string id = c["id"];
                string name = c["name"];

                var dbc = DB.Countries.FirstOrDefault(c2 => c2.Code == id);
                if (dbc == null)
                {
                    dbc = new Country() { Code = id };
                    DB.Countries.Add(dbc);
                }
                dbc.Name = name;
            }
            DB.SaveChanges();

            return RedirectToAction("IndexCountries");
        }

        public ActionResult EditCountry(int ID)
        {
            return EditInsertForm(new CountryForm(ID));
        }

        public ActionResult DeleteCountry(int ID)
        {
            return DeleteForm(new CountryForm(ID));
        }


        public ActionResult IndexIndicators()
        {
            return ViewGrid(new IndicatorsGrid() { ShowInsert = true, InsertActionName = "InsertIndicator" }.Init(this));
        }

        public ActionResult InsertIndicator(string Code)
        {
            if (!IsGet && ModelState.IsValid)
            {
                string descjson = new WebClient().DownloadString("http://api.worldbank.org/indicator/" + Code + "?format=json");
                dynamic desc = new JavaScriptSerializer().DeserializeObject(descjson);
                if (!desc[0].ContainsKey("message"))
                {
                    var ind = DB.Indicators.FirstOrDefault(i => i.Code == Code);
                    if (ind == null)
                    {
                        DB.Indicators.Add(ind = new Indicator() { Code = Code });
                    }

                    ind.Name = desc[1][0]["name"];
                    ind.JsonDescription = descjson;
                    ind.DataSource = DataSources.WorldBank;

                    string datajson = new WebClient().DownloadString("http://api.worldbank.org/countries/all/indicators/" + Code + "?format=json&date=2010:2015&per_page=20000&mrv=3");

                    ind.JsonData = datajson;
                    DB.SaveChanges();

                    return RedirectToAction("IndexIndicators");
                }
                else
                    ModelState.AddModelError("Code", "Code is invalid.");
            }
            return View();
        }

        public ActionResult ViewIndicator(int ID)
        {
            var ind = DB.Indicators.Find(ID);
            Title = ind.Name;
            dynamic vals = new JavaScriptSerializer().DeserializeObject(ind.JsonData);
            var countries = DB.Countries.ToArray();

            var items = countries.SelectMany(c => ((object[])vals[1]).Cast<Dictionary<string, dynamic>>().Where(v => v["country"]["value"] == c.Name).Select(v => new { c = c, v = v })).ToArray();
            var items2 = items
                .Select(c => new ViewIndTuple() { Country = c.c, Value = ParseDec(c.v["value"]), Date = c.v["date"] }).ToArray();

            return ViewGrid(new ViewIndicatorGrid() { Source = items2.AsQueryable() }.Init(this));
        }

        decimal? ParseDec(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            return decimal.Parse(val);
        }

        public ActionResult EditIndicator(int ID)
        {
            return EditInsertForm(new IndicatorForm(ID));
        }

        public ActionResult DeleteIndicator(int ID)
        {
            return DeleteForm(new IndicatorForm(ID));
        }
    }
}

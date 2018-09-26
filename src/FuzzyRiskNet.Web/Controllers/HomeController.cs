using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FuzzyRiskNet.Controllers
{
    public class HomeController : AppControllers
    {
        public ActionResult Index()
        {
            SetTitle("Home Page");
            return View();
        }

        public ActionResult About()
        {
            SetTitle("About Us");
            return View();
        }
    }
}
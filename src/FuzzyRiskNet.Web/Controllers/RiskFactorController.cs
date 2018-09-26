using FuzzyRiskNet.Models;
using FuzzyRiskNet.Models.GridForms;
using System.Web.Mvc;

namespace FuzzyRiskNet.Controllers
{
    [Authorize]
    public class RiskFactorController : AppControllers
    {
        public ActionResult Index()
        {
            return ViewGrid(new RiskFactorGrid() { ShowInsert = true, InsertActionName = "Insert" }.Init(this));
        }

        public ActionResult Insert()
        {
            return EditInsertForm(new RiskFactorForm(null));
        }

        public ActionResult Edit(int ID)
        {
            return EditInsertForm(new RiskFactorForm(ID));
        }

        public ActionResult Delete(int ID)
        {
            return DeleteForm(new RiskFactorForm(ID));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using FuzzyRiskNet.Models;
using FuzzyRiskNet.Libraries.Forms;
using FuzzyRiskNet.Libraries.Grid;
using System.Web.Mvc;
using FuzzyRiskNet.Models.GridForms;
using FuzzyRiskNet.Libraries;

namespace FuzzyRiskNet.Controllers
{
    public abstract class AppControllers : Controller, IRiskContext
    {
        public AppControllers() { DB = new RiskDbContext(); }


        public RiskDbContext DB { get; private set; }

        protected ActionResult ViewHtml(string Html)
        {
            ViewBag.Html = Html;
            return View("ViewHtml");
        }

        public string Title
        {
            get;
            set;
        }

        public void SetTitle(string title) { this.Title = title; }

        protected override void OnActionExecuted(ActionExecutedContext context)
        {
            ViewBag.DB = DB;
            ViewBag.CurrentUser = CurrentUser;
            ViewBag.Title = Title;
            base.OnActionExecuted(context);
        }

        #region UserInfo
        private ApplicationUser _CurrentUser = null;
        public ApplicationUser CurrentUser
        {
            get
            {
                if (_CurrentUser == null)
                {
                    if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                        _CurrentUser = DB.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                    else
                        _CurrentUser = null;
                }
                return _CurrentUser;
            }
        }

        protected void InitForm(FuzzyRiskNet.Libraries.Forms.IFormModel Form)
        {
            if (Form is IFormHasSettableDbContext) (Form as IFormHasSettableDbContext).DB = DB;
            if (Form is IHasSettableContext) (Form as IHasSettableContext).CurrentUser = CurrentUser;

        }
        #endregion

        #region Grid Methods

        protected string GetAndSaveGridParameter(string Name)
        {
            string Value = Request.QueryString[Name];
            return Value;
        }

        protected ActionResult ViewGrid(IGridIndex Grid)
        {
            if (Grid.IsHandleParameters)
            {
                if (Grid.FilterModel == null) throw new Exception("FilterModel should not be null");

                Grid.FilterModel.FillValues(GetAndSaveGridParameter);

                Grid.ColumnsModel.Page = S.SafeParseInt(GetAndSaveGridParameter("page")) ?? 1;
                var column = GetAndSaveGridParameter("Column");
                Grid.ColumnsModel.Sort = column ?? Grid.DefaultSortColumn;
                Grid.ColumnsModel.IsAsc = column != null ? GetAndSaveGridParameter("Direction") == "Ascending" : Grid.DefaultSortIsAsc;
            }

            return View("GridIndex", Grid);
        }

        #endregion 
        #region FormMethods
        public bool IsGet { get { return Request.HttpMethod.ToUpper() == "GET"; } }

        protected ActionResult DeleteForm<T>(CRUDForm<T> Form) where T : class, new()
        {
            return DeleteForm(Form, RedirectToAction("Index"));
        }
        protected ActionResult DeleteForm<T>(CRUDForm<T> Form, ActionResult ReturnResult) where T : class, new()
        {
            if (IsGet) return ViewHtml("Unauthorised.");
            InitForm(Form);
            Form.Delete();

            return ReturnResult;
        }

        string SetQueryValue(string Url, string ParamName, string Value)
        {
            var query = Url.Contains("?") ? Url.Substring(Url.IndexOf("?") + 1) : "";
            var path = Url.Contains("?") ? Url.Substring(0, Url.IndexOf("?")) : Url;
            var dic = System.Web.HttpUtility.ParseQueryString(query);
            dic[ParamName] = Value;
            return path + "?" + string.Join("&", dic.Keys.Cast<string>().Select(k => k + "=" + dic[k]).ToArray());
        }

        protected ActionResult EditInsertForm<T>(CRUDForm<T> Form)
            where T : class, new()
        {
            return EditInsertForm(Form, Redirect(SetQueryValue(Request.Url.ToString(), "editsuccess", "true")));
        }

        protected ActionResult EditInsertForm<T>(CRUDForm<T> Form, ActionResult ReturnResult)
            where T : class, new()
        {
            return EditInsertForm(Form, (f) => ReturnResult);
        }

        protected ActionResult EditInsertForm<T>(CRUDForm<T> Form, Func<CRUDForm<T>, ActionResult> GetReturnResult)
            where T : class, new()
        {
            if (IsGet && Request.QueryString["editsuccess"] == "true") ViewData["FormMessage"] = Messages.EditSuccessfulMessage;

            InitForm(Form);
            bool IsInsert = Form.IsInsert;
            if (Title == null) SetTitle(IsInsert ? Messages.Inserting : Messages.Editing);

            if (IsGet)
            {
                Form.LoadForm();
                return View(Form.InsertEditViewName, Form);
            }
            else
            {
                bool result = Form.TrySave(Request.Form);

                return result ? GetReturnResult(Form) : View(Form.InsertEditViewName, Form);
            }
        }

        protected ActionResult FlexForm<T>(T ObjectForm, Func<T, ActionResult> OnSuccess) where T : FlexForm<T>
        {
            if (OnSuccess == null) throw new ArgumentNullException("OnSuccess");
            if (ObjectForm == null) throw new ArgumentNullException("Form");
            ObjectForm.Object = ObjectForm;
            InitForm(ObjectForm);
            if (IsGet)
            {
                ObjectForm.GetObject();
                return View(ObjectForm.ViewName, ObjectForm);
            }
            else
            {
                var f = new NameValueCollection();
                foreach (string k in Request.Form.Keys) f.Add(k, string.Join(",", Request.Form[k]));
                ObjectForm.SetForm(Request.Form);
                if (ObjectForm.IsValid)
                {
                    ObjectForm.SetObject();
                    if (ObjectForm.IsValid)
                    {
                        return OnSuccess(ObjectForm.Object);
                    }
                }
                return View(ObjectForm.ViewName, ObjectForm);
            }
        }

        protected ActionResult SerializableForm(ISerializableForm Form, String CurrentValue, Func<string, ActionResult> OnSuccess)
        {
            if (OnSuccess == null) throw new ArgumentNullException("OnSuccess");
            if (Form == null) throw new ArgumentNullException("Form");
            InitForm(Form);
            if (IsGet)
            {
                Form.GetObject(CurrentValue);
                return View(Form.ViewName, Form);
            }
            else
            {
                Form.SetForm(Request.Form);
                if (Form.IsValid)
                {
                    var res = Form.SetObject();
                    if (Form.IsValid)
                    {
                        return OnSuccess(res);
                    }
                }
                return View(Form.ViewName, Form);
            }
        }

        #endregion

    }
}

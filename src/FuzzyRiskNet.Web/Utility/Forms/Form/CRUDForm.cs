using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Collections.Specialized;
using FuzzyRiskNet.Libraries.Grid;
using MVCFormsLibrary;
using FuzzyRiskNet.Libraries.Forms;
using FuzzyRiskNet.Helpers;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.Owin;

namespace FuzzyRiskNet.Libraries.Forms
{
    public interface IPageFormModel : IFormModel
    {
        IEnumerable<FormLink> CreateFormLinks(UrlHelper Url);
    }

    public interface ICRUDFormModel : IPageFormModel
    {
    }
    
    public interface IFormHasDbContext : IFormModel
    {
        DbContext DB { get; }
    }

    public interface IFormHasSettableDbContext : IFormHasDbContext
    {
        DbContext DB { get; set; }
    }

    public abstract class CRUDForm<T> : FormModel<T>, ICRUDFormModel, IFormHasSettableDbContext where T : class, new()
    {
        public virtual string InsertEditViewName { get { return "EditForm"; } }

        public DbContext DB { get; set; }

        protected Func<T> GetEditItem { get; set; }
        public bool IsInsert { get { return GetEditItem == null; } }

        [Obsolete("Use SetEditID or set the GetEditItem instead.")]
        protected Expression<Func<T, bool>> FilterItemExpression { set { GetEditItem = () => DB.Set<T>().FirstOrDefault(value); } }

        protected void SetEditID(int ID) { GetEditItem = () => DB.Set<T>().Find(ID); }
        protected void SetEditID(long ID) { GetEditItem = () => DB.Set<T>().Find(ID); }
        protected void SetEditID(Guid ID) { GetEditItem = () => DB.Set<T>().Find(ID); }

        public void LoadForm()
        {
            if (!IsInsert)
            {
                var b = GetEditItem();
                if (b == null) throw new Exception("Object does not exists.");
                GetObject(b);
            }
        }

        public T DbObject { get; private set; }
        public bool HasObjectSet { get { return DbObject != null; } }

        public bool TrySave(NameValueCollection RequestForm)
        {
            SetForm(RequestForm);
            if (IsValid)
            {
                var b = !IsInsert ? GetEditItem() : new T();
                if (!IsInsert && b == null) throw new Exception("Object does not exists.");
                DbObject = b;
                SetObject(b);
                if (IsInsert) DB.Set<T>().Add(b);
                BeforeSave(b);
                if (IsValid)
                {
                    DB.SaveChanges();
                    return true;
                }
                else return false;
            }
            return false;
        }

        public virtual void BeforeSave(T Obj) { }

        public void Delete()
        {
            var b = GetEditItem();
            DB.Set<T>().Remove(b);
            DB.SaveChanges();
        }

        public virtual IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink() { Link = Url.Action("Index"), Text = Messages.ReturnToListTitle }; 
        }

        public void DeserializeSimple(string FieldName, string Value)
        {
            var field = MainFields.First(f => f.FieldName == FieldName);
            if (field is SimpleTextBoxField<T>) (field as SimpleTextBoxField<T>).Deserialize(Value);
            else throw new NotImplementedException();
        }

        public override IEnumerable<Tuple<IFormField, string>> GetCustomValidations()
        {
            foreach (var e in base.GetCustomValidations()) yield return e;
        }
    }

    public class FormLink
    {
        public FormLink() { }
        public FormLink(string Text, string Link) { this.Text = Text; this.Link = Link; }
        public string Link { get; set; }
        public string Text { get; set; }
    }
}
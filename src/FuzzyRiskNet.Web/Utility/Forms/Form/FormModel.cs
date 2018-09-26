using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Linq.Expressions;
using Microsoft.Owin;

namespace FuzzyRiskNet.Libraries.Forms
{
    public interface IFormModel
    {
        IFormField[] MainFields { get; }
        //IEnumerable<Tuple<string, object>> Views { get; }
        void SetForm(NameValueCollection form, HttpFileCollection files = null);
        bool IsValid { get; }
        IEnumerable<Tuple<IFormField, string>> GetAllValidationErrors();
    }  

    public interface IFormModel<T> : IFormModel
    {
        void SetObject(T Obj);
        void GetObject(T Obj);
    }

    public abstract class FormModel<T> : IFormModel<T>
    {
        public FormModel()
        {
        }

        bool IsInitialized;

        public void Init()
        {            
            var listfields= ListMainFields();
            var allfields = listfields == null ? new IFormField[0] : listfields.ToArray();
            MainFields = allfields.Select(f => f != null ? f : new NewLineField()).ToArray();
        }

        void CheckInit()
        {
            if (!IsInitialized) { IsInitialized = true; Init(); }
        }

        IFormField[] _MainFields;
        public IFormField[] MainFields { get { CheckInit(); return _MainFields; } private set { _MainFields = value; } }

        public IEnumerable<IFormField> AllFields { get { foreach (var f in MainFields) foreach (var d in f.FieldWithDescendants()) yield return d; } }

        public virtual IEnumerable<IFormField> ListMainFields() { yield break; }

        public void SetObject(T Obj)
        {
            foreach (var f in AllFields) if (f is IFormField<T>) (f as IFormField<T>).SetObject(Obj);
        }

        public void GetObject(T Obj)
        {
            foreach (var f in AllFields) if (f is IFormField<T>) (f as IFormField<T>).GetObject(Obj);
        }

        public void SetForm(NameValueCollection form, HttpFileCollection files = null)
        {
            foreach (var f in AllFields)
                if (f is IRequiredMultiPartFormField)
                    (f as IRequiredMultiPartFormField).Deserialize(form, files, "");
                else 
                    f.Deserialize(form, "");
        }

        public virtual IEnumerable<Tuple<IFormField, string>> GetCustomValidations()
        {
            yield break;
        }

        public IEnumerable<Tuple<IFormField, string>> GetAllValidationErrors()
        {
            foreach (var f in AllFields) 
                foreach (var str in f.GetValidationErrors()) 
                    yield return new Tuple<IFormField, string>(f, str);
            foreach (var t in GetCustomValidations())
                yield return t;
        }

        public bool IsValid { get { return !GetAllValidationErrors().Any(); } }

        public IFormField<T> FindField(string Name) { return AllFields.Where(f => f is IFormField<T>).Cast<IFormField<T>>().First(f => f.FieldName == Name); }
    }
    
    public interface IFormField
    {
        string FieldName { get; }

        void Deserialize(NameValueCollection Form, string Scope);

        IEnumerable<string> GetValidationErrors();

        bool IsOptional { get; }

        bool IsVisible { get; }

        string ReadOnlyValue { get; }

        string ViewName { get; }
    }

    public interface ITitledFormField : IFormField
    {
        string Title { get; }
    }

    public interface IOrdinaryFormField : ITitledFormField
    {
        string GenerateFieldHtml(string Scope);
    }

    public interface IHasChildFormField : IFormField
    {
        IFormField[] Childs { get; }
    }

    public interface IFormField<T> : ITitledFormField
    {
        void SetObject(T Obj);

        void GetObject(T Obj);
    }
}
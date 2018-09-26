using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.Libraries.Forms
{
    public class LabelField : IOrdinaryFormField, IFormField
    {
        public string FieldName { get; set; }

        public string Title { get; set; }

        public string Value { get; set; }

        public virtual string ReadOnlyValue { get { return Value; } }

        public string GenerateFieldHtml(string Scope)
        {
            return Value;
        }

        public bool IsVisible { get; set; }

        public void Deserialize(NameValueCollection Form, string Scope)
        {
        }

        public IEnumerable<string> GetValidationErrors() { yield break; }

        public bool IsOptional { get { return true; } }

        public string ViewName { get; set; }
    }

    public class FixedWithLabelField<T> : IOrdinaryFormField, IFormField<T>
    {

        public string GenerateFieldHtml(string Scope)
        {
            return ReadOnlyValue;
        }

        public string FieldName { get; set; }

        public string Title { get; set; }

        public int? Value { get; set; }

        public string ReadOnlyValue { get; set; }

        public void Deserialize(NameValueCollection Form, string Scope)
        {
        }

        public bool IsOptional { get { return true; } }

        public bool IsVisible { get; set; }

        public string ViewName { get; set; }

        public virtual void SetObject(T Obj)
        {
            SimpleFormField<T, int?>.SetObject<int?>(Obj, FieldName, Value);
        }

        public virtual void GetObject(T Obj)
        {
            var newval = SimpleFormField<T, int?>.GetObject<int?>(Obj, FieldName);
            if (newval != null && newval != this.Value) throw new Exception("The fixed value is different from the object value.");
        }

        public IEnumerable<string> GetValidationErrors()
        {
            yield break;
        }
    }
}

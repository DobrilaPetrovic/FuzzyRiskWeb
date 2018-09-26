using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using System.Text;
using MVCFormsLibrary;
using FuzzyRiskNet.Libraries.Forms;
using Microsoft.Owin;

namespace FuzzyRiskNet.Libraries.Forms
{
    public abstract class SimpleFormField<T, T2> : IOrdinaryFormField, IFormField<T>
    {
        //public const string RequiredMessage = "{0} is required.";
        //public const string NonNumericalMessage = "Value entered for {0} is not numerical.";
        //public const string IncorrectDateMessage = "Date entered for {0} is in incorrect format.";
        //public const string ShouldBeInThePastMessage = "Value for {0} should be in the past while it is in future.";

        public SimpleFormField()
        {
            IsVisible = true;
        }
        public string FieldName { get; set; }

        public Type GetFieldType() { return typeof(T).GetProperty(FieldName).PropertyType; }

        public string Title { get; set; }

        public T2 Value { get; set; }

        public virtual string ReadOnlyValue { get { if (Value == null) return ""; else return Value.ToString(); } }

        public string ViewName { get; set; }

        public virtual string GenerateFieldHtml(string Scope) { return GenerateFieldHtml(Scope, "text", Value == null ? "" : Value.ToString()); }

        protected string GenerateFieldHtml(string Scope, string InputType, string Value)
        {
            return GenerateFieldHtml(Scope, InputType, Value, null);
        }
        protected string GenerateFieldHtml(string Scope, string InputType, string Value, string CssClass)
        {
            var cls = CustomCssClass ?? CssClass;
            return string.Format("<input type='{2}' id='{0}' name='{0}' value='{1}'{3} />", 
                Scope + FieldName, new HtmlString(Value).ToHtmlString(), InputType, cls != null ? " class='" + cls + "'" : "");
        }

        public virtual void Deserialize(NameValueCollection Form, string Scope)
        {
            Deserialize(Form[Scope + FieldName]);
        }
        public abstract void Deserialize(string Value);

        public Action<T, IFormField<T>, T2> CustomSetObject { get; set; }
        public Func<T, IFormField<T>, T2> CustomGetObject { get; set; }
        public string CustomCssClass { get; set; }

        public virtual void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else 
                SetObject<T2>(Obj, FieldName, Value);
        }

        public virtual void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else 
                this.Value = GetObject<T2>(Obj, FieldName);
        }

        public static void SetObject<VType>(T Obj, string FieldName, VType Value)
        {
            Obj.GetType().GetProperty(FieldName).SetValue(Obj, Value, null);
        }

        public static VType GetObject<VType>(T Obj, string FieldName)
        {
            return (VType)(Obj.GetType().GetProperty(FieldName).GetValue(Obj, null));
        }

        public virtual IEnumerable<string> GetValidationErrors()
        {
            if (!IsOptional && (Value == null || Value.Equals(default(T2)))) yield return string.Format(Messages.RequiredMessage, Title);
            yield break;
        }

        public bool IsOptional { get; set; }
        
        public virtual bool IsVisible { get; set; }
    }

    public class SimpleTextBoxField<T> : SimpleFormField<T, string>
    {
        public override void Deserialize(string Value) { this.Value = Value; }
        public override IEnumerable<string> GetValidationErrors()
        {
            if (!IsOptional && Value == "") yield return string.Format(Messages.RequiredMessage, Title);
            foreach (var str in base.GetValidationErrors()) yield return str;
        }
    }

    public class LTRTextBoxField<T> : SimpleTextBoxField<T>
    {
        public override string GenerateFieldHtml(string Scope) { return GenerateFieldHtml(Scope, "text", Value == null ? "" : Value.ToString(), "ltrfield"); }
    }

    public class MultiLineTextBoxField<T> : SimpleFormField<T, string>
    {
        public MultiLineTextBoxField() { }
        public bool WrapIsOff { get; set; }
        public string CssClass { get { return CustomCssClass; } set { CustomCssClass = value; } }
        public override void Deserialize(string Value) { this.Value = Value; }

        public override string GenerateFieldHtml(string Scope)
        {
            return string.Format("<textarea name='{0}' id='{0}' class='{2}'{3}>{1}</textarea>",
                Scope + FieldName, new HtmlString(Value).ToHtmlString(), CustomCssClass ?? "myformtextarea", WrapIsOff ? " wrap='off'" : "");
        }
    }

    public class SimpleCheckBoxField<T> : SimpleFormField<T, bool>
    {
        public override void Deserialize(string Value) 
        {
            this.Value = Value != null;
        }

        public override string GenerateFieldHtml(string Scope)
        {
            return string.Format("<input type='checkbox' id='{0}' name='{0}' {1} />", Scope + FieldName, Value ? "checked='checked'" : "");
        }

        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
            {
                var t = GetFieldType();
                if (t == typeof(bool?)) SetObject<bool?>(Obj, FieldName, Value);
                else SetObject<bool?>(Obj, FieldName, Value);
            }
        }

        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                var t = GetFieldType();
                if (t == typeof(bool?)) this.Value = GetObject<bool?>(Obj, FieldName) ?? false;
                else this.Value = GetObject<bool>(Obj, FieldName);
            }
        }

        public override string ReadOnlyValue
        {
            get
            {
                return Value ? "✓" : "X";
            }
        }
    }

    public class SimpleNumberField<T> : SimpleTextBoxField<T>
    {
        public SimpleNumberField()
        {
            Value = "0";
        }
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
            {
                var type = typeof(T).GetProperty(FieldName).PropertyType;
                if (type == typeof(decimal))
                    SetObject<decimal>(Obj, FieldName, Value.Trim() != "" ? decimal.Parse(Value) : 0);
                else if (type == typeof(decimal?))
                    SetObject<decimal?>(Obj, FieldName, Value.Trim() != "" ? (decimal?)decimal.Parse(Value) : null);
                else if (type == typeof(int))
                    SetObject<int>(Obj, FieldName, Value.Trim() != "" ? int.Parse(Value) : 0);
                else if (type == typeof(int?))
                    SetObject<int?>(Obj, FieldName, Value.Trim() != "" ? (int?)int.Parse(Value) : null);
            }
        }

        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                var type = typeof(T).GetProperty(FieldName).PropertyType;
                if (type == typeof(decimal))
                    this.Value = GetObject<decimal>(Obj, FieldName).ToString("g0");
                else if (type == typeof(decimal?))
                {
                    var val = GetObject<decimal?>(Obj, FieldName);
                    if (val.HasValue) this.Value = val.Value.ToString("g0"); else this.Value = "";
                }
                else if (type == typeof(int))
                    this.Value = GetObject<int>(Obj, FieldName).ToString("g0");
                else if (type == typeof(int?))
                {
                    var val = GetObject<int?>(Obj, FieldName);
                    if (val.HasValue) this.Value = val.Value.ToString(); else this.Value = "";
                }
            }
        }

        public override IEnumerable<string> GetValidationErrors()
        {
            decimal o;
            if (Value.Trim() != "" && !decimal.TryParse(Value, out o))
                yield return string.Format(Messages.NonNumericalMessage, Title);
            else
                if (!IsOptional && Value.Trim() == "")
                    yield return string.Format(Messages.RequiredMessage, Title);
        }
    }

    public class SimpleDoubleField<T> : SimpleTextBoxField<T>
    {
        public SimpleDoubleField()
        {
            Value = "0";
        }
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
                SetObject<double>(Obj, FieldName, Value.Trim() != "" ? double.Parse(Value) : 0);
        }

        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                this.Value = GetObject<double>(Obj, FieldName).ToString("g0");
            }
        }

        public override IEnumerable<string> GetValidationErrors()
        {
            double o;
            if (Value.Trim() != "" && !double.TryParse(Value, out o))
                yield return string.Format(Messages.NonNumericalMessage, Title);
            else
                if (!IsOptional && Value.Trim() == "")
                    yield return string.Format(Messages.RequiredMessage, Title);
        }
    }
    public class SimpleDropDownField<T> : SimpleFormField<T, string>
    {
        public List<ListItem> AllItems { get; set; }
        public override string GenerateFieldHtml(string Scope)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<select id='{0}' name='{0}'><option value=''></option>\r\n", Scope + FieldName);
            var id = Value;
            
            foreach (var sli in AllItems)
                sb.AppendFormat("<option{2} value='{0}'>{1}</option>\r\n", sli.Value, sli.Text, id != null && sli.Value == id ? " selected='selected'" : "");

            sb.AppendLine("<select>");
            return sb.ToString();
        }
        public override void Deserialize(string Value)
        {
            if (AllItems.Any(a => a.Value == Value) || Value == "") this.Value = Value;
            else this.Value = null;
        }
        public override IEnumerable<string> GetValidationErrors()
        {
            if (!IsOptional && (Value ?? "").Trim() == "") yield return string.Format(Messages.RequiredMessage, Title);
            yield break;
        }
    }
}
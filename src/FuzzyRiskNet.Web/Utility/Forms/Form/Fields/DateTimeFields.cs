using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MVCFormsLibrary;
using FuzzyRiskNet.Libraries.Forms;

namespace FuzzyRiskNet.Libraries.Forms
{
    public class SimpleGregDateField<T> : SimpleFormField<T, string>
    {
        public bool HasTime { get; set; }
        public bool IsInPast { get; set; }

        public override string GenerateFieldHtml(string Scope) { return GenerateFieldHtml(Scope, "text", Value == null ? "" : Value.ToString(), HasTime ? "myformdatetime" : "myformdate"); }

        public override void Deserialize(string Value)
        {
            this.Value = Value;
        }

        public new Action<T, IFormField<T>, DateTime?> CustomSetObject { get; set; }
        public new Func<T, IFormField<T>, DateTime?> CustomGetObject { get; set; }

        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Helpers.Helper.GetDateSafe(Value, true));
            else
            {
                DateTime val; DateTime? val2 = null;
                if (DateTime.TryParse(Value, out val)) val2 = val;
                
                var proptype = Obj.GetType().GetProperty(FieldName).PropertyType;

                if (proptype == typeof(DateTime?))
                    SetObject<DateTime?>(Obj, FieldName, val2);
                if (val2.HasValue)
                    SetObject<DateTime>(Obj, FieldName, val2.Value);
            }
        }

        public override void GetObject(T Obj)
        {
            var proptype = Obj.GetType().GetProperty(FieldName).PropertyType;
            var Value = (CustomGetObject != null) ? CustomGetObject(Obj, this) :
                proptype == typeof(DateTime?) ? GetObject<DateTime?>(Obj, FieldName) : GetObject<DateTime>(Obj, FieldName);
            if (Value.HasValue)
                this.Value = Value.Value.ToShortDateString() + (HasTime ? " " + Value.Value.ToShortTimeString() : "");
            else
                this.Value = "";
        }

        public override IEnumerable<string> GetValidationErrors()
        {
            var IsEmpty = String.IsNullOrEmpty(Value.Trim());
            DateTime val;
            if (IsEmpty)
            {
                if (!IsOptional) yield return String.Format(Messages.RequiredMessage, Title);
            }
            else if (!DateTime.TryParse(Value, out val))
                yield return string.Format(Messages.IncorrectDateMessage, Title);
            else
            {
                var d = Helpers.Helper.GetDateSafe(Value, HasTime);
                if (d.HasValue)
                {
                    if (IsInPast && d > DateTime.Now.AddDays(1)) yield return String.Format(Messages.ShouldBeInThePastMessage, Title);
                }
            }
        }
    }

    public class SimplePersianDateNullField<T> : SimpleFormField<T, string>
    {
        public bool HasTime { get; set; }
        public bool IsInPast { get; set; }

        public override string GenerateFieldHtml(string Scope) { return GenerateFieldHtml(Scope, "text", Value == null ? "" : Value.ToString(), HasTime ? "myformdatetime" : "myformdate"); }

        public override void Deserialize(string Value)
        {
            this.Value = Value;
        }

        public new Action<T, IFormField<T>, DateTime?> CustomSetObject { get; set; }
        public new Func<T, IFormField<T>, DateTime?> CustomGetObject { get; set; }

        System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Helpers.Helper.GetDateSafe(Value, true));
            else
                SetObject<DateTime?>(Obj, FieldName, Helpers.Helper.GetDateSafe(Value, true));
        }

        public override void GetObject(T Obj)
        {
            var Value = (CustomGetObject != null) ? CustomGetObject(Obj, this) : GetObject<DateTime?>(Obj, FieldName);
            System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
            if (Value.HasValue)
                SetFromDateTime(Value.Value);
            else
                this.Value = "";
        }

        public void SetFromDateTime(DateTime Value)
        {
            this.Value = Helpers.Helper.GetPersianDate(Value, HasTime);
        }

        public override IEnumerable<string> GetValidationErrors()
        {            
            var IsEmpty = Value == null || String.IsNullOrEmpty(Value.Trim());
            if (IsEmpty)
            {
                if (!IsOptional) yield return String.Format(Messages.RequiredMessage, Title);
            }
            else if (!Helpers.Helper.ValidDate(Value, HasTime, false))
                yield return string.Format("تاریخ وارد شده برای {0} نادرست است.", Title);
            else
            {
                var d = Helpers.Helper.GetDateSafe(Value, HasTime);
                if (d.HasValue)
                {
                    if (IsInPast && d > DateTime.Now.AddDays(1)) yield return Title + " در آینده است در حالی که باید مربوط به گذشته باشد.";
                }
            }
        }
    }

    public class SimpleTimeNullField<T> : SimpleFormField<T, string>
    {
        public override string GenerateFieldHtml(string Scope) { return GenerateFieldHtml(Scope, "text", Value == null ? "" : Value.ToString(), "myformtime"); }

        public override void Deserialize(string Value)
        {
            this.Value = Value;
        }

        public new Action<T, IFormField<T>, TimeSpan?> CustomSetObject { get; set; }
        public new Func<T, IFormField<T>, TimeSpan?> CustomGetObject { get; set; }

        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Helpers.Helper.GetTimeSafe(Value));
            else
                SetObject<TimeSpan?>(Obj, FieldName, Helpers.Helper.GetTimeSafe(Value));
        }

        public override void GetObject(T Obj)
        {
            var Value = (CustomGetObject != null) ? CustomGetObject(Obj, this) : GetObject<TimeSpan?>(Obj, FieldName);
            if (Value.HasValue)
                SetFromTime(Value.Value);
            else
                this.Value = "";
        }

        public void SetFromTime(TimeSpan Value)
        {
            this.Value = String.Format("{0:D2}:{1:D2}", Value.Hours, Value.Minutes);
        }

        public override IEnumerable<string> GetValidationErrors()
        {
            var IsEmpty = String.IsNullOrEmpty(Value.Trim());
            if (IsEmpty)
            {
                if (!IsOptional)
                    yield return string.Format(Messages.RequiredMessage, Title);
            }
            else if (!Helpers.Helper.ValidTime(Value))
                yield return string.Format("زمان وارد شده برای {0} نادرست است.", Title);
        }
    }
}

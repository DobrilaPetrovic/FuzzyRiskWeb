using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuzzyRiskNet.Libraries.Forms;
using System.Web.UI.WebControls;

namespace FuzzyRiskNet.Libraries.Forms
{
    public class EnumDropDownField<T> : SimpleDropDownField<T>
    {
        public EnumDropDownField(Type EnumType, Func<object, string> GetName = null)
        {
            this.EnumType = EnumType;
            if (GetName == null) GetName = o => o.ToString();
            this.AllItems = Enum.GetValues(EnumType).Cast<object>().Select(o => new ListItem(GetName(o), o.ToString())).ToList();
        }
        public Type EnumType { get; private set; }

        public override void Deserialize(string Value)
        {
            this.Value = Value;
        }

        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
            {
                var p = Obj.GetType().GetProperty(FieldName);
                if ((Value ?? "").Trim() != "")
                {
                    var val = Enum.Parse(EnumType, Value);
                    //if (p.PropertyType == typeof(int)) p.SetValue(Obj, (int)val, null);
                    //else 
                    if (p.PropertyType == typeof(string)) p.SetValue(Obj, val.ToString(), null);
                    else p.SetValue(Obj, (int)val, null);
                }
            }
        }

        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                var p = Obj.GetType().GetProperty(FieldName);
                this.Value = p.PropertyType == typeof(String) ? SimpleFormField<T, string>.GetObject<string>(Obj, FieldName) :
                    p.PropertyType == typeof(int?) ? (SimpleFormField<T, string>.GetObject<int?>(Obj, FieldName).HasValue ? Enum.GetName(EnumType, SimpleFormField<T, string>.GetObject<int?>(Obj, FieldName)) : "") :
                    Enum.GetName(EnumType, SimpleFormField<T, string>.GetObject<int>(Obj, FieldName));
            }
        }
    }
}

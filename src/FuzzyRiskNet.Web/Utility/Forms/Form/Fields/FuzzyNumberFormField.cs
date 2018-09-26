using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FuzzyRiskNet.Libraries.Forms
{
    public class FuzzyNumberFormField<T> : SimpleTextBoxField<T>
    {
        public FuzzyNumberFormField()
        {
            Value = ""; 
        }
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
            {
                TFN val; TFN.TryParse(Value, out val);
                var type = typeof(T).GetProperty(FieldName).PropertyType;
                if (type == typeof(TFN))
                    SetObject<TFN>(Obj, FieldName, Value.Trim() != "" ? val : null);
            }
        }

        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                var type = typeof(T).GetProperty(FieldName).PropertyType;
                if (type == typeof(TFN))
                {
                    var val = GetObject<TFN>(Obj, FieldName);
                    if (val != null) this.Value = val.ToString(); else this.Value = "";
                }
            }
        }

        public override IEnumerable<string> GetValidationErrors()
        {
            TFN o;
            if (Value.Trim() != "" && !TFN.TryParse(Value, out o))
                yield return string.Format("Value of {0} should be a triangular fuzzy number (e.g. [1, 2, 3]).", Title);
            else
                if (!IsOptional && Value.Trim() == "")
                    yield return string.Format(Messages.RequiredMessage, Title);
        }
    }
}


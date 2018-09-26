using FuzzyRiskNet.Libraries.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nik.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FuzzyRiskNet.Fuzzy;

namespace MVCFormsLibrary.Form
{
    public class FormFieldsBuilder<T> : List<IFormField> where T : class 
    {
        public IFormModel<T> Form { get; private set; }
        public FormFieldsBuilder(IFormModel<T> Form)
        {
            this.Form = Form;
        }

        public FormFieldsBuilder<T> AutoGenerateEntityFields(Func<System.Reflection.PropertyInfo, string> GetCustomTitle = null,
            Func<System.Reflection.PropertyInfo, FormFieldsBuilder<T>, bool> CustomPropHandler = null) 
        {
            var props = typeof(T).GetProperties();
            if (GetCustomTitle == null) GetCustomTitle = p => null;
            if (CustomPropHandler == null) CustomPropHandler = (p, f) => false;

            foreach (var p in props)
            {
                //if (p.GetAttr<CalculatedPropertyAttribute>() != null) continue;
                if (CustomPropHandler(p, this)) continue;
                if (NotMappedAttribute.IsDefined(p, typeof(NotMappedAttribute), false)) continue;
                var desc = GetCustomTitle(p);
                if (desc == null && DisplayAttribute.IsDefined(p, typeof(DisplayAttribute), false))
                    desc = (DisplayAttribute.GetCustomAttribute(p, typeof(DisplayAttribute), false) as DisplayAttribute).Name;
                if (desc == null)
                    desc = p.Name;

                DataType? customdt = null;
                if (DataTypeAttribute.IsDefined(p, typeof(DataTypeAttribute), false))
                    customdt = (DataTypeAttribute.GetCustomAttribute(p, typeof(DataTypeAttribute), false) as DataTypeAttribute).DataType;


                var optional = false;// p.GetAttr<System.ComponentModel.DataAnnotations.Schema.>() == null;

                var f = p.Name;
                if (p.Name == "ID")
                    this.Add(Form.CreateKeyField("ID"));
                else if (p.Name.EndsWith("ID"))
                    continue;
                else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
                    this.Add(Form.CreateNumberField(f, desc, optional));
                else if (p.PropertyType == typeof(double) || p.PropertyType == typeof(double?))
                    this.Add(Form.CreateNumberField(f, desc, optional));
                else if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?))
                    this.Add(Form.CreateNumberField(f, desc, optional));
                else if (p.PropertyType == typeof(DateTime?) || p.PropertyType == typeof(DateTime))
                    this.Add(Form.CreateGregDateField(f, desc, optional));
                else if (p.PropertyType == typeof(string))
                {
                    if (customdt == DataType.MultilineText)
                        this.Add(Form.CreateMultiLineTextBoxField(f, desc, optional));
                    else
                        this.Add(Form.CreateTextBoxField(f, desc, optional));
                }
                else if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
                    this.Add(Form.CreateCheckBoxField(f, desc));
                else if (p.PropertyType == typeof(TFN))
                    this.Add(Form.CreateTFNField(f, desc, optional));
                else if (p.PropertyType.IsEnum)
                    this.Add(Form.CreateEnumDropDownField(f, desc, p.PropertyType));
                /*else if (p.PropertyType == typeof(DEAOS.DBEntities.User))
                    fields.Add(Form.CreateUserDrop(p.GetAttr<DEAOSReferenceKeyAttribute>().Key, desc, optional));
                else if (p.PropertyType == typeof(DEAOS.DBEntities.UpgradeOption))
                    fields.Add(Form.CreateUpgradeOptDrop(p.GetAttr<DEAOSReferenceKeyAttribute>().Key, desc, optional));
                else if (p.PropertyType == typeof(DEAOS.DBEntities.Group))
                    fields.Add(Form.CreateGroupDrop(p.GetAttr<DEAOSReferenceKeyAttribute>().Key, desc, optional));*/
            }

            return this;
        }
    }
}

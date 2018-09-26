using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Text;
using System.Data;
using Nik.Expressions;
using Nik.Linq.Dynamic;

namespace FuzzyRiskNet.Libraries.Forms
{
    public class EFSelectItem
    {
        public int ID { get; set; }
        public string Title { get; set; }
    }
    public class EFDropDownFormField<T, T2> : SimpleFormField<T, int?> where T2 : class
    {
        public IQueryable<T2> AllItems { get; set; }
        public Expression<Func<T2, int>> GetIDExpression { get; set; }
        public Expression<Func<T2, string>> GetTitleExpression { get; set; }
        public string EmptyOptionTitle { get; set; }

        public EFSelectItem[] GetAllSelectItems()
        {
            var type = typeof(EFSelectItem);
            var param = GetIDExpression.Parameters[0];
            var listitems = AllItems.Select(Expression.Lambda<Func<T2, EFSelectItem>>(
                Expression.MemberInit(Expression.New(type),
                new MemberBinding[] 
                {
                    Expression.Bind(type.GetMember("Title")[0], ParameterRebinder.ReplaceParameters(GetTitleExpression.Parameters[0], param, GetTitleExpression.Body)),
                    Expression.Bind(type.GetMember("ID")[0], GetIDExpression.Body)
                }),
                new ParameterExpression[] { param })).ToArray();
            return listitems;
        }

        public override string GenerateFieldHtml(string Scope)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<select id='{0}' name='{0}' class='{1}'><option value=''>{2}</option>\r\n", Scope + FieldName, CustomCssClass ?? "", EmptyOptionTitle ?? "");
            int? id = Value;
            foreach (var sli in GetAllSelectItems())
                sb.AppendFormat("<option{2} value='{0}'>{1}</option>\r\n", sli.ID, sli.Title, id.HasValue && sli.ID == id ? " selected='selected'" : "");
            sb.AppendLine("</select>");
            return sb.ToString();
        }

        public override void Deserialize(string Value)
        {
            int id;
            if (int.TryParse(Value, out id))
            {
                this.Value = id;
            }
            else 
                this.Value = null;
        }
        
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
            {
                var type = GetFieldType();
                if (type == typeof(int)) SetObject<int>(Obj, FieldName, Value ?? 0);
                else if (type == typeof(int?)) SetObject<int?>(Obj, FieldName, Value);
                else if (type == typeof(T2))
                {
                    T2 v = Value.HasValue ? AllItems.Where("ID = " + Value.Value).FirstOrDefault() : null;
                    Obj.GetType().GetProperty(FieldName).SetValue(Obj, v, null);
                }
            }
        }

        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                var type = GetFieldType();
                if (type == typeof(int)) this.Value = GetObject<int>(Obj, FieldName);
                else if (type == typeof(int?)) this.Value = GetObject<int?>(Obj, FieldName);
                else if (type == typeof(T2))
                {
                    var v = Obj.GetType().GetProperty(FieldName).GetValue(Obj, null);                   
                    this.Value = v == null ? (int?) null : (int)type.GetProperty("ID").GetValue(v, null);
                }
            }
        }

        public override string ReadOnlyValue
        {
            get
            {
                if (Value.HasValue)
                {
                    var item = GetAllSelectItems().FirstOrDefault(i => i.ID == Value.Value);
                    if (item != null) return item.Title;
                }
                return "";
            }
        }
    }
}
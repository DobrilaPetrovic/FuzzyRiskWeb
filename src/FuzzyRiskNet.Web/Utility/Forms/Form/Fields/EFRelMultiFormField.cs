using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Text;
using System.Data;
using Nik.Expressions;

namespace FuzzyRiskNet.Libraries.Forms
{
    public abstract class MultiIDFormFields<T, T2> : SimpleFormField<T, int[]>, ITitledFormField where T2 : class
    {
        public override void GetObject(T Obj)
        {
            if (CustomGetObject != null)
                this.Value = CustomGetObject(Obj, this);
            else
            {
                var field = typeof(T).GetProperty(FieldName);
                if (field.PropertyType == typeof(int[]))
                {
                    this.Value = field.GetValue(Obj, null) as int[];
                }
                else
                {
                    var p = field.GetValue(Obj, null) as ICollection<T2>;
                    if (p == null) { this.Value = null; return; }
                    this.Value = p.Select(GetIDExpression.Compile()).ToArray();
                }
            }
        }

        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
            {
                var list = (Value ?? new int[0]).ToList();
                var field = typeof(T).GetProperty(FieldName);
                if (field.PropertyType == typeof(int[]))
                {
                    field.SetValue(Obj, list.ToArray(), null);
                }
                else
                {
                    var p = field.GetValue(Obj, null) as ICollection<T2>;
                    var getid = GetIDExpression.Compile();
                    for (int i = 0; i < p.Count; i++)
                        if (!list.Contains(getid(p.ElementAt(i)))) { p.Remove(p.ElementAt(i)); i--; }

                    foreach (var i in list)
                        if (!p.Any(b => getid(b) == i))
                            p.Add(AllItems.SingleOrDefault(ParameterRebinder.FoG(j => j == i, GetIDExpression)));
                }
            }
        }

        public Expression<Func<T2, int>> GetIDExpression { get; set; }
        public Expression<Func<T2, string>> GetTitleExpression { get; set; }

        public IQueryable<T2> AllItems { get; set; }

        public EFSelectItem[] ListAllItems()
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

        public override void Deserialize(string Value)
        {
            try
            {
                this.Value = Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(v => int.Parse(v)).ToArray();
            }
            catch
            {
                this.Value = null;
            }
        }

        public override string ReadOnlyValue { get { return Value == null ? "" : string.Join(" - ", ListAllItems().Where(i => Value.Any(v => v == i.ID)).Select(i => i.Title).ToArray()); } }

    }


    public class EFRelMultiFormField<T, T2> : MultiIDFormFields<T, T2> where T2 : class
    {
        public override string GenerateFieldHtml(string Scope)
        {
            StringBuilder sb = new StringBuilder();
            var list = (Value ?? new int[0]).ToList();

            sb.AppendFormat("<div class='multichk'>");
            foreach (var sli in ListAllItems())
                sb.AppendFormat("<div class='chkitem'><input type='checkbox' name='{2}' value='{0}'{3}> {1}</div>\r\n", sli.ID, new HtmlString(sli.Title).ToHtmlString(), Scope + FieldName, list.Contains(sli.ID) ? " checked" : "");
            sb.AppendFormat("</div>");

            return sb.ToString();
        }
    }

    public class SortableMultiSelectFormField<T, T2> : MultiIDFormFields<T, T2> where T2 : class
    {
        public int MaxNumItems = 10;
        public override string GenerateFieldHtml(string Scope)
        {
            StringBuilder sb = new StringBuilder();
            var list = (Value ?? new int[MaxNumItems]).ToList();

            sb.AppendFormat("<div class='multichk'>");
            for (int i = 0; i < MaxNumItems; i++)
            {
                sb.AppendFormat("<select id='{0}' name='{0}'>", Scope + FieldName);
                sb.AppendLine("<option value='0'></option>");
                foreach (var sli in ListAllItems())
                    sb.AppendFormat("<option value='{0}'{2}> {1}</option>", sli.ID, new HtmlString(sli.Title).ToHtmlString(), list.Count > i && list[i] == sli.ID ? " selected='selected'" : "");
                sb.Append("</select><br/>\r\n");
            }
            sb.AppendFormat("</div>");

            return sb.ToString();
        }
    }
    public class SelectizeFormField<T, T2> : MultiIDFormFields<T, T2> where T2 : class
    {
        public override string GenerateFieldHtml(string Scope)
        {
            StringBuilder sb = new StringBuilder();
            var list = (Value ?? new int[0]).ToList();

            sb.AppendFormat("<select class='notsearchable' style='width: 500px' id='{0}' name='{0}' multiple>", Scope + FieldName);
            //sb.AppendLine("<option value='0'></option>");
            foreach (var sli in ListAllItems())
                sb.AppendFormat("<option value='{0}'{2}> {1}</option>", sli.ID, new HtmlString(sli.Title).ToHtmlString(), list.Contains(sli.ID) ? " selected='selected'" : "");
            sb.Append("</select>\r\n");

            sb.Append("<script>$(document).ready(function () { $('#" + Scope + FieldName + "').selectize({ maxItems: 20 }); });</script>\r\n");

            return sb.ToString();
        }
    }

    /*public class SortableMultiSelectFormFieldWithCustomTitle<T, T2> : MultiIDFormFields<T, T2> where T2 : class
    {
        public int MaxNumItems = 10;
        public override string GenerateFieldHtml(string Scope)
        {
            StringBuilder sb = new StringBuilder();
            var list = (Value ?? new int[MaxNumItems]).ToList();

            sb.AppendFormat("<div class='multichk'>");
            for (int i = 0; i < MaxNumItems; i++)
            {
                sb.AppendFormat("<select id='{0}' name='{0}'>", Scope + FieldName);
                sb.AppendLine("<option value='0'></option>");
                foreach (var sli in ListAllItems())
                    sb.AppendFormat("<option value='{0}'{2}> {1}</option>", sli.ID, new HtmlString(sli.Title).ToHtmlString(), list.Count > i && list[i] == sli.ID ? " selected='selected'" : "");
                sb.Append("</select><br/>\r\n");
            }
            sb.AppendFormat("</div>");

            return sb.ToString();
        }
    }*/
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.UI.WebControls;
using Nik.Linq.Dynamic;
using Nik.Expressions;
using System.ComponentModel.DataAnnotations;
using Nik.Helpers;
using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace FuzzyRiskNet.Libraries.Forms
{
    public static class SimpleFormFieldsExtensions
    {
        public static Expression<Func<T, object>> NewExpr<T>(params Tuple<string, LambdaExpression>[] Properties)
        {
            List<DynamicProperty> properties = new List<DynamicProperty>();
            List<MemberAssignment> bindings = new List<MemberAssignment>();

            foreach (var k in Properties)
            {
                if (k.Item2.Parameters.Count != 1) throw new Exception("Only expressions with a single parameter are allowed.");
                properties.Add(new DynamicProperty(k.Item1, k.Item2.ReturnType));
            }

            Type type = Nik.Linq.Dynamic.DynamicExpression.CreateClass(properties);

            var consexp = (Expression<Func<T, decimal>>)((T item) => 1);

            foreach (var k in Properties)
            {
                bindings.Add(Expression.Bind(type.GetMember(k.Item1)[0],
                    ParameterRebinder.ReplaceParameters(k.Item2.Parameters[0], consexp.Parameters[0], k.Item2.Body)));
            }

            return Expression.Lambda<Func<T, object>>(Expression.MemberInit(Expression.New(type), bindings.ToArray()), consexp.Parameters[0]);
        }

        public static void RenderField(this HtmlHelper Helper, IFormField Field)
        {
            Helper.RenderPartial("FormFields\\" + Field.ViewOrDefault(), Field);
        }

        static string ViewOrDefault(this IFormField Field)
        {
            return Field.ViewName ?? "OrdinaryField";
        }

        public static IEnumerable<IFormField> FieldWithDescendants(this IFormField Field)
        {
            yield return Field;
            if (Field is IHasChildFormField)
                foreach (var c in (Field as IHasChildFormField).Childs)
                    foreach (var d in FieldWithDescendants(c))
                        yield return d;
        }

        public static IEnumerable<IFormField> WithDescendants(this IEnumerable<IFormField> Fields)
        {
            foreach (var Field in Fields)
                foreach (var d in Field.FieldWithDescendants())
                    yield return d;
        }

        public static IntKeyField<T> CreateKeyField<T>(this IFormModel<T> form, string Key = "ID")
        {
            return new IntKeyField<T>() { FieldName = Key, Title = "Key", IsOptional = false };
        }

        public static LongKeyField<T> CreateLongKeyField<T>(this IFormModel<T> form, string Key = "ID")
        {
            return new LongKeyField<T>() { FieldName = Key, Title = "Key", IsOptional = false };
        }

        public static GuidKeyField<T> CreateGuidKeyField<T>(this IFormModel<T> form, string Key = "ID")
        {
            return new GuidKeyField<T>() { FieldName = Key, Title = "Key", IsOptional = false };
        }

        public static FixedIntHiddenField<T> CreateHiddenForeignKeyField<T>(this IFormModel<T> form, string FieldName, int Value)
        {
            return new FixedIntHiddenField<T>(Value) { FieldName = FieldName, Title = "FK_" + FieldName, IsOptional = false };
        }

        public static SimpleTextBoxField<T> CreateTextBoxField<T>(this IFormModel<T> form, string FieldName, string Title)
        {
            return new SimpleTextBoxField<T>() { FieldName = FieldName, Title = Title, IsOptional = false };
        }
        public static SimpleTextBoxField<T> CreateTextBoxField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional)
        {
            return new SimpleTextBoxField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional };
        }
        public static SimpleCheckBoxField<T> CreateCheckBoxField<T>(this IFormModel<T> form, string FieldName, string Title)
        {
            return new SimpleCheckBoxField<T>() { FieldName = FieldName, Title = Title, IsOptional = true };
        }
        public static EFRelMultiFormField<T, T2> CreateMultiCheckBoxField<T, T2>(this IFormModel<T> Form, DbContext DB, IQueryable<T2> AllItems, string FieldName, string Title, string IDFieldName = "ID", string TitleFieldName = "Name", bool IsOptional = false) where T2 : class
        {
            return new EFRelMultiFormField<T, T2>()
            {
                FieldName = FieldName,
                Title = Title,
                GetIDExpression = GetPropExpr<T2, int>(IDFieldName),
                GetTitleExpression = GetPropExpr<T2, string>(TitleFieldName),
                AllItems = AllItems,
                IsOptional = IsOptional
            };
        }
        public static LTRTextBoxField<T> CreateLTRTextBoxField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional)
        {
            return new LTRTextBoxField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional };
        }
        public static SimpleNumberField<T> CreateNumberField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional = false, string DefaultValue = "0")
        {
            return new SimpleNumberField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional, Value = DefaultValue };
        }
        public static SimpleDoubleField<T> CreateDoubleField<T>(this IFormModel<T> form, string FieldName, string Title)
        {
            return new SimpleDoubleField<T>() { FieldName = FieldName, Title = Title, IsOptional = false };
        }
        public static SimpleDoubleField<T> CreateDoubleField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional)
        {
            return new SimpleDoubleField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional };
        }
        public static FuzzyNumberFormField<T> CreateTFNField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional = false, string DefaultValue = "")
        {
            return new FuzzyNumberFormField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional, Value = DefaultValue };
        }
        public static MultiLineTextBoxField<T> CreateMultiLineTextBoxField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional)
        {
            return new MultiLineTextBoxField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional };
        }

        public static SimpleGregDateField<T> CreateGregDateField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional = false, bool HasTime = false, bool IsInPast = false)
        {
            return new SimpleGregDateField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional, HasTime = HasTime, IsInPast = IsInPast, };
        }

        public static SimplePersianDateNullField<T> CreateDateField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional = false)
        {
            return new SimplePersianDateNullField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional, HasTime = false, IsInPast = true };
        }
        public static SimplePersianDateNullField<T> CreateDateTimeField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional)
        {
            return new SimplePersianDateNullField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional, HasTime = true, IsInPast = true };
        }

        public static SimpleTimeNullField<T> CreatTimeField<T>(this IFormModel<T> form, string FieldName, string Title, bool Optional = false)
        {
            return new SimpleTimeNullField<T>() { FieldName = FieldName, Title = Title, IsOptional = Optional };
        }

        public static SimpleDropDownField<T> CreateEnumDropDownField<T>(this IFormModel<T> form, string FieldName, string Title, Type EnumType)
        {
            return new EnumDropDownField<T>(EnumType) { FieldName = FieldName, IsOptional = false, Title = Title, };
        }

        public static SimpleDropDownField<T> CreateSimpleDropDownField<T>(this IFormModel<T> form, string FieldName, string Title, SelectListItem[] Items, bool Optional)
        {
            return new SimpleDropDownField<T>() { FieldName = FieldName, IsOptional = Optional, Title = Title, AllItems = Items.Select(v => new ListItem(v.Text, v.Value)).ToList() };
        }
        [Obsolete("Use CreateHeaderLine. Make sure MultiFields view is included.")]
        public static LabelField CreateHeader<T>(this IFormModel<T> Form, string Title)
        {
            return new LabelField() { Value = "<br/><b>" + Title + "</b><br/>", Title = "", FieldName = "", IsVisible = true };
        }

        public static MultiFields CreateHeaderLine<T>(this IFormModel<T> Form, string Title)
        {
            return new MultiFields(new IFormField[] {
                new NewLineField(),
                new LabelField() { Value = "<b style='display: block; width: 100%; border-bottom: 1px solid black; padding: 5px;'>" + Title + "</b>", Title = "", FieldName = "", IsVisible = true },
                new NewLineField(),
            });
        }

        public static MultiFields CreateNoteLine<T>(this IFormModel<T> Form, string Title)
        {
            return new MultiFields(new IFormField[] {
                new NewLineField(),
                new LabelField() { Value = "<span style='display: block; width: 100%; padding: 5px;'>" + Title + "</span>", Title = "", FieldName = "", IsVisible = true },
                new NewLineField(),
            });
        }

        public static LabelField CreateLabelField<T>(this IFormModel<T> Form, string Title, string Value, string FieldName = "", bool IsVisible = true)
        {
            return new LabelField() { Title = Title, Value = Value, FieldName = FieldName, IsVisible = IsVisible };
        }

        public static LabelField CreateLabelField<T>(this IFormModel<T> Form, string Title, bool Value, string FieldName = "", bool IsVisible = true)
        {
            return new LabelField() { Title = Title, Value = Value ? "بله" : "خیر", FieldName = FieldName, IsVisible = IsVisible };
        }

        public static LabelField CreateHyperText<T>(this IFormModel<T> Form, string Text, bool IsVisible = true)
        {
            return new LabelField() { Title = "", Value = Text, IsVisible = IsVisible, ViewName = "HyperText" };
        }

        public static EFDropDownFormField<T, T2> CreateGenericDrop<T, T2>(IQueryable<T2> Items, string FieldName, string Title, string EntityNameProperty, bool Optional = false) 
            where T : class, new()
            where T2 : class
        {
            return new EFDropDownFormField<T, T2>()
            {
                FieldName = FieldName,
                Title = Title,
                IsOptional = Optional,
                GetIDExpression = GetPropExpr<T2, int>("ID"),
                GetTitleExpression = GetPropExpr<T2, string>(EntityNameProperty),
                AllItems = Items
            };
        }

        public static EFDropDownFormField<T, T2> CreateGenericDrop<T, T2>(IQueryable<T2> Items, string FieldName, string Title, Expression<Func<T2, string>> GetNameExpr, bool Optional = false)
            where T : class, new()
            where T2 : class
        {
            return new EFDropDownFormField<T, T2>()
            {
                FieldName = FieldName,
                Title = Title,
                IsOptional = Optional,
                GetIDExpression = GetPropExpr<T2, int>("ID"),
                GetTitleExpression = GetNameExpr,
                AllItems = Items
            };
        }

        public static EFDropDownFormField<T, T2> CreateGenericDrop<T, T2>(this CRUDForm<T> Form, string FieldName, string Title, string EntityNameProperty, bool Optional = false, IQueryable<T2> Items = null)
            where T : class, new()
            where T2 : class
        {
            if (Items == null) Items = Form.DB.Set<T2>();
            return CreateGenericDrop<T, T2>(Items, FieldName, Title, EntityNameProperty, Optional);
        }

        public static EFDropDownFormField<T, T2> CreateGenericDrop<T, T2>(this CRUDForm<T> Form, string FieldName, string Title, Expression<Func<T2, string>> GetNameExpr, bool Optional = false, IQueryable<T2> Items = null)
            where T : class, new()
            where T2 : class
        {
            if (Items == null) Items = Form.DB.Set<T2>();
            return CreateGenericDrop<T, T2>(Items, FieldName, Title, GetNameExpr, Optional);
        }

        public static Expression<Func<T, T2>> GetPropExpr<T, T2>(string PropName)
        {
            var obj = Expression.Parameter(typeof(T), "Param");
            return LambdaExpression.Lambda<Func<T, T2>>(Expression.Property(obj, typeof(T), PropName), obj);
        }
    }
}

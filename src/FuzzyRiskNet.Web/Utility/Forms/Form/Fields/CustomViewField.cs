using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.Libraries.Forms
{
    public abstract class CustomViewFieldBase : IFormField, IHasChildFormField
    {
        public CustomViewFieldBase(string ViewName, IFormField[] Childs = null)
        {
            this.ViewName = ViewName;
            this.Childs = Childs;
        }

        public CustomViewFieldBase(string ViewName, Func<IEnumerable<IFormField>> GetChilds)
        {
            this.ViewName = ViewName;
            Childs = GetChilds().ToArray();
        }

        public string FieldName { get; private set; }

        public string Title { get; private set; }

        public void Deserialize(NameValueCollection Form, string Scope)
        {
        }

        public IEnumerable<string> GetValidationErrors() { yield break; }

        public bool IsOptional { get { return false; } }

        public bool IsVisible { get { return true; } }

        public string ReadOnlyValue { get { return ""; } }

        public string ViewName
        {
            get;
            private set;
        }

        public IFormField[] Childs
        {
            get;
            protected set;
        }
    }

    public class EditGroupFields : CustomViewFieldBase
    {
        public Tuple<string, IFormField[]>[] Groups { get; private set; }

        public EditGroupFields(Tuple<string, IFormField[]>[] Groups)
            : base("EditGroupFields", Groups.SelectMany(g => g.Item2).ToArray())
        {
            this.Groups = Groups;
        }
    }

    public class NewLineField : CustomViewFieldBase
    {
        public NewLineField() : base("NewLine", new IFormField[0]) { }
    }

    public class MultiFields : CustomViewFieldBase
    {
        public MultiFields(IFormField[] Fields) : base("MultiFields", Fields)
        { }
    }
}

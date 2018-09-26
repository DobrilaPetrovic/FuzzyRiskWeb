using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.Libraries.Forms
{
    public interface IFieldsTable : IFormField
    {
        string[] ColumnNames { get; }
        IEnumerable<IOrdinaryFormField[]> Rows { get; }
    }

    public class FieldsTable<T> : CustomViewFieldBase, IFieldsTable
    {
        public FieldsTable(IEnumerable<T> Source, Action<ColumnBuilder> MakeCols) : base("FieldsTable")
        {
            var cols = new ColumnBuilder();
            MakeCols(cols);
            this.Source = Source;
            this.Columns = cols.ToArray();
            this.Rows = this.Source.Select((r, row) => this.Columns.Select(c => c.GetFieldWithRowNo(r, row)).ToArray()).ToArray();
            this.Childs = this.Rows.SelectMany(r => r.Cast<IFormField>()).ToArray();
        }

        public IEnumerable<T> Source { get; private set; }
        public ColumnDef[] Columns { get; private set; }

        public class ColumnDef 
        {
            public ColumnDef() { GetFieldWithRowNo = (t, r) => GetField(t); }
            public string Name { get; set; } 
            public Func<T, IOrdinaryFormField> GetField { get; set; }
            public Func<T, int, IOrdinaryFormField> GetFieldWithRowNo { get; set; }
        }

        public class ColumnBuilder : List<ColumnDef>
        {
            public void Add(string Name, Func<T, IOrdinaryFormField> GetField)
            {
                Add(new ColumnDef() { Name = Name, GetField = GetField });
            }

            public void Add(string Name, Func<T, string> GetString)
            {
                Add(new ColumnDef() { Name = Name, GetField = (t) => new LabelField() { Title = Name, Value = GetString(t), FieldName = "", IsVisible = true } });
            }

            public void Add(string Name, Func<T, bool?> GetBool)
            {
                Add(new ColumnDef() { Name = Name, GetField = (t) => new LabelField() { Title = Name, Value = ((Func<bool?, string>)(v => v.HasValue ? v.Value ? "✓" : "X" : ""))(GetBool(t)), FieldName = "", IsVisible = true } });
            }

            public void AddRowNo(string Name)
            {
                Add(new ColumnDef() { Name = Name, GetFieldWithRowNo = (t, row) => new LabelField() { Title = Name, Value = (row + 1).ToString(), FieldName = "", IsVisible = true } });
            }
        }

        public string[] ColumnNames
        {
            get { return Columns.Select(c => c.Name).ToArray(); }
        }

        public IEnumerable<IOrdinaryFormField[]> Rows
        {
            get;
            private set; // { return Source.Select(r => Columns.Select(c => c.GetField(r)).ToArray()); }
        }
    }
}

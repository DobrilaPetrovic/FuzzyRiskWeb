using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.Libraries.Forms
{
    public class LongKeyField<T> : KeyField<T, long>
    {
        public LongKeyField() : base(str => S.SafeParseLong(str) ?? 0, false) { }
    }

    public class IntKeyField<T> : KeyField<T, int>
    {
        public IntKeyField() : base(str => S.SafeParseInt(str) ?? 0, false) { }
    }

    public class GuidKeyField<T> : KeyField<T, Guid>
    {
        public GuidKeyField() : base(str => Guid.Parse(str), true) { Value = Guid.NewGuid(); }
    }

    public class KeyField<T, TKey> : SimpleFormField<T, TKey>
    {
        public KeyField(Func<string, TKey> ParseTKeySafe, bool ShouldSet) { IsVisible = false; this.ParseTKeySafe = ParseTKeySafe; this.ShouldSet = ShouldSet; }
        Func<string, TKey> ParseTKeySafe { get; set; }
        public bool ShouldSet { get; private set; }
        public override void Deserialize(string Value)
        {
            this.Value = ParseTKeySafe(Value);
        }

        public override string GenerateFieldHtml(string Scope) { return base.GenerateFieldHtml(Scope, "hidden", Value.ToString()); }
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
                if (Value != null && ShouldSet) base.SetObject(Obj);
        }
        public override IEnumerable<string> GetValidationErrors()
        {
            yield break;
        }
    }

    public class FixedIntHiddenField<T> : FixedHiddenField<T, int>
    {
        public FixedIntHiddenField(int Value) : base(Value) { }
    }

    public class FixedHiddenField<T, TKey> : SimpleFormField<T, TKey>
    {
        public FixedHiddenField(TKey Value) { IsVisible = false; this.Value = Value; }

        public override void Deserialize(string Value)
        {
        }

        public override string GenerateFieldHtml(string Scope) { return base.GenerateFieldHtml(Scope, "hidden", Value.ToString()); }
        
        public override void GetObject(T Obj)
        {
        }
        
        public override void SetObject(T Obj)
        {
            if (CustomSetObject != null)
                CustomSetObject(Obj, this, Value);
            else
                base.SetObject(Obj);
        }
        public override IEnumerable<string> GetValidationErrors()
        {
            yield break;
        }
    }
}

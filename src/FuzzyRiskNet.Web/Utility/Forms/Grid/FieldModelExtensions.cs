using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyRiskNet.Libraries.Grid
{
    public static class FieldModelExtensions
    {
        public static T2 SetClassName<T2>(this T2 Field, string ClassName) where T2 : IColumnModel
        {
            Field.ClassName = ClassName;
            return Field;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

// This file is not used in either SRAA or IRADA
namespace FuzzyRiskNet.Fuzzy
{
    /// <summary>
    /// A parser for generic functions with fuzzy parameters 
    /// </summary>
    public class MFFuncParser
    {
        public static MFFuncModel Parse(string Variables)
        {
            var vars = Variables.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var model = new MFFuncModel();
            var reg = new Regex(regex);
            var dynamicproperties = new List<DynamicProperty>();
            foreach (var v in vars.Where(v => v.Trim() != ""))
            {
                var m = reg.Match(v);
                IMF mf;
                if (m.Groups["l"].Success)
                {
                    var l = System.Linq.Dynamic.DynamicExpression.ParseLambda<LUFunction, double>(m.Groups["l"].Value).Compile();
                    var u = System.Linq.Dynamic.DynamicExpression.ParseLambda<LUFunction, double>(m.Groups["u"].Value).Compile();

                    mf = new CustomIntervalMF(Enumerable.Range(0, 101).Select(i => 1D * i / 100).Select(a => new FuzzyInterval(a, l(new LUFunction() { a = a }), u(new LUFunction() { a = a })))) { Name = m.Groups["varname"].Value };
                }
                else
                {
                    mf = m.Groups["v4"].Success ?
                        (IMF)new TrapMF(double.Parse(m.Groups["v1"].Value), double.Parse(m.Groups["v2"].Value), double.Parse(m.Groups["v3"].Value), double.Parse(m.Groups["v4"].Value), m.Groups["varname"].Value, MFType.MidMF) :
                        new TriMF(double.Parse(m.Groups["v1"].Value), double.Parse(m.Groups["v2"].Value), double.Parse(m.Groups["v3"].Value), m.Groups["varname"].Value);
                }
                model.MFs.Add(mf);
                dynamicproperties.Add(new DynamicProperty(mf.Name, typeof(double)));
            }
            model.ParamClass = System.Linq.Dynamic.DynamicExpression.CreateClass(dynamicproperties.ToArray());

            return model;
        }

        static string numregex = @"[-+]?([0-9]*\.[0-9]+|[0-9]+)";

        static string regex = @"(?<varname>[a-zA-Z][0-9a-zA-Z]*)\s*\=\s*(\[\s*(?<v1>" + numregex + @")\s*,\s*(?<v2>" + numregex + @")\s*,\s*(?<v3>" + numregex + @")\s*(,\s*(?<v4>" + numregex + @")\s*)?\]|\[\s*(?<l>[^#]+)\s*#\s*(?<u>[^#]+)\s*\])\s*\Z";
    }

    public class LUFunction : DynamicClass
    {
        public double a { get; set; }
    }

    public class MFFuncModel
    {
        public MFFuncModel() { MFs = new List<IMF>(); }
        public List<IMF> MFs { get; set; }
        public LambdaExpression Function { get; set; }

        public void ParseFunction(string Formula)
        {
            Function = System.Linq.Dynamic.DynamicExpression.ParseLambda(ParamClass, typeof(double), Formula);
        }

        Delegate compiled;
        public Type ParamClass {  get; set; }

        public double Calculate(double[] Values)
        {
            var c = ParamClass.GetConstructor(new Type[0]).Invoke(new object[0]);
            
            var ind = 0;
            foreach (var mf in MFs)
                ParamClass.GetProperty(mf.Name).SetValue(c, Values[ind++]);
            
            if (compiled == null) compiled = Function.Compile();
            
            return (double)compiled.DynamicInvoke(c);
        }
    }

    public class MFFuncParam
    {
        public string Formula { get; set; }
        public string Parameters { get; set; }
        public string CustomTitle { get; set; }
    }

    public class MFFuncExamples
    {
        public static MFFuncParam[] Examples = new MFFuncParam[] 
        {
            new MFFuncParam() { Formula = "y / x", Parameters = "x = [0.2,1,5]\r\ny = [3,4,5]" },
            new MFFuncParam() { Formula = "x * y", Parameters = "x = [1,2,9]\r\ny = [3,4,9]" },
            new MFFuncParam() { Formula = "z * y / x", Parameters = "x = [0.2,1,5]\r\ny = [3,4,5]\r\nz = [-3,-1,1]" },
            new MFFuncParam() { Formula = "abs(x * y)", Parameters = "x = [-3,-1,1]\r\ny = [1,2,3]" },
            new MFFuncParam() { Formula = "pow(x, 2)", Parameters = "x = [-3,-1,1]" },
            new MFFuncParam() { Formula = "pow(x, 2) + y", Parameters = "x = [-3,-1,1]\r\ny = [1, 5, 6]" },
            new MFFuncParam() { Formula = "y / x", Parameters = "x = [0.2,1,5,8]\r\ny = [3,4,5,6]", CustomTitle = "y / x (Trapezoidal)" },
         };
    }
}
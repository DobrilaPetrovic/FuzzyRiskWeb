using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;

namespace Nik.Expressions
{
    public class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, Expression> map;

        public ParameterRebinder(Dictionary<ParameterExpression, Expression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, Expression>();
        }

        public static Expression ReplaceParameters(Dictionary<ParameterExpression, Expression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        public static Expression ReplaceParameters(ParameterExpression from, Expression to, Expression exp)
        {
            Dictionary<ParameterExpression, Expression> map = new Dictionary<ParameterExpression,Expression>();
            map.Add(from, to);
            return new ParameterRebinder(map).Visit(exp);
        }

        public static Expression<Func<T, T3>> FoG<T, T2, T3>(Expression<Func<T2, T3>> F, Expression<Func<T, T2>> G)
        {
            var t = G.Parameters[0];
            var t2 = F.Parameters[0];
            var exp = ReplaceParameters(t2, G.Body, F.Body);
            return Expression.Lambda<Func<T, T3>>(exp, new ParameterExpression[] { t });
        }

        public static Expression<Func<T, NCT, T3>> FoG<T, T2, T3, NCT>(Expression<Func<T2, NCT, T3>> F, Expression<Func<T, T2>> G)
        {
            var t = G.Parameters[0];
            var t2 = F.Parameters[0];
            var exp = ReplaceParameters(t2, G.Body, F.Body);
            return Expression.Lambda<Func<T, NCT, T3>>(exp, new ParameterExpression[] { t, F.Parameters[1] });
        }

        /*public static Expression<Func<T, T2>> Replace<T, T2, NCT>(Expression<Func<T, NCT, T2>> F, Expression<Func<T, NCT>> G)
        {
            var t = G.Parameters[0];
            var t2 = F.Parameters[1];
            var exp = ReplaceParameters(t2, ReplaceParameters(t, F.Parameters[0], G.Body), F.Body);
            return Expression.Lambda<Func<T, T2>>(exp, new ParameterExpression[] { F.Parameters[0] });
        }*/

        protected override Expression VisitParameter(ParameterExpression p)
        {
            Expression replacement;
            if (map.TryGetValue(p, out replacement))
            {
                return replacement;
            }
            return base.VisitParameter(p);
        }
    }

    public static class ParameterRebinderHelpers
    {
        public static Expression<Func<T, T3>> GoF<T, T2, T3>(this Expression<Func<T2, T3>> F, Expression<Func<T, T2>> G)
        {
            var t = G.Parameters[0];
            var t2 = F.Parameters[0];
            var exp = ParameterRebinder.ReplaceParameters(t2, G.Body, F.Body);
            return Expression.Lambda<Func<T, T3>>(exp, new ParameterExpression[] { t });
        }

        public static Expression<Func<T, T3>> FoG<T, T2, T3>(this Expression<Func<T, T2>> F, Expression<Func<T2, T3>> G)
        {
            var t = F.Parameters[0];
            var t2 = G.Parameters[0];
            var exp = ParameterRebinder.ReplaceParameters(t2, F.Body, G.Body);
            return Expression.Lambda<Func<T, T3>>(exp, new ParameterExpression[] { t });
        }

        public static Expression<Func<T, NCT, T3>> FoG<T, T2, T3, NCT>(this Expression<Func<T2, NCT, T3>> F, Expression<Func<T, T2>> G)
        {
            var t = G.Parameters[0];
            var t2 = F.Parameters[0];
            var exp = ParameterRebinder.ReplaceParameters(t2, G.Body, F.Body);
            return Expression.Lambda<Func<T, NCT, T3>>(exp, new ParameterExpression[] { t, F.Parameters[1] });
        }
    }
}

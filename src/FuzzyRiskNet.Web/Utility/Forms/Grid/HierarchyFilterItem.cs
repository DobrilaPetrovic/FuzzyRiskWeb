using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nik.Expressions;
using MVCFormsLibrary;
using System.Web.Routing;
using FuzzyRiskNet.Libraries.Forms;
using System.Web.Mvc;
using System.Data.Entity;

namespace FuzzyRiskNet.Libraries.Grid
{
    public class HierarchyFilterItem<T> : FilterItem, IQueryFilterItem<T>, IHierarchyFilterItem where T : class
    {
        public HierarchyFilterItem() { this.ViewName = "Hierarchy"; IsAutoPostBack = true; }
        public int? Value { get; set; }

        public void ParseValueSafe(string Value) { this.Value = S.SafeParseInt(Value); }
        public virtual string ValueStr { get { return Value.ToString(); } }

        public Expression<Func<T, T>> GetParent;
        public Expression<Func<T, int>> GetID;
        public Expression<Func<T, string>> GetTitle;
        public DbSet<T> Set;

        public virtual IQueryable<T> Filter(IQueryable<T> Query)
        {
            var getparentidexp = ParameterRebinder.FoG(GetID, GetParent);
            if (Value == -1) return Query;
            if (Value.HasValue)
                return Query.Where(Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(getparentidexp.Body, Expression.Constant(Value)),
                    new ParameterExpression[] { getparentidexp.Parameters[0] }));
            else
                return Query.Where(Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(GetParent.Body, Expression.Constant(null, typeof(object))),
                    new ParameterExpression[] { GetParent.Parameters[0] }));
        }

        public SelectListItem AllItemsLink(UrlHelper Url)
        {
            return new SelectListItem()
            {
                Text = Messages.AllItems,
                Value = Value == -1 ? "" : GetLink(Url, Name, "-1")
            };
        }

        public static string GetLink(UrlHelper Url, string Name, string Value)
        {
            var q = Url.RequestContext.HttpContext.Request.QueryString;
            var current = new RouteValueDictionary(q.AllKeys.ToDictionary(d => d, d => (object)q[d]));
            if (current.ContainsKey(Name)) current[Name] = Value;
            else
                current.Add(Name, Value);
            return Url.Action(null, current);
        }

        public virtual IEnumerable<SelectListItem> GetAllLinks(UrlHelper Url)
        {

            yield return new SelectListItem()
            {
                Text = Messages.Root,
                Value = !Value.HasValue && Value != -1 ? "" : GetLink(Url, Name, " ")
            };

            if (!Value.HasValue || Value == -1) yield break;

            var items = new List<SelectListItem>();

            var val = Value.Value;

            while (true)
            {
                var getexp = Expression.Lambda<Func<T, bool>>(
                        Expression.Equal(GetID.Body, Expression.Constant(val)),
                        new ParameterExpression[] { GetID.Parameters[0] });

                var t = GetParent.Parameters[0];
                Expression exp = t;
                var item = Set.Where(getexp).Select(Expression.Lambda<Func<T, Nodes>>(
                    Expression.MemberInit(Expression.New(typeof(Nodes)),
                        Expression.Bind(typeof(Nodes).GetProperty("Node1"), NewNodeInfo(exp)),
                        Expression.Bind(typeof(Nodes).GetProperty("Node2"), NewNodeInfo(exp = ParameterRebinder.ReplaceParameters(GetParent.Parameters[0], exp, GetParent.Body))),
                        Expression.Bind(typeof(Nodes).GetProperty("Node3"), NewNodeInfo(exp = ParameterRebinder.ReplaceParameters(GetParent.Parameters[0], exp, GetParent.Body))),
                        Expression.Bind(typeof(Nodes).GetProperty("Node4"), NewNodeInfo(exp = ParameterRebinder.ReplaceParameters(GetParent.Parameters[0], exp, GetParent.Body))),
                        Expression.Bind(typeof(Nodes).GetProperty("Node5"), NewNodeInfo(exp = ParameterRebinder.ReplaceParameters(GetParent.Parameters[0], exp, GetParent.Body))),
                        Expression.Bind(typeof(Nodes).GetProperty("Node6"), NewNodeInfo(exp = ParameterRebinder.ReplaceParameters(GetParent.Parameters[0], exp, GetParent.Body)))
                    ), t)).First();

                items.InsertRange(0, new NodeInfo[] { item.Node1, item.Node2, item.Node3, item.Node4, item.Node5, item.Node6 }.Reverse().Select(i => i.CreateSelectItem(Url, Value.Value, (url, id) => GetLink(Url, Name, id))).Where(i => i != null));

                if (!item.Node6.ParentID.HasValue) break; else val = item.Node6.ParentID.Value;
            }

            foreach (var i in items) { yield return i; }
        }

        private MemberInitExpression NewNodeInfo(Expression Item)
        {
            return Expression.MemberInit(Expression.New(typeof(NodeInfo)),
                                    new MemberBinding[] {
                            Expression.Bind(typeof(NodeInfo).GetProperty("Title"), ParameterRebinder.ReplaceParameters(GetTitle.Parameters[0], Item, GetTitle.Body)),
                            Expression.Bind(typeof(NodeInfo).GetProperty("ID"), ParameterRebinder.ReplaceParameters(GetID.Parameters[0], Item, GetID.Body)),
                            Expression.Bind(typeof(NodeInfo).GetProperty("ParentID"), ParameterRebinder.ReplaceParameters(GetParent.Parameters[0], Item, ParameterRebinder.FoG(GetID, GetParent).Body)),
                        });
        }

        public class Nodes
        {
            public NodeInfo Node1 { get; set; }
            public NodeInfo Node2 { get; set; }
            public NodeInfo Node3 { get; set; }
            public NodeInfo Node4 { get; set; }
            public NodeInfo Node5 { get; set; }
            public NodeInfo Node6 { get; set; }
        }

        public class NodeInfo
        {
            public string Title { get; set; }
            public int? ID { get; set; }
            public int? ParentID { get; set; }

            public SelectListItem CreateSelectItem(UrlHelper Url, int Value, Func<UrlHelper, string, string> GetLink)
        {
                if (!ID.HasValue) return null;
                return new SelectListItem() { Text = Title, Value = ID == Value ? "" : GetLink(Url, ID.ToString()) };
            }
        }
    }
}

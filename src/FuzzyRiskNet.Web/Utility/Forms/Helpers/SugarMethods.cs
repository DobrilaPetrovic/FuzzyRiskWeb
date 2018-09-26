using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;

internal static class S
{
    #region Sugar methods

    public static string GetAttrVal(XmlNode node, string attr, string defaultval) { if (node.Attributes[attr] == null) return defaultval; else return node.Attributes[attr].Value; }

    public static int? SafeParseInt(string txt)
    {
        if (txt == null) return null;
        int f;
        if (int.TryParse(txt, out f)) return f;
        return null;
    }
    public static long? SafeParseLong(string txt)
    {
        if (txt == null) return null;
        long f;
        if (long.TryParse(txt, out f)) return f;
        return null;
    }
    public static float? SafeParseFloat(string txt)
    {
        if (txt == null) return null;
        float f;
        if (float.TryParse(txt, out f)) return f;
        return null;
    }
    public static double? SafeParseDbl(string txt)
    {
        if (txt == null) return null;
        double f;
        if (double.TryParse(txt, out f)) return f;
        return null;
    }
    public static bool? SafeParseBool(string txt)
    {
        if (txt == null) return null;
        bool f;
        if (bool.TryParse(txt, out f)) return f;
        return null;
    }
    // Use ?? operator instead.
/*    public static T NullIs<T>(Nullable<T> v, T def) where T : struct
    {
        if (v == null) return def;
        return v.Value;
    } */
    public static Nullable<T> NullIf<T>(T v, T converttonull) where T : struct
    {
        if (v.Equals(converttonull)) return null;
        return v;
    }
    public static float? FloatNullIf(float v, float converttonull)
    {
        if (v == converttonull) return null;
        return v;
    }
    public static double? DoubleNullIf(double v, double converttonull)
    {
        if (v == converttonull) return null;
        return v;
    }
    public static string StringNullIf(string v, string converttonull)
    {
        if (v == converttonull) return null;
        return v;
    }

    public static Type ParseType(string type)
    {
        switch (type.ToLower())
        {
            case "number": return typeof(double);
            case "string": return typeof(string);
            case "boolean": return typeof(bool);
            default: throw new ArgumentException("Invalid type", "type");
        }
    }

    public static string GetVarTableName(string address) { return address.Substring(0, address.IndexOf('(')); }
    public static string[] GetVarTableIndex(string address) 
    { 
        int ind = address.IndexOf('('); 
        string[] inds = address.Substring(ind + 1, address.IndexOf(')') - ind - 1).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < inds.Length; i++) inds[i] = inds[i].Trim();
        return inds;
    }

    public static string FindPath(XmlNode node)
    {
        if (node == node.OwnerDocument.DocumentElement) return "/" + node.Name;
        int pos = 1;
        foreach (XmlNode xn in node.ParentNode.ChildNodes)
            if (xn == node) return FindPath(node.ParentNode) + "/" + node.Name + "[" + pos.ToString() + "]";
            else if (xn.Name == node.Name) pos++;
        return null;
    }
    public static void SetNodeAttribute(XmlNode CurrentNode, string attrname, string value, bool convertnulltoempty)
    {
        if (!convertnulltoempty && (value == null || value == ""))
        {
            if (CurrentNode.Attributes[attrname] != null) CurrentNode.Attributes.Remove(CurrentNode.Attributes[attrname]);
        }
        else
        {
            if (CurrentNode.Attributes[attrname] == null) CurrentNode.Attributes.Append(CurrentNode.OwnerDocument.CreateAttribute(attrname));
            CurrentNode.Attributes[attrname].Value = value ?? "";
        }

    }
    public static void SetNodeAttribute(XmlNode CurrentNode, string attrname, string value)
    {
        SetNodeAttribute(CurrentNode, attrname, value, true);
    }
    public static string GetNodeAttribute(XmlNode CurrentNode, string attrname, string defaultvalue)
    {
        if (CurrentNode.Attributes[attrname] != null) return CurrentNode.Attributes[attrname].Value;
        return defaultvalue;
    }
    public static int IndexOfChildNode(XmlNode parent, XmlNode child)
    {
        for (int i = 0; i < parent.ChildNodes.Count; i++)
            if (parent.ChildNodes[i] == child) return i;
        throw new Exception("Child is not a child of Parent !");
    }

    #endregion
}

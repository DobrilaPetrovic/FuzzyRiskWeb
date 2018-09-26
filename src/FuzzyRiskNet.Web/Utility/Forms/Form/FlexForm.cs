using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MVCFormsLibrary;
using System.IO;
using System.Xml;
using FuzzyRiskNet.Libraries.Forms;
using System.Data.Entity;
using System.Web.Mvc;

namespace FuzzyRiskNet.Libraries.Forms
{
    public abstract class FlexForm<T> : FormModel<T>, IPageFormModel, IFormHasSettableDbContext 
    {
        public FlexForm() { ViewName = "EditForm"; HasObjectSet = false; } 

        public T Object { get; set; }

        public DbContext DB { get; set; }

        public void GetObject() { GetObject(Object); }

        public virtual string ViewName { get; protected set; }

        public void SetObject() { SetObject(Object); HasObjectSet = true; }

        public bool HasObjectSet { get; private set; }
    
        public virtual IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink() { Link = Url.Action("Index"), Text = Messages.ReturnToListTitle };
        }
    }

    public interface ISerializableForm : IFormModel, IPageFormModel, IFormHasSettableDbContext
    {
        void GetObject(string Document);
        string SetObject();
        object Object { get; }
        string ViewName { get; }
    }

    public abstract class SerializableForm<T> : FormModel<T>, ISerializableForm where T : new()
    {
        public SerializableForm() { ViewName = "EditForm"; Object = new T(); } 

        public DbContext DB { get; set; }

        public T Object { get; private set; }

        object ISerializableForm.Object { get { return this.Object; } }

        public void GetObject(string Document) { this.Object = XmlDeserialize(Document); GetObject(this.Object); }

        public virtual string ViewName { get; protected set; }

        public string SetObject() { SetObject(this.Object); return XmlSerialize(this.Object); }
    
        public virtual IEnumerable<FormLink> CreateFormLinks(UrlHelper Url)
        {
            yield return new FormLink() { Link = Url.Action("Index"), Text = Messages.ReturnToListTitle };
        }

        public static string XmlSerialize(T obj)
        {
            var stringwriter = new StringWriter();
            try
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(obj.GetType());
                x.Serialize(stringwriter, obj);                
                return stringwriter.ToString();
            }
            finally
            {
                stringwriter.Close();
            }
        }

        public static T XmlDeserialize(string xml)
        {
            if (xml == null || xml.Trim() == "") return new T();
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(T));
            var stringreader = new StringReader(xml);
            try
            {
                //if (x.CanDeserialize(XmlReader.Create(stringreader)))
                return (T)x.Deserialize(stringreader);
                //else return new T();
            }
            finally
            {
                stringreader.Close();
            }
        } 
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using MVCFormsLibrary;
using FuzzyRiskNet.Libraries.Forms;
using Microsoft.Owin;
using System.Collections.Specialized;

namespace FuzzyRiskNet.Libraries.Forms
{
    public interface IRequiredMultiPartFormField : IFormField
    {
        void Deserialize(NameValueCollection Form, HttpFileCollection Files, string Scope);
    }

    public class UploadFileField<T> : IFormField<T>, IRequiredMultiPartFormField, IOrdinaryFormField
    {
        public string Title { get; set; }

        public string FieldName { get; set; }

        public bool IsOptional { get; set; }

        public virtual bool IsVisible { get; set; }

        public string ViewName { get; set; }

        public Action<T, IFormField<T>, byte[]> CustomContentSetObject { get; set; }
        public Func<T, IFormField<T>, byte[]> CustomContentGetObject { get; set; }

        public Action<T, IFormField<T>, string> CustomFileNameSetObject { get; set; }
        public Func<T, IFormField<T>, string> CustomFileNameGetObject { get; set; }
        
        public string FileNameFieldName { get; set; }

        public string FileNameValue { get; set; }

        public byte[] FileContent { get; set; }

        public void SetObject(T Obj)
        {
            if (CustomContentSetObject == null)
            {
                if (FieldName == null) throw new ArgumentException("FieldName should be set.");
                if (FileNameFieldName == null) throw new ArgumentException("FileNameFieldName should be set.");
                SimpleFormField<T, string>.SetObject(Obj, FieldName, FileContent);
                SimpleFormField<T, string>.SetObject(Obj, FileNameFieldName, FileNameValue);
            }
            else
            {
                if (CustomFileNameSetObject == null) throw new ArgumentException("CustomContentSetObject and CustomFileNameSetObject should be set together.");
                CustomContentSetObject(Obj, this, FileContent);
                CustomFileNameSetObject(Obj, this, FileNameValue);
            }
        }

        public void GetObject(T Obj)
        {
            if (CustomContentGetObject == null)
            {
                if (FieldName == null) throw new ArgumentException("FieldName should be set.");
                if (FileNameFieldName == null) throw new ArgumentException("FileNameFieldName should be set.");
                FileContent = SimpleFormField<T, string>.GetObject<byte[]>(Obj, FieldName);
                FileNameValue = SimpleFormField<T, string>.GetObject<string>(Obj, FileNameFieldName);
            }
            else
            {
                if (CustomFileNameGetObject == null) throw new ArgumentException("CustomContentGetObject and CustomFileNameGetObject should be set together.");
                FileContent = CustomContentGetObject(Obj, this);
                FileNameValue = CustomFileNameGetObject(Obj, this);
            }
        }

        public string GenerateFieldHtml(string Scope)
        {
            return string.Format("<input type='file' id='{0}' name='{0}' size='9' />", Scope + FieldName);
        }

        public void Deserialize(NameValueCollection Form, HttpFileCollection Files, string Scope)
        {
            if (Files[FieldName] != null && Files[FieldName].ContentLength > 0)
            {
                FileContent = new BinaryReader(Files[FieldName].InputStream).ReadBytes((int)Files[FieldName].ContentLength);
                FileNameValue = Files[FieldName].FileName;
            }
            else
            {
                FileContent = null;
                FileNameValue = null;
            }
        }

        public void Deserialize(NameValueCollection Form, string Scope)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetValidationErrors()
        {
            if (!IsOptional && (FileNameValue == null || FileNameValue == "")) yield return string.Format(Messages.RequiredMessage, Title);
            yield break;
        }

        public string ReadOnlyValue
        {
            get { return "(" + FileNameValue ?? "" + ")"; }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using ServiceStack.Text;
using JsonSerializer = ServiceStack.Text.JsonSerializer;

namespace Owin.Web.Formatters
{
    public class ServiceStackJsonFormatter : MediaTypeFormatter, ISerializer,IBodyDeserializer
    {

        public ServiceStackJsonFormatter()
        {
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));

            SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true));
        }

        public override bool CanReadType(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            return true;
        }

        public bool CanSerialize(string contentType)
        {
            return IsJson(contentType);
        }

        public bool CanDeserialize(string contentType, BindingContext context)
        {
            return CanSerialize(contentType);
        }

        public IEnumerable<string> Extensions
        {
            get { yield return "json"; }
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, System.Net.Http.HttpContent content, IFormatterLogger formatterLogger)
        {
            var task = Task<object>.Factory.StartNew(() => JsonSerializer.DeserializeFromStream(type, readStream));
            return task;
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, System.Net.Http.HttpContent content, TransportContext transportContext)
        {
            var task = Task.Factory.StartNew(() => JsonSerializer.SerializeToStream(value, type, writeStream));
            return task;
        }

        public void Serialize<TModel>(string contentType, TModel model, Stream outputStream)
        {
            JsonSerializer.SerializeToStream(model, outputStream);
        }

        public object Deserialize(string contentType, Stream bodyStream, BindingContext context)
        {
            var deserializedObject = JsonSerializer.DeserializeFromStream(context.DestinationType, bodyStream);
            if (context.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Except(context.ValidModelProperties).Any())
            {
                return CreateObjectWithBlacklistExcluded(context, deserializedObject);
            }

            return deserializedObject;
        }

        private object CreateObjectWithBlacklistExcluded(BindingContext context, object deserializedObject)
        {
            var returnObject = Activator.CreateInstance(context.DestinationType);

            foreach (var property in context.ValidModelProperties)
            {
                CopyPropertyValue(property, deserializedObject, returnObject);
            }

            return returnObject;
        }

        private void CopyPropertyValue(PropertyInfo property, object sourceObject, object destinationObject)
        {
            property.SetValue(destinationObject, property.GetValue(sourceObject, null), null);
        }

        private static bool IsJson(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }
            var contentMimeType = contentType.Split(';')[0];
            return contentMimeType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase) ||
                   contentMimeType.Equals("text/json", StringComparison.InvariantCultureIgnoreCase) ||
                   (contentMimeType.StartsWith("application/vnd", StringComparison.InvariantCultureIgnoreCase) &&
                    contentMimeType.EndsWith("+json", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
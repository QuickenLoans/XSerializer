using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Xml;
using XSerializer.Encryption;

namespace XSerializer
{
    internal class DynamicSerializer : IXmlSerializerInternal
    {
        private readonly EncryptAttribute _encryptAttribute;
        private readonly IXmlSerializerOptions _options;

        public static IXmlSerializerInternal GetSerializer<T>(EncryptAttribute encryptAttribute, IXmlSerializerOptions options)
        {
            var serializer = new DynamicSerializer(encryptAttribute, options);

            if (typeof(T) == typeof(object))
            {
                return serializer;
            }
            
            if (typeof(T) == typeof(ExpandoObject))
            {
                return new DynamicSerializerExpandoObjectProxy(serializer);
            }
            
            throw new InvalidOperationException("The only valid generic arguments for DynamicSerializer.GetSerializer<T> are object, dynamic, and ExpandoObject");
        }

        public DynamicSerializer(EncryptAttribute encryptAttribute, IXmlSerializerOptions options)
        {
            _encryptAttribute = encryptAttribute;
            _options = options;
        }

        public void SerializeObject(XSerializerXmlTextWriter writer, object instance, ISerializeOptions options)
        {
            var expando = instance as ExpandoObject;
            if (expando != null || instance == null)
            {
                SerializeExpandoObject(writer, expando, options);
                return;
            }

            IXmlSerializerInternal serializer;

            if (!options.ShouldAlwaysEmitTypes || instance.IsAnonymous())
            {
                serializer = CustomSerializer.GetSerializer(instance.GetType(), _encryptAttribute, _options);
            }
            else
            {
                serializer = CustomSerializer.GetSerializer(typeof(object), _encryptAttribute, _options.WithAdditionalExtraTypes(instance.GetType()));
            }

            serializer.SerializeObject(writer, instance, options);
        }

        public object DeserializeObject(XSerializerXmlReader reader, ISerializeOptions options)
        {
            var isNil = reader.IsNil();

            if (isNil && reader.IsEmptyElement)
            {
                return null;
            }

            object deserializedObject;

            var type = reader.GetXsdType<object>(_options.ExtraTypes);

            if (type != null)
            {
                var serializer = XmlSerializerFactory.Instance.GetSerializer(type, _encryptAttribute, _options.WithRootElementName(reader.Name));
                deserializedObject = serializer.DeserializeObject(reader, options);
            }
            else
            {
                deserializedObject = DeserializeToDynamic(reader, options);
            }

            return
                isNil
                    ? null
                    : deserializedObject;
        }

        private void SerializeExpandoObject(XSerializerXmlTextWriter writer, IDictionary<string, object> expando, ISerializeOptions options)
        {
            if (expando == null && !options.ShouldEmitNil)
            {
                return;
            }

            writer.WriteStartDocument();
            writer.WriteStartElement(_options.RootElementName);
            writer.WriteDefaultDocumentNamespaces();

            using (writer.WriteDefaultNamespace(_options.DefaultNamespace))
            {
                if (expando == null)
                {
                    writer.WriteNilAttribute();
                    writer.WriteEndElement();
                    return;
                }

                var setIsEncryptionEnabledBackToFalse = writer.MaybeSetIsEncryptionEnabledToTrue(_encryptAttribute, options);

                foreach (var property in expando)
                {
                    if (property.Value == null)
                    {
                        continue;
                    }

                    IXmlSerializerInternal serializer;

                    if (property.Value is ExpandoObject)
                    {
                        serializer = DynamicSerializer.GetSerializer<ExpandoObject>(null, _options.WithRootElementName(property.Key));
                    }
                    else
                    {
                        serializer = CustomSerializer.GetSerializer(property.Value.GetType(), null, _options.WithRootElementName(property.Key));
                    }

                    serializer.SerializeObject(writer, property.Value, options);
                }

                if (setIsEncryptionEnabledBackToFalse)
                {
                    writer.IsEncryptionEnabled = false;
                }

                writer.WriteEndElement();
            }
        }

        private dynamic DeserializeToDynamic(XSerializerXmlReader reader, ISerializeOptions options)
        {
            object instance = null;
            var hasInstanceBeenCreated = false;

            var setIsDecryptionEnabledBackToFalse = false;

            Func<bool> isAtRootElement;
            {
                var hasOpenedRootElement = false;

                isAtRootElement = () =>
                {
                    if (!hasOpenedRootElement && reader.Name == _options.RootElementName)
                    {
                        hasOpenedRootElement = true;
                        return true;
                    }

                    return false;
                };
            }

            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (isAtRootElement())
                        {
                            setIsDecryptionEnabledBackToFalse = reader.MaybeSetIsDecryptionEnabledToTrue(_encryptAttribute, options);

                            instance = new ExpandoObject();
                            hasInstanceBeenCreated = true;

                            if (reader.IsEmptyElement)
                            {
                                if (_options.TreatEmptyElementAsString)
                                {
                                    instance = "";
                                }

                                if (setIsDecryptionEnabledBackToFalse)
                                {
                                    reader.IsDecryptionEnabled = false;
                                }

                                return instance;
                            }
                        }
                        else
                        {
                            SetElementPropertyValue(reader, hasInstanceBeenCreated, options, (ExpandoObject)instance);
                        }
                        break;
                    case XmlNodeType.Text:
                        var stringValue = (string)new XmlTextSerializer(typeof(string), _options.RedactAttribute, null, _options.ExtraTypes).DeserializeObject(reader, options);
                        hasInstanceBeenCreated = true;

                        bool boolValue;
                        if (bool.TryParse(stringValue, out boolValue))
                        {
                            instance = boolValue;
                            break;
                        }

                        int intValue;
                        if (int.TryParse(stringValue, NumberStyles.Integer, options.GetCulture(), out intValue))
                        {
                            // If this is a number with leading zeros, treat it as a string so we don't lose those leading zeros.
                            if (stringValue[0] == '0' && stringValue.Length > 1)
                            {
                                instance = stringValue;
                            }
                            else
                            {
                                instance = intValue;
                            }

                            break;
                        }

                        decimal decimalValue;
                        if (decimal.TryParse(stringValue, NumberStyles.Float, options.GetCulture(), out decimalValue))
                        {
                            instance = decimalValue;
                            break;
                        }

                        DateTime dateTimeValue;
                        if (DateTime.TryParse(stringValue, options.GetCulture(), DateTimeStyles.None, out dateTimeValue)
                            || DateTime.TryParse(stringValue, out dateTimeValue))
                        {
                            instance = dateTimeValue.ToUniversalTime();
                            break;
                        }

                        // TODO: add more types to check?

                        instance = stringValue;
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == _options.RootElementName)
                        {
                            if (_options.TreatEmptyElementAsString)
                            {
                                var instanceAsExpando = instance as IDictionary<string, object>;
                                if (instanceAsExpando != null && instanceAsExpando.Count == 0)
                                {
                                    instance = "";
                                }
                            }

                            if (setIsDecryptionEnabledBackToFalse)
                            {
                                reader.IsDecryptionEnabled = false;
                            }

                            return CheckAndReturn(hasInstanceBeenCreated, instance);
                        }
                        break;
                }
            } while (reader.Read());

            throw new InvalidOperationException("Deserialization error: reached the end of the document without returning a value.");
        }

        private void SetElementPropertyValue(XSerializerXmlReader reader, bool hasInstanceBeenCreated, ISerializeOptions options, IDictionary<string, object> expando)
        {
            var propertyName = reader.Name;
            var serializer = DynamicSerializer.GetSerializer<object>(null, _options.WithRootElementName(reader.Name));
            var value = serializer.DeserializeObject(reader, options);
            expando[propertyName] = value;
        }

        private static object CheckAndReturn(bool hasInstanceBeenCreated, object instance)
        {
            if (!hasInstanceBeenCreated)
            {
                throw new InvalidOperationException("Deserialization error: attempted to return a deserialized instance before it was created.");
            }

            return instance;
        }

        private class DynamicSerializerExpandoObjectProxy : IXmlSerializerInternal
        {
            private readonly DynamicSerializer _serializer;

            public DynamicSerializerExpandoObjectProxy(DynamicSerializer serializer)
            {
                _serializer = serializer;
            }

            public void SerializeObject(XSerializerXmlTextWriter writer, object instance, ISerializeOptions options)
            {
                _serializer.SerializeExpandoObject(writer, (ExpandoObject)instance, options);
            }

            public object DeserializeObject(XSerializerXmlReader reader, ISerializeOptions options)
            {
                return (ExpandoObject) _serializer.DeserializeToDynamic(reader, options);
            }
        }
    }
}

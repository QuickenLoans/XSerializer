using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace XSerializer
{
    public sealed class SerializableProperty
    {
        private readonly Lazy<IXmlSerializer> _serializer;

        private readonly Func<object, object> _getValueFunc;
        private readonly Action<object, object> _setValueFunc;
        private readonly Func<object, bool> _shouldSerializeFunc;

        private Func<object, IEnumerator> _getEnumeratorFunc;

        public SerializableProperty(PropertyInfo propertyInfo, string defaultNamespace, Type[] extraTypes)
        {
            _getValueFunc = DynamicMethodFactory.CreateFunc<object>(propertyInfo.GetGetMethod());
            _setValueFunc =
                propertyInfo.IsSerializableReadOnlyProperty()
                    ? GetSerializableReadonlyPropertySetValueFunc(propertyInfo)
                    : DynamicMethodFactory.CreateAction(propertyInfo.GetSetMethod());
            _shouldSerializeFunc = GetShouldSerializeFunc(propertyInfo);
            _serializer = new Lazy<IXmlSerializer>(GetCreateSerializerFunc(propertyInfo, defaultNamespace, extraTypes));
            _getEnumeratorFunc = DynamicMethodFactory.CreateFunc<IEnumerator>(typeof(IEnumerable).GetMethod("GetEnumerator"));
        }

        public string Name { get; private set; }

        public NodeType NodeType { get; private set; }

        public bool UsesDefaultSerializer
        {
            get { return _serializer.Value is DefaultSerializer; }
        }

        public void ReadValue(XmlReader reader, object instance)
        {
            _setValueFunc(instance, _serializer.Value.DeserializeObject(reader));
        }

        public void WriteValue(SerializationXmlTextWriter writer, object instance, XmlSerializerNamespaces namespaces)
        {
            if (_shouldSerializeFunc(instance))
            {
                var value = _getValueFunc(instance);
                if (value != null)
                {
                    _serializer.Value.SerializeObject(writer, value, namespaces);
                }
            }
        }

        private Func<IXmlSerializer> GetCreateSerializerFunc(PropertyInfo propertyInfo, string defaultNamespace, Type[] extraTypes)
        {
            var attributeAttribute = (XmlAttributeAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute));
            if (attributeAttribute != null)
            {
                var attributeName = !string.IsNullOrWhiteSpace(attributeAttribute.AttributeName) ? attributeAttribute.AttributeName : propertyInfo.Name;
                NodeType = NodeType.Attribute;
                Name = attributeName;
                return () => new XmlAttributeSerializer(attributeName, propertyInfo.PropertyType);
            }

            var textAttribute = (XmlTextAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(XmlTextAttribute));
            if (textAttribute != null)
            {
                NodeType = NodeType.Text;
                Name = propertyInfo.Name;
                return () => new XmlTextSerializer(propertyInfo.PropertyType);
            }

            var elementAttribute = (XmlElementAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), false);

            string rootElementName;
            if (elementAttribute != null && !string.IsNullOrWhiteSpace(elementAttribute.ElementName))
            {
                rootElementName = elementAttribute.ElementName;
            }
            else
            {
                rootElementName = propertyInfo.Name;
            }

            NodeType = NodeType.Element;
            Name = rootElementName;
            return () => XmlSerializerFactory.Instance.GetSerializer(propertyInfo.PropertyType, defaultNamespace, extraTypes, rootElementName);
        }

        private Action<object, object> GetSerializableReadonlyPropertySetValueFunc(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsAssignableToNonGenericIDictionary())
            {
                return (instance, value) =>
                    {
                        var instanceDictionary = (IDictionary)_getValueFunc(instance);
                        foreach (DictionaryEntry entry in (IDictionary)value)
                        {
                            instanceDictionary.Add(entry.Key, entry.Value);
                        }
                    };
            }

            if (propertyInfo.PropertyType.IsAssignableToGenericIDictionary())
            {
                var genericIDictionaryType = propertyInfo.PropertyType.GetGenericIDictionaryType();

                var addMethod = genericIDictionaryType.GetMethod("Add", genericIDictionaryType.GetGenericArguments());
                var addToDictionaryFunc = DynamicMethodFactory.CreateTwoArgAction(addMethod);

                var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(genericIDictionaryType.GetGenericArguments());
                var getKeyFunc = DynamicMethodFactory.CreateGetPropertyValueFunc(keyValuePairType, "Key");
                var getValueFunc = DynamicMethodFactory.CreateGetPropertyValueFunc(keyValuePairType, "Value");

                return (copyToInstance, copyFromDictionary) =>
                    {
                        var copyToDictionary = _getValueFunc(copyToInstance);
                        var valueEnumerator = _getEnumeratorFunc(copyFromDictionary);
                        while (valueEnumerator.MoveNext())
                        {
                            addToDictionaryFunc(copyToDictionary, getKeyFunc(valueEnumerator.Current), getValueFunc(valueEnumerator.Current));
                        }
                    };
            }

            throw new InvalidOperationException("Unknown property type - cannot determine the 'SetValueFunc'.");
        }

        private Func<object, bool> GetShouldSerializeFunc(PropertyInfo propertyInfo)
        {
            var xmlIgnoreAttribute = Attribute.GetCustomAttribute(propertyInfo, typeof(XmlIgnoreAttribute));
            if (xmlIgnoreAttribute != null)
            {
                return instance => false;
            }

            Func<object, bool> specifiedFunc = null;
            var specifiedProperty = propertyInfo.DeclaringType.GetProperty(propertyInfo.Name + "Specified");
            if (specifiedProperty != null && specifiedProperty.CanRead)
            {
                specifiedFunc = DynamicMethodFactory.CreateFunc<bool>(specifiedProperty.GetGetMethod());
            }

            Func<object, bool> shouldSerializeFunc = null;
            var shouldSerializeMethod = propertyInfo.DeclaringType.GetMethod("ShouldSerialize" + propertyInfo.Name, Type.EmptyTypes);
            if (shouldSerializeMethod != null)
            {
                shouldSerializeFunc = DynamicMethodFactory.CreateFunc<bool>(shouldSerializeMethod);
            }

            if (specifiedFunc == null && shouldSerializeFunc == null)
            {
                return instance => true;
            }

            if (specifiedFunc != null && shouldSerializeFunc == null)
            {
                return specifiedFunc;
            }

            if (specifiedFunc == null)
            {
                return shouldSerializeFunc;
            }

            return instance => specifiedFunc(instance) && shouldSerializeFunc(instance);
        }
    }
}
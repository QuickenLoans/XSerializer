using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace XSerializer
{
    public class CustomSerializer
    {
        private static readonly Dictionary<int, IXmlSerializer> _serializerCache = new Dictionary<int, IXmlSerializer>();
        protected static readonly Dictionary<int, Type> _typeCache = new Dictionary<int, Type>();

        public static IXmlSerializer GetSerializer(Type type, string defaultNamespace, Type[] extraTypes, string rootElementName)
        {
            IXmlSerializer serializer;
            var key = XmlSerializerFactory.Instance.CreateKey(type, defaultNamespace, extraTypes, rootElementName);

            if (!_serializerCache.TryGetValue(key, out serializer))
            {
                try
                {
                    serializer =
                        (IXmlSerializer)
                        Activator.CreateInstance(typeof (CustomSerializer<>).MakeGenericType(type), defaultNamespace,
                                                 extraTypes, rootElementName);
                }
                // True exception gets masked due to reflection. Preserve stacktrace and rethrow
                catch (TargetInvocationException ex)
                {
                    PreserveStackTrace(ex);

                    throw ex.InnerException;
                }
                _serializerCache[key] = serializer;
            }

            return serializer;
        }

        protected static int CreateTypeCacheKey<T>(string typeName)
        {
            unchecked
            {
                var key = typeof(T).GetHashCode();
                key = (key * 397) ^ typeName.GetHashCode();
                return key;
            }
        }

        //Stackoverflow is awesome
        private static void PreserveStackTrace(Exception e)
        {
            var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var mgr = new ObjectManager(null, ctx);
            var si = new SerializationInfo(e.GetType(), new FormatterConverter());

            e.GetObjectData(si, ctx);
            mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
            mgr.DoFixups(); // ObjectManager calls SetObjectData

            // voila, e is unmodified save for _remoteStackTraceString
        }
    }

    public class CustomSerializer<T> : CustomSerializer, IXmlSerializer<T>
    {
        private readonly string _defaultNamespace;
        private readonly string _rootElementName;
        private readonly Dictionary<Type, SerializableProperty[]> _serializablePropertiesMap = new Dictionary<Type, SerializableProperty[]>();

        public CustomSerializer(string defaultNamespace, Type[] extraTypes, string rootElementName)
        {
            _defaultNamespace = defaultNamespace;
            var type = typeof (T);

            _rootElementName = !string.IsNullOrWhiteSpace(rootElementName) ? rootElementName : GetRootElement(type);

            AssertValidHeirarchy(type);

            var types = new List<Type>();

            if (extraTypes != null)
            {
                types.AddRange(extraTypes);
            }

            types.AddRange(type.GetCustomAttributes(typeof(XmlIncludeAttribute), true).Cast<XmlIncludeAttribute>().Select(a => a.Type));

            if (!type.IsInterface && !type.IsAbstract)
            {
                types.Insert(0, type);
            }

            _serializablePropertiesMap =
                types.Distinct().ToDictionary(
                    t => t,
                    t =>
                        t.GetProperties()
                        .Where(p => p.IsSerializable())
                        .Select(p => new SerializableProperty(p, defaultNamespace, extraTypes))
                        .OrderBy(p => p.NodeType)
                        .ToArray());
        }

        private void AssertValidHeirarchy(Type type)
        {
            if (type.BaseType == typeof (object)) return;

            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                var derivedXmlElement = GetAttribute<XmlElementAttribute>(property);
                var derivedXmlAttribute = GetAttribute<XmlAttributeAttribute>(property);
                var baseProperty = GetBaseProperty(property);
                var hasBaseProperty = baseProperty != null;

                if (hasBaseProperty)
                {
                    AssertPropertyHeirarchy(baseProperty, derivedXmlElement, derivedXmlAttribute);
                }

                if (derivedXmlAttribute != null && !hasBaseProperty)
                {
                    if(string.IsNullOrWhiteSpace(derivedXmlAttribute.AttributeName))
                        throw new InvalidOperationException("XmlAttribute must have a value.");
                }

                if (derivedXmlElement != null && !hasBaseProperty)
                {
                    if (string.IsNullOrWhiteSpace(derivedXmlElement.ElementName))
                        throw new InvalidOperationException("XmlElement must have a value.");
                }
            }
        }

        private void AssertPropertyHeirarchy(PropertyInfo baseProperty, XmlElementAttribute derivedXmlElement, XmlAttributeAttribute derivedXmlAttribute)
        {
            var baseXmlElement = GetAttribute<XmlElementAttribute>(baseProperty);
            var baseXmlAttribute = GetAttribute<XmlAttributeAttribute>(baseProperty);

            if (baseXmlAttribute != null)
            {
                if (derivedXmlElement != null)
                {
                    throw new InvalidOperationException("Derived XmlElement cannot override Base XmlAttribute.");
                }

                if (derivedXmlAttribute != null)
                {
                    if (string.IsNullOrWhiteSpace(derivedXmlAttribute.AttributeName))
                    {
                        if (!string.IsNullOrWhiteSpace(baseXmlAttribute.AttributeName))
                        {
                            throw new InvalidOperationException("Overridden property must have non-empty Attribute.");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(baseXmlAttribute.AttributeName) && !string.IsNullOrWhiteSpace(derivedXmlAttribute.AttributeName))
                        {
                            throw new InvalidOperationException("Virtual property must have non-empty XmlAttribute.");
                        }

                        if (!string.IsNullOrWhiteSpace(baseXmlAttribute.AttributeName) && baseXmlAttribute.AttributeName != derivedXmlAttribute.AttributeName)
                        {
                            throw new InvalidOperationException("Base property and dervied property must have the same attribute.");
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(baseXmlAttribute.AttributeName))
                    {
                        throw new InvalidOperationException("Overridden property must override XmlAttribute");
                    }

                    if (string.IsNullOrWhiteSpace(baseXmlAttribute.AttributeName)) // && string.IsNullOrWhiteSpace(derivedXmlAttribute.AttributeName)
                    {
                        throw new InvalidOperationException("Virtual property must have non-empty XmlAttribute.");
                    }
                }
            }
            else
            {
                if (derivedXmlAttribute != null)
                {
                    throw new InvalidOperationException("Virtual property must have non-empty XmlAttribute.");
                }
            }
            
            

            if (baseXmlElement != null)
            {
                if (derivedXmlAttribute != null)
                {
                    throw new InvalidOperationException("Derived XmlAttribute cannot override Base XmlElement.");
                }

                if (derivedXmlElement != null)
                {
                    if (!string.IsNullOrWhiteSpace(baseXmlElement.ElementName))
                    {
                        if (string.IsNullOrWhiteSpace(derivedXmlElement.ElementName))
                        {
                            throw new InvalidOperationException("Cannot have non-empty Xml Element.");
                        }
                        
                        if (derivedXmlElement.ElementName != baseXmlElement.ElementName)
                        {
                            throw new InvalidOperationException("Dervied Element cannot be different from Base element.");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(derivedXmlElement.ElementName))
                        {
                            throw new InvalidOperationException("Base element cannot be empty.");
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(baseXmlElement.ElementName))
                    {
                        throw new InvalidOperationException("Dervied property must override base property XmlElement.");
                    }
                }
            }
            else
            {
                if (derivedXmlElement != null && !string.IsNullOrWhiteSpace(derivedXmlElement.ElementName))
                {
                    throw new InvalidOperationException("Base property must have XmlElement.");
                }
            }
        }

        private static PropertyInfo GetBaseProperty(PropertyInfo propertyInfo)
        {
            var method = propertyInfo.GetAccessors(true)[0];
            if (method == null)
                return null;

            var baseMethod = method.GetBaseDefinition();

            if (baseMethod == method)
                return propertyInfo;

            const BindingFlags allProperties = BindingFlags.Instance | BindingFlags.Public
                                               | BindingFlags.NonPublic | BindingFlags.Static;

            var arguments = propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray();

            Debug.Assert(baseMethod.DeclaringType != null);

            return baseMethod.DeclaringType.GetProperty(propertyInfo.Name, allProperties,
                null, propertyInfo.PropertyType, arguments, null);
        }

        private TAttribute GetAttribute<TAttribute>(PropertyInfo property) where TAttribute: Attribute
        {
            return property.GetCustomAttributes(typeof(TAttribute), false).FirstOrDefault() as TAttribute;
        }

        private static string GetRootElement(Type type)
        {
            var xmlRootAttribute = (XmlRootAttribute)type.GetCustomAttributes(typeof(XmlRootAttribute), true).FirstOrDefault();
            if (xmlRootAttribute != null && !string.IsNullOrWhiteSpace(xmlRootAttribute.ElementName))
            {
                return xmlRootAttribute.ElementName;
            }
            return type.Name;
        }

        public void Serialize(SerializationXmlTextWriter writer, T instance, XmlSerializerNamespaces namespaces)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(_rootElementName);
            writer.WriteDefaultNamespaces();

            if (!string.IsNullOrWhiteSpace(_defaultNamespace))
            {
                writer.WriteAttributeString("xmlns", null, null, _defaultNamespace);
            }

            if (typeof(T).IsInterface || typeof(T).IsAbstract)
            {
                writer.WriteAttributeString("xsi", "type", null, instance.GetType().Name);
            }

            var type = instance.GetType();
            foreach (var property in _serializablePropertiesMap[type])
            {
                property.WriteValue(writer, instance, namespaces);
            }

            writer.WriteEndElement();
        }

        void IXmlSerializer.SerializeObject(SerializationXmlTextWriter writer, object instance, XmlSerializerNamespaces namespaces)
        {
            Serialize(writer, (T)instance, namespaces);
        }

        public T Deserialize(XmlReader reader)
        {
            T instance = default(T);
            var hasInstanceBeenCreated = false;

            var attributes = new Dictionary<string, string>();

            bool shouldIssueRead;

            do
            {
                shouldIssueRead = true;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == _rootElementName)
                        {
                            instance = CreateInstanceAndSetAttributePropertyValues(reader, attributes);
                            hasInstanceBeenCreated = true;
                        }
                        else
                        {
                            SetElementPropertyValue(reader, hasInstanceBeenCreated, instance, out shouldIssueRead);
                        }
                        break;
                    case XmlNodeType.Text:
                        SetTextNodePropertyValue(reader, hasInstanceBeenCreated, instance);
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == _rootElementName)
                        {
                            return CheckAndReturn(hasInstanceBeenCreated, instance);
                        }
                        break;
                    case XmlNodeType.CDATA:
                        break;
                }
            } while (reader.ReadIfNeeded(shouldIssueRead));

            throw new SerializationException("Couldn't serialize... for some reason. (You know, I should put a better exception message here...)");
        }

        object IXmlSerializer.DeserializeObject(XmlReader reader)
        {
            return Deserialize(reader);
        }

        // ReSharper disable UnusedParameter.Local
        private void SetElementPropertyValue(XmlReader reader, bool hasInstanceBeenCreated, T instance, out bool shouldIssueRead)
        {
            if (!hasInstanceBeenCreated)
            {
                throw new SerializationException("Boo hoo!");
            }

            var property = _serializablePropertiesMap[instance.GetType()].SingleOrDefault(p => reader.Name == p.Name);
            if (property != null)
            {
                property.ReadValue(reader, instance);
                shouldIssueRead = !property.UsesDefaultSerializer;
            }
            else
            {
                shouldIssueRead = true;
            }
        }

        private void SetTextNodePropertyValue(XmlReader reader, bool hasInstanceBeenCreated, T instance)
        {
            if (!hasInstanceBeenCreated)
            {
                throw new SerializationException("I'm think I'm gonna cry!");
            }

            var property = _serializablePropertiesMap[instance.GetType()].SingleOrDefault(p => p.NodeType == NodeType.Text);
            if (property != null)
            {
                property.ReadValue(reader, instance);
            }
        }

        private static T CheckAndReturn(bool hasInstanceBeenCreated, T instance)
        {
            if (!hasInstanceBeenCreated)
            {
                throw new SerializationException("Awwww, crap.");
            }

            return instance;
        }
        // ReSharper restore UnusedParameter.Local

        private T CreateInstanceAndSetAttributePropertyValues(XmlReader reader, Dictionary<string, string> attributes)
        {
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    attributes.Add(reader.Name, reader.Value);
                } while (reader.MoveToNextAttribute());
            }

            var instance = CreateInstance(attributes);

            foreach (var attribute in attributes)
            {
                var property =
                    _serializablePropertiesMap[instance.GetType()]
                        .SingleOrDefault(p => p.NodeType == NodeType.Attribute && p.Name == attribute.Key);
                if (property != null)
                {
                    property.ReadValue(reader, instance);
                }
            }

            return instance;
        }

        private static T CreateInstance(IDictionary<string, string> attributes)
        {
            T instance;

            string typeName;
            if (attributes.TryGetValue("xsi:type", out typeName))
            {
                Type type;
                var key = CreateTypeCacheKey<T>(typeName);
                if (!_typeCache.TryGetValue(key, out type))
                {
                    //// try REAL hard to get the type. (holy crap, this is UUUUUGLY!!!!)

                    var typeNameWithPossibleNamespace = typeName;

                    if (!typeName.Contains('.'))
                    {
                        typeNameWithPossibleNamespace = typeof(T).Namespace + "." + typeName;
                    }

                    var checkPossibleNamespace = typeName != typeNameWithPossibleNamespace;

                    type = Type.GetType(typeName);
                    type = typeof(T).IsAssignableFrom(type) ? type : null;

                    if (type == null)
                    {
                        type = checkPossibleNamespace ? Type.GetType(typeNameWithPossibleNamespace) : null;
                        type = typeof(T).IsAssignableFrom(type) ? type : null;

                        if (type == null)
                        {
                            type = typeof(T).Assembly.GetType(typeName);
                            type = typeof(T).IsAssignableFrom(type) ? type : null;

                            if (type == null)
                            {
                                type = checkPossibleNamespace ? typeof(T).Assembly.GetType(typeNameWithPossibleNamespace) : null;
                                type = typeof(T).IsAssignableFrom(type) ? type : null;

                                if (type == null)
                                {
                                    var matches = typeof(T).Assembly.GetTypes().Where(t => t.Name == typeName && typeof(T).IsAssignableFrom(t)).ToList();
                                    if (matches.Count == 1)
                                    {
                                        type = matches.Single();
                                    }

                                    var entryAssembly = Assembly.GetEntryAssembly();
                                    if (entryAssembly != null)
                                    {
                                        type = entryAssembly.GetType(typeName);
                                        type = typeof(T).IsAssignableFrom(type) ? type : null;

                                        if (type == null)
                                        {
                                            type = checkPossibleNamespace ? entryAssembly.GetType(typeNameWithPossibleNamespace) : null;
                                            type = typeof(T).IsAssignableFrom(type) ? type : null;
                                        }

                                        if (type == null)
                                        {
                                            matches = entryAssembly.GetTypes().Where(t => t.Name == typeName && typeof(T).IsAssignableFrom(t)).ToList();
                                            if (matches.Count == 1)
                                            {
                                                type = matches.Single();
                                            }
                                        }
                                    }

                                    if (type == null)
                                    {
                                        matches = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
                                        {
                                            try
                                            {
                                                return a.GetTypes();
                                            }
                                            catch
                                            {
                                                return Enumerable.Empty<Type>();
                                            }
                                        }).Where(t => t.Name == typeName && typeof(T).IsAssignableFrom(t)).ToList();

                                        if (matches.Count == 1)
                                        {
                                            type = matches.Single();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (type == null)
                    {
                        throw new SerializationException("WAAAAAAA!");
                    }

                    _typeCache[key] = type;
                }

                instance = (T)Activator.CreateInstance(type); // TODO: cache into constructor func
            }
            else
            {
                instance = Activator.CreateInstance<T>(); // TODO: cache into constructor func
            }
            
            return instance;
        }
    }
}
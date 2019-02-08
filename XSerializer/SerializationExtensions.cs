using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using XSerializer.Encryption;

namespace XSerializer
{
    internal static class SerializationExtensions
    {
#if NETSTANDARD2_0
        internal static readonly string ReadOnlyDictionary = typeof(ReadOnlyDictionary<,>).AssemblyQualifiedName;
        private static readonly string IReadOnlyDictionary = typeof(IReadOnlyDictionary<,>).AssemblyQualifiedName;
        private static readonly string IReadOnlyCollection = typeof(IReadOnlyCollection<>).AssemblyQualifiedName;
        private static readonly string IReadOnlyList = typeof(IReadOnlyList<>).AssemblyQualifiedName;
#else
        internal const string ReadOnlyDictionary = "System.Collections.ObjectModel.ReadOnlyDictionary`2, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string IReadOnlyDictionary = "System.Collections.Generic.IReadOnlyDictionary`2, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string IReadOnlyCollection = "System.Collections.Generic.IReadOnlyCollection`1, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string IReadOnlyList = "System.Collections.Generic.IReadOnlyList`1, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#endif

        private static readonly Type[] _readOnlyCollections = new[] { Type.GetType(IReadOnlyCollection), Type.GetType(IReadOnlyList), typeof(ReadOnlyCollection<>) }.Where(t => t != null).ToArray();

        private static readonly ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>> _convertFuncs = new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>(); 

        private static readonly Dictionary<Type, string> _typeToXsdTypeMap = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Type> _xsdTypeToTypeMap = new Dictionary<string, Type>();
        private static readonly ConcurrentDictionary<Tuple<Type, string>, Type> _xsdTypeToTypeCache = new ConcurrentDictionary<Tuple<Type, string>, Type>();

        static SerializationExtensions()
        {
            _typeToXsdTypeMap.Add(typeof(bool), "xsd:boolean");
            _typeToXsdTypeMap.Add(typeof(byte), "xsd:unsignedByte");
            _typeToXsdTypeMap.Add(typeof(sbyte), "xsd:byte");
            _typeToXsdTypeMap.Add(typeof(short), "xsd:short");
            _typeToXsdTypeMap.Add(typeof(ushort), "xsd:unsignedShort");
            _typeToXsdTypeMap.Add(typeof(int), "xsd:int");
            _typeToXsdTypeMap.Add(typeof(uint), "xsd:unsignedInt");
            _typeToXsdTypeMap.Add(typeof(long), "xsd:long");
            _typeToXsdTypeMap.Add(typeof(ulong), "xsd:unsignedLong");
            _typeToXsdTypeMap.Add(typeof(float), "xsd:float");
            _typeToXsdTypeMap.Add(typeof(double), "xsd:double");
            _typeToXsdTypeMap.Add(typeof(decimal), "xsd:decimal");
            _typeToXsdTypeMap.Add(typeof(DateTime), "xsd:dateTime");
            _typeToXsdTypeMap.Add(typeof(string), "xsd:string");

            foreach (var typeToXsdType in _typeToXsdTypeMap)
            {
                _xsdTypeToTypeMap.Add(typeToXsdType.Value, typeToXsdType.Key);
            }
        }

        public static string SerializeObject(
            this IXmlSerializerInternal serializer,
            object instance,
            Encoding encoding,
            Formatting formatting,
            ISerializeOptions options)
        {
            options = options.WithNewSerializationState();

            var sb = new StringBuilder();

            using (var stringWriter = new StringWriterWithEncoding(sb, encoding ?? Encoding.UTF8))
            {
                using (var xmlWriter = new XSerializerXmlTextWriter(stringWriter, options))
                {
                    xmlWriter.Formatting = formatting;
                    serializer.SerializeObject(xmlWriter, instance, options);
                }
            }

            return sb.ToString();
        }

        public static void SerializeObject(
            this IXmlSerializerInternal serializer,
            Stream stream,
            object instance,
            Encoding encoding,
            Formatting formatting,
            ISerializeOptions options)
        {
            options = options.WithNewSerializationState();

            StreamWriter streamWriter = null;

            try
            {
                streamWriter = new StreamWriter(stream, encoding ?? Encoding.UTF8); 

                var xmlWriter = new XSerializerXmlTextWriter(streamWriter, options)
                {
                    Formatting = formatting
                };

                serializer.SerializeObject(xmlWriter, instance, options);
                xmlWriter.Flush();
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Flush();
                }
            }
        }

        public static void SerializeObject(
            this IXmlSerializerInternal serializer,
            TextWriter writer,
            object instance,
            Formatting formatting,
            ISerializeOptions options)
        {
            options = options.WithNewSerializationState();

            var xmlWriter = new XSerializerXmlTextWriter(writer, options)
            {
                Formatting = formatting
            };

            serializer.SerializeObject(xmlWriter, instance, options);
            xmlWriter.Flush();
        }

        public static object DeserializeObject(this IXmlSerializerInternal serializer, string xml, ISerializeOptions options)
        {
            options = options.WithNewSerializationState();

            using (var stringReader = new StringReader(xml))
            {
                using (var xmlReader = new XmlTextReader(stringReader))
                {
                    using (var reader = new XSerializerXmlReader(xmlReader, options.GetEncryptionMechanism(), options.EncryptKey, options.SerializationState))
                    {
                        return serializer.DeserializeObject(reader, options);
                    }
                }
            }
        }

        public static object DeserializeObject(this IXmlSerializerInternal serializer, Stream stream, ISerializeOptions options)
        {
            options = options.WithNewSerializationState();

            var xmlReader = new XmlTextReader(stream);
            var reader = new XSerializerXmlReader(xmlReader, options.GetEncryptionMechanism(), options.EncryptKey, options.SerializationState);
            return serializer.DeserializeObject(reader, options);
        }

        public static object DeserializeObject(this IXmlSerializerInternal serializer, TextReader textReader, ISerializeOptions options)
        {
            options = options.WithNewSerializationState();

            var xmlReader = new XmlTextReader(textReader);
            var reader = new XSerializerXmlReader(xmlReader, options.GetEncryptionMechanism(), options.EncryptKey, options.SerializationState);
            return serializer.DeserializeObject(reader, options);
        }

        private static ISerializeOptions WithNewSerializationState(this ISerializeOptions serializeOptions)
        {
            return new SerializeOptions
            {
                EncryptionMechanism = serializeOptions.EncryptionMechanism,
                EncryptKey = serializeOptions.EncryptKey,
                Namespaces = serializeOptions.Namespaces,
                SerializationState = new SerializationState(),
                ShouldAlwaysEmitTypes = serializeOptions.ShouldAlwaysEmitTypes,
                ShouldEmitNil = serializeOptions.ShouldEmitNil,
                ShouldEncrypt = serializeOptions.ShouldEncrypt,
                ShouldRedact = serializeOptions.ShouldRedact,
                ShouldIgnoreCaseForEnum = serializeOptions.ShouldIgnoreCaseForEnum,
                ShouldSerializeCharAsInt = serializeOptions.ShouldSerializeCharAsInt,
                Culture = serializeOptions.Culture
            };
        }

        private class SerializeOptions : ISerializeOptions
        {
            public XmlSerializerNamespaces Namespaces { get; set; }
            public bool ShouldAlwaysEmitTypes { get; set; }
            public bool ShouldRedact { get; set; }
            public bool ShouldEncrypt { get; set; }
            public bool ShouldEmitNil { get; set; }
            public IEncryptionMechanism EncryptionMechanism { get; set; }
            public object EncryptKey { get; set; }
            public SerializationState SerializationState { get; set; }
            public bool ShouldIgnoreCaseForEnum { get; set; }
            public bool ShouldSerializeCharAsInt { get; set; }
            public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

            public SerializeOptions()
            {

            }
        }

        public static string AppendProperty(this string path, string property)
        {
            if (path == "")
            {
                return property;
            }

            return path + "." + property;
        }

        /// <summary>
        /// Maybe sets the <see cref="XSerializerXmlTextWriter.IsEncryptionEnabled"/> property of 
        /// <paramref name="writer"/> to true. Returns true if the value was changed to true, false 
        /// if it was not changed to true.
        /// </summary>
        internal static bool MaybeSetIsEncryptionEnabledToTrue(this XSerializerXmlTextWriter writer, EncryptAttribute encryptAttribute, ISerializeOptions options)
        {
            if (options.ShouldEncrypt && encryptAttribute != null && !writer.IsEncryptionEnabled)
            {
                writer.IsEncryptionEnabled = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Maybe sets the <see cref="XSerializerXmlTextWriter.IsEncryptionEnabled"/> property of 
        /// <paramref name="reader"/> to true. Returns true if the value was changed to true, false 
        /// if it was not changed to true.
        /// </summary>
        internal static bool MaybeSetIsDecryptionEnabledToTrue(this XSerializerXmlReader reader,
            EncryptAttribute encryptAttribute, ISerializeOptions options)
        {
            if (options.ShouldEncrypt && encryptAttribute != null && !reader.IsDecryptionEnabled)
            {
                reader.IsDecryptionEnabled = true;
                return true;
            }

            return false;
        }

        public static TAttribute[] GetCustomAttributes<TAttribute>(
            this Type type)
            where TAttribute : Attribute
        {
            return type
                .GetCustomAttributesImpl<TAttribute>()
                .ToArray();
        }

        public static TAttribute GetCustomAttribute<TAttribute>(
            this Type type)
            where TAttribute : Attribute
        {
            return type
                .GetCustomAttributesImpl<TAttribute>()
                .FirstOrDefault();
        }

        private static IEnumerable<TAttribute> GetCustomAttributesImpl<TAttribute>(
            this Type type)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            return
                Attribute.GetCustomAttributes(type, attributeType, true)
                .Cast<TAttribute>();
        }

        public static TAttribute GetCustomAttribute<TAttribute>(
            this PropertyInfo property, IXmlSerializerOptions options)
            where TAttribute : Attribute
        {
            return GetCustomAttribute<TAttribute>(property, options.ShouldUseAttributeDefinedInInterface);
        }

        public static TAttribute[] GetCustomAttributes<TAttribute>(
            this PropertyInfo property, bool shouldUseAttributeDefinedInInterface)
            where TAttribute : Attribute
        {
            return property
                .GetCustomAttributesImpl<TAttribute>(shouldUseAttributeDefinedInInterface)
                .ToArray();
        }

        public static TAttribute GetCustomAttribute<TAttribute>(
            this PropertyInfo property, bool shouldUseAttributeDefinedInInterface)
            where TAttribute : Attribute
        {
            return property
                .GetCustomAttributesImpl<TAttribute>(shouldUseAttributeDefinedInInterface)
                .FirstOrDefault();
        }

        private static IEnumerable<TAttribute> GetCustomAttributesImpl<TAttribute>(
            this PropertyInfo property, bool shouldUseAttributeDefinedInInterface)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var attributes =
                Attribute.GetCustomAttributes(property, attributeType, true)
                .Cast<TAttribute>();

            if (shouldUseAttributeDefinedInInterface
                && property.DeclaringType != null)
            {
                var decoratedInterfaceAttributes =
                    (from interfaceType in property.DeclaringType.GetInterfaces()
                     let interfaceProperty = interfaceType.GetProperty(property.Name, property.PropertyType)
                     where interfaceProperty != null
                     let map = property.DeclaringType.GetInterfaceMap(interfaceType)
                     let interfaceMethodsIndex = Array.IndexOf(map.InterfaceMethods, interfaceProperty.GetGetMethod())
                     where interfaceMethodsIndex != -1 && property.GetGetMethod() == map.TargetMethods[interfaceMethodsIndex]
                     select Attribute.GetCustomAttributes(interfaceProperty, attributeType, true))
                    .SelectMany(x => x)
                    .Cast<TAttribute>();

                attributes = attributes.Concat(decoratedInterfaceAttributes).Distinct();
            }

            return attributes;
        }
    
        internal static bool HasDefaultConstructor(this Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        public static bool IsSerializable(this PropertyInfo property, IEnumerable<ParameterInfo> constructorParameters)
        {
            if (property.DeclaringType.IsAnonymous())
            {
                return true;
            }

            if (Attribute.IsDefined(property, typeof(XmlIgnoreAttribute)))
            {
                return false;
            }

            var isSerializable = property.GetIndexParameters().Length == 0 && (property.IsReadWriteProperty() || property.IsSerializableReadOnlyProperty(constructorParameters));
            return isSerializable;
        }

        public static bool IsJsonSerializable(this PropertyInfo property, IEnumerable<ParameterInfo> constructorParameters)
        {
            if (property.DeclaringType.IsAnonymous())
            {
                return true;
            }

            if (Attribute.IsDefined(property, typeof(JsonIgnoreAttribute), true)
                || Attribute.GetCustomAttributes(property, true).Any(attribute =>
                    attribute.GetType().FullName == "Newtonsoft.Json.JsonIgnoreAttribute"))
            {
                return false;
            }

            var isSerializable =
                property.GetIndexParameters().Length == 0
                && (property.IsReadWriteProperty()
                    || property.IsJsonSerializableReadOnlyProperty(constructorParameters)
                    || (property.IsReadOnlyProperty()
                        && property.IsJsonPropertyAttributeDefined()
                        && property.IsJsonPrimitive()));
            return isSerializable;
        }

        private static bool IsJsonPropertyAttributeDefined(this PropertyInfo property)
        {
            if (Attribute.IsDefined(property, typeof(JsonPropertyAttribute), true))
            {
                return true;
            }

            if (property.GetCustomAttributes(typeof(JsonPropertyAttribute), true).Any())
            {
                return true;
            }

            if (Attribute.GetCustomAttributes(property, typeof(JsonPropertyAttribute), true).Any())
            {
                return true;
            }

            var newtonsoftJsonPropertyAttribute =
                Attribute.GetCustomAttributes(property, true).FirstOrDefault(attribute =>
                    attribute.GetType().FullName == "Newtonsoft.Json.JsonPropertyAttribute");

            return newtonsoftJsonPropertyAttribute != null;
        }

        internal static bool IsJsonPrimitive(this PropertyInfo property)
        {
            return property.PropertyType.IsJsonStringType()
                || property.PropertyType.IsJsonNumericType()
                || property.PropertyType.IsJsonBooleanType();
        }

        internal static bool IsJsonStringType(this Type type)
        {
            return type == typeof(string)
                   || type == typeof(DateTime)
                   || type == typeof(DateTime?)
                   || type == typeof(DateTimeOffset)
                   || type == typeof(DateTimeOffset?)
                   || type == typeof(TimeSpan)
                   || type == typeof(TimeSpan?)
                   || type == typeof(Guid)
                   || type == typeof(Guid?)
                   || type.IsEnum
                   || (type.IsNullableType() && Nullable.GetUnderlyingType(type).IsEnum)
                   || type == typeof(Type)
                   || type == typeof(Uri);
        }

        internal static bool IsJsonNumericType(this Type type)
        {
            return type == typeof(double)
                   || type == typeof(double?)
                   || type == typeof(int)
                   || type == typeof(int?)
                   || type == typeof(float)
                   || type == typeof(float?)
                   || type == typeof(long)
                   || type == typeof(long?)
                   || type == typeof(decimal)
                   || type == typeof(decimal?)
                   || type == typeof(byte)
                   || type == typeof(byte?)
                   || type == typeof(sbyte)
                   || type == typeof(sbyte?)
                   || type == typeof(short)
                   || type == typeof(short?)
                   || type == typeof(ushort)
                   || type == typeof(ushort?)
                   || type == typeof(uint)
                   || type == typeof(uint?)
                   || type == typeof(ulong)
                   || type == typeof(ulong?); // TODO: handle more number types.
        }

        internal static bool IsJsonBooleanType(this Type type)
        {
            return type == typeof(bool)
                   || type == typeof(bool?);
        }

        internal static string GetName(this PropertyInfo property, bool shouldUseAttributeDefinedInInterface)
        {
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>(shouldUseAttributeDefinedInInterface);
            if (jsonPropertyAttribute != null && !string.IsNullOrEmpty(jsonPropertyAttribute.Name))
            {
                return jsonPropertyAttribute.Name;
            }

            var newtonsoftJsonPropertyAttribute =
                Attribute.GetCustomAttributes(property, true).FirstOrDefault(attribute =>
                    attribute.GetType().FullName == "Newtonsoft.Json.JsonPropertyAttribute");

            if (newtonsoftJsonPropertyAttribute != null)
            {
                var propertyNameProperty = newtonsoftJsonPropertyAttribute.GetType().GetProperty("PropertyName");

                if (propertyNameProperty != null
                    && propertyNameProperty.PropertyType == typeof(string))
                {
                    var propertyName = (string)propertyNameProperty.GetValue(newtonsoftJsonPropertyAttribute, new object[0]);

                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        return propertyName;
                    }
                }
            }

            return property.Name;
        }
        
        internal static bool IsReadWriteProperty(this PropertyInfo property)
        {
            var isReadWriteProperty = property.HasPublicGetter() && property.HasPublicSetter();
            return isReadWriteProperty;
        }

        internal static bool IsSerializableReadOnlyProperty(this PropertyInfo property, IEnumerable<ParameterInfo> constructorParameters = null)
        {
            return
                property.IsReadOnlyProperty()
                &&
                !IsConditionalProperty(property, constructorParameters)
                &&
                (
                    ((constructorParameters ?? Enumerable.Empty<ParameterInfo>()).Any(p => p.Name.ToLower() == property.Name.ToLower() && p.ParameterType == property.PropertyType))
                    ||
                    (property.PropertyType.IsAnyKindOfDictionary() && property.PropertyType != typeof(ExpandoObject))
                    ||
                    (property.PropertyType.IsAssignableToGenericIEnumerable() && property.PropertyType.HasAddMethodOfType(property.PropertyType.GetGenericIEnumerableType().GetGenericArguments()[0]))
                    ||
                    (property.PropertyType.IsAssignableToNonGenericIEnumerable() && property.PropertyType.HasAddMethod())
                    ||
                    (property.PropertyType.IsReadOnlyCollection())
                    ||
                    (property.PropertyType.IsArray && property.PropertyType.GetArrayRank() == 1)
                    ||
                    (property.PropertyType.IsReadOnlyDictionary())
                ); // TODO: add additional serializable types?
        }

        internal static bool IsJsonSerializableReadOnlyProperty(this PropertyInfo propertyInfo, IEnumerable<ParameterInfo> constructorParameters = null)
        {
            if (!propertyInfo.IsReadOnlyProperty())
            {
                return false;
            }

            if (constructorParameters != null && constructorParameters.Any(p => string.Equals(p.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (typeof(IDictionary).IsAssignableFrom(propertyInfo.PropertyType)
                || propertyInfo.PropertyType.IsAssignableToGenericIDictionary())
            {
                return true;
            }

            if (propertyInfo.PropertyType.IsArray)
            {
                return false;
            }

            if (typeof(IList).IsAssignableFrom(propertyInfo.PropertyType)
                || propertyInfo.PropertyType.IsAssignableToGenericICollection())
            {
                return true;
            }

            return false;
        }

        internal static object ConvertIfNecessary(this object instance, Type targetType)
        {
            if (instance == null)
            {
                return null;
            }

            var instanceType = instance.GetType();

            if (targetType.IsAssignableFrom(instanceType))
            {
                return instance;
            }

            var convertFunc =
                _convertFuncs.GetOrAdd(
                    Tuple.Create(instanceType, targetType),
                    tuple => CreateConvertFunc(tuple.Item1, tuple.Item2));

            return convertFunc(instance);
        }

        private static Func<object, object> CreateConvertFunc(Type instanceType, Type targetType)
        {
            if (instanceType.IsGenericType && instanceType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (targetType.IsArray)
                {
                    return CreateToArrayFunc(targetType.GetElementType());
                }

                if (targetType.IsReadOnlyCollection())
                {
                    var collectionType = targetType.GetReadOnlyCollectionType();
                    return CreateAsReadOnlyFunc(collectionType.GetGenericArguments()[0]);
                }
            }

            // Oh well, we were gonna throw anyway. Just let it happen...
            return x => x;
        }

        private static Func<object, object> CreateToArrayFunc(Type itemType)
        {
            var toArrayMethod =
                typeof(Enumerable)
                    .GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(itemType);

            var sourceParameter = Expression.Parameter(typeof(object), "source");

            var lambda =
                Expression.Lambda<Func<object, object>>(
                    Expression.Call(
                        toArrayMethod,
                        Expression.Convert(sourceParameter, typeof(IEnumerable<>).MakeGenericType(itemType))),
                    sourceParameter);

            return lambda.Compile();
        }

        private static Func<object, object> CreateAsReadOnlyFunc(Type itemType)
        {
            var listType = typeof (List<>).MakeGenericType(itemType);

            var asReadOnlyMethod = listType.GetMethod("AsReadOnly", BindingFlags.Instance | BindingFlags.Public);

            var instanceParameter = Expression.Parameter(typeof(object), "instance");

            var lambda =
                Expression.Lambda<Func<object, object>>(
                    Expression.Call(
                        Expression.Convert(instanceParameter, listType),
                        asReadOnlyMethod),
                    instanceParameter);

            return lambda.Compile();
        }

        internal static bool IsReadOnlyCollection(this Type type)
        {
            var openGenerics = GetOpenGenerics(type);

            foreach (var openGeneric in openGenerics)
            {
                if (openGeneric == typeof(ReadOnlyCollection<>))
                {
                    return true;
                }

                if (openGeneric.AssemblyQualifiedName == IReadOnlyCollection
                    || openGeneric.AssemblyQualifiedName == IReadOnlyList)
                {
                    return true;
                }
            }

            return false;
        }

        internal static Type GetReadOnlyCollectionType(this Type type)
        {
            if (IsReadOnlyCollectionType(type))
            {
                return type;
            }

            var interfaceType = type.GetInterfaces().FirstOrDefault(IsReadOnlyCollectionType);

            if (interfaceType != null)
            {
                return interfaceType;
            }

            do
            {
                type = type.BaseType;

                if (type == null)
                {
                    return null;
                }

                if (IsReadOnlyCollectionType(type))
                {
                    return type;
                }
            } while (true);
        }

        private static bool IsReadOnlyCollectionType(Type i)
        {
            if (i.IsGenericType)
            {
                var genericTypeDefinition = i.GetGenericTypeDefinition();

                if (_readOnlyCollections.Any(readOnlyCollectionType => genericTypeDefinition == readOnlyCollectionType))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsReadOnlyDictionary(this Type type)
        {
            var openGenerics = GetOpenGenerics(type);

            foreach (var openGeneric in openGenerics)
            {
                if (openGeneric.AssemblyQualifiedName == IReadOnlyDictionary
                    || openGeneric.AssemblyQualifiedName == ReadOnlyDictionary)
                {
                    return true;
                }
            }

            return false;
        }

        internal static Type GetIReadOnlyDictionaryInterface(this Type type)
        {
            var iReadOnlyDictionaryType = Type.GetType(IReadOnlyDictionary);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == iReadOnlyDictionaryType)
            {
                return type;
            }

            return
                type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == iReadOnlyDictionaryType);
        }

        private static IEnumerable<Type> GetOpenGenerics(Type type)
        {
            if (type.IsGenericType)
            {
                yield return type.GetGenericTypeDefinition();
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    yield return interfaceType;
                }
            }

            while (true)
            {
                type = type.BaseType;

                if (type == null)
                {
                    yield break;
                }

                if (type.IsGenericType)
                {
                    yield return type.GetGenericTypeDefinition();
                }
            }
        }

        private static bool IsConditionalProperty(PropertyInfo property, IEnumerable<ParameterInfo> constructorParameters)
        {
            if (property.Name.EndsWith("Specified"))
            {
                var otherPropertyName = property.Name.Substring(0, property.Name.LastIndexOf("Specified"));
                return property.DeclaringType.GetProperties().Any(p => p.Name == otherPropertyName);
            }

            return false;
        }

        internal static bool HasAddMethodOfType(this Type type, Type addMethodType)
        {
            return type.GetMethod("Add", new[] { addMethodType }) != null;
        }

        internal static bool HasAddMethod(this Type type)
        {
            return type.GetMethods().Any(m => m.Name == "Add" && m.GetParameters().Length == 1);
        }

        internal static bool IsAnyKindOfDictionary(this Type type)
        {
            return type.IsAssignableToNonGenericIDictionary() || type.IsAssignableToGenericIDictionary();
        }

        internal static bool IsReadOnlyProperty(this PropertyInfo property)
        {
            var canCallGetter = property.HasPublicGetter();
            var canCallSetter = property.HasPublicSetter();

            return canCallGetter && !canCallSetter;
        }

        internal static bool IsAssignableToNonGenericIDictionary(this Type type)
        {
            var isAssignableToIDictionary = typeof(IDictionary).IsAssignableFrom(type);
            return isAssignableToIDictionary;
        }

        internal static bool IsGenericIDictionary(this Type type)
        {
            return type.IsInterface
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IDictionary<,>);
        }

        internal static bool IsAssignableToGenericIDictionary(this Type type)
        {
            var isAssignableToGenericIDictionary =
                (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            return isAssignableToGenericIDictionary;
        }

        internal static bool IsAssignableToGenericIDictionaryWithKeyOrValueOfTypeObject(this Type type)
        {
            var isAssignableToGenericIDictionary = type.IsAssignableToGenericIDictionary();
            if (!isAssignableToGenericIDictionary)
            {
                return false;
            }

            Type iDictionaryType;
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                iDictionaryType = type;
            }
            else
            {
                iDictionaryType = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            }
                
            return iDictionaryType.GetGenericArguments()[0] == typeof(object) || iDictionaryType.GetGenericArguments()[1] == typeof(object);
        }

        internal static bool IsAssignableToGenericIDictionaryOfStringToAnything(this Type type)
        {
            if (type.IsAssignableToGenericIDictionary())
            {
                var dictionaryType = type.GetGenericIDictionaryType();
                var args = dictionaryType.GetGenericArguments();
                return args[0] == typeof(string);
            }

            return false;
        }

        internal static bool IsAssignableToNonGenericIEnumerable(this Type type)
        {
            var isAssignableToIEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
            return isAssignableToIEnumerable;
        }

        internal static bool IsGenericIEnumerable(this Type type)
        {
            return type.IsInterface
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        internal static bool IsAssignableToGenericIEnumerable(this Type type)
        {
            var isAssignableToGenericIEnumerable =
                type.IsGenericIEnumerable()
                || type.GetInterfaces().Any(i => i.IsGenericIEnumerable());
            return isAssignableToGenericIEnumerable;
        }

        internal static bool IsAssignableToGenericIEnumerableOfTypeObject(this Type type)
        {
            var isAssignableToGenericIEnumerable = type.IsAssignableToGenericIEnumerable();
            if (!isAssignableToGenericIEnumerable)
            {
                return false;
            }

            var iEnumerableType =
                type.IsGenericIEnumerable()
                    ? type
                    : type.GetInterfaces().Single(i => i.IsGenericIEnumerable());

            return iEnumerableType.GetGenericArguments()[0] == typeof(object);
        }

        internal static bool IsGenericICollection(this Type type)
        {
            return type.IsInterface
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ICollection<>);
        }

        internal static bool IsAssignableToGenericICollection(this Type type)
        {
            var isAssignableToGenericICollection =
                type.IsGenericICollection()
                || type.GetInterfaces().Any(i => i.IsGenericICollection());
            return isAssignableToGenericICollection;
        }

        internal static Type GetGenericIDictionaryType(this Type type)
        {
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return type;
            }

            return type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        internal static Type GetGenericIEnumerableType(this Type type)
        {
            if (type.IsGenericIEnumerable())
            {
                return type;
            }

            return type.GetInterfaces().First(i => i.IsGenericIEnumerable());
        }

        internal static Type GetGenericICollectionType(this Type type)
        {
            if (type.IsGenericICollection())
            {
                return type;
            }

            return type.GetInterfaces().First(i => i.IsGenericICollection());
        }

        private static bool HasPublicGetter(this PropertyInfo property)
        {
            if (!property.CanRead)
            {
                return false;
            }

            var getMethod = property.GetGetMethod();

            return getMethod != null && getMethod.IsPublic;
        }

        private static bool HasPublicSetter(this PropertyInfo property)
        {
            if (!property.CanWrite)
            {
                return false;
            }

            var setMethod = property.GetSetMethod();

            return setMethod != null && setMethod.IsPublic;
        }

        internal static bool ReadIfNeeded(this XSerializerXmlReader reader, bool shouldRead)
        {
            if (shouldRead)
            {
                return reader.Read();
            }

            return true;
        }

        internal static bool IsNil(this XSerializerXmlReader reader)
        {
            var nilFound = false;

            while (reader.MoveToNextAttribute())
            {
                if (reader.LocalName == "nil" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance")
                {
                    nilFound = true;
                    break;
                }
            }

            reader.MoveToElement();

            return nilFound;
        }

        internal static void WriteNilAttribute(this XmlWriter writer)
        {
            writer.WriteAttributeString("xsi", "nil", null, "true");
        }

        internal static bool IsPrimitiveLike(this Type type)
        {
            return
                type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(Guid)
                || type == typeof(TimeSpan)
                || type == typeof(DateTimeOffset);
        }

        internal static bool IsNullablePrimitiveLike(this Type type)
        {
            return
                type.IsNullableType()
                && type.GetGenericArguments()[0].IsPrimitiveLike();
        }

        internal static bool IsNullableType(this Type type)
        {
            return type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        internal static bool IsReferenceType(this Type type)
        {
            return !type.IsValueType;
        }

        internal static object GetUninitializedObject(this Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }

        public static bool IsAnonymous(this object instance)
        {
            if (instance == null)
            {
                return false;
            }

            return instance.GetType().IsAnonymous();
        }

        public static bool IsAnonymous(this Type type)
        {
            return
                type.Namespace == null
                && type.IsClass
                && type.IsNotPublic
                && type.IsSealed
                && type.DeclaringType == null
                && type.BaseType == typeof(object)
                && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                && type.Name.Contains("AnonymousType")
                && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
        }

        public static string GetElementName(this Type type)
        {
            if (type.IsGenericType)
            {
                return type.Name.Substring(0, type.Name.IndexOf("`")) + "Of" + string.Join("_", type.GetGenericArguments().Select(x => x.GetElementName()));
            }

            if (type.IsArray)
            {
                return "ArrayOf" + type.GetElementType().Name;
            }
            
            return type.Name;
        }

        public static string GetXsdType(this Type type)
        {
            string xsdType;
            if (_typeToXsdTypeMap.TryGetValue(type, out xsdType))
            {
                return xsdType;
            }

            return type.Name;
        }

        public static Type GetXsdType<T>(this XSerializerXmlReader reader, Type[] extraTypes)
        {
            string typeName = null;

            while (reader.MoveToNextAttribute())
            {
                if (reader.LocalName == "type" && reader.LookupNamespace(reader.Prefix) == "http://www.w3.org/2001/XMLSchema-instance")
                {
                    typeName = reader.Value;
                    break;
                }
            }

            reader.MoveToElement();

            if (typeName == null)
            {
                return null;
            }

            Type typeFromXsdType;

            if (_xsdTypeToTypeMap.TryGetValue(typeName, out typeFromXsdType))
            {
                return typeFromXsdType;
            }

            return _xsdTypeToTypeCache.GetOrAdd(
                Tuple.Create(typeof(T), typeName),
                _ =>
                {
                    Type type = null;

                    //// try REAL hard to get the type. (holy crap, this is UUUUUGLY!!!!)

                    if (extraTypes != null)
                    {
                        var matchingExtraTypes = extraTypes.Where(t => t.Name == typeName && typeof(T).IsAssignableFrom(t)).ToList();
                        if (matchingExtraTypes.Count == 1)
                        {
                            type = matchingExtraTypes[0];
                        }
                    }

                    if (type == null)
                    {
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
                                            else if (matches.Count > 1)
                                            {
                                                throw new SerializationException(string.Format("More than one type matches '{0}'. Consider decorating your type with the XmlIncludeAttribute, or pass in the type into the serializer as an extra type.", typeName));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (type == null)
                    {
                        throw new SerializationException(string.Format("No suitable type matches '{0}'. Consider decorating your type with the XmlIncludeAttribute, or pass in the type into the serializer as an extra type.", typeName));
                    }

                    return type;
                });
        }
    }
}

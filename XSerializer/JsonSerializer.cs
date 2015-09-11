﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using XSerializer.Encryption;

namespace XSerializer
{
    public static class JsonSerializer
    {
        private static readonly ConcurrentDictionary<Type, Func<IXSerializer>> _createXmlSerializerFuncs = new ConcurrentDictionary<Type, Func<IXSerializer>>();

        public static IXSerializer Create(Type type)
        {
            var createJsonSerializer = _createXmlSerializerFuncs.GetOrAdd(
                type, t =>
                {
                    var jsonSerializerType = typeof(JsonSerializer<>).MakeGenericType(t);
                    var ctor = jsonSerializerType.GetConstructor(new Type[0]);

                    Debug.Assert(ctor != null);

                    var lambda = Expression.Lambda<Func<IXSerializer>>(Expression.New(ctor));

                    return lambda.Compile();
                });

            return createJsonSerializer();
        }
    }

    public class JsonSerializer<T> : IXSerializer
    {
        private readonly IJsonSerializerConfiguration _configuration;
        private readonly IJsonSerializerInternal _serializer;

        public JsonSerializer()
            : this(new JsonSerializerConfiguration())
        {
        }

        public JsonSerializer(IJsonSerializerConfiguration configuration)
        {
            _configuration = configuration;

            EncryptAttribute encryptAttributeOrNull =
                ((EncryptAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(EncryptAttribute)))
                ?? (configuration.EncryptRootObject
                    ? new EncryptAttribute()
                    : null);

            _serializer = JsonSerializerFactory.GetSerializer(typeof(T), encryptAttributeOrNull);
        }

        string IXSerializer.Serialize(object instance)
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriterWithEncoding(sb, _configuration.Encoding))
            {
                _serializer.SerializeObject(writer, instance, GetJsonSerializeOperationInfo());
            }

            return sb.ToString();
        }

        public string Serialize(T instance)
        {
            return ((IXSerializer)this).Serialize(instance);
        }

        void IXSerializer.Serialize(Stream stream, object instance)
        {
            using (var writer = new StreamWriter(stream, _configuration.Encoding))
            {
                _serializer.SerializeObject(writer, instance, GetJsonSerializeOperationInfo());
            }
        }

        public void Serialize(Stream stream, T instance)
        {
            ((IXSerializer)this).Serialize(stream, instance);
        }

        void IXSerializer.Serialize(TextWriter writer, object instance)
        {
            _serializer.SerializeObject(writer, instance, GetJsonSerializeOperationInfo());
        }

        public void Serialize(TextWriter writer, T instance)
        {
            ((IXSerializer)this).Serialize(writer, instance);
        }

        object IXSerializer.Deserialize(string json)
        {
            using (var stringReader = new StringReader(json))
            {
                var info = GetJsonSerializeOperationInfo();

                using (var reader = new JsonReader(stringReader, info))
                {
                    return _serializer.DeserializeObject(reader, info);
                }
            }
        }

        public T Deserialize(string json)
        {
            return (T)((IXSerializer)this).Deserialize(json);
        }

        object IXSerializer.Deserialize(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, _configuration.Encoding))
            {
                var info = GetJsonSerializeOperationInfo();

                using (var reader = new JsonReader(streamReader, info))
                {
                    return _serializer.DeserializeObject(reader, info);
                }
            }
        }

        public T Deserialize(Stream stream)
        {
            return (T)((IXSerializer)this).Deserialize(stream);
        }

        object IXSerializer.Deserialize(TextReader reader)
        {
            var info = GetJsonSerializeOperationInfo();

            using (var jsonReader = new JsonReader(reader, info))
            {
                return _serializer.DeserializeObject(jsonReader, info);
            }
        }

        public T Deserialize(TextReader reader)
        {
            return (T)((IXSerializer)this).Deserialize(reader);
        }

        private IJsonSerializeOperationInfo GetJsonSerializeOperationInfo()
        {
            return new JsonSerializeOperationInfo
            {
                RedactEnabled = _configuration.RedactEnabled,
                EncryptionEnabled = _configuration.EncryptionEnabled,
                EncryptionMechanism = _configuration.EncryptionMechanism,
                EncryptKey = _configuration.EncryptKey,
                SerializationState = new SerializationState()
            };
        }
    }
}
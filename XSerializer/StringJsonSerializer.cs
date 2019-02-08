using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace XSerializer
{
    internal sealed class StringJsonSerializer : IJsonSerializerInternal
    {
        private static readonly ConcurrentDictionary<Tuple<Type, bool>, StringJsonSerializer> _cache = new ConcurrentDictionary<Tuple<Type, bool>, StringJsonSerializer>();

        private readonly bool _encrypt;
        private readonly bool _nullable;
        private readonly Action<JsonWriter, object> _write;
        private readonly Func<string, IJsonSerializeOperationInfo, string, int, int, object> _read;

        private StringJsonSerializer(Type type, bool encrypt)
        {
            _encrypt = encrypt;
            _nullable = !type.IsValueType || type.IsNullableType();
            SetDelegates(type, out _write, out _read);
        }

        public static StringJsonSerializer Get(Type type, bool encrypt)
        {
            return _cache.GetOrAdd(Tuple.Create(type, encrypt), t => new StringJsonSerializer(t.Item1, t.Item2));
        }

        public void SerializeObject(JsonWriter writer, object instance, IJsonSerializeOperationInfo info)
        {
            if (instance == null)
            {
                writer.WriteNull();
            }
            else
            {
                if (_encrypt)
                {
                    var toggler = new EncryptWritesToggler(writer);
                    toggler.Toggle();

                    _write(writer, instance);

                    toggler.Revert();
                }
                else
                {
                    _write(writer, instance);
                }
            }
        }

        public object DeserializeObject(JsonReader reader, IJsonSerializeOperationInfo info, string path)
        {
            if (!reader.ReadContent(path))
            {
                if (reader.NodeType == JsonNodeType.EndOfString)
                {
                    throw new MalformedDocumentException(MalformedDocumentError.MissingValue,
                        path, reader.Line, reader.Position);
                }

                Debug.Assert(reader.NodeType == JsonNodeType.Invalid);

                if (reader.Value is string)
                {
                    throw GetMissingCloseQuoteException(reader, path);
                }

                throw GetMissingOpenQuoteException(reader, path);
            }

            if (_encrypt)
            {
                var toggler = new DecryptReadsToggler(reader, path);
                toggler.Toggle();

                switch (reader.NodeType)
                {
                    case JsonNodeType.Number:
                    case JsonNodeType.String:
                    case JsonNodeType.Boolean:
                    case JsonNodeType.Null:
                        break;
                    case JsonNodeType.EndOfString:
                        throw new MalformedDocumentException(MalformedDocumentError.MissingValue,
                            path, reader.Line, reader.Position);
                    case JsonNodeType.Invalid:
                        if (reader.Value is string)
                        {
                            throw GetMissingCloseQuoteException(reader, path);
                        }

                        throw GetMissingOpenQuoteException(reader, path);
                    default:
                        throw GetMissingOpenQuoteException(reader, path);
                }

                try
                {
                    return Read(reader, info, path);
                }
                finally
                {
                    toggler.Revert();
                }
            }

            return Read(reader, info, path);
        }

        private object Read(JsonReader reader, IJsonSerializeOperationInfo info, string path)
        {
            string value;

            switch (reader.NodeType)
            {
                case JsonNodeType.Number:
                case JsonNodeType.String:
                    value = (string)reader.Value;
                    break;
                case JsonNodeType.Boolean:
                    value = (bool)reader.Value ? "true" : "false";
                    break;
                default:
                    if (_nullable && reader.NodeType == JsonNodeType.Null)
                    {
                        value = null;
                    }
                    else
                    {
                        throw GetMissingOpenQuoteException(reader, path);
                    }
                    break;
            }

            return _read(value, info, path, reader.Line, reader.Position);
        }

        private void SetDelegates(
            Type type,
            out Action<JsonWriter, object> writeAction,
            out Func<string, IJsonSerializeOperationInfo, string, int, int, object> readFunc)
        {
            Func<string, IJsonSerializeOperationInfo, object> readFuncLocal;

            if (type == typeof(string))
            {
                writeAction = (writer, value) => writer.WriteValue((string)value);
                readFuncLocal = (value, info) => value;
            }
            else if (type == typeof(DateTime)
                || type == typeof(DateTime?))
            {
                writeAction = (writer, value) => writer.WriteValue((DateTime)value);
                readFuncLocal = (value, info) => info.DateTimeHandler.ParseDateTime(value);
            }
            else if (type == typeof(DateTimeOffset)
                || type == typeof(DateTimeOffset?))
            {
                writeAction = (writer, value) => writer.WriteValue((DateTimeOffset)value);
                readFuncLocal = (value, info) => info.DateTimeHandler.ParseDateTimeOffset(value);
            }
            else if (type == typeof(TimeSpan)
                || type == typeof(TimeSpan?))
            {
                writeAction = (writer, value) => writer.WriteValue((TimeSpan)value);
                readFuncLocal = (value, info) => TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture);
            }
            else if (type == typeof(Guid)
                     || type == typeof(Guid?))
            {
                writeAction = (writer, value) => writer.WriteValue((Guid)value);
                readFuncLocal = (value, info) => Guid.Parse(value);
            }
            else if (type.IsEnum)
            {
                writeAction = (writer, value) => writer.WriteValue(value.ToString());
                readFuncLocal = (value, info) => Enum.Parse(type, value);
            }
            else if (type.IsNullableType() && Nullable.GetUnderlyingType(type).IsEnum)
            {
                writeAction = (writer, value) => writer.WriteValue(value.ToString());
                var enumType = Nullable.GetUnderlyingType(type);
                readFuncLocal = (value, info) => Enum.Parse(enumType, value);
            }
            else if (type == typeof(Type))
            {
                writeAction = (writer, value) => writer.WriteValue(GetStringValue((Type)value));
                readFuncLocal = (value, info) => Type.GetType(value);
            }
            else if (type == typeof(Uri))
            {
                writeAction = (writer, value) => writer.WriteValue(value.ToString());
                readFuncLocal = (value, info) => new Uri(value);
            }
            else
            {
                throw new ArgumentException("Unknown string type: " + type, "type");
            }

            readFunc =
                type == typeof(string) || !type.IsNullableType()
                    ? (Func<string, IJsonSerializeOperationInfo, string, int, int, object>)
                        ((value, info, path, line, position) =>
                            Try(readFuncLocal, value, info, type, path, line, position))
                    : (value, info, path, line, position) =>
                        value == null
                            ? null
                            : Try(readFuncLocal, value, info, type, path, line, position);
        }

        private static object Try(Func<string, IJsonSerializeOperationInfo, object> parseFunc,
            string value, IJsonSerializeOperationInfo info, Type type, string path, int line, int position)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return parseFunc(value, info);
            }
            catch (Exception ex)
            {
                throw new MalformedDocumentException(MalformedDocumentError.StringInvalidValue, path, value, line, position, ex, type);
            }
        }

        private static string GetStringValue(Type type)
        {
            return type.FullName + ", " + type.Assembly.GetName().Name;
        }

        private static MalformedDocumentException GetMissingOpenQuoteException(JsonReader reader, string path)
        {
            return new MalformedDocumentException(MalformedDocumentError.StringMissingOpenQuote,
                path, reader.Value, reader.Line, reader.Position);
        }

        private static MalformedDocumentException GetMissingCloseQuoteException(JsonReader reader, string path)
        {
            return new MalformedDocumentException(MalformedDocumentError.StringMissingCloseQuote,
                path, reader.Value, reader.Line, reader.Position);
        }
    }
}

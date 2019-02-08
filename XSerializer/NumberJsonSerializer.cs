using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace XSerializer
{
    internal sealed class NumberJsonSerializer : IJsonSerializerInternal
    {
        private static readonly ConcurrentDictionary<Tuple<Type, bool>, NumberJsonSerializer> _cache = new ConcurrentDictionary<Tuple<Type, bool>, NumberJsonSerializer>();

        private readonly bool _encrypt;
        private readonly bool _nullable;
        private readonly Type _type;
        private readonly Action<JsonWriter, object> _write;
        private readonly Func<string, string, int, int, object> _read;

        private NumberJsonSerializer(Type type, bool encrypt)
        {
            _encrypt = encrypt;
            _nullable = type.IsNullableType();
            _type = type;
            SetDelegates(out _write, out _read);
        }

        public static NumberJsonSerializer Get(Type type, bool encrypt)
        {
            return _cache.GetOrAdd(Tuple.Create(type, encrypt), t => new NumberJsonSerializer(t.Item1, t.Item2));
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
                        path, reader.Value, reader.Line, reader.Position);
                }

                Debug.Assert(reader.NodeType == JsonNodeType.Invalid);

                throw new MalformedDocumentException(MalformedDocumentError.NumberInvalidValue,
                    path, reader.Value, reader.Line, reader.Position, null, _type);
                
            }

            if (_encrypt)
            {
                var toggler = new DecryptReadsToggler(reader, path);
                if (toggler.Toggle())
                {
                    if (reader.NodeType == JsonNodeType.EndOfString)
                    {
                        throw new MalformedDocumentException(MalformedDocumentError.MissingValue,
                            path, reader.Value, reader.Line, reader.Position);
                    }

                    var exception = false;

                    try
                    {
                        return Read(reader, path);
                    }
                    catch (MalformedDocumentException)
                    {
                        exception = true;
                        throw;
                    }
                    finally
                    {
                        if (!exception)
                        {
                            if (reader.NodeType != JsonNodeType.Null && reader.DecryptReads && (reader.ReadContent(path) || reader.NodeType == JsonNodeType.Invalid))
                            {
                                throw new MalformedDocumentException(MalformedDocumentError.ExpectedEndOfDecryptedString,
                                    path, reader.Value, reader.Line, reader.Position, null, reader.NodeType);
                            }

                            toggler.Revert();
                        }
                    }
                }
            }

            return Read(reader, path);
        }

        private object Read(JsonReader reader, string path)
        {
            if (reader.NodeType == JsonNodeType.Number || reader.NodeType == JsonNodeType.String)
            {
                return _read((string)reader.Value, path, reader.Line, reader.Position);
            }

            if (_nullable && reader.NodeType == JsonNodeType.Null)
            {
                return null;
            }

            throw GetNumberInvalidValueException(reader, path);
        }

        private void SetDelegates(
            out Action<JsonWriter, object> writeAction,
            out Func<string, string, int, int, object> readFunc)
        {
            Func<string, object> readFuncLocal;

            if (_type == typeof(double) || _type == typeof(double?))
            {
                writeAction = (writer, value) => writer.WriteValue((double)value);
                readFuncLocal = value => double.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(int) || _type == typeof(int?))
            {
                writeAction = (writer, value) => writer.WriteValue((int)value);
                readFuncLocal = value => int.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(long) || _type == typeof(long?))
            {
                writeAction = (writer, value) => writer.WriteValue((long)value);
                readFuncLocal = value => long.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(uint) || _type == typeof(uint?))
            {
                writeAction = (writer, value) => writer.WriteValue((uint)value);
                readFuncLocal = value => uint.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(byte) || _type == typeof(byte?))
            {
                writeAction = (writer, value) => writer.WriteValue((byte)value);
                readFuncLocal = value => byte.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(sbyte) || _type == typeof(sbyte?))
            {
                writeAction = (writer, value) => writer.WriteValue((sbyte)value);
                readFuncLocal = value => sbyte.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(short) || _type == typeof(short?))
            {
                writeAction = (writer, value) => writer.WriteValue((short)value);
                readFuncLocal = value => short.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(ushort) || _type == typeof(ushort?))
            {
                writeAction = (writer, value) => writer.WriteValue((ushort)value);
                readFuncLocal = value => ushort.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(ulong) || _type == typeof(ulong?))
            {
                writeAction = (writer, value) => writer.WriteValue((ulong)value);
                readFuncLocal = value => ulong.Parse(value, CultureInfo.InvariantCulture);
            }
            else if(_type == typeof(float) || _type == typeof(float?))
            {
                writeAction = (writer, value) => writer.WriteValue((float)value);
                readFuncLocal = value => float.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (_type == typeof(decimal) || _type == typeof(decimal?))
            {
                writeAction = (writer, value) => writer.WriteValue((decimal)value);
                readFuncLocal = value => decimal.Parse(value, CultureInfo.InvariantCulture);
            }
            else
            {
                throw new InvalidOperationException("Unknown number type: " + _type);
            }

            readFunc =
                !_type.IsNullableType()
                    ? (Func<string, string, int, int, object>)
                        ((value, path, line, position) =>
                            Try(readFuncLocal, value, path, line, position))
                    : (value, path, line, position) =>
                        string.IsNullOrEmpty(value)
                            ? null
                            : Try(readFuncLocal, value, path, line, position);
        }

        private object Try(Func<string, object> parseFunc,
            string value, string path, int line, int position)
        {
            try
            {
                return parseFunc(value);
            }
            catch (Exception ex)
            {
                throw new MalformedDocumentException(MalformedDocumentError.NumberInvalidValue,
                    path, value, line, position, ex, _type);
            }
        }

        private MalformedDocumentException GetNumberInvalidValueException(JsonReader reader, string path)
        {
            var invalidValue = reader.Value;

            if (invalidValue is bool)
            {
                invalidValue = invalidValue.ToString().ToLower();
            }

            return new MalformedDocumentException(MalformedDocumentError.NumberInvalidValue,
                path, invalidValue, reader.Line, reader.Position, null, _type);
        }
    }
}

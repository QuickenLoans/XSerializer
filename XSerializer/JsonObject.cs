using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using XSerializer.Encryption;

namespace XSerializer
{
    public sealed class JsonObject : DynamicObject, IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly Dictionary<string, string> _numericStringValues = new Dictionary<string, string>();
        private readonly Dictionary<string, object> _projections = new Dictionary<string, object>();

        private readonly IDateTimeHandler _dateTimeHandler;
        private readonly IJsonSerializeOperationInfo _info;

        public JsonObject()
            : this(new JsonSerializeOperationInfo())
        {
        }

        internal JsonObject(IJsonSerializeOperationInfo info)
            : this(info, Enumerable.Empty<KeyValuePair<string, object>>())
        {
        }

        public JsonObject(params KeyValuePair<string, object>[] values)
            : this(new JsonSerializeOperationInfo(), values)
        {
        }

        internal JsonObject(IJsonSerializeOperationInfo info, params KeyValuePair<string, object>[] values)
            : this(info, (IEnumerable<KeyValuePair<string, object>>)values)
        {
        }

        public JsonObject(IEnumerable<KeyValuePair<string, object>> values)
            : this(new JsonSerializeOperationInfo(), values)
        {
        }

        internal JsonObject(IJsonSerializeOperationInfo info, IEnumerable<KeyValuePair<string, object>> values)
        {
            _info = info;

            foreach (var value in values)
            {
                Add(value.Key, value.Value);
            }
        }

        public void Add(string name, object value)
        {
            if (name == null) throw new ArgumentNullException("name");

            if (value == null
                || value is bool
                || value is string
                || value is JsonArray
                || value is JsonObject)
            {
                _values.Add(name, value);
                return;
            }

            var jsonNumber = value as JsonNumber;
            if (jsonNumber != null)
            {
                _values.Add(name, jsonNumber.DoubleValue);
                _numericStringValues.Add(name, jsonNumber.StringValue);
                return;
            }

            throw new NotSupportedException("Unsupported value type: " + value.GetType());
        }

        public object this[string key]
        {
            get
            {
                object value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
        }

        public JsonObject Decrypt(string name)
        {
            object value;
            if (_values.TryGetValue(name, out value)
                && value is string)
            {
                var decryptedJson = _info.EncryptionMechanism.Decrypt(
                    (string)value, _info.EncryptKey, _info.SerializationState);

                using (var stringReader = new StringReader(decryptedJson))
                {
                    using (var reader = new JsonReader(stringReader, _info))
                    {
                        value = DynamicJsonSerializer.Get(false).DeserializeObject(reader, _info);

                        if (value == null
                            || value is bool
                            || value is string
                            || value is JsonArray
                            || value is JsonObject)
                        {
                            _values[name] = value;
                            return this;
                        }

                        var jsonNumber = value as JsonNumber;
                        if (jsonNumber != null)
                        {
                            _values[name] = jsonNumber.DoubleValue;
                            _numericStringValues[name] = jsonNumber.StringValue;
                            return this;
                        }

                        throw new NotSupportedException("Unsupported value type: " + value.GetType());
                    }
                }
            }

            return this;
        }

        public bool TryGetValue(string name, out object result)
        {
            if (_values.TryGetValue(name, out result)
                || _projections.TryGetValue(name, out result))
            {
                return true;
            }

            return TryGetProjection(name, ref result);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_values).GetEnumerator();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetValue(binder.Name, out result);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _values.Keys;
        }

        private bool TryGetProjection(string name, ref object result)
        {
            string modifiedName;

            if (EndsWith(name, "AsByte", out modifiedName))
            {
                return AsByte(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsSByte", out modifiedName))
            {
                return AsSByte(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsInt16", out modifiedName))
            {
                return AsInt16(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsUInt16", out modifiedName))
            {
                return AsUInt16(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsInt32", out modifiedName))
            {
                return AsInt32(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsUInt32", out modifiedName))
            {
                return AsUInt32(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsInt64", out modifiedName))
            {
                return AsInt64(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsUInt64", out modifiedName))
            {
                return AsUInt64(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsDouble", out modifiedName))
            {
                return AsDouble(ref result, modifiedName);
            }

            if (EndsWith(name, "AsSingle", out modifiedName))
            {
                return AsSingle(ref result, modifiedName);
            }

            if (EndsWith(name, "AsDecimal", out modifiedName))
            {
                return AsDecimal(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsString", out modifiedName))
            {
                return AsString(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsDateTime", out modifiedName))
            {
                return AsDateTime(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsDateTimeOffset", out modifiedName))
            {
                return AsDateTimeOffset(ref result, modifiedName, name);
            }

            if (EndsWith(name, "AsGuid", out modifiedName))
            {
                return AsGuid(ref result, modifiedName, name);
            }

            return false;
        }

        private static bool EndsWith(string binderName, string suffix, out string name)
        {
            if (binderName.EndsWith(suffix, StringComparison.InvariantCulture))
            {
                name = binderName.Substring(
                    0, binderName.LastIndexOf(suffix, StringComparison.InvariantCulture));
                return true;
            }

            name = null;
            return false;
        }

        private bool AsByte(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                byte byteResult;
                if (byte.TryParse(value, out byteResult))
                {
                    result = byteResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsSByte(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                sbyte sbyteResult;
                if (sbyte.TryParse(value, out sbyteResult))
                {
                    result = sbyteResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsInt16(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                short shortResult;
                if (short.TryParse(value, out shortResult))
                {
                    result = shortResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsUInt16(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                ushort ushortResult;
                if (ushort.TryParse(value, out ushortResult))
                {
                    result = ushortResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsInt32(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                int intResult;
                if (int.TryParse(value, out intResult))
                {
                    result = intResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsUInt32(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                uint uintResult;
                if (uint.TryParse(value, out uintResult))
                {
                    result = uintResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsInt64(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                long longResult;
                if (long.TryParse(value, out longResult))
                {
                    result = longResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsUInt64(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                TruncateNumber(ref value);

                ulong ulongResult;
                if (ulong.TryParse(value, out ulongResult))
                {
                    result = ulongResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsDouble(ref object result, string name)
        {
            object value;
            if (_values.TryGetValue(name, out value)
                && value is double)
            {
                result = value;
                return true;
            }

            return false;
        }

        private bool AsSingle(ref object result, string name)
        {
            object value;
            if (_values.TryGetValue(name, out value)
                && value is double)
            {
                result = (float)((double)value);
                return true;
            }

            return false;
        }

        private bool AsDecimal(ref object result, string name, string binderName)
        {
            string value;
            if (_numericStringValues.TryGetValue(name, out value))
            {
                decimal decimalResult;
                if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimalResult))
                {
                    result = decimalResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsString(ref object result, string name, string binderName)
        {
            object value;
            if (_values.TryGetValue(name, out value))
            {
                if (value == null)
                {
                    result = null;
                    _projections.Add(binderName, result);
                    return true;
                }
                var stringValue = value as string;
                if (stringValue != null)
                {
                    result = stringValue;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private bool AsDateTime(ref object result, string name, string binderName)
        {
            object value;
            if (_values.TryGetValue(name, out value))
            {
                if (value == null)
                {
                    result = null;
                    _projections.Add(binderName, result);
                    return true;
                }
                var stringValue = value as string;
                if (stringValue != null)
                {
                    try
                    {
                        result = _dateTimeHandler.ParseDateTime(stringValue);
                        _projections.Add(binderName, result);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private bool AsDateTimeOffset(ref object result, string name, string binderName)
        {
            object value;
            if (_values.TryGetValue(name, out value))
            {
                if (value == null)
                {
                    result = null;
                    _projections.Add(binderName, result);
                    return true;
                }
                var stringValue = value as string;
                if (stringValue != null)
                {
                    try
                    {
                        result = _dateTimeHandler.ParseDateTimeOffset(stringValue);
                        _projections.Add(binderName, result);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private bool AsGuid(ref object result, string name, string binderName)
        {
            object value;
            if (_values.TryGetValue(name, out value)
                && value is string)
            {
                Guid guidResult;
                if (Guid.TryParse((string)value, out guidResult))
                {
                    result = guidResult;
                    _projections.Add(binderName, result);
                    return true;
                }
            }

            return false;
        }

        private static void TruncateNumber(ref string value)
        {
            if (value.Contains('.') || value.Contains('e') || value.Contains('E'))
            {
                var d = double.Parse(value);
                d = Math.Truncate(d);
                value = d.ToString(NumberFormatInfo.InvariantInfo);
            }
        }
    }
}
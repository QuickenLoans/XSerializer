using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using XSerializer.Encryption;

namespace XSerializer
{
    /// <summary>
    /// A representation of a JSON object. Provides an advanced dynamic API as well as a standard
    /// object API.
    /// </summary>
    public sealed class JsonObject : DynamicObject, IDictionary<string, object>
    {
        private static readonly string[] _definedProjections =
        {
            "AsByte",
            "AsSByte",
            "AsInt16",
            "AsUInt16",
            "AsInt32",
            "AsUInt32",
            "AsInt64",
            "AsUInt64",
            "AsDouble",
            "AsSingle",
            "AsDecimal",
            "AsString",
            "AsDateTime",
            "AsDateTimeOffset",
            "AsGuid"
        };

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly Dictionary<string, string> _numericStringValues = new Dictionary<string, string>();
        private readonly Dictionary<string, object> _projections = new Dictionary<string, object>();

        private readonly IJsonSerializeOperationInfo _info;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class.
        /// </summary>
        public JsonObject()
            : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class.
        /// </summary>
        /// <param name="dateTimeHandler">The object that determines how date time values are parsed.</param>
        /// <param name="encryptionMechanism">The object the performs encryption operations.</param>
        /// <param name="encryptKey">A key optionally used by the encryption mechanism during encryption operations.</param>
        /// <param name="serializationState">An object optionally used by the encryption mechanism to carry state across multiple encryption operations.</param>
        public JsonObject(
            IDateTimeHandler dateTimeHandler = null,
            IEncryptionMechanism encryptionMechanism = null,
            object encryptKey = null,
            SerializationState serializationState = null)
            : this(new JsonSerializeOperationInfo
                {
                    DateTimeHandler = dateTimeHandler ?? DateTimeHandler.Default,
                    EncryptionMechanism = encryptionMechanism,
                    EncryptKey = encryptKey,
                    SerializationState = serializationState ?? new SerializationState()
                })
        {
        }

        internal JsonObject(IJsonSerializeOperationInfo info)
        {
            _info = info;
        }

        /// <summary>
        /// Add a property to the JSON object.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name"/> is null.</exception>
        public void Add(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            AddImpl(name, GuardValue(value));
        }

        private void AddImpl(string name, object value)
        {
            var jsonNumber = value as JsonNumber;
            if (jsonNumber != null)
            {
                _values.Add(name, jsonNumber.DoubleValue);
                _numericStringValues.Add(name, jsonNumber.StringValue);
            }
            else
            {
                _values.Add(name, value);
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified property name.
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <exception cref="KeyNotFoundException">When getting the value, if no property exists with the specified name.</exception>
        public object this[string name]
        {
            get
            {
                object value;
                if (TryGetValue(name, out value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                value = GuardValue(value);
                if (!TrySetValueImpl(name, value))
                {
                    AddImpl(name, value);
                }
            }
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            object dummy;
            return TryGetValue(key, out dummy);
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            if (_values.Remove(key))
            {
                RemoveProjections(key);
                return true;
            }

            return false;
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return _values.Keys; }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return _values.Values; }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            _values.Clear();
            _numericStringValues.Clear();
            _projections.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            object value;
            return TryGetValue(item.Key, out value) && Equals(item.Value, value);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_values).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            if (((ICollection<KeyValuePair<string, object>>)_values).Remove(item))
            {
                RemoveProjections(item.Key);
                return true;
            }

            return false;
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return _values.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Decrypts the specified property, changing its value in place.
        /// </summary>
        /// <param name="name">The name of the property to decrypt.</param>
        /// <returns>This instance of <see cref="JsonObject"/>.</returns>
        public JsonObject Decrypt(string name)
        {
            if (_info.EncryptionMechanism != null)
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
                            value = DynamicJsonSerializer.Get(false, JsonMappings.Empty).DeserializeObject(reader, _info, name);

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
            }

            return this;
        }

        /// <summary>
        /// Encrypts the specified property, changing its value in place.
        /// </summary>
        /// <param name="name">The name of the property to encrypt.</param>
        /// <returns>This instance of <see cref="JsonObject"/>.</returns>
        public JsonObject Encrypt(string name)
        {
            if (_info.EncryptionMechanism != null)
            {
                object value;
                if (_values.TryGetValue(name, out value)
                    && value != null)
                {
                    var sb = new StringBuilder();

                    using (var stringwriter = new StringWriter(sb))
                    {
                        using (var writer = new JsonWriter(stringwriter, _info))
                        {
                            DynamicJsonSerializer.Get(false, JsonMappings.Empty).SerializeObject(writer, value, _info);
                        }
                    }

                    value = _info.EncryptionMechanism.Encrypt(sb.ToString(), _info.EncryptKey, _info.SerializationState);
                    _values[name] = value;
                }
            }

            return this;
        }

        /// <summary>
        /// Gets the value of the specified property.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="result">
        /// When this method returns, contains the value of the property, if the name exists; otherwise, null.
        /// </param>
        /// <returns>True if the JSON object contains the specified property; otherwise false.</returns>
        public bool TryGetValue(string name, out object result)
        {
            if (_values.TryGetValue(name, out result)
                || _projections.TryGetValue(name, out result))
            {
                return true;
            }

            return TryGetProjection(name, ref result);
        }

        /// <summary>
        /// Set the value of the specified property.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>True if the JSON object contains the specified property; otherwise false.</returns>
        public bool TrySetValue(string name, object value)
        {
            return TrySetValueImpl(name, GuardValue(value));
        }

        private bool TrySetValueImpl(string name, object value)
        {
            if (_values.ContainsKey(name))
            {
                var jsonNumber = value as JsonNumber;
                if (jsonNumber != null)
                {
                    _values[name] = jsonNumber.DoubleValue;
                    _numericStringValues[name] = jsonNumber.StringValue;
                }
                else
                {
                    _values[name] = value;
                    _numericStringValues.Remove(name);
                }

                RemoveProjections(name);

                return true;
            }

            return false;
        }

        private static object GuardValue(object value)
        {
            if (value == null
                || value is bool
                || value is string
                || value is JsonNumber
                || value is JsonObject
                || value is JsonArray)
            {
                return value;
            }

            if (value is int
                || value is double
                || value is byte
                || value is long
                || value is decimal
                || value is uint
                || value is ulong
                || value is short
                || value is float
                || value is ushort
                || value is sbyte)
            {
                return new JsonNumber(value.ToString());
            }

            if (value is Guid)
            {
                var guid = (Guid)value;
                return guid.ToString("D");
            }

            if (value is DateTime)
            {
                var dateTime = (DateTime)value;
                return dateTime.ToString("O");
            }

            if (value is DateTimeOffset)
            {
                var dateTimeOffset = (DateTimeOffset)value;
                return dateTimeOffset.ToString("O");
            }
            
            throw new XSerializerException("Invalid value for JsonObject member: " + value.GetType().FullName);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_values).GetEnumerator();
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="object"/>.</param>
        /// <returns>
        /// true if the specified <see cref="object"/> is equal to the current <see cref="object"/>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            var other = obj as JsonObject;
            if (other == null)
            {
                return false;
            }

            foreach (var item in _values)
            {
                if (!other._values.ContainsKey(item.Key))
                {
                    return false;
                }

                object value;
                object otherValue;

                if (_numericStringValues.ContainsKey(item.Key))
                {
                    if (!other._numericStringValues.ContainsKey(item.Key))
                    {
                        return false;
                    }

                    value = _numericStringValues[item.Key];
                    otherValue = other._numericStringValues[item.Key];
                }
                else
                {
                    value = _values[item.Key];
                    otherValue = other._values[item.Key];
                }

                if (!Equals(value, otherValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = typeof(JsonObject).GetHashCode();

                foreach (var item in _values.OrderBy(x => x.Key))
                {
                    hashCode = (hashCode * 397) ^ item.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ (item.Value != null ? item.Value.GetHashCode() : 0);
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Provides the implementation for operations that get member values.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation.</param>
        /// <param name="result">The result of the get operation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false.
        /// </returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetValue(binder.Name, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that set member values.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation.</param>
        /// <param name="value">The value to set to the member.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false.
        /// </returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names. 
        /// </summary>
        /// <returns>
        /// A sequence that contains dynamic member names.
        /// </returns>
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

        private void RemoveProjections(string name)
        {
            var toRemove =
                from projectionName in _projections.Keys
                where projectionName.StartsWith(name) && _definedProjections.Any(projectionName.EndsWith)
                select projectionName;

            foreach (var key in toRemove)
            {
                _projections.Remove(key);
            }
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
                if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byteResult))
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
                if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyteResult))
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
                if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out shortResult))
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
                if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushortResult))
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
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intResult))
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
                if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uintResult))
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
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longResult))
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
                if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulongResult))
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
                        result = _info.DateTimeHandler.ParseDateTime(stringValue);
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
                        result = _info.DateTimeHandler.ParseDateTimeOffset(stringValue);
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

using System;
using System.Collections;
using System.Collections.Concurrent;
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
    /// A representation of a JSON array. Provides an advanced dynamic API as well as a standard
    /// object API.
    /// </summary>
    public sealed class JsonArray : DynamicObject, IList<object>
    {
        private delegate bool TryFunc(out object result);

        private readonly ConcurrentDictionary<Type, TryFunc> _convertFuncs = new ConcurrentDictionary<Type, TryFunc>();

        private readonly IJsonSerializeOperationInfo _info;

        private readonly List<object> _values = new List<object>();
        private readonly List<object> _transformableValues = new List<object>();
        private readonly List<Type> _transformedTypes = new List<Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        public JsonArray()
            : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        /// <param name="dateTimeHandler">The object that determines how date time values are parsed.</param>
        /// <param name="encryptionMechanism">The object the performs encryption operations.</param>
        /// <param name="encryptKey">A key optionally used by the encryption mechanism during encryption operations.</param>
        /// <param name="serializationState">An object optionally used by the encryption mechanism to carry state across multiple encryption operations.</param>
        public JsonArray(
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

        internal JsonArray(IJsonSerializeOperationInfo info)
        {
            _info = info;
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="value">The object to be added to the end of the <see cref="JsonArray"/>.</param>
        public void Add(object value)
        {
            var jsonNumber = value as JsonNumber;
            if (jsonNumber != null)
            {
                _values.Add(jsonNumber.DoubleValue);
                _transformableValues.Add(jsonNumber);
            }
            else if (value is string)
            {
                _values.Add(value);
                _transformableValues.Add(value);
            }
            else
            {
                _values.Add(value);
                _transformableValues.Add(null);
            }
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
            var other = obj as JsonArray;
            if (other == null
                || _values.Count != other._values.Count)
            {
                return false;
            }

            for (int i = 0; i < _values.Count; i++)
            {
                if (!Equals(_values[i], other._values[i]))
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
                return _values.Aggregate(
                    typeof(JsonArray).GetHashCode(),
                    (current, item) => (current * 397) ^ (item != null ? item.GetHashCode() : 0));
            }
        }

        /// <summary>
        /// Provides implementation for type conversion operations.
        /// </summary>
        /// <param name="binder">Provides information about the conversion operation.</param>
        /// <param name="result">The result of the type conversion operation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false.
        /// </returns>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            var convertFunc = _convertFuncs.GetOrAdd(binder.Type, GetConvertFunc);

            return convertFunc != null
                ? convertFunc(out result)
                : base.TryConvert(binder, out result);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public object this[int index]
        {
            get { return _values[index]; }
            set
            {
                _values[index] = value;
                _transformableValues[index] = null;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the <see cref="JsonArray"/>.
        /// </summary>
        public int Count
        {
            get { return _values.Count; }
        }

        /// <summary>
        /// Removes all items from the <see cref="JsonArray"/>.
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            _transformableValues.Clear();
            _transformedTypes.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="JsonArray"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="JsonArray"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="JsonArray"/>; otherwise, false.
        /// </returns>
        public bool Contains(object item)
        {
            return _values.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="JsonArray"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="JsonArray"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(object[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="JsonArray"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="JsonArray"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="JsonArray"/>.
        /// </returns>
        public bool Remove(object item)
        {
            var index = _values.IndexOf(item);

            if (index == -1)
            {
                return false;
            }

            _values.RemoveAt(index);
            _transformableValues.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="JsonArray"/>.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="JsonArray"/>.</param>
        public int IndexOf(object item)
        {
            return _values.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="JsonArray"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="JsonArray"/>.</param>
        public void Insert(int index, object item)
        {
            _values.Insert(index, item);
            _transformableValues.Insert(index, null);
        }

        /// <summary>
        /// Removes the <see cref="JsonArray"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            _values.RemoveAt(index);
            _transformableValues.RemoveAt(index);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="JsonArray"/> is read-only.
        /// </summary>
        /// <returns>
        /// true if the <see cref="JsonArray"/> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Decrypts the item at the specified index, changing its value in place.
        /// </summary>
        /// <param name="index">The index of the value to decrypt.</param>
        /// <returns>This instance of <see cref="JsonArray"/>.</returns>
        public JsonArray Decrypt(int index)
        {
            if (_info.EncryptionMechanism != null)
            {
                var value = _transformableValues[index];

                if (value is string)
                {
                    var decryptedJson = _info.EncryptionMechanism.Decrypt(
                        (string)value, _info.EncryptKey, _info.SerializationState);

                    using (var stringReader = new StringReader(decryptedJson))
                    {
                        using (var reader = new JsonReader(stringReader, _info))
                        {
                            value = DynamicJsonSerializer.Get(false, JsonMappings.Empty).DeserializeObject(reader, _info, "[" + index + "]");

                            if (value == null
                                || value is bool
                                || value is JsonArray
                                || value is JsonObject)
                            {
                                _values[index] = value;
                                _transformableValues[index] = null;
                            }
                            else if (value is string)
                            {
                                _values[index] = value;
                                _transformableValues[index] = value;
                            }
                            else
                            {
                                var jsonNumber = value as JsonNumber;
                                if (jsonNumber != null)
                                {
                                    _values[index] = jsonNumber.DoubleValue;
                                    _transformableValues[index] = jsonNumber;
                                }
                                else
                                {
                                    throw new NotSupportedException("Unsupported value type: " + value.GetType());
                                }
                            }
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Encrypts the item at the specified index, changing its value in place.
        /// </summary>
        /// <param name="index">The index of the value to encrypt.</param>
        /// <returns>This instance of <see cref="JsonArray"/>.</returns>
        public JsonArray Encrypt(int index)
        {
            if (_info.EncryptionMechanism != null)
            {
                var value = _values[index];

                if (value != null)
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
                    _values[index] = value;
                }
            }

            return this;
        }

        /// <summary>
        /// Convert all of the items in this <see cref="JsonArray"/> to the type specified by
        /// the <typeparamref name="T"/> generic argument.
        /// </summary>
        /// <typeparam name="T">The type to convert items to.</typeparam>
        /// <returns>This instance of <see cref="JsonArray"/>.</returns>
        public JsonArray TransformItems<T>()
        {
            return TransformItems(typeof(T));
        }


        /// <summary>
        /// Convert all of the items in this <see cref="JsonArray"/> to the type specified by
        /// the <paramref name="type"/> parameter.
        /// </summary>
        /// <param name="type">The type to convert items to.</param>
        /// <returns>This instance of <see cref="JsonArray"/>.</returns>
        public JsonArray TransformItems(Type type)
        {
            if (_transformedTypes.Contains(type))
            {
                return this;
            }

            if (type.IsNullableType())
            {
                var nullableOfType = type.GetGenericArguments()[0];
                if (_transformedTypes.Contains(nullableOfType))
                {
                    return this;
                }

                _transformedTypes.Add(nullableOfType);
            }
            else
            {
                _transformedTypes.Add(type);
            }

            var transform = GetTransform(type);

            for (int i = 0; i < _values.Count; i++)
            {
                if (_transformableValues[i] != null)
                {
                    _values[i] = transform(_values[i], _transformableValues[i]);
                }
            }

            return this;
        }

        private TryFunc GetConvertFunc(Type type)
        {
            if (type.IsInterface && type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(IEnumerable<>)
                    || genericTypeDefinition == typeof(ICollection<>)
                    || genericTypeDefinition == typeof(IList<>))
                {
                    var collectionType = type.GetGenericArguments()[0];

                    if (collectionType == typeof(object))
                    {
                        return ((out object r) =>
                        {
                            r = _values;
                            return true;
                        });
                    }

                    if (collectionType == typeof(JsonObject))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<JsonObject>(_values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(JsonArray))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<JsonArray>(_values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(bool))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<bool>(TransformItems<bool>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(bool?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<bool?>(TransformItems<bool?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(byte))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<byte>(TransformItems<byte>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(byte?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<byte?>(TransformItems<byte?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(sbyte))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<sbyte>(TransformItems<sbyte>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(sbyte?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<sbyte?>(TransformItems<sbyte?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(short))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<short>(TransformItems<short>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(short?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<short?>(TransformItems<short?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(ushort))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<ushort>(TransformItems<ushort>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(ushort?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<ushort?>(TransformItems<ushort?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(int))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<int>(TransformItems<int>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(int?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<int?>(TransformItems<int?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(uint))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<uint>(TransformItems<uint>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(uint?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<uint?>(TransformItems<uint?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(long))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<long>(TransformItems<long>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(long?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<long?>(TransformItems<long?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(ulong))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<ulong>(TransformItems<ulong>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(ulong?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<ulong?>(TransformItems<ulong?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(float))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<float>(TransformItems<float>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(float?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<float?>(TransformItems<float?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(double))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<double>(_values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(double?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<double?>(_values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(decimal))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<decimal>(TransformItems<decimal>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(decimal?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<decimal?>(TransformItems<decimal?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(string))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<string>(_values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(DateTime))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<DateTime>(TransformItems<DateTime>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(DateTime?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<DateTime?>(TransformItems<DateTime?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(DateTimeOffset))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<DateTimeOffset>(TransformItems<DateTimeOffset>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(DateTimeOffset?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<DateTimeOffset?>(TransformItems<DateTimeOffset?>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(Guid))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<Guid>(TransformItems<Guid>()._values);
                            return true;
                        });
                    }

                    if (collectionType == typeof(Guid?))
                    {
                        return ((out object r) =>
                        {
                            r = new ConversionList<Guid?>(TransformItems<Guid?>()._values);
                            return true;
                        });
                    }
                }
            }

            return null;
        }

        private Func<object, object, object> GetTransform(Type type)
        {
            Func<object, object, object> transform = (currentItem, transformableValue) => currentItem;

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                transform = TransformDateTime;
            }

            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                transform = TransformDateTimeOffset;
            }

            if (type == typeof(Guid) || type == typeof(Guid?))
            {
                transform = TransformGuid;
            }

            if (type == typeof(byte) || type == typeof(byte?))
            {
                transform = TransformByte;
            }

            if (type == typeof(sbyte) || type == typeof(sbyte?))
            {
                transform = TransformSByte;
            }

            if (type == typeof(short) || type == typeof(short?))
            {
                transform = TransformInt16;
            }

            if (type == typeof(ushort) || type == typeof(ushort?))
            {
                transform = TransformUInt16;
            }

            if (type == typeof(int) || type == typeof(int?))
            {
                transform = TransformInt32;
            }

            if (type == typeof(uint) || type == typeof(uint?))
            {
                transform = TransformUInt32;
            }

            if (type == typeof(long) || type == typeof(long?))
            {
                transform = TransformInt64;
            }

            if (type == typeof(ulong) || type == typeof(ulong?))
            {
                transform = TransformUInt64;
            }

            if (type == typeof(float) || type == typeof(float?))
            {
                transform = TransformSingle;
            }

            if (type == typeof(decimal) || type == typeof(decimal?))
            {
                transform = TransformDecimal;
            }

            return transform;
        }

        private object TransformDateTime(object currentItem, object transformableValue)
        {
            if (transformableValue is JsonNumber)
            {
                return currentItem;
            }

            try
            {
                return _info.DateTimeHandler.ParseDateTime((string)transformableValue);
            }
            catch
            {
                return currentItem;
            }
        }

        private object TransformDateTimeOffset(object currentItem, object transformableValue)
        {
            if (transformableValue is JsonNumber)
            {
                return currentItem;
            }

            try
            {
                return _info.DateTimeHandler.ParseDateTimeOffset((string)transformableValue);
            }
            catch
            {
                return currentItem;
            }
        }

        private static object TransformGuid(object currentItem, object transformableValue)
        {
            if (transformableValue is JsonNumber)
            {
                return currentItem;
            }

            Guid value;
            return Guid.TryParse((string)transformableValue, out value)
                ? value
                : currentItem;
        }

        private static object TransformByte(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            byte value;
            return byte.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformSByte(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            sbyte value;
            return sbyte.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformInt16(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            short value;
            return short.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformUInt16(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            ushort value;
            return ushort.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformInt32(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            int value;
            return int.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformUInt32(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            uint value;
            return uint.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformInt64(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            long value;
            return long.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformUInt64(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            ulong value;
            return ulong.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformSingle(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            float value;
            return float.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        private static object TransformDecimal(object currentItem, object transformableValue)
        {
            if (transformableValue is string)
            {
                return currentItem;
            }

            decimal value;
            return decimal.TryParse(((JsonNumber)transformableValue).StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : currentItem;
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="List{T}"/> of <see cref="object"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator List<object>(JsonArray jsonArray)
        {
            return jsonArray._values;
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an object array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator object[](JsonArray jsonArray)
        {
            return jsonArray._values.ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="JsonObject"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator JsonObject[](JsonArray jsonArray)
        {
            return jsonArray._values.Cast<JsonObject>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="JsonArray"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator JsonArray[](JsonArray jsonArray)
        {
            return jsonArray._values.Cast<JsonArray>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="string"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator string[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<string>()._values.Cast<string>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="DateTime"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator DateTime[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<DateTime>()._values.Cast<DateTime>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="DateTime"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator DateTime?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<DateTime?>()._values.Cast<DateTime?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="DateTimeOffset"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator DateTimeOffset[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<DateTimeOffset>()._values.Cast<DateTimeOffset>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator DateTimeOffset?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<DateTimeOffset?>()._values.Cast<DateTimeOffset?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="Guid"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator Guid[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<Guid>()._values.Cast<Guid>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="Guid"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator Guid?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<Guid?>()._values.Cast<Guid?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="bool"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator bool[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<bool>()._values.Cast<bool>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="bool"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator bool?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<bool?>()._values.Cast<bool?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="byte"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator byte[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<byte>()._values.Cast<byte>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="byte"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator byte?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<byte?>()._values.Cast<byte?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="sbyte"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator sbyte[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<sbyte>()._values.Cast<sbyte>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="sbyte"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator sbyte?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<sbyte?>()._values.Cast<sbyte?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="short"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator short[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<short>()._values.Cast<short>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="short"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator short?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<short?>()._values.Cast<short?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="ushort"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator ushort[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<ushort>()._values.Cast<ushort>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="ushort"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator ushort?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<ushort?>()._values.Cast<ushort?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="int"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator int[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<int>()._values.Cast<int>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="int"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator int?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<int?>()._values.Cast<int?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="uint"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator uint[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<uint>()._values.Cast<uint>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="uint"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator uint?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<uint?>()._values.Cast<uint?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="long"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator long[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<long>()._values.Cast<long>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="long"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator long?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<long?>()._values.Cast<long?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="ulong"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator ulong[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<ulong>()._values.Cast<ulong>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="ulong"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        [CLSCompliantAttribute(false)]
        public static implicit operator ulong?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<ulong?>()._values.Cast<ulong?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="float"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator float[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<float>()._values.Cast<float>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="float"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator float?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<float?>()._values.Cast<float?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="double"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator double[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<double>()._values.Cast<double>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="double"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator double?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<double?>()._values.Cast<double?>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to a <see cref="decimal"/> array.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator decimal[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<decimal>()._values.Cast<decimal>().ToArray();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="JsonArray"/> object to an array of nullable <see cref="decimal"/>.
        /// </summary>
        /// <param name="jsonArray">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public static implicit operator decimal?[](JsonArray jsonArray)
        {
            return jsonArray.TransformItems<decimal?>()._values.Cast<decimal?>().ToArray();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<object> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class ConversionList<T> : IList<T>
        {
            private readonly List<T> _list = new List<T>();
            private readonly List<object> _backingList;

            public ConversionList(List<object> backingList)
            {
                _backingList = backingList;

                foreach (var item in backingList)
                {
                    _list.Add((T)item);
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_list).GetEnumerator();
            }

            public void Add(T item)
            {
                _list.Add(item);
                _backingList.Add(item);
            }

            public void Clear()
            {
                _list.Clear();
                _backingList.Clear();
            }

            public bool Contains(T item)
            {
                return _list.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                return _list.Remove(item)
                    && _backingList.Remove(item);
            }

            public int Count
            {
                get { return _list.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public int IndexOf(T item)
            {
                return _list.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                _list.Insert(index, item);
                _backingList.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
                _backingList.RemoveAt(index);
            }

            public T this[int index]
            {
                get { return _list[index]; }
                set
                {
                    _list[index] = value;
                    _backingList[index] = value;
                }
            }
        }
    }
}

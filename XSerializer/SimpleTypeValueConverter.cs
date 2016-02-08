using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace XSerializer
{
    internal class SimpleTypeValueConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<int, SimpleTypeValueConverter> _map = new ConcurrentDictionary<int, SimpleTypeValueConverter>();

        private readonly Func<string, ISerializeOptions, object> _parseString;
        private readonly Func<object, ISerializeOptions, string> _getString;

        private SimpleTypeValueConverter(Type type, RedactAttribute redactAttribute)
        {
            if (redactAttribute != null)
            {
                _parseString = GetRedactedGetParseStringFunc(type);
                _getString = GetRedactedGetStringFunc(type, redactAttribute);
            }
            else
            {
                _parseString = GetNonRedactedGetParseStringFunc(type);
                _getString = GetNonRedactedGetStringFunc(type);
            }
        }

        public static SimpleTypeValueConverter Create(Type type, RedactAttribute redactAttribute)
        {
            return _map.GetOrAdd(
                CreateKey(type, redactAttribute),
                _ => new SimpleTypeValueConverter(type, redactAttribute));
        }

        public object ParseString(string value, ISerializeOptions options)
        {
            return _parseString(value, options);
        }

        public string GetString(object value, ISerializeOptions options)
        {
            return _getString(value, options);
        }

        private static Func<string, ISerializeOptions, object> GetRedactedGetParseStringFunc(Type type)
        {
            var defaultValue =
                type.IsValueType
                    ? Activator.CreateInstance(type)
                    : null;

            if (type.IsEnum ||
                (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments()[0].IsEnum))
            {
                return (value, options) => string.IsNullOrEmpty(value) || value == "XXXXXX" ? defaultValue : Enum.Parse(type, value);
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return (value, options) => string.IsNullOrEmpty(value) || value == "XXXXXX" ? defaultValue : Convert.ChangeType(value, type);
            }

            if (type == typeof(DateTime))
            {
                return ParseStringForDateTime;
            }

            if (type == typeof(DateTime?))
            {
                return ParseStringForNullableDateTime;
            }

            if (type == typeof(DateTimeOffset))
            {
                return ParseStringForDateTimeOffset;
            }

            if (type == typeof(DateTimeOffset?))
            {
                return ParseStringForNullableDateTimeOffset;
            }

            if (type == typeof(TimeSpan))
            {
                return ParseStringForTimeSpan;
            }

            if (type == typeof(TimeSpan?))
            {
                return ParseStringForNullableTimeSpan;
            }

            if (type == typeof(Guid))
            {
                return ParseStringForGuid;
            }

            if (type == typeof(Guid?))
            {
                return ParseStringForNullableGuid;
            }

            if (type == typeof(char) || type == typeof(char?))
            {
                return (value, options) => string.IsNullOrEmpty(value) || value == "XXXXXX" ? defaultValue : ParseStringForChar(value, options);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return (value, options) => string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, type.GetGenericArguments()[0]);
            }

            return (value, options) => string.IsNullOrEmpty(value) ? defaultValue : Convert.ChangeType(value, type);
        }

        private static Func<object, ISerializeOptions, string> GetRedactedGetStringFunc(Type type, RedactAttribute redactAttribute)
        {
            if (type == typeof(string))
            {
                return (value, options) => redactAttribute.Redact((string)value, options.ShouldRedact);
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return (value, options) => redactAttribute.Redact((bool?)value, options.ShouldRedact);
            }

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return (value, options) => redactAttribute.Redact((DateTime?)value, options.ShouldRedact);
            }

            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                return (value, options) => redactAttribute.Redact((DateTimeOffset?)value, options.ShouldRedact);
            }

            if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
            {
                return (value, options) => redactAttribute.Redact((TimeSpan?)value, options.ShouldRedact);
            }

            if (type == typeof(char) || type == typeof(char?))
            {
                return (value, options) => redactAttribute.Redact((char?)value, options.ShouldRedact, options.DotNetFrameworkSerializerCompatable);
            }

            return (value, options) => redactAttribute.Redact(value, options.ShouldRedact);
        }

        private static Func<string, ISerializeOptions, object> GetNonRedactedGetParseStringFunc(Type type)
        {
            if (type.IsEnum)
            {
                var defaultValue = Activator.CreateInstance(type);
                return (value, options) => string.IsNullOrEmpty(value) ? defaultValue : Enum.Parse(type, value);
            }

            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                && type.GetGenericArguments()[0].IsEnum)
            {
                var enumType = type.GetGenericArguments()[0];
                return (value, options) => string.IsNullOrEmpty(value) ? null : Enum.Parse(enumType, value);
            }

            if (type == typeof(DateTime))
            {
                return ParseStringForDateTime;
            }

            if (type == typeof(DateTime?))
            {
                return ParseStringForNullableDateTime;
            }

            if (type == typeof(DateTimeOffset))
            {
                return ParseStringForDateTimeOffset;
            }

            if (type == typeof(DateTimeOffset?))
            {
                return ParseStringForNullableDateTimeOffset;
            }

            if (type == typeof(TimeSpan))
            {
                return ParseStringForTimeSpan;
            }

            if (type == typeof(TimeSpan?))
            {
                return ParseStringForNullableTimeSpan;
            }

            if (type == typeof(Guid))
            {
                return ParseStringForGuid;
            }

            if (type == typeof(Guid?))
            {
                return ParseStringForNullableGuid;
            }

            if (type == typeof(char) || type == typeof(char?))
            {
                return ParseStringForChar;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return (value, options) => string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, type.GetGenericArguments()[0]);
            }

            {
                var defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
                return (value, options) => string.IsNullOrEmpty(value) ? defaultValue : Convert.ChangeType(value, type);
            }
        }

        private static Func<object, ISerializeOptions, string> GetNonRedactedGetStringFunc(Type type)
        {
            if (type == typeof(bool))
            {
                return GetStringFromBool;
            }

            if (type == typeof(bool?))
            {
                return GetStringFromNullableBool;
            }

            if (type == typeof(DateTime))
            {
                return GetStringFromDateTime;
            }

            if (type == typeof(DateTime?))
            {
                return GetStringFromNullableDateTime;
            }

            if (type == typeof(DateTimeOffset))
            {
                return GetStringFromDateTimeOffset;
            }

            if (type == typeof(DateTimeOffset?))
            {
                return GetStringFromNullableDateTimeOffset;
            }

            if (type == typeof(TimeSpan))
            {
                return GetStringFromTimeSpan;
            }

            if (type == typeof(TimeSpan?))
            {
                return GetStringFromNullableTimeSpan;
            }

            if (type == typeof(char))
            {
                return GetStringFromChar;
            }

            if (type == typeof(char?))
            {
                return GetStringFromNullableChar;
            }

            return (value, options) => value.ToString();
        }

        private static object ParseStringForDateTime(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new DateTime();
            }

            return DateTime.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        private static object ParseStringForNullableDateTime(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return DateTime.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        private static object ParseStringForDateTimeOffset(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new DateTimeOffset();
            }

            return DateTimeOffset.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        private static object ParseStringForNullableDateTimeOffset(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return DateTimeOffset.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        private static object ParseStringForTimeSpan(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new TimeSpan();
            }

            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        private static object ParseStringForNullableTimeSpan(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        private static object ParseStringForGuid(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new Guid();
            }

            return Guid.Parse(value);
        }

        private static object ParseStringForNullableGuid(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return Guid.Parse(value);
        }

        private static object ParseStringForChar(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default(char);
            }

            // .Net serializers (XmlSerializer & DataContractSerializer) serialize char types as integers
            if (options.DotNetFrameworkSerializerCompatable)
            {
                int asciiValue = int.Parse(value);
                if (asciiValue >= char.MinValue && asciiValue <= char.MaxValue)
                {
                    return (char)asciiValue;
                }
            }

            return (char)Convert.ChangeType(value, typeof(char));
        }

        private static string GetStringFromBool(object value, ISerializeOptions options)
        {
            return value.ToString().ToLower();
        }

        private static string GetStringFromNullableBool(object value, ISerializeOptions options)
        {
            return value == null ? null : GetStringFromBool(value, options);
        }

        private static string GetStringFromDateTime(object value, ISerializeOptions options)
        {
            var dateTime = (DateTime)value;
            return dateTime.ToString("O");
        }

        private static string GetStringFromNullableDateTime(object value, ISerializeOptions options)
        {
            return value == null ? null : GetStringFromDateTime(value, options);
        }

        private static string GetStringFromDateTimeOffset(object value, ISerializeOptions options)
        {
            var dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset.ToString("O");
        }

        private static string GetStringFromNullableDateTimeOffset(object value, ISerializeOptions options)
        {
            return value == null ? null : GetStringFromDateTimeOffset(value, options);
        }

        private static string GetStringFromTimeSpan(object value, ISerializeOptions options)
        {
            var timeSpan = (TimeSpan)value;
            return timeSpan.ToString("G");
        }

        private static string GetStringFromNullableTimeSpan(object value, ISerializeOptions options)
        {
            return value == null ? null : GetStringFromTimeSpan(value, options);
        }

        private static string GetStringFromChar(object value, ISerializeOptions options)
        {
            char ch = (char)value;

            // .Net serializers (XmlSerializer & DataContractSerializer) serialize char types as integers
            if (options.DotNetFrameworkSerializerCompatable)
            {
                return ((int)ch).ToString();
            }

            return ch.ToString();
        }

        private static string GetStringFromNullableChar(object value, ISerializeOptions options)
        {
            return value == null ? null : GetStringFromNullableChar(value, options);
        }

        private static int CreateKey(Type type, RedactAttribute redactAttribute)
        {
            unchecked
            {
                var key = type.GetHashCode();

                if (redactAttribute != null)
                {
                    key = (key * 397) ^ redactAttribute.GetHashCode();
                }

                return key;
            }
        }
    }
}
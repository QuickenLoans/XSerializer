﻿using System;
using System.Collections.Concurrent;
using System.Globalization;
using XSerializer.Encryption;

namespace XSerializer
{
    internal class SimpleTypeValueConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<int, SimpleTypeValueConverter> _map = new ConcurrentDictionary<int, SimpleTypeValueConverter>();

        private readonly Func<string, ISerializeOptions, object> _parseString;
        private readonly Func<object, ISerializeOptions, string> _getString;

        private SimpleTypeValueConverter(Type type, RedactAttribute redactAttribute, EncryptAttribute encryptAttribute)
        {
            if (redactAttribute != null)
            {
                _parseString = GetRedactedGetParseStringFunc(type);
                _getString = GetRedactedGetStringFunc(type, redactAttribute);
            }
            else
            {
                var parseString = GetNonRedactedGetParseStringFunc(type);
                var getString = GetNonRedactedGetStringFunc(type);

                if (encryptAttribute != null)
                {
                    _parseString = (value, options) => parseString(EncryptionProvider.Current.Decrypt(value, options.ShouldEncrypt), options);
                    _getString = (value, options) => EncryptionProvider.Current.Encrypt(getString(value, options), options.ShouldEncrypt);
                }
                else
                {
                    _parseString = parseString;
                    _getString = getString;
                }
            }
        }

        public static SimpleTypeValueConverter Create(Type type, RedactAttribute redactAttribute, EncryptAttribute encryptAttribute)
        {
            return _map.GetOrAdd(
                CreateKey(type, redactAttribute, encryptAttribute),
                _ => new SimpleTypeValueConverter(type, redactAttribute, encryptAttribute));
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
                return (value, options) => value == null || value == "XXXXXX" ? defaultValue : Enum.Parse(type, value);
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return (value, options) => value == null || value == "XXXXXX" ? defaultValue : Convert.ChangeType(value, type);
            }

            if (type == typeof(DateTime))
            {
                return ParseStringForDateTime;
            }

            if (type == typeof(DateTime?))
            {
                return ParseStringForNullableDateTime;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return (value, options) => Convert.ChangeType(value, type.GetGenericArguments()[0]);
            }

            return (value, options) => Convert.ChangeType(value, type);
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

            return (value, options) => redactAttribute.Redact(value, options.ShouldRedact);
        }

        private static Func<string, ISerializeOptions, object> GetNonRedactedGetParseStringFunc(Type type)
        {
            if (type.IsEnum)
            {
                var defaultValue = Activator.CreateInstance(type);
                return (value, options) => value == null ? defaultValue : Enum.Parse(type, value);
            }

            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                && type.GetGenericArguments()[0].IsEnum)
            {
                var enumType = type.GetGenericArguments()[0];
                return (value, options) => value == null ? null : Enum.Parse(enumType, value);
            }

            if (type == typeof(DateTime))
            {
                return ParseStringForDateTime;
            }

            if (type == typeof(DateTime?))
            {
                return ParseStringForNullableDateTime;
            }

            if (type == typeof(Guid))
            {
                return ParseStringForGuid;
            }

            if (type == typeof(Guid?))
            {
                return ParseStringForNullableGuid;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return (value, options) => Convert.ChangeType(value, type.GetGenericArguments()[0]);
            }

            return (value, options) => Convert.ChangeType(value, type);
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

            return (value, options) => value.ToString();
        }

        private static object ParseStringForDateTime(string value, ISerializeOptions options)
        {
            if (value == null)
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
            if (value == null)
            {
                return null;
            }

            return DateTime.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        private static object ParseStringForGuid(string value, ISerializeOptions options)
        {
            if (value == null)
            {
                return new Guid();
            }

            return Guid.Parse(value);
        }

        private static object ParseStringForNullableGuid(string value, ISerializeOptions options)
        {
            if (value == null)
            {
                return null;
            }

            return Guid.Parse(value);
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

        private static int CreateKey(Type type, RedactAttribute redactAttribute, EncryptAttribute encryptAttribute)
        {
            unchecked
            {
                var key = type.GetHashCode();

                if (redactAttribute != null)
                {
                    key = (key * 397) ^ redactAttribute.GetHashCode();
                }

                if (encryptAttribute != null)
                {
                    key = (key * 397) ^ encryptAttribute.GetHashCode();
                }

                return key;
            }
        }
    }
}
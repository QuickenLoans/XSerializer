using System;
using System.Text.RegularExpressions;

namespace XSerializer
{
    public sealed class RedactAttribute : Attribute
    {
        private static readonly Regex Numbers = new Regex(@"\d", RegexOptions.Compiled);
        private static readonly Regex AllNumbers = new Regex(@"[-.0-9]", RegexOptions.Compiled);
        private static readonly Regex Letters = new Regex(@"[a-zA-Z]", RegexOptions.Compiled);

        private static readonly Lazy<IValueConverter> _booleanConverter =
            new Lazy<IValueConverter>(() => SimpleTypeValueConverter.Create(typeof(bool), null));

        private static readonly Lazy<IValueConverter> _dateTimeConverter =
            new Lazy<IValueConverter>(() => SimpleTypeValueConverter.Create(typeof(DateTime), null));

        private static readonly Lazy<IValueConverter> _dateTimeOffsetConverter =
            new Lazy<IValueConverter>(() => SimpleTypeValueConverter.Create(typeof(DateTimeOffset), null));

        private static readonly Lazy<IValueConverter> _timeSpanConverter =
            new Lazy<IValueConverter>(() => SimpleTypeValueConverter.Create(typeof(TimeSpan), null));

        /// <summary>
        /// Redacts the clear-text.
        /// </summary>
        /// <param name="clearText">Some clear-text.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled.</param>
        /// <returns>The redacted text.</returns>
        public string Redact(string clearText, bool redactEnabled)
        {
            if (clearText == null)
            {
                return null;
            }

            return redactEnabled
                ? Letters.Replace(Numbers.Replace(clearText, "1"), "X")
                : clearText;
        }

        /// <summary>
        /// Redacts the string representation of <paramref name="booleanValue"/>.
        /// </summary>
        /// <param name="booleanValue">A <see cref="bool"/>.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled.</param>
        /// <returns>The redacted text.</returns>
        public string Redact(bool? booleanValue, bool redactEnabled)
        {
            if (booleanValue == null)
            {
                return null;
            }

            return redactEnabled
                ? "XXXXXX"
                : _booleanConverter.Value.GetString(booleanValue, null);
        }

        /// <summary>
        /// Redacts the string representation of <paramref name="dateTimeValue"/>.
        /// </summary>
        /// <param name="dateTimeValue">A <see cref="DateTime"/>.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled.</param>
        /// <returns>The redacted text.</returns>
        public string Redact(DateTime? dateTimeValue, bool redactEnabled)
        {
            if (dateTimeValue == null)
            {
                return null;
            }

            return redactEnabled
                ? Numbers.Replace(_dateTimeConverter.Value.GetString(dateTimeValue, null), "1")
                : _dateTimeConverter.Value.GetString(dateTimeValue, null);
        }

        /// <summary>
        /// Redacts the string representation of <paramref name="dateTimeOffsetValue"/>.
        /// </summary>
        /// <param name="dateTimeOffsetValue">A <see cref="DateTimeOffset"/>.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled.</param>
        /// <returns>The redacted text.</returns>
        public string Redact(DateTimeOffset? dateTimeOffsetValue, bool redactEnabled)
        {
            if (dateTimeOffsetValue == null)
            {
                return null;
            }

            return redactEnabled
                ? Numbers.Replace(_dateTimeOffsetConverter.Value.GetString(dateTimeOffsetValue, null), "1")
                : _dateTimeOffsetConverter.Value.GetString(dateTimeOffsetValue, null);
        }

        /// <summary>
        /// Redacts the string representation of <paramref name="timeSpanValue"/>.
        /// </summary>
        /// <param name="timeSpanValue">A <see cref="TimeSpan"/>.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled.</param>
        /// <returns>The redacted text.</returns>
        public string Redact(TimeSpan? timeSpanValue, bool redactEnabled)
        {
            if (timeSpanValue == null)
            {
                return null;
            }

            return redactEnabled
                ? Numbers.Replace(_timeSpanConverter.Value.GetString(timeSpanValue, null), "1")
                : _timeSpanConverter.Value.GetString(timeSpanValue, null);
        }

        /// <summary>
        /// Redacts the string representation of <paramref name="charValue"/>.
        /// </summary>
        /// <param name="charValue">The character value.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled</param>
        /// <param name="serializeAsIntegerValue">Whether to serialize as integer value</param>
        /// <returns>The redacted text.</returns>
        public string Redact(char? charValue, bool redactEnabled, bool serializeAsIntegerValue)
        {
            if (charValue == null)
            {
                return null;
            }

            if (redactEnabled)
            {
                return "XXXXXX";
            }

            if (serializeAsIntegerValue)
            {
                return ((int)charValue.Value).ToString();
            }

            return charValue.ToString();
        }

        /// <summary>
        /// Redacts the string representation of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An object.</param>
        /// <param name="redactEnabled">Whether redaction is currently enabled.</param>
        /// <returns>The redacted text.</returns>
        public string Redact(object value, bool redactEnabled)
        {
            string redactedValue;
            if (ValueTypes.TryRedact(this, value, redactEnabled, out redactedValue))
            {
                return redactedValue;
            }

            return redactEnabled
                ? Letters.Replace(AllNumbers.Replace(value.ToString(), "1"), "X")
                : value.ToString();
        }
    }
}
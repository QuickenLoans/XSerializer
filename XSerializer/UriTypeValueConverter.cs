using System;

namespace XSerializer
{
    internal class UriTypeValueConverter : IValueConverter
    {
        private static readonly Lazy<IValueConverter> _default = new Lazy<IValueConverter>(() => new UriTypeValueConverter(null));

        private readonly RedactAttribute _redactAttribute;

        public UriTypeValueConverter(RedactAttribute redactAttribute)
        {
            _redactAttribute = redactAttribute;
        }

        public static IValueConverter Default
        {
            get { return _default.Value; }
        }

        public object ParseString(string value, ISerializeOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return new Uri(value);
        }

        public string GetString(object value, ISerializeOptions options)
        {
            var uri = value as Uri;

            if (uri == null)
            {
                return null;
            }

            var uriString =
                _redactAttribute != null
                    ? _redactAttribute.Redact(uri, options.ShouldRedact)
                    : uri.ToString();

            return uriString;
        }
    }
}

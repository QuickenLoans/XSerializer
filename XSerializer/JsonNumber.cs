using System;
using System.Globalization;

namespace XSerializer
{
    internal sealed class JsonNumber
    {
        private readonly string _stringValue;
        private readonly double _doubleValue;

        public JsonNumber(string value)
        {
            _stringValue = value;

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _doubleValue))
            {
                throw new ArgumentException("Invalid number value: " + value, "value");
            }
        }

        public string StringValue
        {
            get { return _stringValue; }
        }

        public double DoubleValue
        {
            get { return _doubleValue; }
        }
    }
}

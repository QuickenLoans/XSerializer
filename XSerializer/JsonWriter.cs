using System;
using System.Globalization;
using System.IO;

namespace XSerializer
{
    internal class JsonWriter : IDisposable
    {
        private readonly TextWriter _primaryWriter;
        private readonly IJsonSerializeOperationInfo _info;

        private bool _encryptWrites;

        private StringWriter _encryptingStringWriter;
        private TextWriter _currentWriter;

        public JsonWriter(TextWriter writer, IJsonSerializeOperationInfo info)
        {
            _primaryWriter = writer;
            _currentWriter = _primaryWriter;
            _info = info;
        }

        public bool EncryptWrites
        {
            get { return _encryptWrites; }
            set
            {
                if (value == _encryptWrites)
                {
                    return;
                }

                Flush();
                _encryptWrites = value;

                if (_encryptWrites)
                {
                    // We're starting an encrypt session
                    _encryptingStringWriter = new StringWriter();
                    _currentWriter = _encryptingStringWriter;
                }
                else
                {
                    var plainText = _encryptingStringWriter.GetStringBuilder().ToString();
                    _encryptingStringWriter = null;
                    _currentWriter = _primaryWriter;

                    // We're ending an encrypt session - encrypt if a mechanism exists.
                    if (_info.EncryptionMechanism != null)
                    {
                        var encryptedValue =
                            _info.EncryptionMechanism.Encrypt(
                                plainText,
                                _info.EncryptKey,
                                _info.SerializationState);

                        WriteValue(encryptedValue);
                    }
                    else
                    {
                        // No encryption mechanism exists - just write the plain text.
                        _currentWriter.Write(plainText);
                    }
                }
            }
        }

        public void WriteValue(string value)
        {
            if (value == null)
            {
                WriteNull();
            }
            else
            {
                _currentWriter.Write('"');
                _currentWriter.Write(Escape(value));
                _currentWriter.Write('"');
            }
        }

        public void WriteValue(DateTime value)
        {
            _currentWriter.Write('"');
            _currentWriter.Write(value.ToString("O", CultureInfo.InvariantCulture));
            _currentWriter.Write('"');
        }

        public void WriteValue(DateTimeOffset value)
        {
            _currentWriter.Write('"');
            _currentWriter.Write(value.ToString("O", CultureInfo.InvariantCulture));
            _currentWriter.Write('"');
        }

        public void WriteValue(TimeSpan value)
        {
            _currentWriter.Write('"');
            _currentWriter.Write(value.ToString("c", CultureInfo.InvariantCulture));
            _currentWriter.Write('"');
        }

        public void WriteValue(Guid value)
        {
            _currentWriter.Write('"');
            _currentWriter.Write(value.ToString("D"));
            _currentWriter.Write('"');
        }

        public void WriteValue(double value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(float value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(decimal value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(int value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(long value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(uint value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(ulong value)
        {
            _currentWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(bool value)
        {
            _currentWriter.Write(value ? "true" : "false");
        }

        public void WriteNull()
        {
            _currentWriter.Write("null");
        }

        public void WriteOpenObject()
        {
            _currentWriter.Write('{');
        }

        public void WriteCloseObject()
        {
            _currentWriter.Write('}');
        }

        public void WriteNameValueSeparator()
        {
            _currentWriter.Write(':');
        }

        public void WriteItemSeparator()
        {
            _currentWriter.Write(',');
        }

        public void WriteOpenArray()
        {
            _currentWriter.Write('[');
        }

        public void WriteCloseArray()
        {
            _currentWriter.Write(']');
        }

        public void Flush()
        {
            _currentWriter.Flush();
        }

        public void Dispose()
        {
            // TODO: Something?
        }

        private static string Escape(string value)
        {
            int flags = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '\\':
                        flags |= 0x01;
                        break;
                    case '"':
                        flags |= 0x02;
                        break;
                    case '/':
                        flags |= 0x04;
                        break;
                    case '\b':
                        flags |= 0x08;
                        break;
                    case '\f':
                        flags |= 0x10;
                        break;
                    case '\n':
                        flags |= 0x20;
                        break;
                    case '\r':
                        flags |= 0x40;
                        break;
                    case '\t':
                        flags |= 0x80;
                        break;
                }
            }

            if (flags == 0)
            {
                return value;
            }

            if ((flags & 0x01) == 0x01)
            {
                value = value.Replace(@"\", @"\\");
            }

            if ((flags & 0x02) == 0x02)
            {
                value = value.Replace(@"""", @"\""");
            }

            if ((flags & 0x04) == 0x04)
            {
                value = value.Replace("/", @"\/");
            }

            if ((flags & 0x08) == 0x08)
            {
                value = value.Replace("\b", @"\b");
            }

            if ((flags & 0x10) == 0x10)
            {
                value = value.Replace("\f", @"\f");
            }

            if ((flags & 0x20) == 0x20)
            {
                value = value.Replace("\n", @"\n");
            }

            if ((flags & 0x40) == 0x40)
            {
                value = value.Replace("\r", @"\r");
            }

            if ((flags & 0x80) == 0x80)
            {
                value = value.Replace("\t", @"\t");
            }

            return value;
        }
    }
}

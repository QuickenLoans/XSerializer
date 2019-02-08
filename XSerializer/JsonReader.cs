using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace XSerializer
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class JsonReader : IDisposable
    {
        private static readonly DynamicJsonSerializer _dynamicJsonSerializer = DynamicJsonSerializer.Get(false, JsonMappings.Empty, false);

        private readonly TextReader _primaryReader;
        private readonly IJsonSerializeOperationInfo _info;

        private StringReader _decryptedReader;
        private TextReader _currentReader;
        private bool _decryptReads;

        public JsonReader(TextReader reader, IJsonSerializeOperationInfo info)
        {
            _primaryReader = reader;
            _currentReader = _primaryReader;
            _info = info;
        }

        private JsonNodeType _nodeType;
        public JsonNodeType NodeType
        {
            get { return _nodeType; }
        }

        private object _value;
        public object Value
        {
            get { return _value; }
        }

        private int _currentLine;
        private int _currentPosition;

        public int Line { get; private set; }
        public int Position { get; private set; }

        public bool DecryptReads
        {
            get { return _decryptReads; }
        }

        public void SetDecryptReads(bool value, string path)
        {
            if (value == _decryptReads)
            {
                return;
            }

            _decryptReads = value;

            if (_decryptReads)
            {
                if (NodeType == JsonNodeType.Null)
                {
                    return;
                }

                if (NodeType != JsonNodeType.String)
                {
                    // All encrypted values are transformed into string nodes. That means this
                    // node can't possibly be encrypted, so reverse the decision to decrypt reads.
                    _decryptReads = false;
                    return;
                }

                var decrypted = _info.EncryptionMechanism.Decrypt((string)Value, _info.EncryptKey, _info.SerializationState);

                if (decrypted == (string)Value && decrypted != "")
                {
                    // If the encryption mechanism returns the same value passed to it, assume
                    // that the value is plain-text and reverse the decision to decrypt reads.
                    _decryptReads = false;
                    return;
                }

                _decryptedReader = new StringReader(decrypted);
                _currentReader = _decryptedReader;
                ReadContent(path);
            }
            else
            {
                if (NodeType != JsonNodeType.Null && _decryptedReader.Peek() != -1)
                {
                    throw new InvalidOperationException("Attempted to set DecryptReads to false before the encrypted stream has been consumed.");
                }

                _decryptedReader = null;
                _currentReader = _primaryReader;
            }
        }

        public void Dispose()
        {
            // TODO: Something?
        }

        /// <summary>
        /// Reads the next non-whitespace node from the stream.
        /// </summary>
        /// <returns>true if the next node was read successfully; false if there are no more nodes to read.</returns>
        public bool ReadContent(string path)
        {
            while (true)
            {
                if (!Read(path))
                {
                    return false;
                }

                if (NodeType != JsonNodeType.Whitespace)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Read the properties of a json object. The resulting collection contains the name of each
        /// property. As the collection is enumerated, for each property name, the reader is positioned
        /// at the beginning of the property's value. The caller is expected to parse the value, calling
        /// <see cref="Read"/> or <see cref="ReadContent"/> one or more times, before continuing to
        /// enumerate the collection.
        /// </summary>
        /// <param name="path">
        /// The path to the current object. Used for error reporting.
        /// </param>
        /// <exception cref="XSerializerException">If the JSON object is malformed.</exception>
        public IEnumerable<string> ReadProperties(string path)
        {
            if (NodeType != JsonNodeType.OpenObject)
            {
                throw new MalformedDocumentException(
                    MalformedDocumentError.ObjectMissingOpenCurlyBrace, path, Line, Position);
            }

            while (true)
            {
                if (!ReadContent(path))
                {
                    if (NodeType == JsonNodeType.EndOfString)
                    {
                        throw new MalformedDocumentException(
                            MalformedDocumentError.PropertyNameMissing,
                            path, Line, Position);
                    }

                    Debug.Assert(NodeType == JsonNodeType.Invalid);

                    if (Value is string)
                    {
                        throw new MalformedDocumentException(
                            MalformedDocumentError.PropertyNameMissingCloseQuote,
                            path, Line, Position);
                    }

                    throw new MalformedDocumentException(
                        MalformedDocumentError.PropertyNameMissingOpenQuote,
                        path, Value, Line, Position);
                }

                switch (NodeType)
                {
                    case JsonNodeType.CloseObject:
                        yield break;
                    case JsonNodeType.String:
                        break;
                    default:
                        throw new MalformedDocumentException(
                            MalformedDocumentError.PropertyInvalidName, path, Value, Line, Position);
                }

                var name = (string)Value;

                if (!ReadContent(path) || NodeType != JsonNodeType.NameValueSeparator)
                {
                    throw new MalformedDocumentException(
                        MalformedDocumentError.PropertyMissingNameValueSeparator,
                        path.AppendProperty(name), Line, Position);
                }

                yield return name;

                // The caller is expected to make one or more Read calls after receiving the yielded property name.

                if (!ReadContent(path))
                {
                    throw new MalformedDocumentException(
                        MalformedDocumentError.ObjectMissingCloseCurlyBrace,
                        path.AppendProperty(name), Line, Position);
                }

                switch (NodeType)
                {
                    case JsonNodeType.CloseObject:
                        yield break;
                    case JsonNodeType.ItemSeparator:
                        break;
                    case JsonNodeType.EndOfString:
                        throw new MalformedDocumentException(
                            MalformedDocumentError.PropertyMissingItemSeparator,
                            path.AppendProperty(name), Line, Position);
                    default:
                        throw new MalformedDocumentException(
                            MalformedDocumentError.PropertyMissingItemSeparator,
                            path.AppendProperty(name), Value, Line, Position);
                }
            }
        }

        /// <summary>
        /// Read and discard the next content value. If the next content type is <see cref="JsonNodeType.OpenObject"/>
        /// or <see cref="JsonNodeType.OpenArray"/>, then the reader will continue to read and discard content until
        /// the matching <see cref="JsonNodeType.CloseObject"/> or <see cref="JsonNodeType.CloseArray"/> content type
        /// is found. For all other content types, no additional reads are made.
        /// </summary>
        public void Discard(string path)
        {
            _dynamicJsonSerializer.DeserializeObject(this, _info, path);
        }

        /// <summary>
        /// Reads the next non-whitespace node type without changing the state of the reader. If the
        /// next node type is whitespace, then all leading whitespace is consumed and discarded. The
        /// next node type is then returned, again without changing the state of the reader.
        /// </summary>
        /// <returns>The next non-whitespace node type in the stream.</returns>
        public JsonNodeType PeekContent()
        {
            while (true)
            {
                var peek = _currentReader.Peek();

                switch (peek)
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        ReadWhitespace((char)peek);
                        continue;
                    case '"':
                        return JsonNodeType.String;
                    case '-':
                    case '.':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return JsonNodeType.Number;
                    case 't':
                    case 'f':
                        return JsonNodeType.Boolean;
                    case 'n':
                        return JsonNodeType.Null;
                    case '{':
                        return JsonNodeType.OpenObject;
                    case '}':
                        return JsonNodeType.CloseObject;
                    case ':':
                        return JsonNodeType.NameValueSeparator;
                    case ',':
                        return JsonNodeType.ItemSeparator;
                    case '[':
                        return JsonNodeType.OpenArray;
                    case ']':
                        return JsonNodeType.CloseArray;
                    case -1:
                        return JsonNodeType.EndOfString;
                    default:
                        return JsonNodeType.Invalid;
                }
            }
        }

        /// <summary>
        /// Reads the next node from the stream.
        /// </summary>
        /// <returns>true if the next node was read successfully; false if there are no more nodes to read.</returns>
        public bool Read(string path)
        {
            if (ReferenceEquals(_currentReader, _primaryReader))
            {
                Line = _currentLine;
                Position = _currentPosition;
            }

            var read = ReadCurrent();

            switch (read)
            {
                case -1:
                    _value = null;
                    _nodeType = JsonNodeType.EndOfString;
                    return false;
                case '"':
                    return TryReadString(out _value, out _nodeType);
                case '-':
                case '.':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    _value = ReadNumber((char)read);
                    _nodeType = JsonNodeType.Number;
                    return true;
                case 't':
                    ReadLiteral(path, "true", 'r', 'u', 'e');
                    _value = true;
                    _nodeType = JsonNodeType.Boolean;
                    return true;
                case 'f':
                    ReadLiteral(path, "false", 'a', 'l', 's', 'e');
                    _value = false;
                    _nodeType = JsonNodeType.Boolean;
                    return true;
                case 'n':
                    ReadLiteral(path, "null", 'u', 'l', 'l');
                    _value = null;
                    _nodeType = JsonNodeType.Null;
                    return true;
                case '{':
                    _value = '{';
                    _nodeType = JsonNodeType.OpenObject;
                    return true;
                case '}':
                    _value = '}';
                    _nodeType = JsonNodeType.CloseObject;
                    return true;
                case ':':
                    _value = ':';
                    _nodeType = JsonNodeType.NameValueSeparator;
                    return true;
                case ',':
                    _value = ',';
                    _nodeType = JsonNodeType.ItemSeparator;
                    return true;
                case '[':
                    _value = '[';
                    _nodeType = JsonNodeType.OpenArray;
                    return true;
                case ']':
                    _value = ']';
                    _nodeType = JsonNodeType.CloseArray;
                    return true;
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    _value = ReadWhitespace((char)read);
                    _nodeType = JsonNodeType.Whitespace;
                    return true;
            }

            _value = (char)read;
            _nodeType = JsonNodeType.Invalid;
            return false;
        }

        private void ReadLiteral(string path, string value, params char[] literalMinusFirstChar)
        {
            for (int i = 0; i < literalMinusFirstChar.Length; i++)
            {
                var read = ReadCurrent();

                if (read == -1)
                {
                    throw new MalformedDocumentException(
                        MalformedDocumentError.LiteralInvalidValue,
                        path, value.Substring(0, i + 1), Line, Position, null, value);
                }

                if (read != literalMinusFirstChar[i])
                {
                    throw new MalformedDocumentException(
                        MalformedDocumentError.LiteralInvalidValue,
                        path, value.Substring(0, i + 1) + (char)read, Line, Position, null, value);
                }
            }
        }

        private bool TryReadString(out object value, out JsonNodeType nodeType)
        {
            var sb = new StringBuilder(38); // Large enough to read a DateTime or Guid.

            while (true)
            {
                var read = ReadCurrent();

                switch (read)
                {
                    case '"':
                        value = sb.ToString();
                        nodeType = JsonNodeType.String;
                        return true;
                    case '\\':
                        sb.Append(ReadEscapedChar());
                        break;
                    case -1:
                        value = sb.Insert(0, '"').ToString();
                        nodeType = JsonNodeType.Invalid;
                        return false;
                    default:
                        sb.Append((char)read);
                        break;
                }
            }
        }

        private char ReadEscapedChar()
        {
            var read = ReadCurrent();

            switch (read)
            {
                case '"':
                case '\\':
                case '/':
                    return (char)read;
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'u':
                    throw new NotImplementedException("Hexadecimal unicode escape characters have not yet been implemented.");
                case -1:
                    throw new XSerializerException("Reached end of input before reading escaped character.");
                default:
                    throw new XSerializerException("Unknown escaped character: \\" + (char)read);
            }
        }

        private string ReadNumber(char c)
        {
            var sb = new StringBuilder();
            sb.Append(c);

            while (true)
            {
                var peek = _currentReader.Peek();

                switch (peek)
                {
                    case '+':
                    case '-':
                    case '.':
                    case 'e':
                    case 'E':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    default:
                        return sb.ToString();
                }

                sb.Append((char)ReadCurrent());
            }
        }

        private string ReadWhitespace(char first)
        {
            var sb = new StringBuilder();
            sb.Append(first);

            while (IsWhitespace(_currentReader.Peek()))
            {
                sb.Append((char)ReadCurrent());
            }

            return sb.ToString();
        }

        private static bool IsWhitespace(int c)
        {
            switch (c)
            {
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    return true;
                default:
                    return false;
            }
        }

        private int ReadCurrent()
        {
            var current = _currentReader.Read();

            if (ReferenceEquals(_currentReader, _primaryReader))
            {
                if (current != -1)
                {
                    switch ((char)current)
                    {
                        case '\r':
                            break;
                        case '\n':
                            _currentLine++;
                            _currentPosition = 0;
                            break;
                        default:
                            _currentPosition++;
                            break;
                    }
                    
                }
            }

            return current;
        }

        private string DebuggerDisplay
        {
            get
            {
                string valueString;

                if (Value == null)
                {
                    valueString = "[null]";
                }
                else if (Value is char)
                {
                    valueString = "'" + Value + "'";
                }
                else if (Value is bool)
                {
                    valueString = ((bool)Value) ? "true" : "false";
                }
                else
                {
                    valueString = (string)Value;

                    if (valueString.Length > 0)
                    {
                        if (IsWhitespace(valueString[0]))
                        {
                            valueString = string.Format(@"""{0}""",
                                valueString.Replace("\r", "\\r")
                                    .Replace("\n", "\\n")
                                    .Replace("\t", "\\t"));
                        }
                        else
                        {
                            double dummy;
                            if (!double.TryParse(valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out dummy))
                            {
                                valueString = string.Format(@"""{0}""", valueString);
                            }
                        }
                    }
                    else
                    {
                        valueString = @"""""";
                    }
                }

                return string.Format("{0}: {1}", NodeType, valueString);
            }
        }
    }
}

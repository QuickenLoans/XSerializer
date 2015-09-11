using System;

namespace XSerializer
{
    internal sealed class NumberJsonSerializer : IJsonSerializerInternal
    {
        private static readonly Lazy<NumberJsonSerializer> _clearText = new Lazy<NumberJsonSerializer>(() => new NumberJsonSerializer(false));
        private static readonly Lazy<NumberJsonSerializer> _encrypted = new Lazy<NumberJsonSerializer>(() => new NumberJsonSerializer(true));
        
        private readonly bool _encrypt;

        private NumberJsonSerializer(bool encrypt)
        {
            _encrypt = encrypt;
        }

        public static NumberJsonSerializer Get(bool encrypt)
        {
            return encrypt ? _encrypted.Value : _clearText.Value;
        }

        public void SerializeObject(JsonWriter writer, object instance, IJsonSerializeOperationInfo info)
        {
            var d = (double)instance;

            var toggler = new EncryptWritesToggler(writer);

            if (_encrypt)
            {
                toggler.Toggle();
            }

            writer.WriteValue(d);

            toggler.Revert();
        }

        public object DeserializeObject(JsonReader reader, IJsonSerializeOperationInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
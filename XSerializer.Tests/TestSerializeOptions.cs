using System.Xml.Serialization;
using XSerializer.Encryption;

namespace XSerializer.Tests
{
    public class TestSerializeOptions : ISerializeOptions
    {
        public TestSerializeOptions(bool leaveNamespacesNull = false, bool shouldAlwaysEmitTypes = false)
        {
            if (!leaveNamespacesNull)
            {
                Namespaces = new XmlSerializerNamespaces();
            }

            ShouldAlwaysEmitTypes = shouldAlwaysEmitTypes;
        }

        public TestSerializeOptions(XmlSerializerNamespaces namespaces, bool shouldAlwaysEmitTypes = false)
        {
            Namespaces = namespaces;
            ShouldAlwaysEmitTypes = shouldAlwaysEmitTypes;
        }

        public XmlSerializerNamespaces Namespaces { get; set; }
        public bool ShouldAlwaysEmitTypes { get; set; }
        public bool ShouldRedact { get; private set; }
        public bool ShouldEncrypt { get; private set; }
        public bool ShouldEmitNil { get; private set; }
        public IEncryptionMechanism EncryptionMechanism { get; set; }
        public object EncryptKey { get; set; }
        public SerializationState SerializationState { get; set; }
        public bool DotNetFrameworkSerializerCompatable { get; set; }
    }
}
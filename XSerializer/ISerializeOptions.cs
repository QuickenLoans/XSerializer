using System.Globalization;
using System.Xml.Serialization;
using XSerializer.Encryption;

namespace XSerializer
{
    public interface ISerializeOptions
    {
        XmlSerializerNamespaces Namespaces { get; }
        bool ShouldAlwaysEmitTypes { get; }
        bool ShouldRedact { get; }
        bool ShouldEncrypt { get; }
        bool ShouldEmitNil { get; }
        IEncryptionMechanism EncryptionMechanism { get; }
        object EncryptKey { get; }
        SerializationState SerializationState { get; }
        bool ShouldIgnoreCaseForEnum { get; }
        bool ShouldSerializeCharAsInt { get; }
        CultureInfo Culture { get; }
    }
}

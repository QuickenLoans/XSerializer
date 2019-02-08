using System;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using XSerializer.Encryption;

namespace XSerializer
{
    public class XmlSerializationOptions : IXmlSerializerOptions, ISerializeOptions
    {
        private readonly XmlSerializerNamespaces _namespaces;
        private string _defaultNamespace;
        private string _rootElementName;
        private bool _shouldAlwaysEmitTypes;
        private bool _shouldRedact;
        private bool _shouldEncrypt;
        private Type[] _extraTypes;
        private bool _treatEmptyElementAsString;
        private Encoding _encoding;
        private bool _shouldEncryptRootObject;
        private bool _shouldIndent;
        private bool _emitNil;
        private IEncryptionMechanism _encryptionMechanism;
        private object _encryptKey;
        private bool _shouldIgnoreCaseForEnum;
        private bool _shouldSerializeCharAsInt;
        private bool _shouldUseAttributeDefinedInInterface;
        private CultureInfo _culture;

        public XmlSerializationOptions(
            XmlSerializerNamespaces namespaces,
            Encoding encoding,
            bool shouldEncryptRootObject,
            string defaultNamespace,
            bool shouldIndent,
            string rootElementName,
            bool shouldAlwaysEmitTypes,
            bool shouldRedact,
            bool shouldEncrypt,
            bool treatEmptyElementAsString,
            bool emitNil,
            IEncryptionMechanism encryptionMechanism,
            object encryptKey,
            bool shouldIgnoreCaseForEnum,
            bool shouldSerializeCharAsInt)
            : this(namespaces, encoding, shouldEncryptRootObject, defaultNamespace,
                    shouldIndent, rootElementName, shouldAlwaysEmitTypes, shouldRedact,
                    shouldEncrypt, treatEmptyElementAsString, emitNil, encryptionMechanism,
                    encryptKey, shouldIgnoreCaseForEnum, shouldSerializeCharAsInt, false, CultureInfo.InvariantCulture)
        {
        }

        public XmlSerializationOptions(
            XmlSerializerNamespaces namespaces,
            Encoding encoding,
            bool shouldEncryptRootObject,
            string defaultNamespace,
            bool shouldIndent,
            string rootElementName,
            bool shouldAlwaysEmitTypes,
            bool shouldRedact,
            bool shouldEncrypt,
            bool treatEmptyElementAsString,
            bool emitNil,
            IEncryptionMechanism encryptionMechanism,
            object encryptKey,
            bool shouldIgnoreCaseForEnum,
            bool shouldSerializeCharAsInt,
            CultureInfo culture)
            : this(namespaces, encoding, shouldEncryptRootObject, defaultNamespace,
                    shouldIndent, rootElementName, shouldAlwaysEmitTypes, shouldRedact,
                    shouldEncrypt, treatEmptyElementAsString, emitNil, encryptionMechanism,
                    encryptKey, shouldIgnoreCaseForEnum, shouldSerializeCharAsInt, false, culture)
        {
        }

        public XmlSerializationOptions(
            XmlSerializerNamespaces namespaces = null,
            Encoding encoding = null,
            bool shouldEncryptRootObject = false,
            string defaultNamespace = null,
            bool shouldIndent = false,
            string rootElementName = null,
            bool shouldAlwaysEmitTypes = false,
            bool shouldRedact = true,
            bool shouldEncrypt = true,
            bool treatEmptyElementAsString = false,
            bool emitNil = false,
            IEncryptionMechanism encryptionMechanism = null,
            object encryptKey = null,
            bool shouldIgnoreCaseForEnum = false,
            bool shouldSerializeCharAsInt = false,
            bool shouldUseAttributeDefinedInInterface = false,
            CultureInfo culture = null)
        {
            _namespaces = namespaces ?? new XmlSerializerNamespaces();
            _encoding = encoding ?? Encoding.UTF8;
            _shouldEncryptRootObject = shouldEncryptRootObject;
            _defaultNamespace = defaultNamespace;
            _shouldIndent = shouldIndent;
            _rootElementName = rootElementName;
            _shouldAlwaysEmitTypes = shouldAlwaysEmitTypes;
            _shouldRedact = shouldRedact;
            _shouldEncrypt = shouldEncrypt;
            _extraTypes = null;
            _treatEmptyElementAsString = treatEmptyElementAsString;
            _emitNil = emitNil;
            _encryptionMechanism = encryptionMechanism;
            _encryptKey = encryptKey;
            _shouldIgnoreCaseForEnum = shouldIgnoreCaseForEnum;
            _shouldSerializeCharAsInt = shouldSerializeCharAsInt;
            _shouldUseAttributeDefinedInInterface = shouldUseAttributeDefinedInInterface;
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        internal Encoding Encoding { get { return _encoding; } }
        internal bool ShouldEncryptRootObject { get { return _shouldEncryptRootObject; } }
        string IXmlSerializerOptions.DefaultNamespace { get { return _defaultNamespace; } }
        XmlSerializerNamespaces ISerializeOptions.Namespaces { get { return _namespaces; } }
        internal bool ShouldIndent { get { return _shouldIndent; } }
        string IXmlSerializerOptions.RootElementName { get { return _rootElementName; } }
        bool ISerializeOptions.ShouldAlwaysEmitTypes { get { return _shouldAlwaysEmitTypes; } }
        bool ISerializeOptions.ShouldRedact { get { return _shouldRedact; } }
        bool ISerializeOptions.ShouldEncrypt { get { return _shouldEncrypt; } }
        Type[] IXmlSerializerOptions.ExtraTypes { get { return _extraTypes; } }
        RedactAttribute IXmlSerializerOptions.RedactAttribute { get { return null; } }
        bool IXmlSerializerOptions.TreatEmptyElementAsString { get { return _treatEmptyElementAsString; } }
        public CultureInfo Culture { get { return _culture; } set { _culture = value; } }


        bool IXmlSerializerOptions.ShouldAlwaysEmitNil
        {
            get { return false; }
        }

        bool ISerializeOptions.ShouldEmitNil { get { return _emitNil; } }

        IEncryptionMechanism ISerializeOptions.EncryptionMechanism { get { return _encryptionMechanism; } }
        object ISerializeOptions.EncryptKey { get { return _encryptKey; } }
        SerializationState ISerializeOptions.SerializationState { get { return null; } }
        bool ISerializeOptions.ShouldIgnoreCaseForEnum { get { return _shouldIgnoreCaseForEnum; } }
        bool ISerializeOptions.ShouldSerializeCharAsInt { get { return _shouldSerializeCharAsInt; }}
        bool IXmlSerializerOptions.ShouldUseAttributeDefinedInInterface { get { return _shouldUseAttributeDefinedInInterface; } }

        internal void SetExtraTypes(Type[] extraTypes)
        {
            _extraTypes = extraTypes;
        }

        public XmlSerializationOptions AddNamespace(string prefix, string ns)
        {
            _namespaces.Add(prefix, ns);
            return this;
        }

        public XmlSerializationOptions WithEncoding(Encoding encoding)
        {
            _encoding = encoding;
            return this;
        }

        public XmlSerializationOptions EncryptRootObject()
        {
            _shouldEncryptRootObject = true;
            return this;
        }

        public XmlSerializationOptions WithDefaultNamespace(string defaultNamespace)
        {
            _defaultNamespace = defaultNamespace;
            return this;
        }

        public XmlSerializationOptions Indent()
        {
            _shouldIndent = true;
            return this;
        }

        public XmlSerializationOptions SetRootElementName(string rootElementName)
        {
            _rootElementName = rootElementName;
            return this;
        }

        public XmlSerializationOptions AlwaysEmitTypes()
        {
            _shouldAlwaysEmitTypes = true;
            return this;
        }

        public XmlSerializationOptions DisableRedact()
        {
            _shouldRedact = false;
            return this;
        }

        public XmlSerializationOptions DisableEncryption()
        {
            _shouldEncrypt = false;
            return this;
        }

        public XmlSerializationOptions ShouldTreatEmptyElementAsString()
        {
            _treatEmptyElementAsString = true;
            return this;
        }

        public XmlSerializationOptions EmitNil()
        {
            _emitNil = true;
            return this;
        }

        public XmlSerializationOptions WithEncryptionMechanism(IEncryptionMechanism encryptionMechanism)
        {
            _encryptionMechanism = encryptionMechanism;
            return this;
        }

        public XmlSerializationOptions WithEncryptKey(object encryptKey)
        {
            _encryptKey = encryptKey;
            return this;
        }

        public XmlSerializationOptions IgnoreCaseForEnum()
        {
            _shouldIgnoreCaseForEnum = true;
            return this;
        }

        public XmlSerializationOptions SerializeCharAsInt()
        {
            _shouldSerializeCharAsInt = true;
            return this;
        }

        public XmlSerializationOptions ShouldUseAttributeDefinedInInterface()
        {
            _shouldUseAttributeDefinedInInterface = true;
            return this;
        }
    }
}

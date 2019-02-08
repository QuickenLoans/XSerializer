using System;
using System.Globalization;

namespace XSerializer.Tests
{
    public class TestXmlSerializerOptions : IXmlSerializerOptions
    {
        public static TestXmlSerializerOptions Empty = new TestXmlSerializerOptions();

        public static TestXmlSerializerOptions WithExtraTypes(params Type[] extraTypes)
        {
            return new TestXmlSerializerOptions { ExtraTypes = extraTypes };
        }

        public string DefaultNamespace { get; set; }
        public Type[] ExtraTypes { get; set; }
        public string RootElementName { get; set; }
        public RedactAttribute RedactAttribute { get; set; }
        public bool TreatEmptyElementAsString { get; set; }
        public bool ShouldAlwaysEmitNil { get; set; }
        public bool ShouldUseAttributeDefinedInInterface { get; set; }
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
    }
}

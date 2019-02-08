using System;
using System.Globalization;

namespace XSerializer
{
    public interface IXmlSerializerOptions
    {
        string DefaultNamespace { get; }
        Type[] ExtraTypes { get; }
        string RootElementName { get; }
        RedactAttribute RedactAttribute { get; }
        bool TreatEmptyElementAsString { get; }
        bool ShouldAlwaysEmitNil { get; }
        bool ShouldUseAttributeDefinedInInterface { get; }
        CultureInfo Culture { get; }
    }
}

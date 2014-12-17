﻿using System.Xml.Serialization;

namespace XSerializer
{
    public interface ISerializeOptions
    {
        XmlSerializerNamespaces Namespaces { get; }
        bool ShouldAlwaysEmitTypes { get; }
        bool ShouldRedact { get; }
        bool ShouldEncrypt { get; }
        bool ShouldEmitNil { get; }
    }
}
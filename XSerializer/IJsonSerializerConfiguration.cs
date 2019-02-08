using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using XSerializer.Encryption;

namespace XSerializer
{
    /// <summary>
    /// Contains various configuration settings required by instances of <see cref="JsonSerializer{T}"/>.
    /// </summary>
    public interface IJsonSerializerConfiguration
    {
        /// <summary>
        /// Gets the <see cref="Encoding"/> to be used for serialization operations.
        /// </summary>
        Encoding Encoding { get; }

        /// <summary>
        /// Gets an instance of <see cref="IEncryptionMechanism"/> to be used to encrypted properties
        /// decorated with the <see cref="EncryptAttribute"/> attribute. If null, those properties
        /// will not be encryped.d
        /// </summary>
        IEncryptionMechanism EncryptionMechanism { get; }

        /// <summary>
        /// Gets an object to be used to retrieve encryption settings for an encryption operation.
        /// </summary>
        object EncryptKey { get; }

        /// <summary>
        /// Gets a value indicating whether the root object should be encrypted in its entirety.
        /// </summary>
        bool EncryptRootObject { get; }

        /// <summary>
        /// Gets an object used to create <see cref="DateTime"/> values from a string representation.
        /// </summary>
        IDateTimeHandler DateTimeHandler { get; }

        /// <summary>
        /// Gets a dictionary for type-to-type mappings, where the value type is assignable to the key
        /// type. This is typically a interface-to-implementation or astract-class-to-inheritor
        /// relationship, although a concrete-class-to-inheritor relationship is also possible.
        /// </summary>
        IDictionary<Type, Type> MappingsByType { get; }

        /// <summary>
        /// Gets a dictionary for property-to-type mappings, where the value type is assignable to
        /// the key property's type. This is typically a interface-to-implementation or
        /// astract-class-to-inheritor relationship, although a concrete-class-to-inheritor relationship
        /// is also possible.
        /// </summary>
        IDictionary<PropertyInfo, Type> MappingsByProperty { get; }

        /// <summary>
        /// Gets a value indicating whether attributes defined on an interface property
        /// should be considered when retrieving attributes.
        /// </summary>
        bool ShouldUseAttributeDefinedInInterface { get; }
    }
}

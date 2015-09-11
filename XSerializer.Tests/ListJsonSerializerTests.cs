using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using XSerializer.Encryption;
using XSerializer.Tests.Encryption;

namespace XSerializer.Tests
{
    public class ListJsonSerializerTests
    {
        [Test]
        public void CanSerializeGenericIEnumerable()
        {
            var serializer = new JsonSerializer<List<string>>();

            var json = serializer.Serialize(new List<string> { "abc", "xyz" });

            Assert.That(json, Is.EqualTo(@"[""abc"",""xyz""]"));
        }

        [Test]
        public void CanSerializeGenericIEnumerableEncrypted()
        {
            var encryptionMechanism = new Base64EncryptionMechanism();

            var configuration = new JsonSerializerConfiguration
            {
                EncryptionMechanism = encryptionMechanism,
                EncryptionEnabled = true,
                EncryptRootObject = true
            };

            var serializer = new JsonSerializer<List<string>>(configuration);

            var json = serializer.Serialize(new List<string> { "abc", "xyz" });

            var expected =
                @""""
                + encryptionMechanism.Encrypt(@"[""abc"",""xyz""]")
                + @"""";

            Assert.That(json, Is.EqualTo(expected));
        }

        [Test]
        public void CanSerializeNonGenericIEnumerable()
        {
            var serializer = new JsonSerializer<ArrayList>();

            var json = serializer.Serialize(new ArrayList { "abc", "xyz" });

            Assert.That(json, Is.EqualTo(@"[""abc"",""xyz""]"));
        }

        [Test]
        public void CanSerializeSerializeNonGenericIEnumerableEncrypted()
        {
            var encryptionMechanism = new Base64EncryptionMechanism();

            var configuration = new JsonSerializerConfiguration
            {
                EncryptionMechanism = encryptionMechanism,
                EncryptionEnabled = true,
                EncryptRootObject = true
            };

            var serializer = new JsonSerializer<ArrayList>(configuration);

            var json = serializer.Serialize(new ArrayList { "abc", "xyz" });

            var expected =
                @""""
                + encryptionMechanism.Encrypt(@"[""abc"",""xyz""]")
                + @"""";

            Assert.That(json, Is.EqualTo(expected));
        }

        [Test]
        public void CanSerializeGenericIEnumerableOfObjectWhenAnItemTypeIsDecoratedWithEncrypteAttribute()
        {
            var encryptionMechanism = new Base64EncryptionMechanism();

            var configuration = new JsonSerializerConfiguration
            {
                EncryptionMechanism = encryptionMechanism,
                EncryptionEnabled = true
            };

            var serializer = new JsonSerializer<List<object>>(configuration);

            var json = serializer.Serialize(new List<object> { new Foo { Bar = "abc", Baz = true }, new Foo { Bar = "xyz", Baz = false } });

            var expected =
                "["
                    + @""""
                    + encryptionMechanism.Encrypt(@"{""Bar"":""abc"",""Baz"":true}")
                    + @""""
                + ","
                    + @""""
                    + encryptionMechanism.Encrypt(@"{""Bar"":""xyz"",""Baz"":false}")
                    + @""""
                + "]";

            Assert.That(json, Is.EqualTo(expected));
        }

        [Test]
        public void CanSerializeGenericIEnumerableOfObjectWhenAnItemTypePropertyIsDecoratedWithEncrypteAttribute()
        {
            var encryptionMechanism = new Base64EncryptionMechanism();

            var configuration = new JsonSerializerConfiguration
            {
                EncryptionMechanism = encryptionMechanism,
                EncryptionEnabled = true
            };

            var serializer = new JsonSerializer<List<object>>(configuration);

            var json = serializer.Serialize(new List<object> { new Qux { Bar = "abc", Baz = true }, new Qux { Bar = "xyz", Baz = false } });

            var expected =
                @"[{""Bar"":"
                    + @""""
                    + encryptionMechanism.Encrypt(@"""abc""")
                    + @""""
                + @",""Baz"":true},{""Bar"":"
                    + @""""
                    + encryptionMechanism.Encrypt(@"""xyz""")
                    + @""""
                + @",""Baz"":false}]";

            Assert.That(json, Is.EqualTo(expected));
        }

        [Encrypt]
        public class Foo
        {
            public string Bar { get; set; }
            public bool Baz { get; set; }
        }

        public class Qux
        {
            [Encrypt]
            public string Bar { get; set; }
            public bool Baz { get; set; }
        }
    }
}
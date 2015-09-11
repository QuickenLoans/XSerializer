﻿using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using XSerializer.Tests.Encryption;

namespace XSerializer.Tests
{
    public class JsonReaderTests
    {
        [TestCase("true", new[] { JsonNodeType.Boolean })]
        [TestCase("false", new[] { JsonNodeType.Boolean })]
        [TestCase("null", new[] { JsonNodeType.Null })]
        [TestCase("123.45", new[] { JsonNodeType.Number })]
        [TestCase("\"abc\"", new[] { JsonNodeType.String })]
        [TestCase("{\"foo\":\"bar\"}", JsonNodeType.OpenObject, JsonNodeType.String, JsonNodeType.NameValueSeparator, JsonNodeType.String, JsonNodeType.CloseObject)]
        [TestCase("[\"foo\",\"bar\"]", JsonNodeType.OpenArray, JsonNodeType.String, JsonNodeType.ItemSeparator, JsonNodeType.String, JsonNodeType.CloseArray)]
        public void CanReadEachNodeType(string json, params JsonNodeType[] expectedNodeTypes)
        {
            var reader = new JsonReader(new StringReader(json), new JsonSerializeOperationInfo());
            Assert.That(reader.NodeType, Is.EqualTo(JsonNodeType.None));

            foreach (var expectedNodeType in expectedNodeTypes)
            {
                reader.Read();
                Assert.That(reader.NodeType, Is.EqualTo(expectedNodeType));
            }

            reader.Read();
            Assert.That(reader.NodeType, Is.EqualTo(JsonNodeType.None));
        }

        [TestCase("true", new[] { JsonNodeType.Boolean })]
        [TestCase("false", new[] { JsonNodeType.Boolean })]
        [TestCase("null", new[] { JsonNodeType.Null })]
        [TestCase("123.45", new[] { JsonNodeType.Number })]
        [TestCase("\"abc\"", new[] { JsonNodeType.String })]
        [TestCase("{\"foo\":\"bar\"}", JsonNodeType.OpenObject, JsonNodeType.String, JsonNodeType.NameValueSeparator, JsonNodeType.String, JsonNodeType.CloseObject)]
        [TestCase("[\"foo\",\"bar\"]", JsonNodeType.OpenArray, JsonNodeType.String, JsonNodeType.ItemSeparator, JsonNodeType.String, JsonNodeType.CloseArray)]
        public void CanDecryptCurrentStringValueAndAccessDecryptedNodes(string plainTextJson, params JsonNodeType[] expectedNodeTypes)
        {
            var cipherTextJson = "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plainTextJson)) + "\"";

            var info = new JsonSerializeOperationInfo
            {
                EncryptionMechanism = new Base64EncryptionMechanism(),
                EncryptionEnabled = true
            };

            var reader = new JsonReader(new StringReader(cipherTextJson), info);
            Assert.That(reader.NodeType, Is.EqualTo(JsonNodeType.None));
            
            reader.Read();
            Assert.That(reader.NodeType, Is.EqualTo(JsonNodeType.String));

            reader.DecryptCurrentStringValue();

            foreach (var expectedNodeType in expectedNodeTypes)
            {
                Assert.That(reader.NodeType, Is.EqualTo(expectedNodeType));
                reader.Read();
            }

            Assert.That(reader.NodeType, Is.EqualTo(JsonNodeType.None));
        }
    }
}
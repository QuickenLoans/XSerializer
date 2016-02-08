using System;
using System.Globalization;
using System.Xml.Serialization;
using NUnit.Framework;

namespace XSerializer.Tests
{
    public class RedactTestsForXmlText
    {
        private XmlSerializer<RedactTestClassForXmlText> _serializer;
        private RedactTestClassForXmlText _testClass;

        [SetUp]
        public void Setup()
        {
            _serializer = new XmlSerializer<RedactTestClassForXmlText>();
            _testClass = new RedactTestClassForXmlText();
        }

        [TestCase("abc123", "XXX111")]
        [TestCase("123-45-6789", "111-11-1111")]
        [TestCase("123 Main Street", "111 XXXX XXXXXX")]
        public void StringTextRedactsCorrectly(string value, string expectedAttributeValue)
        {
            _testClass.StringTextProperty = new StringText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<StringTextProperty>{0}</StringTextProperty>", expectedAttributeValue)));
        }

        [Test]
        public void StringAttributeWithNullValueDoesNotSerialize()
        {
            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Is.Not.StringContaining("StringTextProperty"));
        }

        [Test]
        public void RedactedStringAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <StringTextProperty>XXX111</StringTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.StringTextProperty.Value, Is.EqualTo("XXX111"));
        }

        [TestCase(true, "XXXXXX")]
        [TestCase(false, "XXXXXX")]
        public void NullableBoolTextRedactsCorrectly(bool value, string expectedAttributeValue)
        {
            _testClass.NullableBoolTextProperty = new NullableBoolText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<NullableBoolTextProperty>{0}</NullableBoolTextProperty>", expectedAttributeValue)));
        }

        [TestCase(true, "XXXXXX")]
        [TestCase(false, "XXXXXX")]
        public void BoolTextRedactsCorrectly(bool value, string expectedAttributeValue)
        {
            _testClass.BoolTextProperty = new BoolText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<BoolTextProperty>{0}</BoolTextProperty>", expectedAttributeValue)));
        }

        [TestCase('A', "XXXXXX")]
        public void NullableCharTextRedactsCorrectly(char value, string expectedAttributeValue)
        {
            _testClass.NullableCharTextProperty = new NullableCharText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<NullableCharTextProperty>{0}</NullableCharTextProperty>", expectedAttributeValue)));
        }

        [TestCase('A', "XXXXXX")]
        public void CharTextRedactsCorrectly(char value, string expectedAttributeValue)
        {
            _testClass.CharTextProperty = new CharText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<CharTextProperty>{0}</CharTextProperty>", expectedAttributeValue)));
        }


        [Test]
        public void NullableBoolAttributeWithNullValueDoesNotSerialize()
        {
            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Is.Not.StringContaining("NullableBoolTextProperty"));
        }

        [Test]
        public void RedactedNullableBoolAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableBoolTextProperty>XXXXXX</NullableBoolTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableBoolTextProperty.Value, Is.Null);
        }

        [Test]
        public void RedactedBoolAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <BoolTextProperty>XXXXXX</BoolTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.BoolTextProperty.Value, Is.False);
        }

        [Test]
        public void RedactedCharAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <CharTextProperty>XXXXXX</CharTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.CharTextProperty.Value, Is.EqualTo(default(char)));
        }

        [Test]
        public void RedactedNullableCharAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableCharTextProperty>XXXXXX</NullableCharTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableCharTextProperty.Value, Is.Null);
        }

        [TestCase(RedactEnumForXmlText.First, "XXXXXXXXXX")]
        [TestCase(RedactEnumForXmlText.Second, "XXXXXXXXXX")]
        [TestCase(RedactEnumForXmlText.Third, "XXXXXXXXXX")]
        public void NullableEnumTextRedactsCorrectly(RedactEnumForXmlText value, string expectedAttributeValue)
        {
            _testClass.NullableEnumTextProperty = new NullableRedactEnumText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<NullableEnumTextProperty>{0}</NullableEnumTextProperty>", expectedAttributeValue)));
        }

        [TestCase(RedactEnumForXmlText.First, "XXXXXXXXXX")]
        [TestCase(RedactEnumForXmlText.Second, "XXXXXXXXXX")]
        [TestCase(RedactEnumForXmlText.Third, "XXXXXXXXXX")]
        public void EnumTextRedactsCorrectly(RedactEnumForXmlText value, string expectedAttributeValue)
        {
            _testClass.EnumTextProperty = new RedactEnumText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<EnumTextProperty>{0}</EnumTextProperty>", expectedAttributeValue)));
        }

        [Test]
        public void NullableEnumAttributeWithNullValueDoesNotSerialize()
        {
            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Is.Not.StringContaining("NullableEnumTextProperty"));
        }

        [Test]
        public void RedactedNullableEnumAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableEnumTextProperty>XXXXXX</NullableEnumTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableEnumTextProperty.Value, Is.Null);
        }

        [Test]
        public void RedactedEnumAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <EnumTextProperty>XXXXXX</EnumTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.EnumTextProperty.Value, Is.EqualTo(default(RedactEnumForXmlText)));
        }

        [Test]
        public void NullableDateTimeAtrributesWithLocalValuesRedactCorrectly()
        {
            var value = DateTime.Now;

            var expectedOffsetSign =
                TimeZone.CurrentTimeZone.GetUtcOffset(value) < TimeSpan.Zero
                ? "-"
                : "+";

            _testClass.NullableDateTimeTextProperty = new NullableDateTimeText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<NullableDateTimeTextProperty>1111-11-11T11:11:11.1111111{0}11:11</NullableDateTimeTextProperty>", expectedOffsetSign)));
        }

        [Test]
        public void NullableDateTimeAtrributesWithUtcValuesRedactCorrectly()
        {
            var value = DateTime.UtcNow;

            _testClass.NullableDateTimeTextProperty = new NullableDateTimeText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring("<NullableDateTimeTextProperty>1111-11-11T11:11:11.1111111Z</NullableDateTimeTextProperty>"));
        }

        [Test]
        public void DateTimeAtrributesWithLocalValuesRedactCorrectly()
        {
            var value = DateTime.Now;

            var expectedOffsetSign =
                TimeZone.CurrentTimeZone.GetUtcOffset(value) < TimeSpan.Zero
                ? "-"
                : "+";

            _testClass.DateTimeTextProperty = new DateTimeText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<DateTimeTextProperty>1111-11-11T11:11:11.1111111{0}11:11</DateTimeTextProperty>", expectedOffsetSign)));
        }

        [Test]
        public void DateTimeAtrributesWithUtcValuesRedactCorrectly()
        {
            var value = DateTime.UtcNow;

            _testClass.DateTimeTextProperty = new DateTimeText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring("<DateTimeTextProperty>1111-11-11T11:11:11.1111111Z</DateTimeTextProperty>"));
        }

        [Test]
        public void NullableDateTimeAttributeWithNullValueDoesNotSerialize()
        {
            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Is.Not.StringContaining("NullableDateTimeTextProperty"));
        }

        [Test]
        public void RedactedDateTimeAtrributesFromLocalValueDeserializesCorrectly()
        {
            var expectedDateTime =
                DateTime.ParseExact(
                    "1111-11-11T11:11:11.1111111-04:00",
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <DateTimeTextProperty>1111-11-11T11:11:11.1111111-04:00</DateTimeTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.DateTimeTextProperty.Value, Is.EqualTo(expectedDateTime));
        }

        [Test]
        public void RedactedNullableDateTimeAtrributesFromLocalValueDeserializesCorrectly()
        {
            var expectedDateTime =
                DateTime.ParseExact(
                    "1111-11-11T11:11:11.1111111-04:00",
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableDateTimeTextProperty>1111-11-11T11:11:11.1111111-04:00</NullableDateTimeTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableDateTimeTextProperty.Value, Is.EqualTo(expectedDateTime));
        }

        [Test]
        public void RedactedDateTimeAtrributesFromUtcValueDeserializesCorrectly()
        {
            var expectedDateTime =
                DateTime.ParseExact(
                    "1111-11-11T11:11:11.1111111Z",
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <DateTimeTextProperty>1111-11-11T11:11:11.1111111Z</DateTimeTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.DateTimeTextProperty.Value, Is.EqualTo(expectedDateTime));
        }

        [Test]
        public void RedactedNullableDateTimeAtrributesFromUtcValueDeserializesCorrectly()
        {
            var expectedDateTime =
                DateTime.ParseExact(
                    "1111-11-11T11:11:11.1111111Z",
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableDateTimeTextProperty>1111-11-11T11:11:11.1111111Z</NullableDateTimeTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableDateTimeTextProperty.Value, Is.EqualTo(expectedDateTime));
        }

        [TestCase(123, "111")]
        [TestCase(-123, "1111")]
        public void NullableIntTextRedactsCorrectly(int value, string expectedAttributeValue)
        {
            _testClass.NullableIntTextProperty = new NullableIntText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<NullableIntTextProperty>{0}</NullableIntTextProperty>", expectedAttributeValue)));
        }

        [TestCase(123, "111")]
        [TestCase(-123, "1111")]
        public void IntTextRedactsCorrectly(int value, string expectedAttributeValue)
        {
            _testClass.IntTextProperty = new IntText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<IntTextProperty>{0}</IntTextProperty>", expectedAttributeValue)));
        }

        [Test]
        public void NullableIntAttributeWithNullValueDoesNotSerialize()
        {
            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Is.Not.StringContaining("NullableIntTextProperty"));
        }

        [Test]
        public void RedactedNullableIntAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableIntTextProperty>11111</NullableIntTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableIntTextProperty.Value, Is.EqualTo(11111));
        }

        [Test]
        public void RedactedIntAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <IntTextProperty>11111</IntTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.IntTextProperty.Value, Is.EqualTo(11111));
        }

        [TestCase(123, "111")]
        [TestCase(-123, "1111")]
        [TestCase(123.45, "111111")]
        [TestCase(-123.45, "1111111")]
        public void NullableDoubleTextRedactsCorrectly(double value, string expectedAttributeValue)
        {
            _testClass.NullableDoubleTextProperty = new NullableDoubleText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<NullableDoubleTextProperty>{0}</NullableDoubleTextProperty>", expectedAttributeValue)));
        }

        [TestCase(123, "111")]
        [TestCase(-123, "1111")]
        [TestCase(123.45, "111111")]
        [TestCase(-123.45, "1111111")]
        public void DoubleTextRedactsCorrectly(double value, string expectedAttributeValue)
        {
            _testClass.DoubleTextProperty = new DoubleText { Value = value };

            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Contains.Substring(string.Format("<DoubleTextProperty>{0}</DoubleTextProperty>", expectedAttributeValue)));
        }

        [Test]
        public void NullableDoubleAttributeWithNullValueDoesNotSerialize()
        {
            var xml = Serialize();
            Console.WriteLine(xml);

            Assert.That(xml, Is.Not.StringContaining("NullableDoubleTextProperty"));
        }

        [Test]
        public void RedactedNullableDoubleAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <NullableDoubleTextProperty>11111</NullableDoubleTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.NullableDoubleTextProperty.Value, Is.EqualTo(11111));
        }

        [Test]
        public void RedactedDoubleAttributeDeserializesCorrectly()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RedactTestClassForXmlText xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <DoubleTextProperty>11111</DoubleTextProperty>
</RedactTestClassForXmlText>";

            var instance = _serializer.Deserialize(xml);

            Assert.That(instance.DoubleTextProperty.Value, Is.EqualTo(11111));
        }

        [Test]
        public void RedactingDoesNotHappenWhenDisabled()
        {
            var instance = new RedactTestClassForXmlText
            {
                BoolTextProperty = new BoolText { Value = true },
                DateTimeTextProperty = new DateTimeText { Value = new DateTime(2222, 2, 22, 22, 22, 22, 222, DateTimeKind.Utc) },
                DoubleTextProperty = new DoubleText { Value = 123.45 },
                EnumTextProperty = new RedactEnumText { Value = RedactEnumForXmlText.Second },
                IntTextProperty = new IntText { Value = 12345 },
                NullableBoolTextProperty = new NullableBoolText { Value = false },
                NullableDateTimeTextProperty = new NullableDateTimeText { Value = new DateTime(3333, 3, 30, 3, 33, 33, 333, DateTimeKind.Utc) },
                NullableDoubleTextProperty = new NullableDoubleText { Value = 987.65 },
                NullableEnumTextProperty = new NullableRedactEnumText { Value = RedactEnumForXmlText.Third },
                NullableIntTextProperty = new NullableIntText { Value = 98765 },
                StringTextProperty = new StringText { Value = "abc123" }
            };

            var serializer = new XmlSerializer<RedactTestClassForXmlText>(options => options.DisableRedact());

            var xml = serializer.Serialize(instance);

            Assert.That(xml, Contains.Substring("true"));
            Assert.That(xml, Contains.Substring("2222-02-22T22:22:22.2220000Z"));
            Assert.That(xml, Contains.Substring("123.45"));
            Assert.That(xml, Contains.Substring("Second"));
            Assert.That(xml, Contains.Substring("12345"));
            Assert.That(xml, Contains.Substring("false"));
            Assert.That(xml, Contains.Substring("3333-03-30T03:33:33.3330000Z"));
            Assert.That(xml, Contains.Substring("987.65"));
            Assert.That(xml, Contains.Substring("Third"));
            Assert.That(xml, Contains.Substring("98765"));
            Assert.That(xml, Contains.Substring("abc123"));
        }

        private string Serialize()
        {
            return _serializer.Serialize(_testClass);
        }

        public class RedactTestClassForXmlText
        {
            public StringText StringTextProperty { get; set; }
            public NullableBoolText NullableBoolTextProperty { get; set; }
            public NullableRedactEnumText NullableEnumTextProperty { get; set; }
            public NullableDateTimeText NullableDateTimeTextProperty { get; set; }
            public NullableIntText NullableIntTextProperty { get; set; }
            public NullableDoubleText NullableDoubleTextProperty { get; set; }
            public BoolText BoolTextProperty { get; set; }
            public RedactEnumText EnumTextProperty { get; set; }
            public DateTimeText DateTimeTextProperty { get; set; }
            public IntText IntTextProperty { get; set; }
            public DoubleText DoubleTextProperty { get; set; }
            public CharText CharTextProperty { get; set; }
            public NullableCharText NullableCharTextProperty { get; set; }
        }

        public class StringText
        {
            [Redact]
            [XmlText]
            public string Value { get; set; }
        }

        public class NullableBoolText
        {
            [Redact]
            [XmlText]
            public bool? Value { get; set; }
        }

        public class NullableRedactEnumText
        {
            [Redact]
            [XmlText]
            public RedactEnumForXmlText? Value { get; set; }
        }

        public class NullableDateTimeText
        {
            [Redact]
            [XmlText]
            public DateTime? Value { get; set; }
        }

        public class NullableIntText
        {
            [Redact]
            [XmlText]
            public int? Value { get; set; }
        }

        public class NullableDoubleText
        {
            [Redact]
            [XmlText]
            public double? Value { get; set; }
        }

        public class BoolText
        {
            [Redact]
            [XmlText]
            public bool Value { get; set; }
        }

        public class CharText
        {
            [Redact]
            [XmlText]
            public char Value { get; set; }
        }

        public class NullableCharText
        {
            [Redact]
            [XmlText]
            public char? Value { get; set; }
        }

        public class RedactEnumText
        {
            [Redact]
            [XmlText]
            public RedactEnumForXmlText Value { get; set; }
        }

        public class DateTimeText
        {
            [Redact]
            [XmlText]
            public DateTime Value { get; set; }
        }

        public class IntText
        {
            [Redact]
            [XmlText]
            public int Value { get; set; }
        }

        public class DoubleText
        {
            [Redact]
            [XmlText]
            public double Value { get; set; }
        }

        public enum RedactEnumForXmlText
        {
            First,
            Second,
            Third
        }
    }
}
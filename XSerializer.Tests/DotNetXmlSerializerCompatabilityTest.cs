namespace XSerializer.Tests
{
    using System.Xml.Linq;

    using NUnit.Framework;

    public class TestDotNetXmlSerializerCompatability
    {
        [Test]
        public void CanDeserializeXmlFromDotNetXmlSerializer()
        {
            XDocument xml = new XDocument();
            var dotNetXmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TestObject));
            using (var writer = xml.CreateWriter())
            {
                dotNetXmlSerializer.Serialize(writer, this.testObject);
            }

            var xmlString = xml.ToString();
            var xSerializer = new XmlSerializer<TestObject>(new XmlSerializationOptions(dotNetFrameworkSerializerCompatable: true));

            var actual = xSerializer.Deserialize(xmlString);

            Assert.That(actual.Bool, Is.EqualTo(this.testObject.Bool));
            Assert.That(actual.Char, Is.EqualTo(this.testObject.Char));
            Assert.That(actual.Double, Is.EqualTo(this.testObject.Double));
            Assert.That(actual.Int, Is.EqualTo(this.testObject.Int));
            Assert.That(actual.String, Is.EqualTo(this.testObject.String));
        }

        [Test]
        public void DotNetXmlSerializerCanDeserializeXml()
        {
            var xSerializer = new XmlSerializer<TestObject>(new XmlSerializationOptions(dotNetFrameworkSerializerCompatable: true));
            var xmlString = xSerializer.Serialize(this.testObject);

            XDocument xml = XDocument.Parse(xmlString);
            var dotNetXmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TestObject));
            TestObject actual;
            using (var reader = xml.CreateReader())
            {
                actual = (TestObject)dotNetXmlSerializer.Deserialize(reader);
            }

            Assert.That(actual.Bool, Is.EqualTo(this.testObject.Bool));
            Assert.That(actual.Char, Is.EqualTo(this.testObject.Char));
            Assert.That(actual.Double, Is.EqualTo(this.testObject.Double));
            Assert.That(actual.Int, Is.EqualTo(this.testObject.Int));
            Assert.That(actual.String, Is.EqualTo(this.testObject.String));
        }

        public TestObject testObject = new TestObject()
                                           {
                                               Bool = true,
                                               Char = 'A',
                                               Double = 1.2,
                                               Int = 3,
                                               String = "string"
                                           };

        public class TestObject
        {
            public bool Bool { get; set; }

            public int Int { get; set; }

            public double Double { get; set; }

            public char Char { get; set; }

            public string String { get; set; }
        }
    }
}
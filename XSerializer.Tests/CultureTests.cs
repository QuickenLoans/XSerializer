using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace XSerializer.Tests
{
    public class CultureTests
    {
        [Test]
        public void XmlTest()
        {
            var foo = new Foo()
            {
                DecimalValue         = 1.1m,
                FloatValue           = 2.1f,
                DoubleValue          = 3.1d,
                NullableDecimalValue = 1.2m,
                NullableFloatValue   = 2.2f,
                NullableDoubleValue  = 3.3d

            };

            var serializer = new XmlSerializer<Foo>();

            var xml = serializer.Serialize(foo);
            Assert.False(xml.Contains(","));

            var d = serializer.Deserialize(xml);

            Assert.AreEqual(foo.DecimalValue,         d.DecimalValue);
            Assert.AreEqual(foo.FloatValue,           d.FloatValue);
            Assert.AreEqual(foo.DoubleValue,          d.DoubleValue);
            Assert.AreEqual(foo.NullableDecimalValue, d.NullableDecimalValue);
            Assert.AreEqual(foo.NullableFloatValue,   d.NullableFloatValue);
            Assert.AreEqual(foo.NullableDoubleValue,  d.NullableDoubleValue);
        }

        [Test]
        public void XmlTest2()
        {
            var ci =  Thread.CurrentThread.CurrentCulture;
            var uci = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentCulture =
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru");

                XmlTest();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture   = ci;
                Thread.CurrentThread.CurrentUICulture = uci;

            }
        }

        [Test]
        public void XmlTest3()
        {
            var foo = new Foo()
            {
                DecimalValue         = 1.1m,
                FloatValue           = 2.1f,
                DoubleValue          = 3.1d,
                NullableDecimalValue = 1.2m,
                NullableFloatValue   = 2.2f,
                NullableDoubleValue  = 3.3d

            };

            var serializer = new XmlSerializer<Foo>(o => { o.Culture = CultureInfo.GetCultureInfo("ru"); });

            var xml = serializer.Serialize(foo);
            Assert.True(xml.Contains(","));

            var d = serializer.Deserialize(xml);

            Assert.AreEqual(foo.DecimalValue,         d.DecimalValue);
            Assert.AreEqual(foo.FloatValue,           d.FloatValue);
            Assert.AreEqual(foo.DoubleValue,          d.DoubleValue);
            Assert.AreEqual(foo.NullableDecimalValue, d.NullableDecimalValue);
            Assert.AreEqual(foo.NullableFloatValue,   d.NullableFloatValue);
            Assert.AreEqual(foo.NullableDoubleValue,  d.NullableDoubleValue);
        }

        [Test]
        public void JsonTest()
        {
            var foo = new Foo()
            {
                DecimalValue         = 1.1m,
                FloatValue           = 2.1f,
                DoubleValue          = 3.1d,
                NullableDecimalValue = 1.2m,
                NullableFloatValue   = 2.2f,
                NullableDoubleValue  = 3.3d

            };

            var serializer = new JsonSerializer<Foo>();

            var json = serializer.Serialize(foo);
            Assert.False(json.Contains("1,1"));
            Assert.False(json.Contains("2,1"));
            Assert.False(json.Contains("3,1"));
            Assert.False(json.Contains("1,2"));
            Assert.False(json.Contains("2,2"));
            Assert.False(json.Contains("3,3"));

            var d = serializer.Deserialize(json);

            Assert.AreEqual(foo.DecimalValue,         d.DecimalValue);
            Assert.AreEqual(foo.FloatValue,           d.FloatValue);
            Assert.AreEqual(foo.DoubleValue,          d.DoubleValue);
            Assert.AreEqual(foo.NullableDecimalValue, d.NullableDecimalValue);
            Assert.AreEqual(foo.NullableFloatValue,   d.NullableFloatValue);
            Assert.AreEqual(foo.NullableDoubleValue,  d.NullableDoubleValue);
        }

        public class Foo
        {
            public decimal  DecimalValue         { get; set; }
            public float    FloatValue           { get; set; }
            public double   DoubleValue          { get; set; }

            public decimal? NullableDecimalValue { get; set; }
            public float?   NullableFloatValue   { get; set; }
            public double?  NullableDoubleValue  { get; set; }
        }
    }
}

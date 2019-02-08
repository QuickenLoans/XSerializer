using System;
using NUnit.Framework;

namespace XSerializer.Tests
{
    public class DefaultDateTimeHandlerTests
    {
        [TestCase("", 0, TestName = "No Fractional Second Digits")]
        [TestCase(".1", 1000000, TestName = "One Fractional Second Digits")]
        [TestCase(".12", 1200000, TestName = "Two Fractional Second Digits")]
        [TestCase(".123", 1230000, TestName = "Three Fractional Second Digits")]
        [TestCase(".1234", 1234000, TestName = "Four Fractional Second Digits")]
        [TestCase(".12345", 1234500, TestName = "Five Fractional Second Digits")]
        [TestCase(".123456", 1234560, TestName = "Six Fractional Second Digits")]
        [TestCase(".1234567", 1234567, TestName = "Seven Fractional Second Digits")]
        public void CanParseDateTimeWithLocalKind(string fractionalPart, int expectedAdditionalTicks)
        {
            var handler = new DefaultDateTimeHandler();

            var s = string.Format("2016-02-24T02:29:33{0}-05:00", fractionalPart);
            var value = handler.ParseDateTime(s);

            var expectedValue = new DateTime(2016, 2, 24, 7, 29, 33, DateTimeKind.Utc)
                .ToLocalTime()
                .AddTicks(expectedAdditionalTicks);

            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(value.Kind, Is.EqualTo(DateTimeKind.Local));
        }

        [TestCase("", 0, TestName = "No Fractional Second Digits")]
        [TestCase(".1", 1000000, TestName = "One Fractional Second Digits")]
        [TestCase(".12", 1200000, TestName = "Two Fractional Second Digits")]
        [TestCase(".123", 1230000, TestName = "Three Fractional Second Digits")]
        [TestCase(".1234", 1234000, TestName = "Four Fractional Second Digits")]
        [TestCase(".12345", 1234500, TestName = "Five Fractional Second Digits")]
        [TestCase(".123456", 1234560, TestName = "Six Fractional Second Digits")]
        [TestCase(".1234567", 1234567, TestName = "Seven Fractional Second Digits")]
        public void CanParseDateTimeWithUtcKind(string fractionalPart, int expectedAdditionalTicks)
        {
            var handler = new DefaultDateTimeHandler();

            var s = string.Format("2016-02-24T02:29:33{0}Z", fractionalPart);
            var value = handler.ParseDateTime(s);

            var expectedValue = new DateTime(2016, 2, 24, 2, 29, 33, DateTimeKind.Utc).AddTicks(expectedAdditionalTicks);

            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(value.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [TestCase("", 0, TestName = "No Fractional Second Digits")]
        [TestCase(".1", 1000000, TestName = "One Fractional Second Digits")]
        [TestCase(".12", 1200000, TestName = "Two Fractional Second Digits")]
        [TestCase(".123", 1230000, TestName = "Three Fractional Second Digits")]
        [TestCase(".1234", 1234000, TestName = "Four Fractional Second Digits")]
        [TestCase(".12345", 1234500, TestName = "Five Fractional Second Digits")]
        [TestCase(".123456", 1234560, TestName = "Six Fractional Second Digits")]
        [TestCase(".1234567", 1234567, TestName = "Seven Fractional Second Digits")]
        public void CanParseDateTimeWithUnspecifiedKind(string fractionalPart, int expectedAdditionalTicks)
        {
            var handler = new DefaultDateTimeHandler();

            var s = string.Format("2016-02-24T02:29:33{0}", fractionalPart);
            var value = handler.ParseDateTime(s);

            var expectedValue = new DateTime(2016, 2, 24, 2, 29, 33, DateTimeKind.Unspecified).AddTicks(expectedAdditionalTicks);

            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(value.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        }

        [TestCase("", 0, TestName = "No Fractional Second Digits")]
        [TestCase(".1", 1000000, TestName = "One Fractional Second Digits")]
        [TestCase(".12", 1200000, TestName = "Two Fractional Second Digits")]
        [TestCase(".123", 1230000, TestName = "Three Fractional Second Digits")]
        [TestCase(".1234", 1234000, TestName = "Four Fractional Second Digits")]
        [TestCase(".12345", 1234500, TestName = "Five Fractional Second Digits")]
        [TestCase(".123456", 1234560, TestName = "Six Fractional Second Digits")]
        [TestCase(".1234567", 1234567, TestName = "Seven Fractional Second Digits")]
        public void CanParseDateTimeOffsetWithOffsetSpecified(string fractionalPart, int expectedAdditionalTicks)
        {
            var handler = new DefaultDateTimeHandler();

            var s = string.Format("2016-02-24T02:29:33{0}-05:00", fractionalPart);
            var value = handler.ParseDateTimeOffset(s);

            var expectedValue = new DateTimeOffset(2016, 2, 24, 2, 29, 33, TimeSpan.FromHours(-5)).AddTicks(expectedAdditionalTicks);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [TestCase("", 0, TestName = "No Fractional Second Digits")]
        [TestCase(".1", 1000000, TestName = "One Fractional Second Digits")]
        [TestCase(".12", 1200000, TestName = "Two Fractional Second Digits")]
        [TestCase(".123", 1230000, TestName = "Three Fractional Second Digits")]
        [TestCase(".1234", 1234000, TestName = "Four Fractional Second Digits")]
        [TestCase(".12345", 1234500, TestName = "Five Fractional Second Digits")]
        [TestCase(".123456", 1234560, TestName = "Six Fractional Second Digits")]
        [TestCase(".1234567", 1234567, TestName = "Seven Fractional Second Digits")]
        public void CanParseDateTimeOffsetWithZuluTimeSpecified(string fractionalPart, int expectedAdditionalTicks)
        {
            var handler = new DefaultDateTimeHandler();

            var s = string.Format("2016-02-24T02:29:33{0}Z", fractionalPart);
            var value = handler.ParseDateTimeOffset(s);

            var expectedValue = new DateTimeOffset(2016, 2, 24, 2, 29, 33, TimeSpan.FromHours(0)).AddTicks(expectedAdditionalTicks);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [TestCase("", 0, TestName = "No Fractional Second Digits")]
        [TestCase(".1", 1000000, TestName = "One Fractional Second Digits")]
        [TestCase(".12", 1200000, TestName = "Two Fractional Second Digits")]
        [TestCase(".123", 1230000, TestName = "Three Fractional Second Digits")]
        [TestCase(".1234", 1234000, TestName = "Four Fractional Second Digits")]
        [TestCase(".12345", 1234500, TestName = "Five Fractional Second Digits")]
        [TestCase(".123456", 1234560, TestName = "Six Fractional Second Digits")]
        [TestCase(".1234567", 1234567, TestName = "Seven Fractional Second Digits")]
        public void CanParseDateTimeOffsetWithNoOffsetSpecified(string fractionalPart, int expectedAdditionalTicks)
        {
            var handler = new DefaultDateTimeHandler();

            var s = string.Format("2016-02-24T02:29:33{0}", fractionalPart);
            var value = handler.ParseDateTimeOffset(s);

            var utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            var expectedValue = new DateTimeOffset(2016, 2, 24, 2, 29, 33, utcOffset).AddTicks(expectedAdditionalTicks);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [TestCase("2016-02-24T02:29", TestName = "No Seconds Specified")]
        [TestCase("2016-02-24T02", TestName = "No Minutes Specified")]
        [TestCase("2016-02-24", TestName = "No Hours Specified")]
        [TestCase("2016-02", TestName = "No Day Specified")]
        [TestCase("2016", TestName = "No Month Specified")]
        [TestCase("", TestName = "No Year Specified")]
        [TestCase("2016/02-24T02:29:33.1234567-05:00", TestName = "Wrong Year/Month Separator")]
        [TestCase("2016-02/24T02:29:33.1234567-05:00", TestName = "Wrong Month/Day Separator")]
        [TestCase("2016-02-24 02:29:33.1234567-05:00", TestName = "Wrong Date/Time Separator")]
        [TestCase("2016-02-24T02.29:33.1234567-05:00", TestName = "Wrong Hour/Minute Separator")]
        [TestCase("2016-02-24T02:29.33.1234567-05:00", TestName = "Wrong Minute/Second Separator")]
        [TestCase("2016-02-24T02:29:33.1234567-05.00", TestName = "Wrong Offset Separator")]
        [TestCase("X016-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 1")]
        [TestCase("2X16-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 2")]
        [TestCase("20X6-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 3")]
        [TestCase("201X-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 4")]
        [TestCase("2016-X2-24T02:29:33.1234567-05:00", TestName = "Invalid Month 1")]
        [TestCase("2016-0X-24T02:29:33.1234567-05:00", TestName = "Invalid Month 2")]
        [TestCase("2016-02-X4T02:29:33.1234567-05:00", TestName = "Invalid Day 2")]
        [TestCase("2016-02-2XT02:29:33.1234567-05:00", TestName = "Invalid Day 1")]
        [TestCase("2016-02-24TX2:29:33.1234567-05:00", TestName = "Invalid Hour 2")]
        [TestCase("2016-02-24T0X:29:33.1234567-05:00", TestName = "Invalid Hour 1")]
        [TestCase("2016-02-24T02:X9:33.1234567-05:00", TestName = "Invalid Minute 2")]
        [TestCase("2016-02-24T02:2X:33.1234567-05:00", TestName = "Invalid Minute 1")]
        [TestCase("2016-02-24T02:29:X3.1234567-05:00", TestName = "Invalid Second 2")]
        [TestCase("2016-02-24T02:29:3X.1234567-05:00", TestName = "Invalid Second 1")]
        [TestCase("2016-02-24T02:29:3X.X234567-05:00", TestName = "Invalid Fractional 1")]
        [TestCase("2016-02-24T02:29:3X.1X34567-05:00", TestName = "Invalid Fractional 2")]
        [TestCase("2016-02-24T02:29:3X.12X4567-05:00", TestName = "Invalid Fractional 3")]
        [TestCase("2016-02-24T02:29:3X.123X567-05:00", TestName = "Invalid Fractional 4")]
        [TestCase("2016-02-24T02:29:3X.1234X67-05:00", TestName = "Invalid Fractional 5")]
        [TestCase("2016-02-24T02:29:3X.12345X7-05:00", TestName = "Invalid Fractional 6")]
        [TestCase("2016-02-24T02:29:3X.123456X-05:00", TestName = "Invalid Fractional 7")]
        [TestCase("2016-02-24T02:29:3X.1234567X", TestName = "Invalid Offset 1")]
        [TestCase("2016-02-24T02:29:3X.1234567-X5:00", TestName = "Invalid Offset 2")]
        [TestCase("2016-02-24T02:29:3X.1234567-0X:00", TestName = "Invalid Offset 3")]
        [TestCase("2016-02-24T02:29:3X.1234567-05:X0", TestName = "Invalid Offset 4")]
        [TestCase("2016-02-24T02:29:3X.1234567-05:0X", TestName = "Invalid Offset 5")]
        public void CannotParseDateTime(string s)
        {
            var handler = new DefaultDateTimeHandler();

            TestDelegate f = () =>
            {
                try
                {
                    handler.ParseDateTime(s);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            };

            Assert.That(f, Throws.InstanceOf<FormatException>());
        }

        [TestCase("2016-02-24T02:29", TestName = "No Seconds Specified")]
        [TestCase("2016-02-24T02", TestName = "No Minutes Specified")]
        [TestCase("2016-02-24", TestName = "No Hours Specified")]
        [TestCase("2016-02", TestName = "No Day Specified")]
        [TestCase("2016", TestName = "No Month Specified")]
        [TestCase("", TestName = "No Year Specified")]
        [TestCase("2016/02-24T02:29:33.1234567-05:00", TestName = "Wrong Year/Month Separator")]
        [TestCase("2016-02/24T02:29:33.1234567-05:00", TestName = "Wrong Month/Day Separator")]
        [TestCase("2016-02-24 02:29:33.1234567-05:00", TestName = "Wrong Date/Time Separator")]
        [TestCase("2016-02-24T02.29:33.1234567-05:00", TestName = "Wrong Hour/Minute Separator")]
        [TestCase("2016-02-24T02:29.33.1234567-05:00", TestName = "Wrong Minute/Second Separator")]
        [TestCase("2016-02-24T02:29:33.1234567-05.00", TestName = "Wrong Offset Separator")]
        [TestCase("X016-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 1")]
        [TestCase("2X16-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 2")]
        [TestCase("20X6-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 3")]
        [TestCase("201X-02-24T02:29:33.1234567-05:00", TestName = "Invalid Year 4")]
        [TestCase("2016-X2-24T02:29:33.1234567-05:00", TestName = "Invalid Month 1")]
        [TestCase("2016-0X-24T02:29:33.1234567-05:00", TestName = "Invalid Month 2")]
        [TestCase("2016-02-X4T02:29:33.1234567-05:00", TestName = "Invalid Day 2")]
        [TestCase("2016-02-2XT02:29:33.1234567-05:00", TestName = "Invalid Day 1")]
        [TestCase("2016-02-24TX2:29:33.1234567-05:00", TestName = "Invalid Hour 2")]
        [TestCase("2016-02-24T0X:29:33.1234567-05:00", TestName = "Invalid Hour 1")]
        [TestCase("2016-02-24T02:X9:33.1234567-05:00", TestName = "Invalid Minute 2")]
        [TestCase("2016-02-24T02:2X:33.1234567-05:00", TestName = "Invalid Minute 1")]
        [TestCase("2016-02-24T02:29:X3.1234567-05:00", TestName = "Invalid Second 2")]
        [TestCase("2016-02-24T02:29:3X.1234567-05:00", TestName = "Invalid Second 1")]
        [TestCase("2016-02-24T02:29:3X.X234567-05:00", TestName = "Invalid Fractional 1")]
        [TestCase("2016-02-24T02:29:3X.1X34567-05:00", TestName = "Invalid Fractional 2")]
        [TestCase("2016-02-24T02:29:3X.12X4567-05:00", TestName = "Invalid Fractional 3")]
        [TestCase("2016-02-24T02:29:3X.123X567-05:00", TestName = "Invalid Fractional 4")]
        [TestCase("2016-02-24T02:29:3X.1234X67-05:00", TestName = "Invalid Fractional 5")]
        [TestCase("2016-02-24T02:29:3X.12345X7-05:00", TestName = "Invalid Fractional 6")]
        [TestCase("2016-02-24T02:29:3X.123456X-05:00", TestName = "Invalid Fractional 7")]
        [TestCase("2016-02-24T02:29:3X.1234567X", TestName = "Invalid Offset 1")]
        [TestCase("2016-02-24T02:29:3X.1234567-X5:00", TestName = "Invalid Offset 2")]
        [TestCase("2016-02-24T02:29:3X.1234567-0X:00", TestName = "Invalid Offset 3")]
        [TestCase("2016-02-24T02:29:3X.1234567-05:X0", TestName = "Invalid Offset 4")]
        [TestCase("2016-02-24T02:29:3X.1234567-05:0X", TestName = "Invalid Offset 5")]
        public void CannotParseDateOffsetTime(string s)
        {
            var handler = new DefaultDateTimeHandler();

            TestDelegate f = () =>
            {
                try
                {
                    handler.ParseDateTimeOffset(s);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            };

            Assert.That(f, Throws.InstanceOf<FormatException>());
        }
    }
}

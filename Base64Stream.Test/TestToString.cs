using System;
using System.IO;
using NUnit.Framework;
using Base64Stream;

namespace Base64Stream.Test
{
    [TestFixture]
    public class TestToString
    {
        [SetUp] public void Setup() { }

        [Test, Order(1)]
        public void ToStringViaStringBuilder()
        {
            //This internal method is only used when the content length cannot be determined.
            //No argument checking is performed within this internal method

            var ms = new MemoryStream(TestData.TestBytes);
            string actual;
            int blockSize = 1024 + 257;

            ms.Position = 0;
            actual = Base64.ToBase64StringStringBuilder(ms, false, blockSize);
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Convert binary stream to base64 string via StringBuilder.");

            ms.Position = 0;
            actual = Base64.ToBase64StringStringBuilder(ms, true, blockSize);
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual, 
                "Convert binary stream to formatted base64 string via StringBuilder.");

            //ms.Position = 0; do not reset pointer...
            actual = Base64.ToBase64StringStringBuilder(ms, false, blockSize);
            Assert.AreEqual(string.Empty, actual,
                "Convert binary (exhausted) stream to base64 string via StringBuilder.");
        }

        [Test, Order(2)]
        public void ToStringViaUnsafePointersTest()
        {
            //This internal method is used when the content is explicitly known.
            //No argument checking is performed within this internal method

            var ms = new MemoryStream(TestData.TestBytes);
            string actual;
            int blockSize = 1024 + 257;

            ms.Position = 0;
            actual = Base64.ToBase64StringUnsafe(ms, (int)ms.Length, false, blockSize);
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Convert binary stream to base64 string via pointers.");

            ms.Position = 0;
            actual = Base64.ToBase64StringUnsafe(ms, (int)ms.Length, true, blockSize);
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 string via pointers.");

            //ms.Position = 0; do not reset pointer...
            actual = Base64.ToBase64StringUnsafe(ms, (int)ms.Length, false, blockSize);
            Assert.AreEqual(string.Empty, actual,
                "Convert binary (exhausted) stream to base64 string via pointers.");

            ms.Position = 0;
            actual = Base64.ToBase64StringUnsafe(ms, (int)ms.Length + 100, true, blockSize);
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 string via pointers with specified content length > actual length.");
        }

        [Test, Order(3)]
        public void TopLevelToStringTest()
        {
            //This is the public method that performs all the validation and directs which of the above internal methods is to be used.

            var ms = new MemoryStream(TestData.TestBytes);

            ms.Position = 0;
            string actual = Base64.ToString(ms, -1, true, 1024);
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual, 
                "Unformatted string result. Uses unsafe string code because content length is retrieved from stream.");

            ms.Position = 0;
            actual = Base64.ToString(ms, -1, false, 1024);
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Formatted string result. Uses unsafe string code because content length is retrieved from stream.");

            ms.Position = 0;
            actual = Base64.ToString(new NonSeekableStream(ms), -1, false, 1024);
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Unformatted string result. Forces ToString to use StringBuilder code because it cannot determine the content length.");

            ms.Position = 0;
            actual = Base64.ToString(new NonSeekableStream(ms), (int)ms.Length + 100, false, 1024);
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Unformatted string result. Uses unsafe string code because content length is to large causing allocated string to be trimmed.");

            //Assert.Catch<ArgumentOutOfRangeException>(
            //    () => Base64.ToBase64StringUnsafe(ms, (int)ms.Length + 100, false, 1024),
            //    "Exception will occur only when stream length cannot be determined and the specified contentLength exceeds the the actual stream content.");
        }
    }
}

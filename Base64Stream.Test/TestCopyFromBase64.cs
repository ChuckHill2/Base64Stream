using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Base64Stream;
using System.IO;

namespace Base64Stream.Test
{
    [TestFixture]
    public class TestCopyFromBase64
    {
        private const int blockSize = 1024;
        //int blockSize = 1024 + 257;

        [SetUp] public void Setup() { }

        [Test, Order(1)]
        public void Base64ToEndOfStream()
        {
            var dst = new MemoryStream();
            var src = new StringReader(TestData.TestBase64String);
            Base64.CopyFrom(src, -1, dst, blockSize);
            Assert.IsTrue(TestData.TestBytes.SequenceEqual(dst.ToArray()));

            dst = new MemoryStream();
            Base64.CopyFrom(src, -1, dst, blockSize);
            Assert.IsTrue(dst.ToArray().Length == 0, "Exhausted Text stream.");
        }

        [Test, Order(2)]
        public void ValidBase64Length()
        {
            int srcLen = TestData.TestBase64String.Length;
            var dst = new MemoryStream();
            var src = new StringReader(TestData.TestBase64String + "12345678");
            Base64.CopyFrom(src, srcLen, dst, blockSize);
            Assert.IsTrue(TestData.TestBytes.SequenceEqual(dst.ToArray()));
            Assert.AreEqual((char)src.Read(), '1', "Verify text stream pointer.");
        }

        [Test, Order(3)]
        public void InvalidBase64Length()
        {
            int srcLen = 10;
            var dst = new MemoryStream();
            var src = new StringReader(TestData.TestBase64String);
            Assert.Catch<System.FormatException>(() => Base64.CopyFrom(src, srcLen, dst, blockSize));
        }

        [Test, Order(4)]
        public void ValidBase64ZeroLength()
        {
            var dst = new MemoryStream();
            var src = new StringReader(TestData.TestBase64String);
            Base64.CopyFrom(src, 0, dst, blockSize);
            Assert.IsTrue(dst.ToArray().Length == 0);
        }

        [Test, Order(5)]
        public void Base64WithLinebreaksToEndOfStream()
        {
            var dst = new MemoryStream();
            var src = new StringReader(TestData.TestBase64StringWithLineBreaks);
            Base64.CopyFrom(src, -1, dst, blockSize);
            Assert.IsTrue(TestData.TestBytes.SequenceEqual(dst.ToArray()));
        }

        [Test, Order(6)]
        public void Base64ToInvalidChar()
        {
            var dst = new MemoryStream();
            var src = new StringReader(TestData.TestBase64String + "<EndOfBase64String>");
            Base64.CopyFrom(src, -1, dst, blockSize);
            Assert.IsTrue(TestData.TestBytes.SequenceEqual(dst.ToArray()));

            dst = new MemoryStream();
            Base64.CopyFrom(src, -1, dst, blockSize);
            Assert.IsTrue(dst.ToArray().Length == 0, "Exhausted Text stream.");

            Assert.AreEqual((char)src.Read(), '<', "Verify text stream pointer.");
        }
    }
}

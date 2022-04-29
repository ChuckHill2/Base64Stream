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
    public class TestCopyToBase64
    {
        [SetUp] public void Setup() { }

        [Test, Order(1)]
        public void BinStreamCopyToBase64BinStream()
        {
            var src = new MemoryStream(TestData.TestBytes);
            var dst = new MemoryStream();
            string actual;
            int blockSize = 1024;
            //int blockSize = 1024 + 257;

            src.Position = 0;
            dst.SetLength(0);
            Base64.CopyTo(src, (int)src.Length, dst, false, blockSize);
            dst.Position = 0;
            actual = Encoding.ASCII.GetString(dst.ToArray());
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Convert binary stream to base64 stream with content length specified.");

            src.Position = 0;
            dst.SetLength(0);
            Base64.CopyTo(src, -1, dst, true, blockSize);
            dst.Position = 0;
            actual = Encoding.ASCII.GetString(dst.ToArray());
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 stream, with content length NOT specified.");

            src.Position = 0;
            dst.SetLength(0);
            Base64.CopyTo(new NonSeekableStream(src), -1, dst, true, blockSize);
            dst.Position = 0;
            actual = Encoding.ASCII.GetString(dst.ToArray());
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 stream, with content length indeterminate.");

            src.Position = 0;
            dst.SetLength(0);
            Base64.CopyTo(src, (int)src.Length + 100, dst, true, blockSize);
            dst.Position = 0;
            actual = Encoding.ASCII.GetString(dst.ToArray());
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 stream, with content length too large.");
        }

        [Test, Order(2)]
        public void BinStreamCopyToTextStream()
        {
            //public static void CopyTo(Stream stream, int contentLength, Stream base64Stream, bool insertBreakLines = false, int blockSize = 1024 * 1024)
            //public static void CopyTo(Stream stream, int contentLength, TextWriter base64Stream, bool insertBreakLines = false, int blockSize = 1024 * 1024)

            var src = new MemoryStream(TestData.TestBytes);
            StringWriter dst;

            // === Alternate Method (more filestream-like) ===
            // var msdst = new MemoryStream();
            // var dst = new StreamWriter(msdst);
            // msdst.Position = 0;
            // actual = new StreamReader(msdst).ReadToEnd();

            string actual;
            int blockSize = 1024;
            //int blockSize = 1024 + 257;

            src.Position = 0;
            dst = new StringWriter();
            Base64.CopyTo(src, (int)src.Length, dst, false, blockSize);
            actual = dst.ToString();
            Assert.AreEqual(TestData.TestBase64String, actual,
                "Convert binary stream to base64 stream with content length specified.");

            src.Position = 0;
            dst = new StringWriter();
            Base64.CopyTo(src, -1, dst, true, blockSize);
            actual = dst.ToString();
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 stream, with content length NOT specified.");

            src.Position = 0;
            dst = new StringWriter();
            Base64.CopyTo(new NonSeekableStream(src), -1, dst, true, blockSize);
            actual = dst.ToString();
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 stream, with content length indeterminate.");

            src.Position = 0;
            dst = new StringWriter();
            Base64.CopyTo(src, (int)src.Length + 100, dst, true, blockSize);
            actual = dst.ToString();
            Assert.AreEqual(TestData.TestBase64StringWithLineBreaks, actual,
                "Convert binary stream to formatted base64 stream, with content length too large.");
        }
    }
}

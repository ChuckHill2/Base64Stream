using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Base64Stream
{
    /// <summary>
    /// Efficient conversion of base64 data to and from streams via intermediate serially chunked buffers.
    /// </summary>
    public static class Base64
    {
        // 76 is the magic line length for SMTP base64 Content-Transfer-Encoding
        // See enum System.Base64FormattingOptions.InsertLineBreaks 
        private const int base64LineBreakPosition = 76;

        /// <summary>
        /// Read stream from current position in chunks and write content into base64 string.
        /// </summary>
        /// <param name="stream">Binary stream to read.</param>
        /// <param name="contentLength">
        ///     Number of bytes in stream to convert. 
        ///     If undefiined, will attempt to read length from stream and read the entire content of the stream.
        ///     If length cannot be read from the stream, the entire content of the stream will be converted.
        ///     The stream pointer will be left at the end of this content length.
        /// </param>
        /// <param name="insertBreakLines">
        ///     Insert line breaks after every 76 characters. 76 is the magic line length 
        ///     for SMTP base64 Content-Transfer-Encoding Default is false.
        /// </param>
        /// <param name="blockSize">Serial read chunk size. Default is 1Mb</param>
        /// <returns>Base64 string.</returns>
        /// <exception cref="ArgumentNullException">Input stream is null.</exception>
        /// <exception cref="NotSupportedException">Input stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Specified block/chunk size is less than 1KB.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Block size must be >= 1Kb (e.g. 1024).</exception>
        /// <exception cref="ArgumentOutOfRangeException">Content length has exceeded length of stream.</exception>
        /// <exception cref="IOException">A stream read I/O error occured.</exception>
        /// <exception cref="ObjectDisposedException">The stream was already closed.</exception>
        /// <remarks>
        /// The data is read and converted in chunks to reduce the memory footprint.
        /// The actual base64 conversion is performed by System.Convert.ToBase64CharArray().
        /// If the stream content length is not specified (either explicitly or implicitly thru stream.Length), much of 
        /// the speed and low memory footprint is lost by using an intermediate chunk collector (e.g.StringBuilder).
        /// </remarks>
        public static string ToString(Stream stream, int contentLength = -1, bool insertBreakLines = false, int blockSize = 1024 * 1024)
        {
            //https://stackoverflow.com/questions/5784636/faster-alternative-to-convert-tobase64string
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new NotSupportedException("Cannot read stream.");
            if (contentLength < -1) throw new ArgumentOutOfRangeException(nameof(contentLength), contentLength, "Content length must be >= 0 or -1 (e.g. undefined).");
            if (contentLength == -1) contentLength = stream.CanSeek ? (int)(stream.Length-stream.Position) : -1;
            else
            {
                //Trim contentLength to the actual size of content in stream, if possible.
                //Exception will occur only when stream length cannot be determined and the specified contentLength exceeds the the actual stream content.
                int len = stream.CanSeek ? (int)(stream.Length - stream.Position) : -1;
                if (len != -1 && contentLength > len) contentLength = len;
            }
            if (contentLength == 0) return string.Empty; //nothing to do
            if (blockSize < 1024) throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be >= 1Kb (e.g. 1024).");

            if (contentLength > 0 && contentLength <= blockSize)
            {
                byte[] inBuffer = new byte[contentLength];
                int len = stream.Read(inBuffer, 0, contentLength);
                return Convert.ToBase64String(inBuffer, 0, len);
            }

            return contentLength < 0 
                ? ToBase64StringStringBuilder(stream, insertBreakLines, blockSize) 
                : ToBase64StringUnsafe(stream, contentLength, insertBreakLines, blockSize);
        }

        internal static string ToBase64StringStringBuilder(Stream stream, bool insertBreakLines, int blockSize)
        {
            BinaryToBase64Lengths(blockSize, -1, insertBreakLines, out int inChunkSize, out int outChunkSize, out int totalAllocated);
            Base64FormattingOptions breaklines = insertBreakLines ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;

            byte[] inBuffer = new byte[inChunkSize];
            char[] outBuffer = new char[outChunkSize];
            int bytesRead = 0;

            var sb = new StringBuilder();

            while ((bytesRead = stream.Read(inBuffer, 0, inChunkSize)) != 0)
            {
                var charsConverted = System.Convert.ToBase64CharArray(inBuffer, 0, bytesRead, outBuffer, 0, breaklines);
                if (insertBreakLines && sb.Length != 0) sb.AppendLine();
                sb.Append(outBuffer, 0, charsConverted);
            }

            return sb.ToString();
        }

        internal static unsafe string ToBase64StringUnsafe(Stream stream, int contentLength, bool insertBreakLines, int blockSize)
        {
            if (contentLength < 1) return string.Empty;
            BinaryToBase64Lengths(blockSize, contentLength, insertBreakLines, out int inChunkSize, out int outChunkSize, out int totalAllocated);
            Base64FormattingOptions breaklines = insertBreakLines ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;

            byte[] inBuffer = new byte[inChunkSize];
            char[] outBuffer = new char[outChunkSize];
            int bytesRead = 0;

            string dstStr = FastAllocateString(totalAllocated);

            int totalStreamBytesToRead = contentLength;
            int totalStreamBytesActuallyRead = 0;
            int chunkOffset = 0; //bytes
            fixed (char* dstPtr = dstStr)
            {
                while ((bytesRead = stream.Read(inBuffer, 0, inChunkSize)) != 0)
                {
                    totalStreamBytesActuallyRead += bytesRead;
                    var charsConverted = System.Convert.ToBase64CharArray(inBuffer, 0, bytesRead, outBuffer, 0, breaklines);

                    if (insertBreakLines && chunkOffset != 0)
                    {
                        *(short*)((IntPtr)dstPtr + chunkOffset) = (short)13;
                        chunkOffset += 2;
                        *(short*)((IntPtr)dstPtr + chunkOffset) = (short)10;
                        chunkOffset += 2;
                    }

                    fixed (char* outBufPtr = outBuffer)
                        memcpy((byte*)dstPtr + chunkOffset, (byte*)outBufPtr, charsConverted * 2);

                    chunkOffset += charsConverted * 2;

                    totalStreamBytesToRead -= bytesRead;
                    if (totalStreamBytesToRead <= 0) break; //do not read the stream past what we are allowed to read to maintain accurate stream pointer .
                    if (totalStreamBytesToRead < inChunkSize) inChunkSize = totalStreamBytesToRead; //Only read what we are supposed to read. No more.
                }
            }

            //dstStr buffer is larger than what was used. The result is a string with a lot of trailing '\0's. This can cause issues so we create a trimmed string.
            if (totalStreamBytesToRead > 0)
            {
                if (totalStreamBytesActuallyRead == 0) return string.Empty;

                string dstStr2 = FastAllocateString(chunkOffset / 2);
                fixed (char* dstPtr = dstStr2)
                fixed (char* srcPtr = dstStr)
                    memcpy((byte*)dstPtr, (byte*)srcPtr, dstStr2.Length * 2);

                return dstStr2;
            }

            return dstStr;
        }

        /// <summary>
        /// Copy binary stream into another binary stream as base64 ASCII chars..
        /// </summary>
        /// <param name="stream">Binary stream to read, starting at the current position.</param>
        /// <param name="contentLength">Explicit number of bytes to read or -1 to slurp up the stream to the end</param>
        /// <param name="base64Stream">Binary stream to write to, starting at current position</param>
        /// <param name="insertBreakLines">
        ///     Insert line breaks after every 76 characters. 76 is the magic line length 
        ///     for SMTP base64 Content-Transfer-Encoding Default is false.
        /// </param>
        /// <param name="blockSize">Serial read chunk size. Default is 1Mb</param>
        /// <remarks>
        /// This is faster with a smaller foorprint, but careful consideration must be made if the destination stream 
        /// also contains other string data as this will only work with UTF7 or UTF8 encoded files/byte arrays.
        /// </remarks>
        public static void CopyTo(Stream stream, int contentLength, Stream base64Stream, bool insertBreakLines = false, int blockSize = 1024 * 1024)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (base64Stream == null) throw new ArgumentNullException(nameof(base64Stream));
            if (!stream.CanRead) throw new NotSupportedException("Cannot read source stream.");
            if (contentLength < -1) throw new ArgumentOutOfRangeException(nameof(contentLength), contentLength, "Content length must be >= 0 or -1 (e.g. undefined).");
            if (contentLength == -1) contentLength = stream.CanSeek ? (int)(stream.Length - stream.Position) : -1;
            else
            {
                // Trim contentLength to the actual size of content in stream, if possible.
                //Exception will occur only when stream length cannot be determined and the specified contentLength exceeds the the actual stream content.
                int len = stream.CanSeek ? (int)(stream.Length - stream.Position) : -1;
                if (len != -1 && contentLength > len) contentLength = len;
            }
            if (contentLength == 0) return; //nothing to do
            if (blockSize < 1024) throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be >= 1Kb (e.g. 1024).");

            BinaryToBase64Lengths(blockSize, contentLength, insertBreakLines, out int inChunkSize, out int outChunkSize, out int totalAllocated);

            Base64FormattingOptions breaklines = insertBreakLines ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;
            byte[] inBuffer = new byte[inChunkSize];
            char[] outBuffer = new char[outChunkSize];
            int totalStreamBytesToRead = contentLength < 0 ? int.MaxValue : contentLength;
            int totalStreamBytesActuallyRead = 0;
            int bytesRead = 0;

            while ((bytesRead = stream.Read(inBuffer, 0, inChunkSize)) != 0)
            {
                var charsConverted = System.Convert.ToBase64CharArray(inBuffer, 0, bytesRead, outBuffer, 0, breaklines);

                if (insertBreakLines && totalStreamBytesActuallyRead != 0) { base64Stream.WriteByte((byte)13); base64Stream.WriteByte((byte)10); }
                for (int i = 0; i < charsConverted; i++) base64Stream.WriteByte((byte)outBuffer[i]);

                totalStreamBytesActuallyRead += bytesRead;

                totalStreamBytesToRead -= bytesRead;
                if (totalStreamBytesToRead <= 0) break; //do not read the stream past what we are allowed to read to maintain accurate stream pointer .
                if (totalStreamBytesToRead < inChunkSize) inChunkSize = totalStreamBytesToRead; //Only read what we are supposed to read. No more.
            }

            base64Stream.Flush();
        }

        /// <summary>
        /// Copy binary stream into a base64 text stream.
        /// </summary>
        /// <param name="stream">Binary stream to read, starting at the current position.</param>
        /// <param name="contentLength">Explicit number of bytes to read or -1 to slurp up the stream to the end</param>
        /// <param name="base64Stream">Text stream to write to, starting at current position</param>
        /// <param name="insertBreakLines">
        ///     Insert line breaks after every 76 characters. 76 is the magic line length 
        ///     for SMTP base64 Content-Transfer-Encoding Default is false.
        /// </param>
        /// <param name="blockSize">Serial read chunk size. Default is 1Mb</param>
        public static void CopyTo(Stream stream, int contentLength, TextWriter base64Stream, bool insertBreakLines = false, int blockSize = 1024 * 1024)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (base64Stream == null) throw new ArgumentNullException(nameof(base64Stream));
            if (!stream.CanRead) throw new NotSupportedException("Cannot read source stream.");
            if (contentLength < -1) throw new ArgumentOutOfRangeException(nameof(contentLength), contentLength, "Content length must be >= 0 or -1 (e.g. undefined).");
            if (contentLength == -1) contentLength = stream.CanSeek ? (int)(stream.Length - stream.Position) : -1;
            else
            {
                // Trim contentLength to the actual size of content in stream, if possible.
                //Exception will occur only when stream length cannot be determined and the specified contentLength exceeds the the actual stream content.
                int len = stream.CanSeek ? (int)(stream.Length - stream.Position) : -1;
                if (len != -1 && contentLength > len) contentLength = len;
            }
            if (contentLength == 0) return; //nothing to do
            if (blockSize < 1024) throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be >= 1Kb (e.g. 1024).");

            BinaryToBase64Lengths(blockSize, contentLength, insertBreakLines, out int inChunkSize, out int outChunkSize, out int totalAllocated);

            byte[] inBuffer = new byte[inChunkSize];
            char[] outBuffer = new char[outChunkSize];
            Base64FormattingOptions breaklines = insertBreakLines ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;

            int totalStreamBytesToRead = contentLength < 0 ? int.MaxValue : contentLength;
            int totalStreamBytesActuallyRead = 0;
            int bytesRead = 0;

            while ((bytesRead = stream.Read(inBuffer, 0, inChunkSize)) != 0)
            {
                var charsConverted = System.Convert.ToBase64CharArray(inBuffer, 0, bytesRead, outBuffer, 0, breaklines);

                if (insertBreakLines && totalStreamBytesActuallyRead != 0) base64Stream.WriteLine();
                base64Stream.Write(outBuffer, 0, charsConverted);

                totalStreamBytesActuallyRead += bytesRead;

                totalStreamBytesToRead -= bytesRead;
                if (totalStreamBytesToRead <= 0) break; //do not read the stream past what we are allowed to read to maintain accurate stream pointer .
                if (totalStreamBytesToRead < inChunkSize) inChunkSize = totalStreamBytesToRead; //Only read what we are supposed to read. No more.
            }

            base64Stream.Flush();
        }

        /// <summary>
        /// Copy base64 text stream into a binary stream.
        /// </summary>
        /// <param name="base64Stream">Text stream to read, starting at the current position.</param>
        /// <param name="contentLength">Explicit number of characters to read or -1 to slurp up the stream to the end or to the first non-base64 char.</param>
        /// <param name="stream">Binary stream to write starting at current position.</param>
        /// <param name="blockSize">Serial read chunk size. Default is 1Mb</param>
        /// <exception cref="ArgumentNullException">Input stream is null.</exception>
        /// <exception cref="ArgumentNullException">Output stream is null.</exception>
        /// <exception cref="NotSupportedException">Output stream is not writeable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Block size must be >= 1Kb (e.g. 1024).</exception>
        /// <exception cref="ArgumentOutOfRangeException">Text content length must be >= 0 or  -1 to read to end of stream or to the first invalid base64 char.</exception>
        /// <exception cref="IOException">A stream read/write I/O error occured.</exception>
        /// <exception cref="ObjectDisposedException">The stream was already closed.</exception>
        /// <exception cref="FormatException">Invalid length for a Base-64 char array or string.</exception>
        public static void CopyFrom(TextReader base64Stream, int contentLength, Stream stream, int blockSize = 1024 * 1024)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (base64Stream == null) throw new ArgumentNullException(nameof(base64Stream));
            if (!stream.CanWrite) throw new NotSupportedException("Cannot write destination stream.");
            if (contentLength < -1) throw new ArgumentOutOfRangeException(nameof(contentLength), contentLength, "Content length must be >= 0 or -1 (e.g. undefined).");
            if (contentLength == 0) return; //nothing to do
            if (blockSize < 1024) throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be >= 1Kb (e.g. 1024).");

            int outChunkSize = (blockSize / 3) * 3;
            int inChunkSize = (blockSize / 3) * 4;
            if (contentLength > 0 && inChunkSize > contentLength) inChunkSize = contentLength;
            char[] inBuffer = new char[inChunkSize];
            byte[] outBuffer = new byte[outChunkSize];

            int totalStreamCharsToRead = contentLength < 0 ? int.MaxValue : contentLength;
            int totalStreamCharsActuallyRead = 0;
            int charsRead = 0;

            while ((charsRead = ReadChunk(base64Stream, inBuffer, inChunkSize)) != 0)
            {
                var bytes = System.Convert.FromBase64CharArray(inBuffer, 0, charsRead);

                stream.Write(bytes,0, bytes.Length);

                totalStreamCharsActuallyRead += charsRead;

                totalStreamCharsToRead -= charsRead;
                if (totalStreamCharsToRead <= 0) break; //do not read the stream past what we are allowed to read to maintain accurate stream pointer .
                if (totalStreamCharsToRead < inChunkSize) inChunkSize = totalStreamCharsToRead; //Only read what we are supposed to read. No more.
            }

            stream.Flush();
        }

        /// <summary>
        /// Carefully read only base16 chars ignoring whitespace chars. Stop when buffer is full or when it runs into a non-base16 char.
        /// Stream pointer is always set to the next char to read in the stream, even if it is a non-base16 char.
        /// </summary>
        /// <param name="tr">TextReader stream</param>
        /// <param name="buffer">Buffer to put base16 chars into.</param>
        /// <param name="bufLen">Number of base16 chars to retrieve. Must be <= to buffer length.</param>
        /// <returns>Number of base16 chars retrieved.</returns>
        private static int ReadChunk(TextReader tr, char[] buffer, int bufLen)
        {
            if (buffer.Length < bufLen) bufLen = buffer.Length;
            int iChar;
            char c;
            int i = 0;

            if ((iChar = tr.Peek()) == -1) return 0;
            c = (char)iChar;
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '=' || c == ' ' || c == '\t' || c == '\r' || c == '\n')) return 0;

            while ((iChar = tr.Read())!=-1)
            {
                c = (char)iChar;
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') continue;
                buffer[i++] = c;
                if (i == bufLen) return i;

                if ((iChar = tr.Peek()) != -1)
                {
                    c = (char)iChar;
                    if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '=' || c == ' ' || c == '\t' || c == '\r' || c == '\n')) break;
                }
            }

            return i;
        }

        /// <summary>
        /// Compute length properties for converting from binary lengths to base64 lengths
        /// </summary>
        /// <param name="blockSize">Input  binary stream read chunk size.</param>
        /// <param name="contentLength">Input  binary stream content length to read or -1 if unknown.</param>
        /// <param name="insertBreakLines">Flag to adjust sizes for CRLF newlines at 76 character lines</param>
        /// <param name="inChunkSize">Actual input binary stream  bytes to read upon each iteration.</param>
        /// <param name="outChunkSize">Actual number of converted output chars for given input chunk size.</param>
        /// <param name="totalAllocated">Actual total number of characters written or -1 if contentLength==-1</param>
        private static void BinaryToBase64Lengths(int blockSize, int contentLength, bool insertBreakLines, out int inChunkSize, out int outChunkSize, out int totalAllocated)
        {
            if (contentLength < 0)
            {
                totalAllocated = -1;
                if (insertBreakLines)
                {
                    int b2 = (blockSize / 3) * 4;
                    int linesPerBlock = b2 / base64LineBreakPosition;
                    outChunkSize = (base64LineBreakPosition + 2) * linesPerBlock - 2;
                    inChunkSize = (base64LineBreakPosition * linesPerBlock) / 4 * 3;
                }
                else
                {
                    inChunkSize = (blockSize / 3) * 3;
                    outChunkSize = (blockSize / 3) * 4;
                }
            }
            else
            {
                if (contentLength == 0)
                {
                    totalAllocated = 0;
                    inChunkSize = 0;
                    outChunkSize = 0;
                }
                else
                {
                    totalAllocated = ToBase64_CalculateAndValidateOutputLength(contentLength, insertBreakLines);
                    if (insertBreakLines)
                    {
                        int unformattedOutChunkSize = (blockSize / 3) * 4;
                        int linesPerBlock = unformattedOutChunkSize / base64LineBreakPosition;
                        outChunkSize = (base64LineBreakPosition + 2) * linesPerBlock - 2;
                        inChunkSize = (base64LineBreakPosition * linesPerBlock) / 4 * 3;
                    }
                    else
                    {
                        inChunkSize = (blockSize / 3) * 3;
                        outChunkSize = (blockSize / 3) * 4;
                    }
                }
            }
        }

        private static int ToBase64_CalculateAndValidateOutputLength(int inputLength, bool insertLineBreaks)
        {
            long num1 = (long)inputLength / 3L * 4L + (inputLength % 3 != 0 ? 4L : 0L);
            if (num1 == 0L) return 0;
            if (insertLineBreaks)
            {
                long num2 = num1 / (long)base64LineBreakPosition;
                if (num1 % (long)base64LineBreakPosition == 0L) --num2;
                num1 += num2 * 2L;
            }
            return num1 <= (long)int.MaxValue ? (int)num1 : throw new OutOfMemoryException();
        }

        #region Hacks for unsafe string/pointer manipulation
        // Yes, there are many arguments for NOT hacking immutable C# strings, but if you want performance (speed & 
        // memory), then this is what you have to do. Microsoft does this very thing internally in within their own 
        // System.String.String.cs source code. No, it's not portable outside a Windows environment, but there you are.

        [DllImport("NTDLL.dll", CallingConvention = CallingConvention.Cdecl)] unsafe private static extern byte* memcpy(byte* dest, byte* src, int count);

        private static string FastAllocateString(int length)
        {
            // The following code exists in Microsoft String.cs as:
            // 
            // [SecurityCritical]
            // [MethodImpl(MethodImplOptions.InternalCall)]
            // internal static extern string FastAllocateString(int length);
            // 
            // However using the above code compiles but always throws the runtime exception:
            //     SecurityException: ECall methods must be packaged into a system module
            // whenever it is called directly. Using reflection is apparently OK.
            // This method actually resides within COM mscoree.dll

            var x = typeof(String).GetMethod("FastAllocateString", BindingFlags.NonPublic | BindingFlags.Static);
            if (x == null) throw new NullReferenceException("String.FastAllocateString() not found via reflection.");
            var str = (String)x.Invoke(null, new object[] { length });
            return str;
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64Stream.Test
{
    public class NonSeekableStream : Stream
    {
        private Stream m_stream;
        public NonSeekableStream(Stream baseStream) => m_stream = baseStream;

        public override bool CanRead => m_stream.CanRead;
        public override bool CanSeek => false; //m_stream.CanSeek;
        public override bool CanWrite => m_stream.CanWrite;
        public override void Flush() => m_stream.Flush();
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => m_stream.Position;
            set => throw new NotSupportedException();
        }
        public override int Read(byte[] buffer, int offset, int count) => m_stream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>  m_stream.Write(buffer, offset, count);
    }
}

using System;
using System.IO;

namespace SharpCompress.Compressors
{
    public enum CompressionMode
    {
        Compress = 0,
        Decompress = 1
    }
}

namespace SharpCompress.Compressors.BZip2
{
    public class BZip2Stream : Stream, IDisposable
    {
        public BZip2Stream(Stream output, CompressionMode compress, bool b)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}
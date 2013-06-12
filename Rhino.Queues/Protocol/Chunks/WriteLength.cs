using System;
using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol.Chunks
{
    public class WriteLength : Chunk
    {
        private readonly int _length;

        public WriteLength(int length, string sender) : base(sender)
        {
            _length = length;
        }

        public WriteLength(int length) : this(length, null)
        {
        }

        protected override Task ProcessInternalAsync(Stream stream)
        {
            var bufferLenInBytes = BitConverter.GetBytes(_length);
            return stream.WriteAsync(bufferLenInBytes, 0, bufferLenInBytes.Length);
        }

        public override string ToString()
        {
            return string.Format("Writing Length: {0}", _length);
        }
    }
}
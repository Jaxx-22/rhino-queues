using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol.Chunks
{
    public class WriteMessage : Chunk
    {
        private readonly byte[] _buffer;

        public WriteMessage(byte[] buffer, string destination) : base(destination)
        {
            _buffer = buffer;
        }

        public WriteMessage(byte[] buffer) : this(buffer, null)
        {
        }

        protected override Task ProcessInternalAsync(Stream stream)
        {
            return stream.WriteAsync(_buffer, 0, _buffer.Length);
        }

        public override string ToString()
        {
            return string.Format("Writing Bytes {0}", _buffer.Length);
        }
    }
}
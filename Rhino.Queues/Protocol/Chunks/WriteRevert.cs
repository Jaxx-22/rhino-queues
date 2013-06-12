using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol.Chunks
{
    public class WriteRevert : Chunk
    {
        public WriteRevert(string endpoint) : base(endpoint)
        {
        }

        public WriteRevert() : this(null)
        {
        }

        protected override Task ProcessInternalAsync(Stream stream)
        {
            return stream.WriteAsync(ProtocolConstants.RevertBuffer, 0, ProtocolConstants.RevertBuffer.Length);
        }

        public override string ToString()
        {
            return "Write Revert";
        }
    }
}
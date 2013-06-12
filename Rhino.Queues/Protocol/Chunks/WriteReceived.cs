using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol.Chunks
{
    public class WriteReceived : Chunk
    {
        public WriteReceived(string endpoint) : base(endpoint)
        {
        }

        public WriteReceived() : this(null)
        {
        }

        protected override Task ProcessInternalAsync(Stream stream)
        {
            _logger.DebugFormat("Sending reciept notice to {0}", _endpoint);
            return stream.WriteAsync(ProtocolConstants.ReceivedBuffer, 0, ProtocolConstants.ReceivedBuffer.Length);
        }

        public override string ToString()
        {
            return "Write Received";
        }
    }
}
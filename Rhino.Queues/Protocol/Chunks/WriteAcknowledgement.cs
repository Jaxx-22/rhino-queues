using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol.Chunks
{
    public class WriteAcknowledgement : Chunk
    {
        public WriteAcknowledgement(string sender) : base(sender)
        {
        }

        public WriteAcknowledgement() : this(null)
        {
        }

        protected override Task ProcessInternalAsync(Stream stream)
        {
            return stream.WriteAsync(ProtocolConstants.AcknowledgedBuffer, 0, ProtocolConstants.AcknowledgedBuffer.Length);
        }

        public override string ToString()
        {
            return string.Format("Writing Acknowledgement");
        }
    }
}
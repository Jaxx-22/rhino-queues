using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rhino.Queues.Exceptions;

namespace Rhino.Queues.Protocol.Chunks
{
    public class ReadAcknowledgement : Chunk
    {
        public ReadAcknowledgement(string sender)
            : base(sender)
        {
        }

        public ReadAcknowledgement()
            : this(null)
        {
        }

        protected override async Task ProcessInternalAsync(Stream stream)
        {
            _logger.DebugFormat("Reading acknowledgement about accepting messages to {0}", _endpoint);
            var recieveBuffer = new byte[ProtocolConstants.AcknowledgedBuffer.Length];
            await stream.ReadBytesAsync(recieveBuffer, "receive confirmation", false);
            var recieveRespone = Encoding.Unicode.GetString(recieveBuffer);
            if (recieveRespone != ProtocolConstants.Acknowledged)
            {
                _logger.WarnFormat("Response from sender acknowledgement was the wrong format", _endpoint);
                throw new InvalidAcknowledgementException();
            }
        }

        public override string ToString()
        {
            return "Read Acknowledgement";
        }
    }
}
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rhino.Queues.Exceptions;

namespace Rhino.Queues.Protocol.Chunks
{
    public class ReadReceived : Chunk
    {
        public ReadReceived(string sender) : base(sender)
        {
        }

        public ReadReceived() : this(null)
        {
        }

        protected override async Task ProcessInternalAsync(Stream stream)
        {
            var recieveBuffer = new byte[ProtocolConstants.ReceivedBuffer.Length];
            await stream.ReadBytesAsync(recieveBuffer, "receive confirmation", false);
            var recieveRespone = Encoding.Unicode.GetString(recieveBuffer);
            if (recieveRespone == ProtocolConstants.QueueDoesNotExists)
            {
                _logger.WarnFormat("Response from reciever {0} is that queue does not exists", _endpoint);
                throw new QueueDoesNotExistsException();
            }
            if (recieveRespone != ProtocolConstants.Received)
            {
                _logger.WarnFormat("Response from receiver {0} is not the expected one, unexpected response was: {1}",
                    _endpoint, recieveRespone);
                throw new UnexpectedReceivedMessageFormatException();
            }
        }
    }
}
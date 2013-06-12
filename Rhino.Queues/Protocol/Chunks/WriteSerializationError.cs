using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol.Chunks
{
    public class WriteSerializationError : Chunk
    {
        protected override async Task ProcessInternalAsync(Stream stream)
        {
            await stream.WriteAsync(ProtocolConstants.SerializationFailureBuffer, 0, ProtocolConstants.SerializationFailureBuffer.Length);
        }
    }
}
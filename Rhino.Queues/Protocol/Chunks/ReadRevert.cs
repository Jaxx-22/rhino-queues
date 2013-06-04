using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rhino.Queues.Exceptions;

namespace Rhino.Queues.Protocol.Chunks
{
    public class ReadRevert : Chunk
    {
        public ReadRevert(string destination) : base(destination)
        {
        }

        public ReadRevert() : this(null)
        {
        }

        protected override async Task ProcessInternalAsync(Stream stream)
        {
            var buffer = new byte[ProtocolConstants.RevertBuffer.Length];
            try
            {
                await stream.ReadBytesAsync(buffer, "revert", true);
            }
            catch (Exception)
            {
                //a revert was not sent, send is complete
                return;
            }
            var revert = Encoding.Unicode.GetString(buffer);
            if (revert == ProtocolConstants.Revert)
            {
                throw new RevertSendException();
            }
        }
    }
}
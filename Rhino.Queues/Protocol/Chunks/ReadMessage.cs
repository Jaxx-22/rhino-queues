using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Rhino.Queues.Model;

namespace Rhino.Queues.Protocol.Chunks
{
    public class ReadMessage : Chunk<Message[]>
    {
        private readonly byte[] _buffer;

        public ReadMessage(byte[] buffer, string endpoint) : base(endpoint)
        {
            _buffer = buffer;
        }

        public ReadMessage(byte[] buffer) : this(buffer, null)
        {
        }

        protected async override Task<Message[]> GetInternalAsync(Stream stream)
        {
            await stream.ReadBytesAsync(_buffer, "message data", false);
            Exception serializationException = null;
            Message[] messages = null;
            try
            {
                messages = SerializationExtensions.ToMessages(_buffer);
                _logger.DebugFormat("Deserialized {0} messages from {1}", messages.Length, _endpoint);
            }
            catch (Exception exception)
            {
                serializationException = exception;
                _logger.Warn("Failed to deserialize messages from " + _endpoint, exception);
            }
            if (serializationException != null)
            {
                await stream.WriteAsync(ProtocolConstants.SerializationFailureBuffer, 0, ProtocolConstants.SerializationFailureBuffer.Length);
                throw new SerializationException("Failed to deserialize message", serializationException);
            }
            return messages;
        }

        public override string ToString()
        {
            return "Reading Message";
        }
    }
}
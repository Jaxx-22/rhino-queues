using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rhino.Queues.Exceptions;
using Rhino.Queues.Protocol;
using Rhino.Queues.Protocol.Chunks;
using Xunit;

namespace Rhino.Queues.Tests.Protocol
{
    public class ProtocolChunkTester
    {
        [Fact]
        public async Task can_write_length_chunk()
        {
            const int length = 5;
            var ms = new MemoryStream();

            var chunkWriter = new WriteLength(length);
            await chunkWriter.ProcessAsync(ms);

            var result = ms.ToArray();
            Assert.Equal(length, BitConverter.ToInt32(result, 0));
        }

        [Fact]
        public async Task can_write_message()
        {
            var message = new byte[] {1, 2, 5};
            var ms = new MemoryStream();

            var chunkWriter = new WriteMessage(message);
            await chunkWriter.ProcessAsync(ms);

            var result = ms.ToArray();
            Assert.Equal(message, result);
        }

        [Fact]
        public async Task can_read_received_chunk()
        {
            var ms = new MemoryStream(ProtocolConstants.ReceivedBuffer);

            var chunkReader = new ReadReceived();
            await chunkReader.ProcessAsync(ms);

            //nothing to assert, but should not throw or hang
        }

        [Fact]
        public async Task can_read_received_but_queue_doesn_not_exist_chunk()
        {
            var ms = new MemoryStream(ProtocolConstants.QueueDoesNoExiststBuffer);
            bool threwError = false;

            var chunkReader = new ReadReceived();
            try
            {
                await chunkReader.ProcessAsync(ms);
            }
            catch (QueueDoesNotExistsException)
            {
                threwError = true;
            }

            Assert.True(threwError);
        }

        [Fact]
        public async Task when_format_is_unexpected_should_throw()
        {
            var ms = new MemoryStream(Encoding.Unicode.GetBytes("Reciever"));
            bool threwError = false;

            var chunkReader = new ReadReceived();
            try
            {
                await chunkReader.ProcessAsync(ms);
            }
            catch (UnexpectedReceivedMessageFormatException)
            {
                threwError = true;
            }

            Assert.True(threwError);
        }
    }
}
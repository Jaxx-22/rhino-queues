using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Common.Logging;
using Rhino.Queues.Exceptions;
using Rhino.Queues.Model;
using Rhino.Queues.Protocol.Chunks;

namespace Rhino.Queues.Protocol
{
    public class ReceivingProtocolCoordinator
    {
        private ILog _logger = LogManager.GetLogger<ReceivingProtocolCoordinator>();

        public async Task ReadMessagesAsync(string endpoint, Stream stream,
                                            Func<Message[], IMessageAcceptance> acceptMessages)
        {
            bool serializationErrorOccurred = false;
            Message[] messages = null;
            try
            {
                var buffer = await new ReadLength(endpoint).GetAsync(stream);
                messages = await new ReadMessage(buffer, endpoint).GetAsync(stream);
            }
            catch (SerializationException exception)
            {
                _logger.Warn("Unable to deserialize messages", exception);
                serializationErrorOccurred = true;
            }

            if (serializationErrorOccurred)
            {
                await new WriteSerializationError().ProcessAsync(stream);
            }

            IMessageAcceptance acceptance = null;
            byte[] errorBytes = null;
            try
            {
                acceptance = acceptMessages(messages);
                _logger.DebugFormat("All messages from {0} were accepted", endpoint);
            }
            catch (QueueDoesNotExistsException)
            {
                _logger.WarnFormat("Failed to accept messages from {0} because queue does not exists", endpoint);
                errorBytes = ProtocolConstants.QueueDoesNoExiststBuffer;
            }
            catch (Exception exception)
            {
                errorBytes = ProtocolConstants.ProcessingFailureBuffer;
                _logger.Warn("Failed to accept messages from " + endpoint, exception);
            }

            if (errorBytes != null)
            {
                await new WriteProcessingError(errorBytes, endpoint).ProcessAsync(stream);
                return;
            }

            try
            {
                await new WriteReceived(endpoint).ProcessAsync(stream);
            }
            catch (Exception)
            {
                acceptance.Abort();
                return;
            }

            try
            {
                await new ReadAcknowledgement(endpoint).ProcessAsync(stream);
            }
            catch (Exception)
            {
                acceptance.Abort();
                return;
            }

            bool commitSuccessful;
            try
            {
                acceptance.Commit();
                commitSuccessful = true;
            }
            catch (Exception exception)
            {
                _logger.Warn("Unable to commit messages from " + endpoint, exception);
                commitSuccessful = false;
            }

            if (commitSuccessful == false)
            {
                await new WriteRevert(endpoint).ProcessAsync(stream);
            }
        }
    }
}
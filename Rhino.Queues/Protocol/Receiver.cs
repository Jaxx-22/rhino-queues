using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Common.Logging;
using Rhino.Queues.Exceptions;
using Rhino.Queues.Model;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol
{
    public class Receiver : IDisposable
    {
        private readonly IPEndPoint endpointToListenTo;
        private readonly bool enableEndpointPortAutoSelection;
        private readonly Func<Message[], IMessageAcceptance> acceptMessages;
        private TcpListener listener;
        private readonly ILog logger = LogManager.GetLogger(typeof(Receiver));

        public event Action CompletedRecievingMessages;

        public Receiver(IPEndPoint endpointToListenTo, Func<Message[], IMessageAcceptance> acceptMessages)
            :this(endpointToListenTo, false, acceptMessages)
        { }

        public Receiver(IPEndPoint endpointToListenTo, bool enableEndpointPortAutoSelection, Func<Message[], IMessageAcceptance> acceptMessages)
        {
            this.endpointToListenTo = endpointToListenTo;
            this.enableEndpointPortAutoSelection = enableEndpointPortAutoSelection;
            this.acceptMessages = acceptMessages;
        }

        public void Start()
        {
            logger.DebugFormat("Starting to listen on {0}", endpointToListenTo);
            while (endpointToListenTo.Port < 65536)
            {
                try
                {
                    TryStart(endpointToListenTo);
                    logger.DebugFormat("Now listen on {0}", endpointToListenTo);
                    return;
                }
                catch(SocketException ex)
                {
                    if (enableEndpointPortAutoSelection &&
                        ex.Message == "Only one usage of each socket address (protocol/network address/port) is normally permitted")
                    {
                        endpointToListenTo.Port = SelectAvailablePort();
                        logger.DebugFormat("Port in use, new enpoint selected: {0}", endpointToListenTo);
                    }
                    else
                        throw;
                }
            }
        }

        private void TryStart(IPEndPoint endpointToListenTo)
        {
            listener = new TcpListener(endpointToListenTo);
            listener.Start();
            AcceptTcpClientAsync();
        }

        private async void AcceptTcpClientAsync()
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                logger.DebugFormat("Accepting connection from {0}", client.Client.RemoteEndPoint);
                await ProcessRequest(client);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.Warn("Error on EndAcceptTcpClient", ex);
            }
            AcceptTcpClientAsync();
        }

        private static int SelectAvailablePort()
        {
            const int START_OF_IANA_PRIVATE_PORT_RANGE = 49152;
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();

            var allInUseTcpPorts = tcpListeners.Select(tcpl => tcpl.Port)
                .Union(tcpConnections.Select(tcpi => tcpi.LocalEndPoint.Port));

            var orderedListOfPrivateInUseTcpPorts = allInUseTcpPorts
                .Where(p => p >= START_OF_IANA_PRIVATE_PORT_RANGE)
                .OrderBy(p => p);

            var candidatePort = START_OF_IANA_PRIVATE_PORT_RANGE;
            foreach (var usedPort in orderedListOfPrivateInUseTcpPorts)
            {
                if (usedPort != candidatePort) break;
                candidatePort++;
            }
            return candidatePort;
        }

        private async Task ProcessRequest(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var sender = client.Client.RemoteEndPoint;

                    var lenOfDataToReadBuffer = new byte[sizeof(int)];

                    try
                    {
                        await stream.ReadBytesAsync(lenOfDataToReadBuffer, "length data", false);
                    }
                    catch (Exception exception)
                    {
                        logger.Warn("Unable to read length data from " + sender, exception);
                        return;
                    }

                    var lengthOfDataToRead = BitConverter.ToInt32(lenOfDataToReadBuffer, 0);
                    if (lengthOfDataToRead < 0)
                    {
                        logger.WarnFormat("Got invalid length {0} from sender {1}", lengthOfDataToRead, sender);
                        return;
                    }
                    logger.DebugFormat("Reading {0} bytes from {1}", lengthOfDataToRead, sender);

                    var buffer = new byte[lengthOfDataToRead];

                    try
                    {
                        await stream.ReadBytesAsync(buffer, "message data", false);
                    }
                    catch (Exception exception)
                    {
                        logger.Warn("Unable to read message data from " + sender, exception);
                        return;
                    }

                    Message[] messages = null;
                    try
                    {
                        messages = SerializationExtensions.ToMessages(buffer);
                        logger.DebugFormat("Deserialized {0} messages from {1}", messages.Length, sender);
                    }
                    catch (Exception exception)
                    {
                        logger.Warn("Failed to deserialize messages from " + sender, exception);
                    }

                    if (messages == null)
                    {
                        try
                        {
                            await stream.WriteAsync(ProtocolConstants.SerializationFailureBuffer, 0, ProtocolConstants.SerializationFailureBuffer.Length);
                        }
                        catch (Exception exception)
                        {
                            logger.Warn("Unable to send serialization format error to " + sender, exception);
                            return;
                        }
                    }

                    IMessageAcceptance acceptance = null;
                    byte[] errorBytes = null;
                    try
                    {
                        acceptance = acceptMessages(messages);
                        logger.DebugFormat("All messages from {0} were accepted", sender);
                    }
                    catch (QueueDoesNotExistsException)
                    {
                        logger.WarnFormat("Failed to accept messages from {0} because queue does not exists", sender);
                        errorBytes = ProtocolConstants.QueueDoesNoExiststBuffer;
                    }
                    catch (Exception exception)
                    {
                        errorBytes = ProtocolConstants.ProcessingFailureBuffer;
                        logger.Warn("Failed to accept messages from " + sender, exception);
                    }

                    if (errorBytes != null)
                    {
                        try
                        {
                            await stream.WriteAsync(errorBytes, 0, errorBytes.Length);
                        }
                        catch (Exception exception)
                        {
                            logger.Warn("Unable to send processing failure from " + sender, exception);
                            return;
                        }
                        return;
                    }

                    logger.DebugFormat("Sending reciept notice to {0}", sender);
                    try
                    {
                        await stream.WriteAsync(ProtocolConstants.ReceivedBuffer, 0, ProtocolConstants.ReceivedBuffer.Length);
                    }
                    catch (Exception exception)
                    {
                        logger.Warn("Could not send reciept notice to " + sender, exception);
                        acceptance.Abort();
                        return;
                    }

                    logger.DebugFormat("Reading acknowledgement about accepting messages to {0}", sender);

                    var acknowledgementBuffer = new byte[ProtocolConstants.AcknowledgedBuffer.Length];

                    try
                    {
                        await stream.ReadBytesAsync(acknowledgementBuffer, "acknowledgement", false);
                    }
                    catch (Exception exception)
                    {
                        logger.Warn("Error reading acknowledgement from " + sender, exception);
                        acceptance.Abort();
                        return;
                    }

                    var senderResponse = Encoding.Unicode.GetString(acknowledgementBuffer);
                    if (senderResponse != ProtocolConstants.Acknowledged)
                    {
                        logger.WarnFormat("Sender did not respond with proper acknowledgement, the reply was {0}",
                                          senderResponse);
                        acceptance.Abort();
                    }

                    bool commitSuccessful;
                    try
                    {
                        acceptance.Commit();
                        commitSuccessful = true;
                    }
                    catch (Exception exception)
                    {
                        logger.Warn("Unable to commit messages from " + sender, exception);
                        commitSuccessful = false;
                    }

                    if (commitSuccessful == false)
                    {
                        try
                        {
                            await stream.WriteAsync(ProtocolConstants.RevertBuffer, 0, ProtocolConstants.RevertBuffer.Length);
                        }
                        catch (Exception e)
                        {
                            logger.Warn("Unable to send revert message to " + sender, e);
                        }
                    }
                }
            }
            finally
            {
                var copy = CompletedRecievingMessages;
                if (copy != null)
                    copy();
            }
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
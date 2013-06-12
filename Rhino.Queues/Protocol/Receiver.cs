using System;
using System.Net;
using System.Net.Sockets;
using Common.Logging;
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
        private bool disposed;
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
                    StartAccepting();
                    logger.DebugFormat("Now listen on {0}", endpointToListenTo);
                    return;
                }
                catch(SocketException ex)
                {
                    if (enableEndpointPortAutoSelection &&
                        ex.Message == "Only one usage of each socket address (protocol/network address/port) is normally permitted")
                    {
                        endpointToListenTo.Port = new PortFinder().Find();
                        logger.DebugFormat("Port in use, new enpoint selected: {0}", endpointToListenTo);
                    }
                    else
                        throw;
                }
            }
        }

        private async void StartAccepting()
        {
            while (!disposed)
            {
                await AcceptTcpClientAsync();
            }
        }

        private void TryStart(IPEndPoint endpointToListenTo)
        {
            listener = new TcpListener(endpointToListenTo);
            listener.Start();
        }

        private async Task AcceptTcpClientAsync()
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                logger.DebugFormat("Accepting connection from {0}", client.Client.RemoteEndPoint);
                await ProcessRequest(client);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                logger.Warn("Error on ProcessRequest " + ex.Message, ex);
            }
            finally
            {
                var copy = CompletedRecievingMessages;
                if (copy != null)
                    copy();
            }
        }

        private async Task ProcessRequest(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var sender = client.Client.RemoteEndPoint.ToString();
                await new ReceivingProtocolCoordinator().ReadMessagesAsync(sender, stream, acceptMessages);
            }
        }

        public void Dispose()
        {
            disposed = true;
            listener.Stop();
        }
    }
}
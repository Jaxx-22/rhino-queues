using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Logging;
using Rhino.Queues.Exceptions;
using Rhino.Queues.Model;
using Rhino.Queues.Protocol.Chunks;
using Rhino.Queues.Storage;

namespace Rhino.Queues.Protocol
{
    public class Sender
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof (Sender));

        public event Action SendCompleted;
        public Func<MessageBookmark[]> Success { get; set; }
        public Action<Exception> Failure { get; set; }
        public Action<MessageBookmark[]> Revert { get; set; }
        public Action Connected { get; set; }
        public Action<Exception> FailureToConnect { get; set; }
        public Action Commit { get; set; }
        public Endpoint Destination { get; set; }
        public Message[] Messages { get; set; }

        public Sender()
        {
            Connected = () => { };
            FailureToConnect = e => { };
            Failure = e => { };
            Success = () => null;
            Revert = bookmarks => { };
            Commit = () => { };
        }

        public async void Send()
        {
            _logger.DebugFormat("Starting to send {0} messages to {1}", Messages.Length, Destination);
            await SendInternalAsync();
        }

        private async Task SendInternalAsync()
        {
            using (var client = new TcpClient())
            {
                var connected = await Connect(client);
                if (!connected)
                    return;

                using (var stream = client.GetStream())
                {
                    var buffer = Messages.Serialize();
                    MessageBookmark[] bookmarks = null;
                    try
                    {
                        await new WriteLength(buffer.Length, Destination.ToString()).ProcessAsync(stream);
                        await new WriteMessage(buffer, Destination.ToString()).ProcessAsync(stream);
                        await new ReadReceived(Destination.ToString()).ProcessAsync(stream);
                        await new WriteAcknowledgement(Destination.ToString()).ProcessAsync(stream);
                        bookmarks = Success();
                        await new ReadRevert(Destination.ToString()).ProcessAsync(stream);
                        Commit();
                    }
                    catch (RevertSendException)
                    {
                        _logger.WarnFormat("Got back revert message from receiver {0}, reverting send", Destination);
                        Revert(bookmarks);
                    }
                    catch (Exception exception)
                    {
                        Failure(exception);
                    }
                    finally
                    {
                        var completed = SendCompleted;
                        if (completed != null)
                            completed();
                    }
                }
            }
        }

        private async Task<bool> Connect(TcpClient client)
        {
            try
            {
                await client.ConnectAsync(Destination.Host, Destination.Port);
            }
            catch (Exception exception)
            {
                _logger.WarnFormat("Failed to connect to {0} because {1}", Destination, exception);
                FailureToConnect(exception);
                return false;
            }

            _logger.DebugFormat("Successfully connected to {0}", Destination);
            return true;
        }
    }
}
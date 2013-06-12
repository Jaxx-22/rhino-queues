using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Transactions;
using Xunit;

namespace Rhino.Queues.Tests
{
    public class CanObserveMessages : IDisposable
    {
        private readonly QueueManager sender;
        private readonly ObservableQueue receiver;

        public CanObserveMessages()
        {
            if (Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            if (Directory.Exists("test2.esent"))
                Directory.Delete("test2.esent", true);

            sender = new QueueManager(new IPEndPoint(IPAddress.Loopback, 23456), "test.esent");
            sender.Start();

            receiver = new ObservableQueue(new IPEndPoint(IPAddress.Loopback, 23457), "test2.esent");
            receiver.CreateQueues("h", "a");
            receiver.Start();
        }

        [Fact(Skip = "Need to make transaction strategy first")]
        public void CanSendToQueue()
        {
            var handle = new ManualResetEvent(false);
            byte[] data = null;
            var observable = receiver.Receive("h", null);
            observable.Subscribe(message =>
            {
                data = message.Data;
                handle.Set();
            });
            using (var tx = new TransactionScope())
            {
                sender.Send(new Uri("rhino.queues://localhost:23457/h"),
                            new MessagePayload
                                {
                                    Data = new byte[] {1, 2, 4, 5}
                                });
                tx.Complete();
            }

            handle.WaitOne(TimeSpan.FromSeconds(10));
            Assert.Equal(new byte[] {1, 2, 4, 5}, data);
        }

        public void Dispose()
        {
            receiver.Dispose();
            sender.Dispose();
        }
    }
}
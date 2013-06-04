using System;
using System.IO;
using System.Threading.Tasks;
using Common.Logging;

namespace Rhino.Queues.Protocol.Chunks
{
    public abstract class Chunk
    {
        protected readonly ILog _logger = LogManager.GetLogger<Chunk>();
        protected readonly string _destination;

        protected Chunk(string destination = null)
        {
            _destination = destination ?? "unknown";
        }

        public async Task ProcessAsync(Stream stream)
        {
            try
            {
                _logger.DebugFormat("{0} to Destination: {1}", ToString(), _destination);
                await ProcessInternalAsync(stream);
                _logger.DebugFormat("Successfully {0} to Destination: {1}", ToString(), _destination);
            }
            catch (Exception exception)
            {
                _logger.WarnFormat("Could not process {0} for Desitination: {1}", ToString(), _destination, exception);
                throw;
            }
        }

        protected abstract Task ProcessInternalAsync(Stream stream);
    }
}
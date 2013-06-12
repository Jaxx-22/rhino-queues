using System;
using System.IO;
using System.Threading.Tasks;
using Common.Logging;

namespace Rhino.Queues.Protocol.Chunks
{
    public abstract class Chunk
    {
        protected readonly ILog _logger = LogManager.GetLogger<Chunk>();
        protected readonly string _endpoint;

        protected Chunk(string endpoint = null)
        {
            _endpoint = endpoint ?? "unknown";
        }

        public async Task ProcessAsync(Stream stream)
        {
            try
            {
                _logger.DebugFormat("{0} with Endpoint: {1}", ToString(), _endpoint);
                await ProcessInternalAsync(stream);
                _logger.DebugFormat("Successfully {0} with Endpoint: {1}", ToString(), _endpoint);
            }
            catch (Exception exception)
            {
                _logger.WarnFormat("Could not process {0} for Endpoint: {1}", ToString(), _endpoint, exception);
                throw;
            }
        }

        protected abstract Task ProcessInternalAsync(Stream stream);
    }

    public abstract class Chunk<T> : Chunk
    {
        protected Chunk(string endpoint) : base(endpoint)
        {
        }

        public async Task<T> GetAsync(Stream stream)
        {
            T retVal = default(T);
            try
            {
                _logger.DebugFormat("{0} with Endpoint: {1}", ToString(), _endpoint);
                 retVal = await GetInternalAsync(stream);
                _logger.DebugFormat("Successfully {0} with Endpoint: {1}", ToString(), _endpoint);
            }
            catch (Exception exception)
            {
                _logger.WarnFormat("Could not process {0} for Endpoint: {1}", ToString(), _endpoint, exception);
                throw;
            }
            return retVal;
        }

        protected abstract Task<T> GetInternalAsync(Stream stream);

        protected override Task ProcessInternalAsync(Stream stream)
        {
            return new Task(() => { });
        }
    }
}
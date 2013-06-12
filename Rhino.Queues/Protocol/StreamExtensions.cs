using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rhino.Queues.Protocol
{
    public static class StreamExtensions
    {
        public static async Task ReadBytesAsync(this Stream stream, byte[] buffer, string type, bool expectedToHaveNoData)
        {
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead);
               
                if(bytesRead == 0)
                {
                    if (expectedToHaveNoData)
                        break;

                    throw new InvalidOperationException("Could not read value for " + type);
                }

                totalBytesRead += bytesRead;
            }
        }
    }
}
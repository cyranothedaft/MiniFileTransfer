using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;



namespace mift;

internal class FakeClientConnection : IClientConnection {
   private readonly ILogger? _logger;

   public FakeClientConnection(ILogger? logger) {
      _logger = logger;
   }


   public async Task SendFile(FileInfo fileToSend) {
      _logger?.LogInformation("Connected client is now sending file [{fileToSend}]", fileToSend);
      await Task.Delay(TimeSpan.FromMilliseconds(150));
      _logger?.LogInformation("... send send send...");
      await Task.Delay(TimeSpan.FromMilliseconds(1150));
      _logger?.LogInformation("Done sending.");
   }

   public void Dispose() { }
}

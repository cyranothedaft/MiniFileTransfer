using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace mift;

internal class FakeServer : IServer {
   private readonly ILogger? _logger;

   public FakeServer(ILogger? logger) {
      _logger = logger;
   }


   private static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

   public async Task RunAsync(int listenOnPort, bool isReceiveFile) {
      _logger?.LogInformation("The server begins; it will end after {duration:F1} seconds.", Duration.TotalSeconds);
      await Task.Delay(Duration);
      _logger?.LogInformation("The server has now ended.");
   }
}

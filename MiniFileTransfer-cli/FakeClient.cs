using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace mift;

internal class FakeClient : IClient {
   private readonly ILogger? _logger;

   public FakeClient(ILogger? logger) {
      _logger = logger;
   }


   private static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

   public async Task RunAsync(int? listenOnPort, bool isReceiveFile) {
      _logger?.LogInformation("The client begins; it will end after {duration:F1} seconds.", Duration.TotalSeconds);
      await Task.Delay(Duration);
      _logger?.LogInformation("The client has now ended.");
   }
}

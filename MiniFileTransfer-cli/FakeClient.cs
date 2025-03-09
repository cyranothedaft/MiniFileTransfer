using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace mift;

internal class FakeClient : IClient {
   private readonly ILogger? _logger;

   public FakeClient(ILogger? logger) {
      _logger = logger;
   }


   private static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

   public async Task<IClientConnection> ConnectAsync(IPAddress connectToAddress, int connectToPort) {
      _logger?.LogInformation("The client is connected");
      await Task.Delay(TimeSpan.FromMilliseconds(150));
      return new FakeClientConnection(_logger);
   }
}

using System;
using Microsoft.Extensions.Logging;


namespace mift;

internal class SocketTransportFactory : ITransportFactory {
   public IServer BuildServer(ILogger? logger) => new SocketServer(logger);
}

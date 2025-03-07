using System;
using Microsoft.Extensions.Logging;



namespace mift;

internal class FakeTransportFactory : ITransportFactory {
   public IServer BuildServer(ILogger? logger) => new FakeServer(logger);
}

using System;
using Microsoft.Extensions.Logging;



namespace mift;

internal interface ITransportFactory {
   IServer BuildServer(ILogger? logger);
}

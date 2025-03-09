using System;
using System.Net;
using System.Threading.Tasks;



namespace mift;

internal interface IClient {
   Task<IClientConnection> ConnectAsync(IPAddress connectToAddress, int connectToPort);
}

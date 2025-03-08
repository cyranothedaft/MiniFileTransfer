using System;
using System.Net;
using System.Threading.Tasks;



namespace mift;

internal interface IClient {
   Task ConnectAsync(IPAddress? connectToAddress, int? connectToPort);
}

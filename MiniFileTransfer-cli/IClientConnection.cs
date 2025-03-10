using System;
using System.Threading.Tasks;



namespace mift;

internal interface IClientConnection : IDisposable {
   Task SendFile(string filePathToSend);
}


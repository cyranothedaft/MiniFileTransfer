using System;
using System.IO;
using System.Threading.Tasks;



namespace mift;

internal interface IClientConnection : IDisposable {
   Task SendFile(FileInfo fileToSend);
}


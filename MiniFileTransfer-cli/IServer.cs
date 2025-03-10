using System;
using System.Threading.Tasks;



namespace mift;

internal interface IServer {
   Task RunAsync(int listenOnPort, bool isReceiving
                 // , 
                 // string? receiveFileName
         );
}

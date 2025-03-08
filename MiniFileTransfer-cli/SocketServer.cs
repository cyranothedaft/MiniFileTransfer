using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;



namespace mift;

internal class SocketServer : IServer {
   private readonly ILogger? _logger;

   public SocketServer(ILogger? logger) {
      _logger = logger;
   }


   public async Task RunAsync(int listenOnPort, bool receiveFileOption) {
      // TODO: terminate after timeout (partially for security reasons)
      IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Any, listenOnPort);
      _logger?.LogDebug("Starting up listener on endpoint: {endpoint}", ipEndpoint);

      using ( Socket listener = new(ipEndpoint.AddressFamily,
                                    SocketType.Stream,
                                    ProtocolType.Tcp) ) {
         _logger?.LogTrace("Binding to endpoint...");
         listener.Bind(ipEndpoint);
         _logger?.LogTrace("Listening on endpoint...");
         listener.Listen(); // TODO: specify backLog?

         _logger?.LogTrace("Accepting...");
         Socket handler = await listener.AcceptAsync();
         _logger?.LogTrace("Accepted");
         await receiveWithHandlerAsync(handler, _logger);
      }
   }


   private static async Task receiveWithHandlerAsync(Socket socket, ILogger? logger) {
      // TODO: for flow control that inherently handles backpressure, rewrite using Reactive Streams (Akka.Sreams)
      //    (https://getakka.net/articles/streams/workingwithstreamingio.html)
      //    or RSocket


      string fileName = "received.file";
      logger?.LogTrace("Opening stream for writing file [{fileName}]",fileName);
      await using FileStream fileStream = File.OpenWrite(fileName);

      logger?.LogTrace("/-- Receiving...");
      int totalBytesReceived = 0;
      await foreach ((byte[] buffer, int size) in socket.ReceiveAsync(Program.TransferBufferSize)) {
         totalBytesReceived += size;
         logger?.LogTrace(" ->> Streaming buffer ({size,4}/{max,4}) to file: {totalBytesReceived:N0} bytes read so far",
                          size, Program.TransferBufferSize, totalBytesReceived);
         await fileStream.WriteAsync(buffer); // TODO: use AsMemory ?
      }
      logger?.LogTrace("\\-- Detected end of incoming stream");

      logger?.LogTrace("Flushing file buffer");
      await fileStream.FlushAsync();
      logger?.LogDebug("Finished receiving; total bytes received: [{totalBytesReceived:N0}]. File will be closed when disposed. Server will shut down when disposed.", totalBytesReceived);


      // IAsyncEnumerable<byte[]> iae = socket.ReceiveAsync();
      

      // handshake / receive transmission plan

      // byte[] handshakeBuffer = new byte[Program.HandshakeBufferSize];
      // bytesReceived = await socket.ReceiveAsync(handshakeBuffer, SocketFlags.None);
      // TxPlan txPlan = JsonSerializer.Deserialize<TxPlan>(handshakeBuffer[..bytesReceived])
      //              ?? throw new Exception("Failed to deserialize handshake buffer");
      //
      //
      // logger?.LogDebug("Received transmission plan: {txPlan}", txPlan);
      //
      // // send ACK / signal ready to receive transmission
      // var ackMessage = ".OK.PROCEED.";
      // var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
      // logger?.LogDebug("Sending handshake response");
      // await socket.SendAsync(echoBytes, SocketFlags.None);
      //
      // logger?.LogTrace("Opening stream for writing file [{fileName}]",txPlan.FileName);
      // await using FileStream fileStream = File.OpenWrite(txPlan.FileName);
      //
      // byte[] transferBuffer = new byte[Program.TransferBufferSize];
      // long totalBytesReceived = 0;
      // while (totalBytesReceived < txPlan.FileSize) {
      //    bytesReceived = await socket.ReceiveAsync(transferBuffer, SocketFlags.None);
      //    totalBytesReceived += bytesReceived;
      //    logger?.LogTrace("Received {bytesReceived} bytes (total read so far: {totalBytesReceived} / {pct:P1})",
      //                     bytesReceived, totalBytesReceived, (decimal)totalBytesReceived / txPlan.FileSize);
      //    logger?.LogTrace("Writing {bytesReceived} bytes to file", bytesReceived);
      //    await fileStream.WriteAsync(transferBuffer[..bytesReceived]); // TODO: use AsMemory ?
      // }
      // logger?.LogDebug("Finished receiving; total bytes received: [{totalBytesReceived:N0}]. Server will shut down when disposed.", totalBytesReceived);


      //         while (true) {
      //            // Receive message
      //            byte[] buffer = new byte[Program.BufferSize];
      //            int bytesReceived = await handler.ReceiveAsync(buffer, SocketFlags.None);
      //
      //            bool proceed = handleMessage(buffer[..bytesReceived]);
      //            if (!proceed)
      //               break;
      //
      //            // string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
      //            // var eom = "<|EOM|>";
      //            // if (response.IndexOf(eom) > -1 /* is end of message */) {
      //               Console.WriteLine($"Socket server received message: \"{Encoding.UTF8.GetString(buffer, 0, bytesReceived)}\"");
      //
      //               // var ackMessage = "<|ACK|>";
      //               // var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
      //               // await handler.SendAsync(echoBytes, 0);
      //               // Console.WriteLine($"Socket server sent acknowledgment: \"{ackMessage}\"");
      //
      //               break;
      //            // }
      //
      //            // Sample output:
      //            //    Socket server received message: "Hi friends 👋!"
      //            //    Socket server sent acknowledgment: "<|ACK|>"
      //         }


   }
}

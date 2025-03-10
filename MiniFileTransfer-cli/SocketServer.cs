using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
      IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Any, listenOnPort);

      _logger?.LogDebug("Starting up listener on endpoint: {endpoint}", ipEndpoint);
      TcpListener tpcListener = new TcpListener(ipEndpoint);
      // TODO: terminate after timeout (partially for security reasons)
      tpcListener.Start();

      TcpClient acceptedClient = await tpcListener.AcceptTcpClientAsync();

      _logger?.LogTrace("Getting client network stream");
      Socket clientSocket = acceptedClient.Client;
      await processFileDataAsync(clientSocket);

      string fileName = "received.file";
      _logger?.LogTrace("Opening stream for writing file [{fileName}]",fileName);
      await using FileStream fileStream = File.OpenWrite(fileName);

      _logger?.LogTrace("Concatenating network stream and file stream");
      await stream.CopyToAsync(fileStream);

      _logger?.LogTrace("Finished receiving - flushing and closing file stream");
      await fileStream.FlushAsync();
      fileStream.Close();

      // TODO: externalize this
      _logger?.LogInformation("File received    : {fileName}", fileName);
      byte[] checksum = SHA256.HashData(File.ReadAllBytes(fileName));
      _logger?.LogInformation("Checksum (SHA256): {checksum}", System.Convert.ToHexString(checksum));
   }


   // https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/
   private async Task processFileDataAsync(Socket socket) {
      var pipe = new Pipe();
      Task writing = FillPipeFromSocketAsync(socket, pipe.Writer);
      Task reading = ReadPipeAsync(pipe.Reader);

      return Task.WhenAll(reading, writing);
   }



   async Task FillPipeFromSocketAsync(Socket socket, PipeWriter writer) {
      const int minimumBufferSize = 512;

      while (true) {
         // Allocate at least 512 bytes from the PipeWriter
         Memory<byte> memory = writer.GetMemory(minimumBufferSize);
         try {
            int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
            if (bytesRead == 0) {
               break;
            }

            // Tell the PipeWriter how much was read from the Socket
            writer.Advance(bytesRead);
         }
         catch (Exception ex) {
            _logger?.LogError(ex, "trying to read from socket");
            break;
         }

         // Make the data available to the PipeReader
         FlushResult result = await writer.FlushAsync();

         if (result.IsCompleted) {
            break;
         }
      }

      // Tell the PipeReader that there's no more data coming
      writer.Complete();
   }


   async Task ReadPipeAsync(PipeReader reader, Action<ReadOnlySequence<byte>> processBuffer) {
      while (true) {
         ReadResult result = await reader.ReadAsync();

         ReadOnlySequence<byte> buffer = result.Buffer;

            // Process the buffer
            processBuffer(buffer);

         // Tell the PipeReader how much of the buffer we have consumed
         reader.AdvanceTo(buffer.Start, buffer.End);

         // Stop reading if there's no more data coming
         if (result.IsCompleted) {
            break;
         }
      }

      // Mark the PipeReader as complete
      reader.Complete();
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
         logger?.LogTrace(" -<-< Streaming buffer ({size,4}/{max,4}) to file: {totalBytesReceived:N0} bytes read so far",
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

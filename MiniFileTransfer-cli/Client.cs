using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;



namespace mift;

internal static class Client {
   internal static async Task SendRequestAsync(IPAddress address, int port, FileInfo fileToSend, ILogger? logger) {
      IPEndPoint ipEndPoint = new IPEndPoint(address, port);


      using ( Socket client = new(ipEndPoint.AddressFamily,
                                  SocketType.Stream,
                                  ProtocolType.Tcp) ) {
         logger?.LogDebug("Connecting to endpoint: {ipEndPoint}", ipEndPoint);
         await client.ConnectAsync(ipEndPoint);

         TxPlan txPlan = new(fileToSend.Name,
                             fileToSend.Length);
         logger?.LogDebug("Sending handshake / transmission plan: {txPlan}", txPlan);
         await client.SendAsync(JsonSerializer.SerializeToUtf8Bytes(txPlan));

         logger?.LogDebug("Awaiting handshake response");
         byte[] buffer = new byte[Program.HandshakeBufferSize];
         int bytesReceived = await client.ReceiveAsync(buffer);
         string handshakeResponse = Encoding.UTF8.GetString(buffer[..bytesReceived]);
         if (handshakeResponse != ".OK.PROCEED.") {
            throw new Exception($"Invalid response received: [{handshakeResponse}]");
         }
         logger?.LogDebug("Received handshake response - sending file");

         // TODO: ?
         // await client.SendFileAsync(fileToSend.FullName);

         byte[] transferBuffer = new byte[Program.TransferBufferSize];
         long totalBytesSent = 0;
         logger?.LogTrace("Opening stream for file to send");
         await using ( FileStream fileStream = fileToSend.OpenRead() ) {
            long totalBytesRead = 0;
            do {
               int fileBytesRead = await fileStream.ReadAsync(transferBuffer);
               totalBytesRead += fileBytesRead;
               logger?.LogTrace("Read next {fileBytesRead} bytes from file (file pos: {pos}, total read so far: {totalBytesRead} / {pct:P1})",
                                fileBytesRead, fileStream.Position, totalBytesRead, (decimal)totalBytesRead / txPlan.FileSize);
               logger?.LogTrace("Sending {fileBytesRead} bytes of buffer", fileBytesRead);
               int bytesSent = await client.SendAsync(transferBuffer[..fileBytesRead]);
               totalBytesSent += bytesSent;
               logger?.LogTrace("Sent {bytesSent} bytes ({totalBytesSent} total so far)", bytesSent, totalBytesSent);
            } while (totalBytesRead < fileToSend.Length);

            logger?.LogTrace("Closing file stream");
            fileStream.Close();
         }
         logger?.LogDebug("Total bytes sent: {totalBytesSent:N0}", totalBytesSent);


         // byte[] sendFileBytes = await File.ReadAllBytesAsync(fileToSend.FullName);
         // for (int i = 0; i < sendFileBytes.Length; i += Program.BufferSize) 
         //    await client.SendAsync(sendFileBytes[i..(i + Program.BufferSize)], SocketFlags.None);

         // // Receive ack.
         // var buffer = new byte[1_024];
         // var received = await client.ReceiveAsync(buffer, SocketFlags.None);
         // var response = Encoding.UTF8.GetString(buffer, 0, received);
         // if (response == "<|ACK|>") {
         //    Console.WriteLine(
         //                      $"Socket client received acknowledgment: \"{response}\"");
         //    break;
         // }

         logger?.LogDebug("Shutting down client");
         client.Shutdown(SocketShutdown.Both);
      }
   }

}

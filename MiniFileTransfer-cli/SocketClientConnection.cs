using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;



namespace mift;

internal class SocketClientConnection : IClientConnection {
   private readonly Socket _client;
   private readonly ILogger? _logger;


   public SocketClientConnection(Socket client, ILogger? logger) {
      _client = client;
      _logger = logger;
   }


   public async Task SendFile(FileInfo fileToSend) {
      long fileSize = fileToSend.Length;
      _logger?.LogDebug("File size is: {fileSize}", fileSize);

      _logger?.LogTrace("/-- Sending...");

      byte[] transferBuffer = new byte[Program.TransferBufferSize];
      long totalBytesSent = 0;
      _logger?.LogTrace("Opening stream for file to send");
      await using ( FileStream fileStream = fileToSend.OpenRead() ) {
         long totalBytesRead = 0;
         do {
            int fileBytesRead = await fileStream.ReadAsync(transferBuffer);
            totalBytesRead += fileBytesRead;
            _logger?.LogTrace("Read next {fileBytesRead} bytes from file (file pos: {pos}, total read so far: {totalBytesRead} / {pct:P1})",
                              fileBytesRead, fileStream.Position, totalBytesRead, (decimal)totalBytesRead / fileSize);
            _logger?.LogTrace(" <-<- Sending {fileBytesRead} bytes of buffer", fileBytesRead);
            int bytesSent = await _client.SendAsync(transferBuffer[..fileBytesRead]);
            totalBytesSent += bytesSent;
            _logger?.LogTrace("Sent {bytesSent} bytes ({totalBytesSent} total so far)", bytesSent, totalBytesSent);
         } while (totalBytesRead < fileToSend.Length);

         _logger?.LogTrace("Closing file stream");
         fileStream.Close();
      }

      _logger?.LogTrace(" <<- Sent {totalBytesSent:N0} bytes", totalBytesSent);
      _logger?.LogTrace("\\-- Sent.");
   }


   public void Dispose() {
      _logger?.LogDebug("Shutting down client");
      _client.Shutdown(SocketShutdown.Both);
      _client.Dispose();
   }
}

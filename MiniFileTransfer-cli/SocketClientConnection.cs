using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;



namespace mift;

internal class SocketClientConnection : IClientConnection {
   private readonly TcpClient _client;
   private readonly ILogger? _logger;


   public SocketClientConnection(TcpClient client, ILogger? logger) {
      _client = client;
      _logger = logger;
   }


   public async Task SendFile(string filePathToSend) {
      FileInfo fileToSend = new FileInfo(filePathToSend);
      if (!fileToSend.Exists) throw new Exception($"File [{fileToSend}] doesn't exist.");

      long fileSize = fileToSend.Length;
      _logger?.LogDebug("File size is: {fileSize}", fileSize);

      _logger?.LogTrace("Getting TCP client network stream");
      NetworkStream networkStream = _client.GetStream();

      _logger?.LogTrace("Opening stream for file to send");
      await using ( FileStream fileStream = fileToSend.OpenRead() ) {
         _logger?.LogTrace("Concatenating file-read stream to network-write stream");
         await fileStream.CopyToAsync(networkStream);

         _logger?.LogTrace("Closing file stream");
         fileStream.Close();
      }

      // TODO: externalize this
      _logger?.LogInformation("File sent        : {fileName}", fileToSend.Name);
      byte[] checksum = SHA256.HashData(File.ReadAllBytes(fileToSend.FullName));
      _logger?.LogInformation("Checksum (SHA256): {checksum}", System.Convert.ToHexString(checksum));
   }


   public void Dispose() {
      _logger?.LogDebug("Shutting down client - this also signals to the receiver that the transfer is complete");
      _client.Close(); //.Shutdown(SocketShutdown.Both);
      _client.Dispose();
   }
}

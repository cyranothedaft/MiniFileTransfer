using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;



namespace mift;

internal class FileReadWrite {
   internal static async Task ReceiveFile(NetworkStream networkStream, string receiveFileName, ILogger? logger) {
      logger?.LogInformation("Receiving file [{fileName}] over network stream", receiveFileName);

      logger?.LogTrace("Opening stream for writing file [{fileName}]", receiveFileName);
      await using ( FileStream fileStream = File.OpenWrite(receiveFileName) ) {
         logger?.LogTrace("Concatenating network-read stream and file-write stream - copying to the end");
         await networkStream.CopyToAsync(fileStream);

         logger?.LogTrace("Finished receiving - flushing and closing file stream");
         await fileStream.FlushAsync();
         fileStream.Close();
      }

      // TODO: externalize this
      logger?.LogInformation("File received    : {fileName}", receiveFileName);
      byte[] checksum = SHA256.HashData(File.ReadAllBytes(receiveFileName));
      logger?.LogInformation("Checksum (SHA256): {checksum}", System.Convert.ToHexString(checksum));
   }
}

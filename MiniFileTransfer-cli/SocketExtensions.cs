using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;



namespace mift;

internal static class SocketExtensions {
   // TODO: optimize?
   public static async IAsyncEnumerable<(byte[] data,int size)> ReceiveAsync(this Socket socket, int bufferSize) {
      byte[] buffer = new byte[bufferSize];
      int bytesRead;
      do {
         bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
         if (bytesRead > 0)
            yield return (buffer[..bytesRead], bytesRead );
      } while (bytesRead > 0);
   }


}
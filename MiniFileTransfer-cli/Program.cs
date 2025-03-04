using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;



namespace mift;

internal class Program {

   public const int DefaultPort = 9099;
   public const int HandshakeBufferSize = 1_024;
   public const int TransferBufferSize = 1_024;


   // examples:
   // # mift --upon-request --on-port 1999 --send-file path\to\file
   // # mift --upon-request --receive-file
   // mift await --on-port 1999 --send-file path\to\file
   // mift await --receive-file
   // mift now --send-file path\to\file --via-listener-at 192.168.7.17:1999
   // mift now --receive-file --via-listener-at 192.168.7.17:1999


   private static async Task Main(string[] args) {

      // TODO: gracefully handle ctrl-c via Console.CancelKeyPress event
      // TODO: also AppDomain.CurrentDomain.ProcessExit ?
      // (see https://medium.com/@rainer_8955/gracefully-shutdown-c-apps-2e9711215f6d)


      LogLevel minimumLogLevel = LogLevel.Trace;
      using ILoggerFactory loggerFactory = buildLoggerFactory(minimumLogLevel);
      ILogger logger = loggerFactory.CreateLogger("");

      // TODO: ? decide on a default port for this application, instead of using random ports (or somehow allow for both options)

      await new RootCommand("Send/receive single file over TCP/IP socket")
           .WithGlobalOption(new Option<int?>("--on-port"),
                             out Option<int?> portOption)
           .WithCommand(new Command("wait", "\"server\" mode - await incoming request from \"client\"")
                             .WithAsyncHandler(handleWaitAsync, portOption))
           .WithCommand(new Command("now", "\"client\" mode - instantly send request to specified \"server\"")
                       .WithOption(new Option<FileInfo>("--send-file"),
                                   out Option<FileInfo> sendFileOption)
                       .WithOption(new Option<IPAddress>("--via-listener-at")
                                        .WithAliases("--from", "--to"),
                                   out Option<IPAddress> addressOption)
                       .WithAsyncHandler(handleNowAsync, addressOption, portOption, sendFileOption))

            // .WithGlobalOption(new Option<LogLevel>("-v").WithAlias("--verbosity"),
            //                   out Option<LogLevel> verbosityOption)
            // .WithOption(new Option<string>("--for").WithAlias("--for-userid")
            //                                        .WithRequired(true),
            //             out Option<string> userIdOption)
            // .WithOption(new Option<string>("--of").WithAlias("--of-path")
            //                                       .WithRequired(true),
            //             out Option<string> pathOption)
            // .WithHandler(handleGetEffectivePerms, verbosityOption, userIdOption, pathOption)
           .InvokeAsync(args); // TODO: if loglevel is Trace, display args and their interpretation

      Task handleWaitAsync(int? port) => handleServerAsync(logger, port);
      Task handleNowAsync(IPAddress? address, int? port, FileInfo fileToSend) => handleClientAsync(logger, address, port, fileToSend);
   }


   private static async Task<int> handleServerAsync(ILogger? logger, int? listenOnPort) {
      using ( logger?.BeginScope("<server>") ) {
         IPAddress? listenOnAddress = null;
         logger?.LogDebug("Given IP address: {address}, Port: {port}",
                          listenOnAddress is null ? "(unspecified)" : $"[{listenOnAddress}]",
                          listenOnPort is null ? "(unspecified)" : $"[{listenOnPort}]");
         IPAddress address = listenOnAddress ?? selectListeningAddress(logger);
         int       port    = listenOnPort    ?? selectDefaultPort(logger);
         await Server.RunAsync(address, port, logger);
         return 0;
      }
   }


   private static async Task<int> handleClientAsync(ILogger? logger, IPAddress? connectToAddress, int? connectToPort, FileInfo fileToSend) {
      using ( logger?.BeginScope("<client>") ) {
         logger?.LogDebug("Given IP address: {address}, Port: {port}",
                          connectToAddress is null ? "(unspecified)" : $"[{connectToAddress}]",
                          connectToPort is null ? "(unspecified)" : $"[{connectToPort}]");
         IPAddress address = connectToAddress ?? selectRemoteAddress(logger);
         int       port    = connectToPort    ?? selectDefaultPort(logger);
         await Client.SendRequestAsync(address, port, fileToSend, logger);
         return 0;
      }
   }


   private static IPAddress selectListeningAddress(ILogger? logger) {
      IPAddress address = IPAddress.Loopback;
      logger?.LogDebug("Auto-selected listening address: {address}", address);
      return address;
   }


   private static IPAddress selectRemoteAddress(ILogger? logger) {
      IPAddress address = IPAddress.Loopback;
      logger?.LogDebug("Auto-selected remote address: {address}", address);
      return address;
   }


   private static int selectRandomUnusedPort(ILogger? logger) {
      throw new NotImplementedException();
      // TODO
      // logger?.LogDebug("Auto-selected random unused port: {port}");
      // return port;
   }


   private static int selectDefaultPort(ILogger? logger) {
      int port = DefaultPort;
      logger?.LogDebug("Auto-selected default port: {port}", port);
      return port;
   }


   private static ILoggerFactory buildLoggerFactory(LogLevel minimumLogLevel) => LoggerFactory.Create(builder =>
                                                                                                            builder.AddSimpleConsole(options => {
                                                                                                                                        options.IncludeScopes   = true;
                                                                                                                                        options.SingleLine      = true;
                                                                                                                                        options.TimestampFormat = "HH:mm:ss.ffffff ";
                                                                                                                                        options.ColorBehavior   = LoggerColorBehavior.Enabled;
                                                                                                                                     })
                                                                                                                   .SetMinimumLevel(minimumLogLevel));
}

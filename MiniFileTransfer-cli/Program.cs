using System;
using System.CommandLine;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using mift.CommandLineExtensions;


namespace mift;

internal class Program {

   public const int DefaultPort = 9099;
   public const string DefaultReceiveFileName = "received.file";


   // examples:
   // # mift --upon-request --on-port 1999 --send-file path\to\file
   // # mift --upon-request --receive-file

   // mift now --send-file path\to\file --via-listener-at 192.168.7.17:1999
   // mift now --receive-file --via-listener-at 192.168.7.17:1999
   // mift now --send 

   // mift await                             <-- (server) waits then receives unnamed file on default port
   // mift await --receive                   <-- (server) waits then receives unnamed file on default port
   // mift await --on-port 1999 --receive    <-- (server) waits then receives unnamed file on port 1999
   // mift now --file file.name --to localhost  <-- (client) immediately sends file named "file.name" to default port on localhost
   // mift now --send --file                        <-- (client) immediately sends file named "file.name" to default port


   private static async Task Main(string[] args) {

      // TODO: gracefully handle ctrl-c via Console.CancelKeyPress event
      // TODO: also AppDomain.CurrentDomain.ProcessExit ?
      // (see https://medium.com/@rainer_8955/gracefully-shutdown-c-apps-2e9711215f6d)

      // TODO: ? decide on a default port for this application, instead of using random ports (or somehow allow for both options)

      await new RootCommand("Send/receive single file over TCP/IP socket")
           .WithGlobalOption(new Option<bool>("--fake")
                                  .WithIsHidden(true),
                             out Option<bool> useFakeTransportOption)
           .WithGlobalOption(new Option<int?>("--on-port"),
                             out Option<int?> portOption)
           .WithGlobalOption(new Option<bool>("--receive"),
                             out Option<bool> isReceivingOption)
           .WithGlobalOption(new Option<bool>("--send"),   
                             out Option<bool> isSendingOption)
           .WithCommand(new Command("await", "\"server\" mode - await incoming request from \"client\"")
                       .WithHandler(handleAwaitCommandAsync, useFakeTransportOption, portOption, isReceivingOption))
           .WithCommand(new Command("now", "\"client\" mode - instantly send request to specified \"server\"")
                       .WithOption(new Option<IPAddress>("--via-listener-at")
                                   .WithAliases("--from", "--to")
                                   // TODO: .WithAliases("--from", "--to")
                                   ,
                                   out Option<IPAddress> addressOption)
           
                        .WithHandler(handleNowCommandAsync, useFakeTransportOption, addressOption, portOption, 
                       isReceivingOption, isSendingOption
                                     )
                       )


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

      Task handleAwaitCommandAsync(bool useFake, int? port, bool isReceiving
                                   // string? receiveFileName
            )
         => runAsync(useFake, (logger, transportFactory) => runServerAsync(logger, transportFactory.BuildServer,
                                                                           port, isReceiving
                                                                           // receiveFileName
                                                                           ));

      Task handleNowCommandAsync(bool useFake, IPAddress address, int? port, 
                                 bool isReceiving,bool  isSending
                                 // ,
                                 // string? receiveFileName
            )
         => runAsync(useFake, (logger, transportFactory) => runClientAsync(logger, transportFactory.BuildClient,
                                                                           address, port));

      async Task runAsync(bool useFake, Func<ILogger?, ITransportFactory, Task> handleCommandAsync) {
         LogLevel minimumLogLevel = LogLevel.Trace;
         using ILoggerFactory loggerFactory = buildLoggerFactory(minimumLogLevel);

         await handleCommandAsync(loggerFactory.CreateLogger(""),
                                  useFake ? new FakeTransportFactory()
                                          : new SocketTransportFactory()
                                 );
      }
   }


   private static async Task<int> runServerAsync(ILogger? logger, Func<ILogger?, IServer> buildServer,
                                                 int? listenOnPort, bool isReceiving
                                                 // string? receiveFileName
         ) {
      using ( logger?.BeginScope("[server]") ) {
         logger?.LogDebug("Preparing server:  listenOnPort({port}), isReceiving({isReceiving})",
                          listenOnPort    is null ? "<unspecified>" : listenOnPort,
                          isReceiving
                          // receiveFileName is null ? "<unspecified>" : receiveFileName
                          );

         IServer server = buildServer(logger);
         logger?.LogTrace("Instantiated server ({type})", server.GetType().Name);

         int port = listenOnPort ?? selectDefaultPort(logger);
         // string fileName = receiveFileName ?? selectDefaultFileName(logger);

         logger?.LogInformation("Receiving file (??) on port {port}", port);
         await server.RunAsync(port, isReceiving);

         return 0;
      }
   }


   private static async Task<int> runClientAsync(ILogger? logger, Func<ILogger?, IClient> buildClient,
                                                 IPAddress? connectToAddress, int? connectToPort, 
                                                 string? filePathToSend) {
      using ( logger?.BeginScope("[client]") ) {
         logger?.LogDebug("Preparing client:  connectToAddress({address}), connectToPort({port}), isReceiving({isReceiving}), fileToSend({fileToSend})",
                          connectToAddress is null ? "<unspecified>" : connectToAddress,
                          connectToPort    is null ? "<unspecified>" : connectToPort,
                          filePathToSend   is null ? "<unspecified>" : filePathToSend);

         IClient client = buildClient(logger);
         logger?.LogTrace("Instantiated client ({type})", client.GetType().Name);

         IPAddress address = connectToAddress ?? selectRemoteAddress(logger);
         int port = connectToPort             ?? selectDefaultPort(logger);

         using ( IClientConnection connectedClient = await client.ConnectAsync(address, port) )
            await connectedClient.SendFile(filePathToSend);

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


   private static string selectDefaultFileName(ILogger? logger) {
      string fileName = DefaultReceiveFileName;
      logger?.LogDebug("Auto-selected receive filename: {fileName}", fileName);
      return fileName;
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

﻿using System;
using System.CommandLine;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using mift.Extensions;


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

      // TODO: ? decide on a default port for this application, instead of using random ports (or somehow allow for both options)

      await new RootCommand("Send/receive single file over TCP/IP socket")
           .WithGlobalOption(new Option<bool>("--fake")
                                  .WithIsHidden(true),
                             out Option<bool> useFakeTransportOption)
           .WithGlobalOption(new Option<int?>("--on-port"),
                             out Option<int?> portOption)
           .WithGlobalOption(new Option<bool>("--receive-file"),
                             out Option<bool> receivingFileOption)
           .WithCommand(new Command("await", "\"server\" mode - await incoming request from \"client\"")
                       // .WithOption(new Option<bool>("--receive-file"),
                       //             out Option<bool> receivingFileOption)
                       .WithHandler(handleAwaitCommandAsync, useFakeTransportOption, portOption, receivingFileOption))
           .WithCommand(new Command("now", "\"client\" mode - instantly send request to specified \"server\"")
                       .WithOption(new Option<FileInfo>("--send-file"),
                                   out Option<FileInfo> sendFileOption)
                       .WithOption(new Option<IPAddress>("--via-listener-at")

                                   // TODO: .WithAliases("--from", "--to")
                                   ,
                                   out Option<IPAddress> addressOption)

                        .WithHandler(handleNowCommandAsync, useFakeTransportOption, addressOption, portOption, 
                        receivingFileOption
                                     // sendFileOption
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

      Task handleAwaitCommandAsync(bool useFake, int? port, bool isReceiving)
         => runAsync(useFake, (logger, transportFactory) => runServerAsync(logger, transportFactory.BuildServer, port, isReceiving));

      Task handleNowCommandAsync(bool useFake, IPAddress? address, int? port, bool isReceiving)
         => runAsync(useFake, (logger, transportFactory) => runClientAsync(logger, 
                                                                           // transportFactory.BuildClient, 
                                                                           address, port, isReceiving));


      async Task runAsync(bool useFake, Func<ILogger?, ITransportFactory, Task> handleCommandAsync) {
         LogLevel minimumLogLevel = LogLevel.Trace;
         using ILoggerFactory loggerFactory = buildLoggerFactory(minimumLogLevel);

         await handleCommandAsync(loggerFactory.CreateLogger(""),
                                  useFake ? new FakeTransportFactory()
                                          : new SocketTransportFactory());
      }
   }


   private static async Task<int> runServerAsync(ILogger? logger, Func<ILogger?, IServer> buildServer,
                                                 int? listenOnPort, bool isReceiving) {
      using ( logger?.BeginScope("[server]") ) {
         logger?.LogDebug("Preparing server:  listenOnPort({port}), isReceiving({isReceiving})",
                          listenOnPort is null ? "<unspecified>" : listenOnPort,
                          isReceiving);

         IServer server = buildServer(logger);
         logger?.LogTrace("Instantiated server ({type})", server.GetType().Name);

         await server.RunAsync(listenOnPort, isReceiving);

         // int? listenOnPort
         // IPAddress? listenOnAddress = null;
         // logger?.LogDebug("Given IP address: {address}, Port: {port}",
         //                  listenOnAddress is null ? "(unspecified)" : $"[{listenOnAddress}]",
         //                  listenOnPort is null ? "(unspecified)" : $"[{listenOnPort}]");
         // IPAddress address = listenOnAddress ?? selectListeningAddress(logger);
         // int       port    = listenOnPort    ?? selectDefaultPort(logger);
         // await SocketServer.RunAsync(address, port, logger);

         return 0;
      }
   }


   private static async Task<int> runClientAsync(ILogger? logger, Func<ILogger?, IClient> buildClient,
                                                 IPAddress? connectToAddress, int? connectToPort, 
                                                 bool isReceiving
                                                 // FileInfo fileToSend
                                                       ) {
      using ( logger?.BeginScope("[client]") ) {
         logger?.LogDebug("Preparing client:  connectToAddress({address}), connectToPort({port}), isReceiving({isReceiving})",
                          connectToAddress is null ? "<unspecified>" : connectToAddress,
                          connectToPort    is null ? "<unspecified>" : connectToPort,
                          isReceiving);

         IClient client = buildClient(logger);
         logger?.LogTrace("Instantiated client ({type})", client.GetType().Name);

         await client.ConnectAsync(connectToAddress, connectToPort);
//         IPAddress address = connectToAddress ?? selectRemoteAddress(logger);
//         int       port    = connectToPort    ?? selectDefaultPort(logger);
//         await SocketClient.SendRequestAsync(address, port, fileToSend, logger);
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

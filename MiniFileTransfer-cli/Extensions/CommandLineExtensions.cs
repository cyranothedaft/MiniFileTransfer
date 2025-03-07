using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Threading.Tasks;


namespace mift.Extensions;


public static class CommandLineExtensions {
   public static TCommand WithCommand<TCommand, TSubCommand>(this TCommand command,
                                                             TSubCommand subCommand)
         where TCommand : Command
         where TSubCommand : Command {
      command.AddCommand(subCommand);
      return command;
   }


   public static TCommand WithHandler<TCommand, V1        >(this TCommand command, Func<V1        , Task> handler, IValueDescriptor<V1> value1                                                          ) where TCommand : Command => doWithAndReturn(command, c => { c.SetHandler(handler, value1                ); });
   public static TCommand WithHandler<TCommand, V1, V2    >(this TCommand command, Func<V1, V2    , Task> handler, IValueDescriptor<V1> value1, IValueDescriptor<V2> value2                             ) where TCommand : Command => doWithAndReturn(command, c => { c.SetHandler(handler, value1, value2        ); });
   public static TCommand WithHandler<TCommand, V1, V2, V3>(this TCommand command, Func<V1, V2, V3, Task> handler, IValueDescriptor<V1> value1, IValueDescriptor<V2> value2, IValueDescriptor<V3> value3) where TCommand : Command => doWithAndReturn(command, c => { c.SetHandler(handler, value1, value2, value3); });


   public static TCommand WithGlobalOption<TCommand, TOption>(this TCommand command,
                                                              TOption globalOption,
                                                              out TOption passBack)
         where TCommand : Command
         where TOption : Option {
      command.AddGlobalOption(globalOption);
      passBack = globalOption;
      return command;
   }


   public static TCommand WithOption<TCommand, TOption>(this TCommand command,
                                                        TOption Option,
                                                        out TOption passBack)
         where TCommand : Command
         where TOption : Option {
      command.AddOption(Option);
      passBack = Option;
      return command;
   }


   private static T doWithAndReturn<T>(T obj, Action<T> action) {
      action(obj);
      return obj;
   }
}

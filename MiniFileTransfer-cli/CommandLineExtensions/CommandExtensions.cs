using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using mift.CommandLineExtensions;


namespace mift.Extensions;


public static class CommandExtensions {
   public static TCommand WithCommand<TCommand, TSubCommand>(this TCommand command,
                                                             TSubCommand subCommand)
         where TCommand : Command
         where TSubCommand : Command
      => command.DoAndReturn(c => { c.AddCommand(subCommand); });


   public static TCommand WithHandler<TCommand, V1            >(this TCommand command, Func<V1            , Task> handler, IValueDescriptor<V1> value1                                                                                       ) where TCommand : Command => command.DoAndReturn(c => { c.SetHandler(handler, value1                        ); });
   public static TCommand WithHandler<TCommand, V1, V2        >(this TCommand command, Func<V1, V2        , Task> handler, IValueDescriptor<V1> value1, IValueDescriptor<V2> value2                                                          ) where TCommand : Command => command.DoAndReturn(c => { c.SetHandler(handler, value1, value2                ); });
   public static TCommand WithHandler<TCommand, V1, V2, V3    >(this TCommand command, Func<V1, V2, V3    , Task> handler, IValueDescriptor<V1> value1, IValueDescriptor<V2> value2, IValueDescriptor<V3> value3                             ) where TCommand : Command => command.DoAndReturn(c => { c.SetHandler(handler, value1, value2, value3        ); });
   public static TCommand WithHandler<TCommand, V1, V2, V3, V4>(this TCommand command, Func<V1, V2, V3, V4, Task> handler, IValueDescriptor<V1> value1, IValueDescriptor<V2> value2, IValueDescriptor<V3> value3, IValueDescriptor<V4> value4) where TCommand : Command => command.DoAndReturn(c => { c.SetHandler(handler, value1, value2, value3, value4); });


   public static TCommand WithGlobalOption<TCommand, TOption>(this TCommand command,
                                                              TOption globalOption,
                                                              out TOption passBack)
         where TCommand : Command
         where TOption : Option {
      passBack = globalOption;
      return command.DoAndReturn(c => { c.AddGlobalOption(globalOption); });
   }


   public static TCommand WithOption<TCommand, TOption>(this TCommand command,
                                                        TOption Option,
                                                        out TOption passBack)
         where TCommand : Command
         where TOption : Option {
      passBack = Option;
      return command.DoAndReturn(c => { c.AddOption(Option); });
   }

}

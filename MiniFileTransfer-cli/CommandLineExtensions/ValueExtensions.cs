using System;
using System.Collections.Generic;
using System.CommandLine;


namespace mift.CommandLineExtensions;


public static class ValueExtensions {
   public static TOption WithAliases<TOption>(this TOption option, params IEnumerable<string> aliases) where TOption : Option
      => option.DoAndReturn(o => {
                               foreach (string alias in aliases) {
                                  o.AddAlias(alias);
                               }
                            });


   public static TOption WithIsHidden<TOption>(this TOption option, bool isHidden) where TOption : Option
      => option.DoAndReturn(o => { o.IsHidden = isHidden; });


   public static TOption WithIsRequired<TOption>(this TOption option, bool isRequired) where TOption : Option
      => option.DoAndReturn(o => { o.IsRequired = isRequired; });
}

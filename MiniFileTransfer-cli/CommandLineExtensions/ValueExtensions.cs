using System;
using System.CommandLine;
using mift.CommandLineExtensions;


namespace mift.Extensions;


public static class ValueExtensions {
   public static TOption WithIsHidden<TOption>(this TOption option, bool isHidden) where TOption : Option
      => option.DoAndReturn(o => { o.IsHidden = isHidden; });
}

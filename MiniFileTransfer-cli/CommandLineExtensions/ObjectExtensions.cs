using System;

namespace mift.CommandLineExtensions;

public static class ObjectExtensions {
   public static T DoAndReturn<T>(this T obj, Action<T> action) {
      action(obj);
      return obj;
   }
}

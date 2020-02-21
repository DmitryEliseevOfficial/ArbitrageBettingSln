using System;
using System.Collections.Generic;
using System.Linq;

namespace ABServer
{
    public static class Helpers
    {
        public static string GetRnd(this IList<string> source)
        {
            if (!source.Any())
                throw new ArgumentException("source.Count must be > 0");
            var max = source.Count() - 1;
            var i = new Random().Next(0, max);

            return source[i];
           
        }
    }
}

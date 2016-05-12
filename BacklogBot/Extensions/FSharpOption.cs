using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BacklogBot.Extensions
{
    public static class FSharpOption
    {
        public static bool IsSome<T>(this FSharpOption<T> opt)
        {
            return FSharpOption<T>.get_IsSome(opt);
        }
        public static bool IsNone<T>(this FSharpOption<T> opt)
        {
            return FSharpOption<T>.get_IsNone(opt);
        }
    }
}
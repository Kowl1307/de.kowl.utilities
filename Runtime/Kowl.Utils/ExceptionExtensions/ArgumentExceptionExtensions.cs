using System;
using System.Numerics;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.ExceptionExtensions
{
    public static class ArgumentExceptionExtensions
    {
        const string ExceptionMessage = "Value cannot be zero!";

        public static void ThrowIfDefault<T>(T value)
        {
            if(value.Equals(default))
            {
                throw new ArgumentException(ExceptionMessage, nameof(value));
            }
        }
    }
}

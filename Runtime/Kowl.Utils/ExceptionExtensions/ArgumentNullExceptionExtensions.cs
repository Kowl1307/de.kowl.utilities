using System.Runtime.CompilerServices;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.ExceptionExtensions
{
    public static class ArgumentNullExceptionExtensions
    {
        public static void ThrowIfNull(object value)
        {
            if(value is null)
            {
                throw new System.ArgumentNullException();
            }
        }
    }
}

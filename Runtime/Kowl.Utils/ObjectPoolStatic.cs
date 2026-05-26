using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils
{
    /// <summary>
    /// An Object pool for any type. Rented objects may have any set values!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObjectPoolStatic<T> where T : class
    {
        private static readonly Stack<T> objects = new();

        public static T Rent()
        {
            if(objects.TryPop(out var obj))
            {
                return obj;
            }

            return Activator.CreateInstance<T>();
        }

        public static T Rent(params object[] args)
        {
            if (objects.TryPop(out var obj))
            {
                return obj;
            }

            try
            {
                // Unity-Structs brauchen BindingFlags
                return (T)Activator.CreateInstance(
                    typeof(T),
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    args,
                    null)!;
            }
            catch (MissingMethodException ex)
            {
                throw new InvalidOperationException($"Pool Rent failed: {ex.Message}\nArgs: [{string.Join(", ", args.Select(a => a?.GetType().Name ?? "null"))}]");
                throw;
            }
        }

        public static void Return(T instance)
        {
            objects.Push(instance);
        }
    }
}

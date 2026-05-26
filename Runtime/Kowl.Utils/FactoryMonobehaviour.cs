using UnityEngine;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils
{
    public abstract class FactoryMonobehaviour<T> : MonoBehaviour where T : class
    {
        public abstract T ProduceObject();
    }
}

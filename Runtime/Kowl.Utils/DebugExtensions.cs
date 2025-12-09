using UnityEngine;

namespace Kowl.Utils
{
    public static class DebugExtensions
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(this MonoBehaviour obj, object message)
        {
            Debug.Log(message, obj);
        }
    }
}
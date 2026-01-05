using System.Numerics;
using UnityEngine;

namespace Kowl.Utils
{
    public static class VectorExtensions
    {
        public static float ManhattenDistance(this UnityEngine.Vector3 vec)
        {
            return vec.x + vec.y + vec.z;
        }

        public static Vector3Int RoundDown(this UnityEngine.Vector3 vec)
        {
            return new Vector3Int
            {
                x = (int)vec.x,
                y = (int)vec.y,
                z = (int)vec.z
            };
        }
    }
}
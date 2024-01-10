using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEEP.Utils
{
    public static class CustomGravity
    {
        static List<GravitySource> sources = new List<GravitySource>();

        public static int GravitySourceCount => sources.Count;

        public static void Register(GravitySource source)
        {
            Debug.Assert(
                !sources.Contains(source),
                "Duplicate registration of gravity source!", source
            );
            sources.Add(source);
        }

        public static void Unregister(GravitySource source)
        {
            Debug.Assert(
                sources.Contains(source),
                "Unregistration of unknown gravity source!", source
            );
            sources.Remove(source);
        }

        public static Vector3 GetGravity(Vector3 position)
        {
            return sources.Aggregate(Vector3.zero, (current, t) => current + t.GetGravity(position));
        }

        public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
        {
            upAxis = GetUpAxis(position);
            return GetGravity(position);
        }

        public static Vector3 GetUpAxis(Vector3 position)
        {
            return -GetGravity(position).normalized;
        }
    }
}
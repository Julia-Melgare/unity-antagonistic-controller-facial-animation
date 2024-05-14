using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    public static class RayExt
    {
        /// <summary>
        /// Returns the closest point along this ray to another point
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="point">The referenced closest point to find along this ray</param>
        /// <returns></returns>
        public static Vector3 ClosestPointAlongRay(this Ray ray, Vector3 point)
        {
            Vector3 a = point - ray.origin;
            Vector3 b = ray.direction * a.magnitude;

            float dot = (a.x * b.x + a.y * b.y + a.z * b.z) / (b.x * b.x + b.y * b.y + b.z * b.z);

            if (dot < 0.0f)
            {
                dot = 0.0f;
            }

            return dot * b + ray.origin;
        }

        /// <summary>
        /// Returns the closest point along this ray to another point
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="point">The referenced closest point to find along this ray</param>
        /// <param name="distance">The distance along the ray where the point resides</param>
        /// <returns></returns>
        public static Vector3 ClosestPointAlongRay(this Ray ray, Vector3 point, out float distance)
        {
            point = ray.ClosestPointAlongRay(point);

            distance = Vector3.Distance(ray.origin, point);
            return point;
        }
    }
}



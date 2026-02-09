using System.Collections.Generic;
using UnityEngine;

namespace ContentContent
{
    public struct Circle2D
    {
        public Vector2 center;
        public float radius;

        public static Circle2D zero => new() { center = Vector2.zero, radius = 0 };
    }

    public static class MathUtils
    {
        /// <summary>
        /// Converts a 2D direction vector into a Quaternion rotation for top-down gameplay.
        /// Assumes the sprite/transform faces Right (Positive X) at 0 degrees.
        /// </summary>
        /// <param name="direction">The movement or aim direction.</param>
        /// <returns>A Quaternion representing the rotation on the Z-axis.</returns>
        public static Quaternion DirectionToRotation2D(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                return Quaternion.identity;
            }

            float degree = DirectionToAngleDegree(direction);

            // Apply to the Z-axis for 2D orientation
            return Quaternion.Euler(0, 0, degree);
        }
        
        /// <summary>
        /// Calculates the angle in degrees for a direction vector.
        /// Useful for UI or logic that only needs the float value.
        /// </summary>
        public static float DirectionToAngleDegree(Vector2 direction)
        {
            // Atan2 returns the angle in radians between the x-axis and the vector.
            // Result is in the range [-pi, pi].
            // We multiply by Rad2Deg to convert to degree
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        public static Vector2 RadianToDirection(float radian)
        {
            return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        }

        public static Vector2 DegreeToVector2(float degree)
        {
            return RadianToDirection(degree * Mathf.Deg2Rad);
        }
        
        public static float Wrap360(float degree)
        {
            return degree >= 0 ? degree % 360 : degree % 360 + 360;
        }

        public static Vector3 GetNearestPoint(Vector3 from, Vector3[] targets)
        {
            if (targets == null || targets.Length == 0)
                return Vector3.zero;

            var nearestIndex = 0;
            var nearestDistanceSquared = float.MaxValue;
            for (var i = 0; i < targets.Length; ++i)
            {
                Vector3 target = targets[i];
                float distSqr = (target - from).sqrMagnitude;
                if (nearestDistanceSquared >= distSqr)
                {
                    nearestDistanceSquared = distSqr;
                    nearestIndex = i;
                }
            }

            return targets[nearestIndex];
        }

        public static uint Fibonacci(uint n)
        {
            if (n == 0) return 0;
            if (n == 1) return 1;
            uint count = n - 1;
            var fib = new uint[count + 1];
            fib[0] = 0;
            fib[1] = 1;
            for (var i = 2; i <= count; ++i) fib[i] = fib[i - 2] + fib[i - 1];

            return fib[count];
        }

        public static List<Vector2> PointsOnLine(Vector2 start, Vector2 end, int pointCount)
        {
            List<Vector2> points = new();

            // Edge case: If you only want 1 point, return the center (or start)
            if (pointCount <= 1)
            {
                points.Add(Vector2.Lerp(start, end, 0.5f));
                return points;
            }

            for (var i = 0; i < pointCount; i++)
            {
                // Calculate t (0.0 to 1.0)
                // We cast to float to ensure we don't get integer division
                float t = i / (float)(pointCount - 1);

                Vector2 point = Vector2.Lerp(start, end, t);
                points.Add(point);
            }

            return points;
        }

        public static List<Vector2> PointsOnLine(Vector2 start, Vector2 end, float spacing)
        {
            List<Vector2> points = new();

            float totalDistance = Vector2.Distance(start, end);
            Vector2 direction = (end - start).normalized;

            // Start at 0, move by 'spacing' until we hit the total distance
            for (float distCovered = 0; distCovered <= totalDistance; distCovered += spacing)
            {
                Vector2 point = start + direction * distCovered;
                points.Add(point);
            }

            return points;
        }

        public static Vector2 PointInCircle(float degree, Vector2 center, float radius = 1, bool clockwise = false)
        {
            degree = Wrap360(degree);
            degree = clockwise ? -degree : degree;
            return DegreeToVector2(degree) * radius + center;
        }
        
        public static Vector3 GetCentroid(List<Vector2> points)
        {
            if (points == null || points.Count == 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            foreach (Vector2 point in points)
            {
                sum += point;
            }

            return sum / points.Count;
        }

        public static Vector2 RandomPointInCircle(Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle * radius;
        }

        /// <summary>
        ///     Get random point within two circles (or a donut)
        /// </summary>
        public static Vector2 RandomPointInAnnulus(Vector2 center, float minRadius, float maxRadius)
        {
            // 1. Get a random angle in radian
            float randomAngle = Random.Range(0f, Mathf.PI * 2f);

            // 2. Get a random distance
            // We square the radii to interpolate by Area, not strictly by Distance.
            // This prevents points from clumping around the inner ring.
            float minSquared = minRadius * minRadius;
            float maxSquared = maxRadius * maxRadius;
            float randomDistSquared = Random.Range(minSquared, maxSquared);

            // 3. Calculate final distance
            float distance = Mathf.Sqrt(randomDistSquared);

            // 4. Convert to Vector2
            float x = Mathf.Cos(randomAngle) * distance;
            float y = Mathf.Sin(randomAngle) * distance;

            return center + new Vector2(x, y);
        }

        /// <summary>
        ///     Gets evenly distributed points on a circle
        /// </summary>
        public static List<Vector2> PointsOnCircle(Vector2 center, float radius, int pointCount)
        {
            List<Vector2> points = new();

            // Safety check
            if (pointCount <= 0) return points;

            // Calculate the angle 'slice' per point (in Radians)
            float angleStep = 2 * Mathf.PI / pointCount;

            for (var i = 0; i < pointCount; i++)
            {
                // Current angle for this specific point
                float currentAngle = i * angleStep;

                // Calculate X and Y using Sine and Cosine
                float x = Mathf.Cos(currentAngle) * radius;
                float y = Mathf.Sin(currentAngle) * radius;

                // Add the center offset to move it to world position
                Vector2 pos = center + new Vector2(x, y);

                points.Add(pos);
            }

            return points;
        }

        public static Vector2 RandomPointOnCircle(Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle.normalized * radius;
        }

        /// <summary>
        ///     Approximate a circle from a list of points (assuming the points form a circle)
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Circle2D CircleFromPoints(List<Vector2> points)
        {
            // Safety check to prevent division by zero
            if (points == null || points.Count == 0)
                return Circle2D.zero;

            // PASS 1: Find Center
            Vector2 sum = Vector2.zero;
            for (var i = 0; i < points.Count; i++) sum += points[i];

            Vector2 center = sum / points.Count;

            // PASS 2: Find Radius (Average Distance)
            var distSum = 0f;
            for (var i = 0; i < points.Count; i++) distSum += Vector2.Distance(center, points[i]);

            float radius = distSum / points.Count;
            return new Circle2D { center = center, radius = radius };
        }

        /// <summary>
        ///     Calculates a new direction vector by applying a random spread to a base direction.
        ///     Useful for machine guns (spray and pray).
        /// </summary>
        public static Vector2 RandomDirectionWithSpread(Vector2 baseDirection, float totalSpreadAngle)
        {
            float randomAngle =
                Random.Range(-totalSpreadAngle / 2f, totalSpreadAngle / 2f); // Halved so spreadAngle is the total cone
            Quaternion rotation = Quaternion.Euler(0, 0, randomAngle);
            return rotation * baseDirection;
        }

        /// <summary>
        ///     Calculates an array of directions evenly distributed across the spread angle.
        /// </summary>
        /// <param name="baseDirection">The forward direction.</param>
        /// <param name="totalSpreadAngle">The total width of the cone in degrees.</param>
        /// <param name="count">How many projectiles to generate.</param>
        public static Vector2[] DirectionsWithSpread(Vector2 baseDirection, float totalSpreadAngle, int count)
        {
            Vector2[] directions = new Vector2[count];

            // 1. Edge Case: Single shot always goes straight
            if (count <= 1)
            {
                directions[0] = baseDirection;
                return directions;
            }

            // 2. Calculate the step angle between each projectile
            // Example: 90 degree spread, 3 shots. Step = 45 degrees.
            // (Shot 1 at -45, Shot 2 at 0, Shot 3 at 45)
            float stepAngle = totalSpreadAngle / (count - 1);

            // 3. Start at the negative half of the angle
            float startAngle = -totalSpreadAngle / 2f;

            for (var i = 0; i < count; i++)
            {
                // Calculate the specific offset for this projectile index
                float currentAngleOffset = startAngle + stepAngle * i;

                // Create rotation
                Quaternion rotation = Quaternion.Euler(0, 0, currentAngleOffset);

                // Apply rotation
                directions[i] = rotation * baseDirection;
            }

            return directions;
        }
    }
}
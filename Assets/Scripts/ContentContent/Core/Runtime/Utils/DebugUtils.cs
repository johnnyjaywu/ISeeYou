using UnityEngine;

namespace ContentContent
{
    public static class DebugUtils
    {
        public static void DebugDrawCircle(Vector2 center, float radius, Color color, float duration = 10f)
        {
            var segments = 36;
            float angleStep = 360f / segments;

            Vector3 prevPoint = center + new Vector2(radius, 0);

            for (var i = 1; i <= segments + 1; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                Debug.DrawLine(prevPoint, newPoint, color, duration);
                prevPoint = newPoint;
            }
        }
    }
}
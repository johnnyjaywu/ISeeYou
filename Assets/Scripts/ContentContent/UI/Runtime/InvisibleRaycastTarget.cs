using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.UI
{
    /// <summary>
    /// A UI component that is completely invisible but can still intercept user input like clicks and touches.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class InvisibleRaycastTarget : Graphic
    {
        // Optional: Editor helper to visualize the bounds in the Scene view 
        // purely for debugging purposes.
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Only draw if this object is selected to reduce clutter
            if (Selection.activeGameObject != gameObject) return;

            var rt = transform as RectTransform;
            if (rt == null) return;

            // Save current gizmo matrix
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Semi-transparent magenta
            Gizmos.matrix = rt.localToWorldMatrix;

            // Draw a cube representing the rect
            Gizmos.DrawCube(rt.rect.center, rt.rect.size);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(rt.rect.center, rt.rect.size);

            // Restore gizmo matrix
            Gizmos.matrix = oldMatrix;
        }
#endif
        /// <summary>
        ///     Overriding this method and clearing the vertex helper ensures
        ///     no geometry is generated for the GPU, reducing draw calls.
        /// </summary>
        /// <param name="vh">The vertex helper used to build the UI mesh.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
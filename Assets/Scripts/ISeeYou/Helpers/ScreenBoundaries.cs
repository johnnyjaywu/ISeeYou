using UnityEngine;
using NaughtyAttributes;

namespace ISeeYou
{
    [RequireComponent(typeof(RectTransform))]
    public class ScreenBoundaries : MonoBehaviour
    {
        // Internal marker class to uniquely identify walls created by this script
        public class GeneratedWall : MonoBehaviour { }

        [Header("Settings")]
        [SerializeField] private float wallThickness = 100f;
        [SerializeField, Layer] private int physicsLayer;
        [SerializeField] private PhysicsMaterial2D wallMaterial;

        // We only need to store the Colliders to update them
        private BoxCollider2D topWall;
        private BoxCollider2D bottomWall;
        private BoxCollider2D leftWall;
        private BoxCollider2D rightWall;
        
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CreateColliders();
        }

        private void Start()
        {
            UpdateBoundaries();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateBoundaries();
        }

        private void CreateColliders()
        {
            // SAFETY CHECK:
            // Iterate backwards through all children.
            // Only destroy the object if it has our unique 'GeneratedWall' component.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<GeneratedWall>() != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // Create default material if missing
            if (wallMaterial == null)
            {
                wallMaterial = new PhysicsMaterial2D("HardWall");
                wallMaterial.friction = 0f;
                wallMaterial.bounciness = 0.5f;
            }

            topWall = CreateWall("TopWall");
            bottomWall = CreateWall("BottomWall");
            leftWall = CreateWall("LeftWall");
            rightWall = CreateWall("RightWall");
        }

        private BoxCollider2D CreateWall(string name)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(transform, false);
            wall.layer = physicsLayer;

            // 1. Add our unique Marker so we can identify this later
            wall.AddComponent<GeneratedWall>();

            // 2. Add Physics
            Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.sharedMaterial = wallMaterial;
            
            return col;
        }

        [Button("Force Update Boundaries")]
        public void UpdateBoundaries()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (topWall == null) return; 

            Rect rect = rectTransform.rect;
            float width = rect.width;
            float height = rect.height;
            float centerX = rect.center.x;
            float centerY = rect.center.y;
            
            topWall.size = new Vector2(width, wallThickness);
            topWall.offset = new Vector2(centerX, rect.yMax + wallThickness / 2);

            bottomWall.size = new Vector2(width, wallThickness);
            bottomWall.offset = new Vector2(centerX, rect.yMin - wallThickness / 2);

            leftWall.size = new Vector2(wallThickness, height);
            leftWall.offset = new Vector2(rect.xMin - wallThickness / 2, centerY);

            rightWall.size = new Vector2(wallThickness, height);
            rightWall.offset = new Vector2(rect.xMax + wallThickness / 2, centerY);
        }
    }
}
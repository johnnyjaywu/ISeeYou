using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class WorldToScreenTracker : MonoBehaviour
    {
        [Header("Positioning")]
        public Vector3 offset = Vector3.zero;

        public bool clampToScreen = true;

        [Tooltip("Padding in Screen Pixels.")]
        public Vector2 screenMargin = new(20f, 20f);

        public float fixedWorldDistance = 10f;

        [Header("Billboard")]
        public bool billboard = true;

        [Header("Indicators")]
        [SerializeField] private GameObject arrowIndicator;

        // Cached array to avoid GC alloc in LateUpdate
        private readonly Vector3[] corners = new Vector3[4];
        private Camera mainCamera;
        private Canvas parentCanvas;

        private RectTransform rectTransform;
        private Transform targetTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            UpdateTracking();
        }

        public void Initialize(Camera cam)
        {
            mainCamera = cam;
        }

        public void Track(Transform target)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            targetTransform = target;
            UpdateTracking();
        }

        private void UpdateTracking()
        {
            if (targetTransform == null || mainCamera == null || parentCanvas == null) return;

            // 1. Force layout rebuild for dynamic content
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            // 2. Calculate the UI's actual size in Screen Pixels
            // We get world corners, then convert them to screen points to find the pixel width/height
            rectTransform.GetWorldCorners(corners);
            float screenWidthUI = Vector3.Distance(mainCamera.WorldToScreenPoint(corners[0]),
                mainCamera.WorldToScreenPoint(corners[3]));
            float screenHeightUI = Vector3.Distance(mainCamera.WorldToScreenPoint(corners[0]),
                mainCamera.WorldToScreenPoint(corners[1]));

            float halfWidth = screenWidthUI * 0.5f;
            float halfHeight = screenHeightUI * 0.5f;

            // 3. Target Position
            Vector3 targetWorldPos = targetTransform.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPos);

            // 4. Clamping logic using actual screen pixel boundaries
            float minX = screenMargin.x + halfWidth;
            float maxX = Screen.width - screenMargin.x - halfWidth;
            float minY = screenMargin.y + halfHeight;
            float maxY = Screen.height - screenMargin.y - halfHeight;

            bool isOffScreen = IsPositionOffScreen(screenPos, minX, maxX, minY, maxY);

            if (clampToScreen && isOffScreen)
            {
                ClampToScreenEdges(ref screenPos, minX, maxX, minY, maxY);
                ApplyPosition(screenPos);

                if (arrowIndicator != null)
                {
                    if (!arrowIndicator.activeSelf) arrowIndicator.SetActive(true);
                    RotateArrowTowardsTarget(screenPos, targetWorldPos);
                }
            }
            else
            {
                ApplyPosition(screenPos);

                if (arrowIndicator != null && arrowIndicator.activeSelf)
                    arrowIndicator.SetActive(false);
            }

            if (billboard && parentCanvas.renderMode == RenderMode.WorldSpace) HandleBillboard();
        }

        private void ApplyPosition(Vector3 screenPos)
        {
            if (parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // In World Space, we project the screen point back into the world 
                // at a fixed distance so it doesn't clip into geometry.
                screenPos.z = fixedWorldDistance;
                transform.position = mainCamera.ScreenToWorldPoint(screenPos);
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPos,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint);

                rectTransform.anchoredPosition = localPoint;
            }
        }

        private bool IsPositionOffScreen(Vector3 screenPos, float minX, float maxX, float minY, float maxY)
        {
            return screenPos.x < minX || screenPos.x > maxX ||
                   screenPos.y < minY || screenPos.y > maxY ||
                   screenPos.z < 0;
        }

        private void ClampToScreenEdges(ref Vector3 screenPos, float minX, float maxX, float minY, float maxY)
        {
            if (screenPos.z < 0)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
            screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);
        }

        private void RotateArrowTowardsTarget(Vector3 clampedScreenPos, Vector3 targetWorldPos)
        {
            Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(targetWorldPos);
            if (targetScreenPos.z < 0) targetScreenPos *= -1;

            Vector2 direction = (Vector2)targetScreenPos - (Vector2)clampedScreenPos;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            arrowIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void HandleBillboard()
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }

        public void SetIndicatorActive(bool active)
        {
            if (arrowIndicator != null) arrowIndicator.SetActive(active);
        }
    }
}
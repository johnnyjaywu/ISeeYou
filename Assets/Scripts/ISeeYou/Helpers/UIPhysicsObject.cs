using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ISeeYou
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(RectTransform))]
    public class UIPhysicsObject : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float bounceFactor = 0.5f;
        [SerializeField] private float linearDamping = 1f; 
        [SerializeField] private float repulsionForce = 500f;
        [SerializeField] private float throwPower = 1.0f; 

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private RectTransform rectTransform;
        private Draggable draggable;
        
        // Physics State
        private Vector2 lastPosition;
        private Vector2 smoothedVelocity; // Stored momentum
        
        // Optimizations
        private readonly List<Collider2D> hitBuffer = new List<Collider2D>(10);
        private ContactFilter2D contactFilter;

        private bool isDragging = false;
        private bool isInLayout = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            rectTransform = GetComponent<RectTransform>();
            draggable = GetComponent<Draggable>();

            contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false; 
            contactFilter.useLayerMask = false; 

            SetupPhysics();
        }

        private void OnEnable()
        {
            if (draggable != null)
            {
                draggable.OnDragStarted += HandleDragStart;
                draggable.OnDragEnded += HandleDragEnd;
            }
        }

        private void OnDisable()
        {
            if (draggable != null)
            {
                draggable.OnDragStarted -= HandleDragStart;
                draggable.OnDragEnded -= HandleDragEnd;
            }
        }

        private void SetupPhysics()
        {
            rb.gravityScale = 0;
            rb.linearDamping = linearDamping;  
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            if (rb.sharedMaterial == null)
            {
                PhysicsMaterial2D mat = new PhysicsMaterial2D("UIBounce");
                mat.bounciness = bounceFactor;
                mat.friction = 0.4f;
                rb.sharedMaterial = mat;
            }
            
            UpdateColliderSize();
        }

        private void Update()
        {
            if (rectTransform.hasChanged)
            {
                UpdateColliderSize();
            }
        }
        
        private void UpdateColliderSize()
        {
             if (rectTransform.rect.size != boxCollider.size)
             {
                 boxCollider.size = rectTransform.rect.size;
             }
        }

        private void FixedUpdate()
        {
            // CASE 1: DRAGGING
            if (isDragging)
            {
                // CRITICAL FIX:
                // We calculate velocity for the throw, but we DO NOT apply it to the Rigidbody yet.
                // Applying velocity to a Kinematic body causes it to drift from the cursor.
                
                if (!isInLayout)
                {
                    Vector2 currentPos = rb.position; 
                    Vector2 rawVelocity = (currentPos - lastPosition) / Time.fixedDeltaTime;
                    
                    // Store this for later (OnDragEnd)
                    smoothedVelocity = Vector2.Lerp(smoothedVelocity, rawVelocity, 10f * Time.fixedDeltaTime);
                    lastPosition = currentPos;
                }
                
                // Ensure physics engine doesn't move it
                rb.linearVelocity = Vector2.zero; 
                return;
            }

            // CASE 2: DOCKED OR KINEMATIC
            if (isInLayout || rb.bodyType == RigidbodyType2D.Kinematic) return;

            // CASE 3: FLOATING PHYSICS
            int hitCount = Physics2D.OverlapBox(
                transform.position, 
                boxCollider.size * 1.1f, 
                0f, 
                contactFilter, 
                hitBuffer
            );
            
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                if (hit == boxCollider) continue;

                if (hit.attachedRigidbody != null && hit.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                {
                    Vector2 direction = (transform.position - hit.transform.position).normalized;
                    rb.AddForce(direction * repulsionForce * Time.fixedDeltaTime);
                }
            }
        }

        private void HandleDragStart(PointerEventData data)
        {
            isDragging = true;
            
            if (isInLayout)
            {
                SetLayoutState(false);
            }
            
            rb.bodyType = RigidbodyType2D.Kinematic; 
            
            lastPosition = rb.position;
            smoothedVelocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }

        private void HandleDragEnd(PointerEventData data)
        {
            isDragging = false;

            if (!isInLayout)
            {
                // Released into open space -> Dynamic Physics
                rb.bodyType = RigidbodyType2D.Dynamic;
                
                // FIX: NOW we apply the stored velocity to the Rigidbody
                rb.linearVelocity = smoothedVelocity * throwPower;
            }
            else
            {
                // Released into a slot -> Locked
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void SetLayoutState(bool docked)
        {
            isInLayout = docked;
            
            if (docked)
            {
                isDragging = false; 
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                
                if (boxCollider != null) boxCollider.isTrigger = true;
            }
            else
            {
                if (boxCollider != null) boxCollider.isTrigger = false;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (boxCollider != null && rectTransform != null) UpdateColliderSize();
        }
#endif
    }
}
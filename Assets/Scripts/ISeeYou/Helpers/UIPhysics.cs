using System.Collections.Generic;
using UnityEngine;

namespace ISeeYou
{
    /// <summary>
    /// Responsibility: Manages physical simulation properties for UI elements.
    /// Syncs 2D Colliders with RectTransforms and provides ambient anti-overlap repulsion.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(RectTransform))]
    public class UIPhysics : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private PhysicsMaterial2D physicsMaterial;
        [SerializeField] private float bounceFactor = 0.5f;
        [SerializeField] private float friction = 0.4f;

        [Header("Simulation Settings")]
        [SerializeField] private float linearDamping = 1f;
        [SerializeField] private float angularDamping = 0.5f;
        [SerializeField] private bool freezeRotation = true;
        
        [Header("Repulsion (Anti-Overlap)")]
        [Tooltip("Force applied to separate this object from others.")]
        [SerializeField] private float repulsionForce = 500f;
        
        [Tooltip("Bounds inflation for proximity detection.")]
        [SerializeField] private float detectionBuffer = 1.1f;

        // Dependencies
        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private RectTransform rectTransform;

        // State & Optimization
        private Vector2 lastKnownSize;
        private readonly List<Collider2D> hitBuffer = new List<Collider2D>(10);
        private ContactFilter2D contactFilter;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            rectTransform = GetComponent<RectTransform>();

            InitializePhysics();
        }

        private void InitializePhysics()
        {
            // Rigidbody Configuration
            rb.gravityScale = 0;
            rb.linearDamping = linearDamping;
            rb.angularDamping = angularDamping;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = freezeRotation;

            // Material Configuration
            if (rb.sharedMaterial == null)
            {
                if (physicsMaterial != null)
                {
                    rb.sharedMaterial = physicsMaterial;
                }
                else
                {
                    // Fallback to ensure consistent behavior
                    PhysicsMaterial2D mat = new PhysicsMaterial2D("UIPhysicsDefault");
                    mat.bounciness = bounceFactor;
                    mat.friction = friction;
                    rb.sharedMaterial = mat;
                }
            }

            // Optimization Configuration
            contactFilter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = false // Enable if specific UI Physics Layers are added later
            };

            SyncColliderSize();
        }

        private void Update()
        {
            // Dirty Check: Sync Collider if RectTransform dimensions change
            if (rectTransform.rect.size != lastKnownSize)
            {
                SyncColliderSize();
            }
        }

        private void FixedUpdate()
        {
            // Optimization: Skip repulsion for Kinematic (Drag) or Sleeping objects
            if (rb.bodyType == RigidbodyType2D.Kinematic || rb.IsSleeping()) return;

            ApplyRepulsionForces();
        }

        // -------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------

        /// <summary>
        /// Sets the physics mode.
        /// <para><b>Dynamic:</b> Simulation enabled, Collider enabled (Falling/Settling).</para>
        /// <para><b>Kinematic:</b> Simulation disabled, Collider disabled (Dragging/Docked).</para>
        /// </summary>
        public void SetSimulationMode(bool isDynamic)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

            if (isDynamic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                boxCollider.enabled = true;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                
                // Disable collider to prevent knocking things over while dragging/docked
                boxCollider.enabled = false;
            }
        }

        /// <summary>
        /// Applies an immediate velocity change to the object.
        /// Useful for flinging/throwing mechanics.
        /// </summary>
        public void SetLinearVelocity(Vector2 velocity)
        {
            if (rb != null)
            {
                rb.linearVelocity = velocity;
            }
        }

        /// <summary>
        /// Manually forces the BoxCollider to match the RectTransform size.
        /// Useful when size is changed via animation/tweening.
        /// </summary>
        public void SyncColliderSize()
        {
            lastKnownSize = rectTransform.rect.size;
            if (boxCollider.size != lastKnownSize)
            {
                boxCollider.size = lastKnownSize;
            }
        }

        private void ApplyRepulsionForces()
        {
            int hitCount = Physics2D.OverlapBox(
                transform.position,
                boxCollider.size * detectionBuffer, 
                0f, 
                contactFilter, 
                hitBuffer
            );

            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = hitBuffer[i];
                if (hit == boxCollider) continue; // Ignore self

                // Filter: Only repel from other Dynamic bodies
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                {
                    Vector2 direction = (transform.position - hit.transform.position).normalized;
                    rb.AddForce(direction * (repulsionForce * dt));
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        }
#endif
    }
}
using UnityEngine;

namespace HeadBust.AI
{
    /// <summary>
    /// The sensory system that gives AI bots human-like sight.
    /// Handles viewing distance, vision cone (FOV), and line-of-sight raycasts through walls.
    /// </summary>
    public class EnemySensory : MonoBehaviour
    {
        [Header("Vision Settings")]
        [Tooltip("How far the bot can see in meters")]
        public float viewRadius = 35f;
        [Tooltip("Field of View in degrees (e.g. 110 = human peripheral vision)")]
        [Range(0f, 360f)]
        public float viewAngle = 110f;
        [Tooltip("Layers that block line of sight (e.g., Walls, Obstacles, Ground)")]
        public LayerMask obstacleMask = ~0; // Default: checks all colliders

        [Header("Target Tracking")]
        [Tooltip("The current enemy fighter this bot has locked onto")]
        public Transform currentTarget;

        [Tooltip("Offset height from feet to eyes (matches head/gun level)")]
        public float eyeHeight = 3.0f;

        private void Update()
        {
            FindVisibleTarget();
        }

        /// <summary>
        /// Scans for any fighter with Health within the vision cone,
        /// ensuring walls and obstacles block line of sight.
        /// </summary>
        public void FindVisibleTarget()
        {
            // Reset target if current target died
            if (currentTarget != null)
            {
                Health targetHp = currentTarget.GetComponentInParent<Health>();
                if (targetHp != null && targetHp.IsDead)
                {
                    currentTarget = null;
                }
            }

            // Find all colliders within viewing radius
            Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, viewRadius);

            Transform closestTarget = null;
            float closestDistance = Mathf.Infinity;

            Vector3 eyeOrigin = transform.position + Vector3.up * eyeHeight;

            foreach (Collider col in targetsInRadius)
            {
                // Must be tagged Fighter and have a Health component
                if (!col.CompareTag("Fighter")) continue;

                // Never target yourself!
                if (col.transform.root == transform.root) continue;

                Health targetHealth = col.GetComponentInParent<Health>();
                if (targetHealth == null || targetHealth.IsDead) continue;

                Transform targetRoot = col.transform.root;
                Vector3 targetCenter = targetRoot.position + Vector3.up * (eyeHeight * 0.7f);
                Vector3 dirToTarget = (targetCenter - eyeOrigin).normalized;

                // 1. Angle Check: Is the target inside our forward vision cone?
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2f)
                {
                    float distToTarget = Vector3.Distance(eyeOrigin, targetCenter);

                    // 2. Line of Sight Check: Cast a ray from our eyes to the target center
                    if (Physics.Raycast(eyeOrigin, dirToTarget, out RaycastHit hit, distToTarget, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        // If the first object hit is the target (not a wall in between):
                        if (hit.transform.root == targetRoot)
                        {
                            if (distToTarget < closestDistance)
                            {
                                closestDistance = distToTarget;
                                closestTarget = targetRoot;
                            }
                        }
                    }
                }
            }

            currentTarget = closestTarget;
        }

        /// <summary>
        /// Draws the vision cone in the Scene View so you can watch the AI see in real time!
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = currentTarget != null ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 eyeOrigin = transform.position + Vector3.up * eyeHeight;
            Vector3 viewAngleA = DirFromAngle(-viewAngle / 2f, false);
            Vector3 viewAngleB = DirFromAngle(viewAngle / 2f, false);

            Gizmos.DrawLine(eyeOrigin, eyeOrigin + viewAngleA * viewRadius);
            Gizmos.DrawLine(eyeOrigin, eyeOrigin + viewAngleB * viewRadius);

            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(eyeOrigin, currentTarget.position + Vector3.up * eyeHeight);
            }
        }

        private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }
}

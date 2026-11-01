using System.Collections;
using UnityEngine;

namespace HeadBust.AI
{
    /// <summary>
    /// Physical shooting component for AI bots.
    /// Handles 3D positional gunshots, hitscan raycasting from the bot's barrel,
    /// bullet tracers, muzzle flash, and human-like aim inaccuracy.
    /// </summary>
    public class EnemyShooter : MonoBehaviour
    {
        [Header("Muzzle & Visuals")]
        [Tooltip("The tip of the bot's gun barrel where bullets and sparks originate")]
        public Transform firePoint;
        [Tooltip("Particle system for barrel flash/sparks")]
        public ParticleSystem muzzleFlash;
        [Tooltip("Line Renderer for the bullet tracer beam")]
        public LineRenderer bulletTracer;
        [Tooltip("How long the tracer stays visible in seconds")]
        public float tracerDuration = 0.04f;

        [Header("Gunplay Settings")]
        [Tooltip("Rounds per second (e.g. 3 = fires once every 0.33 seconds)")]
        public float fireRate = 3.5f;
        [Tooltip("Maximum shooting distance in meters")]
        public float range = 60f;
        [Tooltip("Aim imperfection in degrees so bots behave like humans, not aimbots")]
        [Range(0f, 5f)]
        public float accuracySpread = 1.2f;
        [Tooltip("Layers the bullet can hit")]
        public LayerMask hitLayers = ~0;

        [Header("Audio (3D Positional)")]
        [Tooltip("Gunshot sound effect (.wav)")]
        public AudioClip gunshotSound;
        [Range(0f, 1f)] public float gunshotVolume = 0.85f;
        public Vector2 pitchRange = new Vector2(0.93f, 1.07f);

        [Header("Impact Decals")]
        [Tooltip("The Quad prefab to spawn on wall impacts")]
        public GameObject bulletHolePrefab;
        public float bulletHoleLifetime = 15f;

        private AudioSource audioSource;
        private float nextTimeToFire = 0f;

        public bool CanShoot => Time.time >= nextTimeToFire;

        private void Awake()
        {
            // Configure 3D AudioSource so gunshots are spatialized in the arena
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 100% 3D spatial sound!
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 45f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        /// <summary>
        /// Attempts to fire a bullet towards the given target position.
        /// Returns true if a bullet was discharged, false if on cooldown.
        /// </summary>
        public bool ShootAt(Vector3 targetPosition)
        {
            if (!CanShoot) return false;

            nextTimeToFire = Time.time + (1f / fireRate);

            Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 3f;

            // 1. Calculate direction towards target with human-like spread
            Vector3 baseDir = (targetPosition - startPos).normalized;
            Vector3 spreadVector = new Vector3(
                Random.Range(-accuracySpread, accuracySpread),
                Random.Range(-accuracySpread, accuracySpread),
                Random.Range(-accuracySpread, accuracySpread)
            ) * 0.02f;

            Vector3 shootDir = (baseDir + spreadVector).normalized;

            // 2. Play Muzzle Flash
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // 3. Play 3D Gunshot Sound
            if (gunshotSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.PlayOneShot(gunshotSound, gunshotVolume);
            }

            // 4. Hitscan Raycast
            Ray ray = new Ray(startPos, shootDir);
            RaycastHit[] hits = Physics.RaycastAll(ray, range, hitLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 impactPoint = ray.GetPoint(range);
            bool hasValidHit = false;
            RaycastHit validHit = default;

            foreach (var h in hits)
            {
                // Ignore self
                if (h.transform.root == transform.root) continue;

                validHit = h;
                hasValidHit = true;
                impactPoint = h.point;
                break;
            }

            if (hasValidHit)
            {
                // Inflict damage if target has Health
                Health targetHp = validHit.collider.GetComponentInParent<Health>();
                if (targetHp != null)
                {
                    targetHp.TakeDamage(1);
                }
                else if (bulletHolePrefab != null)
                {
                    // Spawn bullet hole decal on walls
                    Vector3 decalPos = validHit.point + (validHit.normal * 0.002f);
                    Quaternion decalRot = Quaternion.LookRotation(-validHit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    GameObject hole = Instantiate(bulletHolePrefab, decalPos, decalRot);
                    Destroy(hole, bulletHoleLifetime);
                }
            }

            // 5. Draw bullet tracer
            if (bulletTracer != null && firePoint != null)
            {
                StartCoroutine(RenderTracer(firePoint.position, impactPoint));
            }

            return true;
        }

        private IEnumerator RenderTracer(Vector3 start, Vector3 end)
        {
            bulletTracer.SetPosition(0, start);
            bulletTracer.SetPosition(1, end);
            bulletTracer.enabled = true;

            yield return new WaitForSeconds(tracerDuration);

            bulletTracer.enabled = false;
        }
    }
}

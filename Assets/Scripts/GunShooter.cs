using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GunShooter : MonoBehaviour
{
    [Header("Gun References")]
    [Tooltip("The tip of the barrel where visuals will originate")]
    public Transform firePoint;

    [Header("Ammo & Reloading")]
    [Tooltip("Maximum bullets per magazine")]
    public int maxAmmo = 30;
    [Tooltip("Seconds required to reload")]
    public float reloadTime = 3f;
    [Tooltip("Text element in top-right corner to display ammo count")]
    public TextMeshProUGUI ammoText;

    private int currentAmmo;
    private bool isReloading = false;

    [Header("Visual Effects")]
    [Tooltip("Particle system for barrel flash/sparks")]
    public ParticleSystem muzzleFlash;
    [Tooltip("Line Renderer for the bullet tracer beam")]
    public LineRenderer bulletTracer;
    [Tooltip("How long the tracer stays visible in seconds")]
    public float tracerDuration = 0.04f;

    [Header("Audio")]
    [Tooltip("Gunshot sound effect (.wav)")]
    public AudioClip gunshotSound;
    [Range(0f, 1f)] public float gunshotVolume = 0.85f;
    [Tooltip("Random pitch range so rapid fire sounds dynamic (min, max)")]
    public Vector2 pitchRange = new Vector2(0.93f, 1.07f);

    [Header("Procedural Recoil")]
    [Tooltip("The Gun 3D model that will kick and pitch (defaults to firePoint's parent)")]
    public Transform gunTransform;
    [Tooltip("How far the gun punches backward in meters")]
    public float recoilKick = 0.08f;
    [Tooltip("How much the barrel flips upward in degrees")]
    public float recoilPitch = 4f;
    [Tooltip("How fast the gun returns to rest position (higher = snappier)")]
    public float recoilRecoverySpeed = 16f;

    [Header("Camera Muzzle Climb (Aim Recoil)")]
    [Tooltip("How much the camera/crosshair kicks upward per bullet in degrees")]
    public float cameraRecoilUp = 1.0f;
    public float cameraRecoilSide = 0.3f;

    private AudioSource audioSource;
    private ThirdPersonCamera thirdPersonCamera;
    private Vector3 originalGunPos;
    private Quaternion originalGunRot;
    private Vector3 currentRecoilPos;
    private Vector3 currentRecoilRot;

    [Header("Aiming & Crosshair Position")]
    [Tooltip("Normalized screen coordinates (0.5, 0.5 = dead center. 0.5, 0.55 = slightly above center)")]
    public Vector2 screenAimPoint = new Vector2(0.5f, 0.55f);

    [Header("Impact Decals")]
    [Tooltip("The Quad prefab to spawn on wall hits")]
    public GameObject bulletHolePrefab;
    [Tooltip("Seconds before the bullet hole disappears to save memory")]
    public float bulletHoleLifetime = 15f;
    [Tooltip("Maximum distance the bullet can travel")]
    public float range = 100f;
    [Tooltip("Rounds per second (e.g., 5 = 5 shots every second)")]
    public float fireRate = 5f;
    [Tooltip("Damage dealt to targets with health")]
    public float damage = 25f;
    [Tooltip("Layers the bullet can hit")]
    public LayerMask hitLayers = ~0; // Collides with everything by default

    private Camera mainCam;
    private float nextTimeToFire = 0f;

    private void Awake()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            thirdPersonCamera = mainCam.GetComponent<ThirdPersonCamera>();
        }

        // Setup AudioSource speaker
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D clean audio for player
        audioSource.volume = 1f;       // Full volume regardless of movement state!

        // Auto-assign gunTransform if left empty
        if (gunTransform == null && firePoint != null)
        {
            gunTransform = firePoint.parent;
        }

        if (gunTransform != null)
        {
            originalGunPos = gunTransform.localPosition;
            originalGunRot = gunTransform.localRotation;
        }

        // Initialize ammo
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
    }

    private void Update()
    {
        // 1. Recover recoil smoothly back to rest position
        HandleRecoilRecovery();

        // If game is in menu or game over, block shooting
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive) return;

        // 2. Check for Manual Reload Input ('R' key)
        bool reloadPressed = false;
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            reloadPressed = true;
        }
        #else
        reloadPressed = Input.GetKeyDown(KeyCode.R);
        #endif

        if (reloadPressed && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }

        // 3. If currently reloading, block shooting completely
        if (isReloading) return;

        // 4. Check fire rate cooldown
        if (Time.time < nextTimeToFire) return;

        // 5. Read Fire Input (Left Click)
        bool fireTriggered = false;
        #if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            fireTriggered = Mouse.current.leftButton.isPressed;
        }
        #else
        fireTriggered = Input.GetMouseButton(0);
        #endif

        if (fireTriggered)
        {
            // Auto-reload if empty
            if (currentAmmo <= 0)
            {
                StartCoroutine(ReloadCoroutine());
                return;
            }

            nextTimeToFire = Time.time + (1f / fireRate);
            currentAmmo--;
            UpdateAmmoUI();
            ExecuteShoot();

            // If this was the last bullet, auto-start reload
            if (currentAmmo <= 0)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }
    }

    private void ExecuteShoot()
    {
        // 1. Play Muzzle Flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // 2. Play Gunshot Audio (with random pitch variation)
        if (gunshotSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            audioSource.PlayOneShot(gunshotSound, gunshotVolume);
        }

        if (mainCam == null) mainCam = Camera.main;

        // Create ray shooting out from the configured screen aim point
        Ray ray = mainCam.ViewportPointToRay(new Vector3(screenAimPoint.x, screenAimPoint.y, 0f));
        Vector3 targetPoint;
        bool hasHit = false;
        RaycastHit validHit = default;

        // Perform Hitscan and ignore our own player's body
        RaycastHit[] hits = Physics.RaycastAll(ray, range, hitLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            // Ignore ourselves!
            if (h.transform.root == transform.root) continue;

            validHit = h;
            hasHit = true;
            break;
        }

        if (hasHit)
        {
            targetPoint = validHit.point;

            // 1. If we hit a character with Health (Enemy/Player), inflict damage (no wall bullet hole decal!)
            Health targetHealth = validHit.collider.GetComponentInParent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(1); // 1 bullet hit!
            }
            // 2. Otherwise, we hit an environment surface (Walls, Ground, Obstacles) -> Spawn Bullet Hole Decal!
            else if (bulletHolePrefab != null)
            {
                // Nudge slightly along normal to prevent Z-fighting flickering
                Vector3 decalPos = validHit.point + (validHit.normal * 0.002f);

                // Align face flat against wall, with random roll rotation
                Quaternion decalRot = Quaternion.LookRotation(-validHit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                GameObject hole = Instantiate(bulletHolePrefab, decalPos, decalRot);

                // Auto-cleanup after lifetime expires
                Destroy(hole, bulletHoleLifetime);
            }

            Debug.Log($"[Hitscan] Hit: {validHit.collider.name} at {validHit.point} | Surface Normal: {validHit.normal}");
        }
        else
        {
            // If we hit nothing (like the sky), extend ray to max range
            targetPoint = ray.GetPoint(range);
            Debug.Log("[Hitscan] Missed (Hit Sky/Air)");
        }

        // Draw a debug line in the Scene View to see the raycast in action
        if (firePoint != null)
        {
            Debug.DrawLine(firePoint.position, targetPoint, Color.red, 1.0f);
        }
        else
        {
            Debug.DrawLine(ray.origin, targetPoint, Color.yellow, 1.0f);
        }

        // 2. Trigger the Visual Bullet Tracer
        if (bulletTracer != null && firePoint != null)
        {
            StartCoroutine(RenderTracer(firePoint.position, targetPoint));
        }

        // 3. Apply Procedural Weapon Recoil
        ApplyRecoil();
    }

    private void ApplyRecoil()
    {
        // 1. Visual Gun Kick (Model Kickback & Barrel Flip)
        if (gunTransform != null)
        {
            currentRecoilPos += new Vector3(0f, 0f, -recoilKick);
            currentRecoilRot += new Vector3(-recoilPitch, Random.Range(-recoilPitch * 0.25f, recoilPitch * 0.25f), 0f);
        }

        // 2. Camera Muzzle Climb (The crosshair physically climbs up toward the sky!)
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.currentPitch -= cameraRecoilUp;
            thirdPersonCamera.currentYaw += Random.Range(-cameraRecoilSide, cameraRecoilSide);
        }
    }

    private void HandleRecoilRecovery()
    {
        if (gunTransform == null) return;

        // Smoothly interpolate recoil back to zero over time
        currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        // Apply to the gun transform relative to its initial rest pose
        gunTransform.localPosition = originalGunPos + currentRecoilPos;
        gunTransform.localRotation = originalGunRot * Quaternion.Euler(currentRecoilRot);
    }

    private System.Collections.IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        bulletTracer.SetPosition(0, start); // Start at the barrel
        bulletTracer.SetPosition(1, end);   // End at the impact point
        bulletTracer.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        bulletTracer.enabled = false;
    }

    private System.Collections.IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        float elapsed = 0f;

        // Display animated countdown during reload
        while (elapsed < reloadTime)
        {
            float remaining = Mathf.Max(0f, reloadTime - elapsed);
            if (ammoText != null)
            {
                ammoText.text = $"<color=#FF5555>RELOAD {remaining:F1}s</color>";
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
        }
    }
}

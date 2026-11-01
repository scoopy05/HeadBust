using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Total bullets required to eliminate this character")]
    public int maxHits = 8;

    [Header("UI References (Optional for AI, Required for Player)")]
    [Tooltip("The Green HealthFill Image in the top-left corner")]
    public Image healthBarFill;

    [Header("Color Feedback")]
    [Tooltip("Color changes dynamically as health depletes (Green -> Orange -> Red)")]
    public bool dynamicColor = true;
    public Color fullHealthColor = new Color(0.15f, 0.95f, 0.25f);
    public Color lowHealthColor = new Color(0.95f, 0.2f, 0.15f);

    private int currentHits;
    private bool isDead = false;

    public bool IsDead => isDead;
    public int CurrentHits => currentHits;
    public int MaxHits => maxHits;

    private void Awake()
    {
        currentHits = maxHits;
        UpdateHealthUI();
    }

    private void Update()
    {
        // Testing shortcut: Press 'H' to test taking 1 bullet damage!
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }
        #else
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(1);
        }
        #endif
    }

    /// <summary>
    /// Reduces health by bullet count (default: 1 bullet)
    /// </summary>
    public void TakeDamage(int bulletDamage = 1)
    {
        if (isDead) return;

        currentHits = Mathf.Max(0, currentHits - bulletDamage);
        UpdateHealthUI();

        Debug.Log($"[{gameObject.name}] Took hit! Remaining HP: {currentHits} / {maxHits}");

        if (currentHits <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill == null) return;

        // Calculate fill percentage: 8/8 = 1.0, 7/8 = 0.875, etc.
        float healthPercent = (float)currentHits / maxHits;
        healthBarFill.fillAmount = healthPercent;

        // Smoothly shift color from Neon Green to Dangerous Red
        if (dynamicColor)
        {
            healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"<color=red><b>[{gameObject.name}] ELIMINATED!</b></color>");

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. Disable player controls if attached
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        GunShooter gunShooter = GetComponent<GunShooter>();
        if (gunShooter != null) gunShooter.enabled = false;

        // 2. Disable AI brain, navigation, and weapon systems
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        HeadBust.AI.BehaviorTree.Tree tree = GetComponent<HeadBust.AI.BehaviorTree.Tree>();
        if (tree != null) tree.enabled = false;

        HeadBust.AI.EnemySensory sensory = GetComponent<HeadBust.AI.EnemySensory>();
        if (sensory != null) sensory.enabled = false;

        HeadBust.AI.EnemyShooter shooter = GetComponent<HeadBust.AI.EnemyShooter>();
        if (shooter != null) shooter.enabled = false;

        // Release Rigidbody if attached
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Disable all colliders so dead body doesn't block bullets or players
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 4. Low-Poly Arcade Knockdown: Smoothly tilt onto the floor without sinking
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(-85f, 0f, 15f); // Natural backward slump
        Vector3 startPos = transform.position;

        // Capsule radius is 1.0m. Raising the pivot by 1.0m as it rotates horizontally
        // keeps the bottom edge resting flush on top of the pavement!
        Vector3 targetPos = new Vector3(startPos.x, startPos.y + 1.0f, startPos.z);

        float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth ease-out fall curve
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.rotation = Quaternion.Slerp(startRot, targetRot, easeT);
            transform.position = Vector3.Lerp(startPos, targetPos, easeT);
            yield return null;
        }

        // 5. Detect if this is the Player (has PlayerController) or an AI bot
        bool isPlayer = GetComponent<PlayerController>() != null;

        if (isPlayer)
        {
            Debug.Log("<color=yellow>[Health] Player eliminated -> Showing Game Over Screen!</color>");
            yield return new WaitForSeconds(1.0f); // Brief dramatic pause after falling
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowGameOver();
            }
        }
        else
        {
            Debug.Log($"<color=orange>[Health] AI Bot [{gameObject.name}] eliminated -> Despawning in 3s</color>");
            yield return new WaitForSeconds(2.5f); // Stay visible on floor then clean up
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Restores full health (useful for respawning / restarting)
    /// </summary>
    public void ResetHealth()
    {
        currentHits = maxHits;
        isDead = false;
        UpdateHealthUI();

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = true;

        GunShooter gunShooter = GetComponent<GunShooter>();
        if (gunShooter != null) gunShooter.enabled = true;
    }
}

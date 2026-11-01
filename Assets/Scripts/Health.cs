using UnityEngine;
using UnityEngine.UI;

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
        isDead = true;
        Debug.Log($"<color=red><b>[{gameObject.name}] ELIMINATED!</b></color>");

        // Optional: Disable shooter/movement on death
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        GunShooter gunShooter = GetComponent<GunShooter>();
        if (gunShooter != null)
        {
            gunShooter.enabled = false;
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

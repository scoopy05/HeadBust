using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls high-level match state:
/// - Start Menu (Title screen, cursor unlocked)
/// - Gameplay (HUD visible, mouse locked for aiming)
/// - Game Over (Player death screen, Play Again button)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("The Start Menu panel containing the Title and Play button")]
    public GameObject startMenuPanel;
    [Tooltip("The Game Over panel containing 'Eliminated' and Play Again button")]
    public GameObject gameOverPanel;
    [Tooltip("The in-game HUD (Crosshair, Health Bar, Ammo counter)")]
    public GameObject hudPanel;

    [Header("Game State")]
    [Tooltip("Is the player actively in combat?")]
    public bool isGameActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Start in the Menu state
        ShowStartMenu();
    }

    /// <summary>
    /// Displays the Start Menu and frees the mouse cursor so you can click Play.
    /// </summary>
    public void ShowStartMenu()
    {
        isGameActive = false;

        // Safeguard: Never disable the root Canvas itself, otherwise all menu panels inside it get disabled!
        if (hudPanel != null && hudPanel.GetComponent<Canvas>() == null)
        {
            hudPanel.SetActive(false);
        }

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Called when the 'PLAY' button is clicked.
    /// Locks cursor into third-person aim mode and reveals the HUD.
    /// </summary>
    public void StartGame()
    {
        isGameActive = true;

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null && hudPanel.GetComponent<Canvas>() == null)
        {
            hudPanel.SetActive(true);
        }

        // Lock cursor for PUBG-style camera aiming
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Called by Health.cs when the Player's health reaches zero.
    /// </summary>
    public void ShowGameOver()
    {
        isGameActive = false;

        if (hudPanel != null && hudPanel.GetComponent<Canvas>() == null)
        {
            hudPanel.SetActive(false);
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Unlock cursor so the player can click 'Play Again'
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Called when the 'PLAY AGAIN' button is clicked.
    /// Cleanly reloads the entire scene from scratch!
    /// </summary>
    public void RestartGame()
    {
        // Reload current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

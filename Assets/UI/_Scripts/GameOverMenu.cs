using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Robust Game Over (win) menu:
/// - Subscribes to EventManager.ON_GAME_OVER
/// - Also checks InventoryManager on Start (in case the event fired earlier)
/// - Allows inspector override for requiredDeliveredToWin (0 = use InventoryManager setting)
/// </summary>
public class GameOverMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;         // root panel (inactive by default)
    public Button nextLevelButton;
    public Button quitButton;

    [Header("Scene")]
    [Tooltip("Either the build index or scene name for Level 2. Use one or the other.")]
    public string nextLevelSceneName = ""; // preferred: scene name
    public int nextLevelBuildIndex = -1;   // fallback: build index if >= 0

    [Header("Game Completion")]
    [Tooltip("If >0, this value overrides InventoryManager.totalItemsNeeded for deciding completion.")]
    public int requiredDeliveredToWin = 0;

    [Header("Events")]
    public UnityEvent onGameOverShown;

    private InventoryManager inventoryManager;
    private bool gameOverShown = false;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelPressed);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);
    }

    void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_GAME_OVER, OnGameOverEvent);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_GAME_OVER, OnGameOverEvent);
    }

    void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();

        // Defensive: if inventoryManager exists we can check current state in case the event fired earlier
        if (inventoryManager != null)
        {
            int delivered = GetDeliveredCountFromInventory();
            int target = GetRequiredToWin();

            if (delivered >= target)
            {
                // show (will also set Time.timeScale etc.)
                ShowGameOver();
            }
        }
    }

    // Event handler for EventManager.ON_GAME_OVER
    private void OnGameOverEvent(object[] data)
    {
        // Extract delivered & total if present (not strictly required because we do a robust check with InventoryManager)
        int delivered = -1, total = -1;
        if (data != null)
        {
            if (data.Length >= 1 && data[0] is int) delivered = (int)data[0];
            if (data.Length >= 2 && data[1] is int) total = (int)data[1];
        }

        // Optional logging
        if (delivered >= 0 && total >= 0)
            Debug.Log($"GameOverMenu: ON_GAME_OVER event received - delivered {delivered}/{total}");
        else
            Debug.Log("GameOverMenu: ON_GAME_OVER event received (payload missing or partial).");

        // Final check: only show once
        if (!gameOverShown)
        {
            ShowGameOver();
        }
    }

    /// <summary>
    /// Show the Game Over panel and pause the game.
    /// Idempotent — Safe to call multiple times.
    /// </summary>
    public void ShowGameOver()
    {
        if (gameOverShown) return;
        gameOverShown = true;

        if (panelRoot != null) panelRoot.SetActive(true);

        // Stop gameplay
        Time.timeScale = 0f;

        // show cursor and unlock for menu navigation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        onGameOverShown?.Invoke();
    }

    public void OnNextLevelPressed()
    {
        // Resume timescale before loading (prevent load-time stuck)
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
        }
        else if (nextLevelBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextLevelBuildIndex);
        }
        else
        {
            Debug.LogWarning("GameOverMenu: nextLevel not configured (sceneName or buildIndex).");
        }
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Public debug helper to force show
    public void ForceShow()
    {
        ShowGameOver();
    }

    // Helper to get delivered count (requires your InventoryManager to expose it publicly)
    private int GetDeliveredCountFromInventory()
    {
        if (inventoryManager == null) return 0;

        // we try reflection-safe access in case the field is private. Best is to expose a getter on InventoryManager.
        // For now attempt to access a public property or field name 'deliveredCount' or 'DeliveredCount' or call method.
        var type = inventoryManager.GetType();
        var field = type.GetField("deliveredCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null)
            return (int)field.GetValue(inventoryManager);

        var prop = type.GetProperty("DeliveredCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null)
            return (int)prop.GetValue(inventoryManager);

        // fallback: assume InventoryManager provides Save/Load only — return 0 and rely on event
        Debug.LogWarning("GameOverMenu: Could not read deliveredCount from InventoryManager. Make sure InventoryManager exposes the delivered count or set requiredDeliveredToWin in this component.");
        return 0;
    }

    private int GetRequiredToWin()
    {
        if (requiredDeliveredToWin > 0) return requiredDeliveredToWin;

        if (inventoryManager != null)
        {
            var type = inventoryManager.GetType();
            var field = type.GetField("totalItemsNeeded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
                return (int)field.GetValue(inventoryManager);

            var prop = type.GetProperty("TotalItemsNeeded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop != null)
                return (int)prop.GetValue(inventoryManager);
        }

        // default fallback
        return 5;
    }
}

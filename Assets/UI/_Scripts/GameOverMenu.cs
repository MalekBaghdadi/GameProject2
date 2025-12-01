using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;


/// <summary>
/// Passive Game Over (win) menu.
/// - Listens for EventManager.ON_GAME_OVER to show the panel.
/// - Exposes two buttons: NextLevel (load nextScene) and Quit.
/// - Stops time and manages cursor while active.
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

    [Header("Events")]
    public UnityEvent onGameOverShown;

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

    private void OnGameOverEvent(object[] data)
    {
        ShowGameOver();
    }

    /// <summary>
    /// Show the Game Over panel and pause the game.
    /// </summary>
    public void ShowGameOver()
    {
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
}

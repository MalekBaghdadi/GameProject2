using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("Default button (for keyboard/controller focus)")]
    public GameObject defaultButton; // assign ResumeButton

    // Reference to the main pause toggling logic (usually a UIManager)
    // Make sure to assign this or have it accessible globally.
    // If your UIManager is a Singleton, you don't need this field.
    // private Game.UI.UIManager UIManagerInstance; 

    // --- NEW: Check for Escape Key Press ---
    void Update()
    {
        // Check if the Escape key was pressed down
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseState();
        }
    }
    // ----------------------------------------

    void OnEnable()
    {
        // Set the default selected UI element for keyboard/controller navigation
        if (defaultButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(defaultButton);
        }
    }

    // A method to handle the actual pause toggling logic
    private void TogglePauseState()
    {

        if (Game.UI.UIManager.Instance != null)
        {
            // The UIManager handles the state change and shows/hides this menu
            Game.UI.UIManager.Instance.TogglePause();
        }
        else
        {
            bool isCurrentlyPaused = gameObject.activeSelf;
            gameObject.SetActive(!isCurrentlyPaused);
            Time.timeScale = !isCurrentlyPaused ? 0f : 1f;
        }
    }

    // Called by ResumeButton OnClick
    public void OnResumePressed()
    {
        TogglePauseState(); 
    }

    // Called by QuitButton OnClick
    public void OnQuitPressed()
    {
        Debug.Log("Quit pressed - application will close (editor will stop play mode).");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
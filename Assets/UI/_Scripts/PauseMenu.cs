using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Passive Pause panel UI. UIManager controls show/hide and pause state.
/// This script simply focuses the default button when enabled and forwards resume/quit clicks.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Default button (for keyboard/controller focus)")]
    public GameObject defaultButton; // assign ResumeButton

    void OnEnable()
    {
        // When the panel becomes active, focus the default button
        if (defaultButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(defaultButton);
        }
    }

    // Called by ResumeButton OnClick
    public void OnResumePressed()
    {
        Debug.Log("PauseMenu.OnResumePressed called");
        if (Game.UI.UIManager.Instance != null)
            Game.UI.UIManager.Instance.TogglePause();
        else
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    // Called by QuitButton OnClick
    public void OnQuitPressed()
    {
        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogError("PauseMenu: PersistenceManager missing! Game not saved.");
        }
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
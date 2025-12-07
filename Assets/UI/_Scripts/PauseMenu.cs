using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Passive Pause panel UI. UIManager controls show/hide and pause state.
/// This script simply focuses the default button when enabled and forwards resume/quit clicks.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Default button (for keyboard/controller focus)")]
    public GameObject defaultButton; // assign ResumeButton
    
    [Header("Audio Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    void OnEnable()
    {
        // When the panel becomes active, focus the default button
        if (defaultButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(defaultButton);
        }
        
        if (AudioSettingsManager.Instance != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = AudioSettingsManager.Instance.BgmVolume;
                bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = AudioSettingsManager.Instance.SfxVolume;
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }
        }
    }
    
    void OnDisable()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
    }

    private void OnBgmSliderChanged(float v)
    {
        if (AudioSettingsManager.Instance != null)
            AudioSettingsManager.Instance.SetBgmVolume(v);
    }

    private void OnSfxSliderChanged(float v)
    {
        if (AudioSettingsManager.Instance != null)
            AudioSettingsManager.Instance.SetSfxVolume(v);
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
    
    public void OnSavePressed()
    {
        if (PersistenceManager.Instance != null)
        {
            Debug.Log("PauseMenu: Manual Save triggered.");
            PersistenceManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogError("PauseMenu: PersistenceManager missing! Cannot save.");
        }
    }


    // Called by QuitButton OnClick
    public void OnQuitPressed()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
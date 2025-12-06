using System;
using UnityEngine;

/// <summary>
/// Persistent singleton that stores BGM and SFX volumes and broadcasts changes.
/// Volumes are 0..1.
/// </summary>
public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    [Range(0f,1f)] [SerializeField] private float bgmVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private bool persistBetweenSessions = true; // optional: store in PlayerPrefs

    public event Action<float> OnBgmVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    private const string PREF_BGM = "Audio_BGM_Volume";
    private const string PREF_SFX = "Audio_SFX_Volume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // load saved values if present
            if (persistBetweenSessions)
            {
                if (PlayerPrefs.HasKey(PREF_BGM)) bgmVolume = PlayerPrefs.GetFloat(PREF_BGM, bgmVolume);
                if (PlayerPrefs.HasKey(PREF_SFX)) sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, sfxVolume);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetBgmVolume(float v, bool save = true)
    {
        bgmVolume = Mathf.Clamp01(v);
        OnBgmVolumeChanged?.Invoke(bgmVolume);
        if (persistBetweenSessions && save)
            PlayerPrefs.SetFloat(PREF_BGM, bgmVolume);
    }

    public void SetSfxVolume(float v, bool save = true)
    {
        sfxVolume = Mathf.Clamp01(v);
        OnSfxVolumeChanged?.Invoke(sfxVolume);
        if (persistBetweenSessions && save)
            PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);
    }
}
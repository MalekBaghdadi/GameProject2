using UnityEngine;

/// <summary>
/// Central SFX player. Subscribes to EventManager.ON_PLAY_SFX and plays clips
/// with the current SFX volume from AudioSettingsManager.
/// Also provides a direct API for other scripts.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D by default for UI / global SFX
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_PLAY_SFX, OnPlaySfxEvent);
        if (AudioSettingsManager.Instance != null)
            AudioSettingsManager.Instance.OnSfxVolumeChanged += OnSfxVolumeChanged;
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_PLAY_SFX, OnPlaySfxEvent);
        if (AudioSettingsManager.Instance != null)
            AudioSettingsManager.Instance.OnSfxVolumeChanged -= OnSfxVolumeChanged;
    }

    private void OnSfxVolumeChanged(float v)
    {
        // we don't set audioSource.volume since PlayOneShot uses provided volume
        // but if you use audioSource.clip + Play(), update volume here:
        audioSource.volume = v;
    }

    private void OnPlaySfxEvent(object[] payload)
    {
        if (payload == null || payload.Length == 0) return;
        if (!(payload[0] is AudioClip clip)) return;

        float vol = 1f;
        if (payload.Length >= 2 && payload[1] is float vf) vol = vf;

        PlayOneShot(clip, vol);
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        float master = (AudioSettingsManager.Instance != null) ? AudioSettingsManager.Instance.SfxVolume : 1f;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume * master));
    }
}

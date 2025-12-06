using System.Collections;
using UnityEngine;

/// <summary>
/// Simple background music manager:
/// - Persistent singleton.
/// - Subscribes to EventManager.ON_PLAY_BGM (payload: AudioClip clip, optional float volume (0..1), optional bool loop)
/// - Provides Play/Stop/Pause/Resume and smooth crossfade.
/// </summary>
[DisallowMultipleComponent]
public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource musicSourcePrefab; // if null we'll create a default AudioSource
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private float defaultFadeTime = 1.0f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_PLAY_BGM, OnPlayBgmEvent);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_PLAY_BGM, OnPlayBgmEvent);
    }

    private void SetupAudioSource()
    {
        if (musicSourcePrefab != null)
        {
            audioSource = Instantiate(musicSourcePrefab, transform);
            audioSource.playOnAwake = false;
        }
        else
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // 2D
        }
        audioSource.volume = masterVolume;
    }

    /// <summary>
    /// Event handler signature expects payload: [0] AudioClip, [1] (optional) float volume, [2] (optional) bool loop
    /// </summary>
    private void OnPlayBgmEvent(object[] payload)
    {
        if (payload == null || payload.Length == 0) return;
        if (!(payload[0] is AudioClip clip)) return;

        float vol = 1f;
        bool loop = true;

        if (payload.Length >= 2)
        {
            if (payload[1] is float vf) vol = vf;
            else if (payload[1] is bool bf) loop = bf;
        }
        if (payload.Length >= 3)
        {
            if (payload[2] is bool bf2) loop = bf2;
            else if (payload[2] is float vf2) vol = vf2;
        }

        Play(clip, loop, vol, defaultFadeTime);
    }

    #region Public API

    public void Play(AudioClip clip, bool loop = true, float volume = 1f, float fadeSeconds = -1f)
    {
        if (clip == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        audioSource.loop = loop;
        audioSource.volume = Mathf.Clamp01(masterVolume * volume);

        if (!audioSource.isPlaying || audioSource.clip != clip)
        {
            fadeSeconds = (fadeSeconds <= 0f) ? defaultFadeTime : fadeSeconds;
            fadeCoroutine = StartCoroutine(FadeToClip(clip, volume, fadeSeconds, loop));
        }
        else
        {
            // same clip already playing - just ensure volume & loop
            audioSource.volume = Mathf.Clamp01(masterVolume * volume);
            audioSource.loop = loop;
        }
    }

    public void Stop(float fadeSeconds = -1f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeSeconds = (fadeSeconds <= 0f) ? defaultFadeTime : fadeSeconds;
        fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeSeconds));
    }

    public void Pause()
    {
        if (audioSource.isPlaying) audioSource.Pause();
    }

    public void Resume()
    {
        if (!audioSource.isPlaying && audioSource.clip != null) audioSource.UnPause();
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        audioSource.volume = Mathf.Clamp01(audioSource.volume * masterVolume);
    }

    #endregion

    #region Fades (unscaled time so it works while timescale==0)

    private IEnumerator FadeToClip(AudioClip newClip, float targetVolume, float duration, bool loop)
    {
        // fade out existing
        float startVol = audioSource.isPlaying ? audioSource.volume : 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = 1f - Mathf.Clamp01(t / duration);
            audioSource.volume = startVol * lerp;
            yield return null;
        }

        // switch clip
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.loop = loop;
        audioSource.volume = 0f;
        audioSource.Play();

        // fade in
        t = 0f;
        float target = Mathf.Clamp01(masterVolume * targetVolume);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(target * Mathf.Clamp01(t / duration));
            yield return null;
        }

        audioSource.volume = target;
        fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVol = audioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(startVol * (1f - (t / duration)));
            yield return null;
        }
        audioSource.Stop();
        audioSource.clip = null;
        fadeCoroutine = null;
    }

    #endregion
}

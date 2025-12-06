using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple pooled Audio Manager that listens to EventManager.ON_PLAY_SFX
/// and plays audio clips either at a world position (3D) or globally (2D).
///
/// Usage (via EventManager):
///   // Play globally / UI-style:
///   EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, clip);
///
///   // Play at a world position (spatialized):
///   EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, clip, position);
///
///   // Play with custom volume:
///   EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, clip, position, 0.8f);
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    [Header("Pool")]
    [Tooltip("Initial number of pooled AudioSources")]
    [SerializeField] private int initialPoolSize = 8;

    [Header("Default settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("Spatial blend for spawned sources when positional (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)] public float spatialBlendFor3D = 1f;
    [Tooltip("Max distance for 3D sounds (rolloff).")]
    public float maxDistance3D = 30f;

    [Header("Rate limiting")]
    [Tooltip("Minimum seconds between repeated plays of the same clip")]
    public float minRepeatInterval = 0.1f;

    // Pool / bookkeeping
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private Transform poolRoot;

    // track last play time per clip to avoid spam
    private readonly Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();

    private void Awake()
    {
        poolRoot = new GameObject("AudioManager_Pool").transform;
        poolRoot.SetParent(transform, false);

        for (int i = 0; i < initialPoolSize; i++)
            pool.Add(CreatePooledSource());
    }

    private void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_PLAY_SFX, OnPlaySfxEvent);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_PLAY_SFX, OnPlaySfxEvent);
    }

    private void OnDestroy()
    {
        // cleanup pool objects
        foreach (var src in pool)
        {
            if (src != null)
                Destroy(src.gameObject);
        }
        pool.Clear();
    }

    /// <summary>
    /// Event callback signature; expects payload[0] == AudioClip,
    /// optionally payload[1] == Vector3 (position), payload[2] == float (volume)
    /// </summary>
    private void OnPlaySfxEvent(object[] payload)
    {
        if (payload == null || payload.Length == 0) return;
        if (!(payload[0] is AudioClip clip)) return;

        Vector3? pos = null;
        float volume = 1f;

        if (payload.Length >= 2)
        {
            // If a Vector3 provided as second parameter
            if (payload[1] is Vector3 v) pos = v;
            // If second param is float (volume) and no position provided
            else if (payload[1] is float vf) volume = vf;
        }

        if (payload.Length >= 3)
        {
            if (payload[2] is float vf2) volume = vf2;
            else if (payload[2] is Vector3 v2) pos = v2; // flexible ordering
        }

        Play(clip, pos, volume);
    }

    /// <summary>
    /// Public API to play a clip.
    /// </summary>
    public void Play(AudioClip clip, Vector3? position = null, float volume = 1f)
    {
        if (clip == null) return;

        // rate-limit same clip
        if (lastPlayTime.TryGetValue(clip, out float last) && Time.unscaledTime - last < minRepeatInterval)
            return;

        lastPlayTime[clip] = Time.unscaledTime;

        AudioSource src = GetFreeSource();

        src.clip = clip;
        src.volume = Mathf.Clamp01(masterVolume * volume);
        src.spatialBlend = position.HasValue ? spatialBlendFor3D : 0f;
        src.maxDistance = maxDistance3D;
        src.transform.position = position ?? Vector3.zero;
        src.transform.SetParent(position.HasValue ? null : poolRoot, true); // if 3D leave in world, else keep under pool root
        src.gameObject.SetActive(true);
        src.Play();

        // schedule it to be returned to pool when finished
        StartCoroutine(ReleaseWhenFinished(src));
    }

    /// <summary>
    /// Coroutine that waits until source finishes and then resets it for reuse.
    /// </summary>
    private System.Collections.IEnumerator ReleaseWhenFinished(AudioSource src)
    {
        if (src.clip == null)
            yield break;

        // Wait while playing (use unscaledTime so it still works with timeScale=0 for UI sounds)
        float length = src.clip.length;
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < length)
            yield return null;

        // reset
        src.Stop();
        src.clip = null;
        src.gameObject.SetActive(false);
        src.transform.SetParent(poolRoot, true);
    }

    /// <summary>
    /// Find a free (inactive) source from pool or create a new one.
    /// </summary>
    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            var s = pool[i];
            if (s == null)
            {
                pool[i] = CreatePooledSource();
                return pool[i];
            }

            if (!s.gameObject.activeInHierarchy)
                return s;
        }

        // none free -> expand pool
        var newSource = CreatePooledSource();
        pool.Add(newSource);
        return newSource;
    }

    /// <summary>
    /// Create a fresh AudioSource GameObject configured for pooling.
    /// </summary>
    private AudioSource CreatePooledSource()
    {
        GameObject go = new GameObject($"PooledAudioSource_{pool.Count}");
        go.transform.SetParent(poolRoot, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = maxDistance3D;
        src.gameObject.SetActive(false);
        return src;
    }
}

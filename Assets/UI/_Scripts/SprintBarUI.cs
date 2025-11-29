using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Subscribes to EventManager.ON_STAMINA_CHANGED and updates a UI Slider to reflect current stamina.
/// Expects the event payload to contain:
///   - data[0]: (float) currentStamina (required)
///   - data[1]: (float) maxStamina (optional). If absent, this component uses serialized maxStaminaFallback.
/// 
/// The script uses Time.deltaTime smoothing (not unscaled) to animate the slider.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SprintBarUI : MonoBehaviour
{
    [Header("Stamina mapping")]
    [Tooltip("Fallback max stamina (used if the event does not provide max).")]
    public float maxStaminaFallback = 100f;

    [Header("Smoothing")]
    [Tooltip("Higher values make the bar follow stamina more tightly.")]
    public float lerpSpeed = 8f;

    // Slider component reference (this GO should contain a Slider)
    Slider slider;

    // Internal targets
    float targetNormalized = 1f;
    float displayed = 1f;

    // Keep latest known max for normalization
    float currentMaxStamina;

    void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError("SprintBarUI requires a Slider component on the same GameObject.");
            enabled = false;
            return;
        }

        // Setup slider to normalized 0..1 if desired
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        // initialize
        currentMaxStamina = Mathf.Max(0.0001f, maxStaminaFallback);
        slider.value = 1f;
        displayed = 1f;
        targetNormalized = 1f;
    }

    void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_STAMINA_CHANGED, OnStaminaChanged);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_STAMINA_CHANGED, OnStaminaChanged);
    }

    void Update()
    {
        // Smooth interpolation (use Time.deltaTime as requested)
        if (!Mathf.Approximately(displayed, targetNormalized))
        {
            displayed = Mathf.Lerp(displayed, targetNormalized, Time.deltaTime * lerpSpeed);
            slider.value = displayed;
        }
    }

    /// <summary>
    /// Event callback. payload is object[] data.
    /// Expected forms:
    ///  - [currentStamina (float)]
    ///  - [currentStamina (float), maxStamina (float)]
    /// The method is robust to int -> float and boxed numeric types.
    /// </summary>
    void OnStaminaChanged(object[] data)
    {
        if (data == null || data.Length == 0) return;

        // parse current stamina
        float current = ParseFloatSafe(data[0], 0f);

        // optional max stamina from event
        if (data.Length > 1)
        {
            float parsedMax = ParseFloatSafe(data[1], currentMaxStamina);
            if (parsedMax > 0.0001f) currentMaxStamina = parsedMax;
        }

        // if event didn't provide max, use fallback or last-known max
        if (currentMaxStamina <= 0.0001f) currentMaxStamina = Mathf.Max(0.0001f, maxStaminaFallback);

        // compute normalized target in 0..1
        targetNormalized = Mathf.Clamp01(current / currentMaxStamina);
    }

    static float ParseFloatSafe(object o, float fallback)
    {
        if (o == null) return fallback;

        if (o is float f) return f;
        if (o is double d) return (float)d;
        if (o is int i) return (float)i;
        if (o is long l) return (float)l;

        // try Convert
        try
        {
            return Convert.ToSingle(o);
        }
        catch
        {
            return fallback;
        }
    }

    #region Public API (optional helpers)
    /// <summary>Immediate set (no smoothing)</summary>
    public void SetStaminaInstant(float current, float max = -1f)
    {
        if (max > 0f) currentMaxStamina = max;
        targetNormalized = Mathf.Clamp01(current / Mathf.Max(currentMaxStamina, 0.0001f));
        displayed = targetNormalized;
        slider.value = targetNormalized;
    }

    /// <summary>Programmatically set max stamina fallback</summary>
    public void SetMaxStamina(float max)
    {
        currentMaxStamina = Mathf.Max(0.0001f, max);
    }
    #endregion
}

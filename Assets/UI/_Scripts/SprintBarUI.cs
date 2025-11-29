using UnityEngine;
using UnityEngine.UI;

public class SprintBarUI : MonoBehaviour
{
    [Header("Components")]
    public Slider slider;
    public float smoothSpeed = 8f; // higher = snappier

    float target = 1f;

    void Reset() { slider = GetComponent<Slider>(); }

    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
    }

    void Update()
    {
        // Smoothly lerp toward target to look polished.
        if (!Mathf.Approximately(slider.value, target))
            slider.value = Mathf.Lerp(slider.value, target, Time.unscaledDeltaTime * smoothSpeed);
    }

    // API for gameplay code
    public void SetSprintNormalized(float normalized) => target = Mathf.Clamp01(normalized);
    public void SetSprintInstant(float normalized) { target = Mathf.Clamp01(normalized); slider.value = target; }
}


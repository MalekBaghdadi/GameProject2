using UnityEngine;
using System.Collections;

public class HurtEffect : MonoBehaviour
{
    public CanvasGroup hurtCanvasGroup;
    public float maxAlpha = 0.8f;       // How dark the red gets (0 to 1)
    public float displayDuration = 4f; // How long it stays at max alpha
    public float fadeSpeed = 0.2f;      // Higher = fades faster

    void Start()
    {
        if (hurtCanvasGroup != null) hurtCanvasGroup.alpha = 0;
    }

    public void TriggerHurt()
    {
        // Stop any current fade so they don't fight each other
        StopAllCoroutines();
        StartCoroutine(FadeHurt());
    }

    IEnumerator FadeHurt()
    {
        // 1. Instant Flash
        hurtCanvasGroup.alpha = maxAlpha;

        // 2. Wait/Hold (This makes it feel like a real hit)
        yield return new WaitForSeconds(displayDuration);

        // 3. Smooth Fade
        while (hurtCanvasGroup.alpha > 0)
        {
            // We use -= here to gradually reduce alpha over time
            hurtCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        hurtCanvasGroup.alpha = 0;
    }
}
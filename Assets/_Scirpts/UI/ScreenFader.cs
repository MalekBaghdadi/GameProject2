using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;

    public IEnumerator FadeIn(float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / duration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
    }
}
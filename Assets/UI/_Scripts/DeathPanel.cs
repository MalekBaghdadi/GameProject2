using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DeathPanel : MonoBehaviour
{
    [Header("UI references (GameObjects)")]
    public GameObject panelRoot;        // DeathPanel root (full-screen)
    public GameObject youDiedGO;        // "YOU DIED" GameObject
    public GameObject respawningGO;     // "Respawning in" GameObject
    public GameObject countdownGO;      // GameObject that contains the Text component

    [Header("Countdown Settings")]
    public float respawnDuration = 5f;  // adjustable in Inspector

    [Header("Optional Callbacks")]
    public UnityEvent onRespawn;

    private Text countdownText;
    private Coroutine countdownRoutine;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (countdownGO != null)
            countdownText = countdownGO.GetComponentInChildren<Text>();

        if (countdownText == null)
            Debug.LogError("DeathPanel: countdownGO has no Text component inside it!");
    }

    /// <summary>
    /// Call this when the player dies.
    /// </summary>
    public void ShowDeath(float overrideDuration = -1f)
    {
        float duration = (overrideDuration > 0f) ? overrideDuration : respawnDuration;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (youDiedGO != null) youDiedGO.SetActive(true);
        if (respawningGO != null) respawningGO.SetActive(true);
        if (countdownGO != null) countdownGO.SetActive(true);

        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(CountdownRoutine(duration));
    }

    IEnumerator CountdownRoutine(float duration)
    {
        float remaining = duration;

        UpdateCountdown(remaining);

        while (remaining > 0f)
        {
            yield return null;
            remaining -= Time.unscaledDeltaTime;   // runs even if timeScale = 0
            UpdateCountdown(remaining);
        }

        // done
        countdownRoutine = null;

        // hide UI
        if (panelRoot != null) panelRoot.SetActive(false);

        // respawn callback
        onRespawn?.Invoke();
    }

    private void UpdateCountdown(float remaining)
    {
        if (countdownText != null)
            countdownText.text = Mathf.CeilToInt(Mathf.Max(remaining, 0f)).ToString();
    }
}

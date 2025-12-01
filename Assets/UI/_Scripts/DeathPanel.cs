using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


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
    private TMP_Text countdownTMP;
    private Coroutine countdownRoutine;
    
    void Awake()
    {
        // 1. Force find the text component robustly.
        if (countdownGO != null)
        {
            // Searches the countdownGO and all children, regardless of active state.
            countdownText = countdownGO.GetComponentInChildren<Text>(true);
            if (countdownText == null)
                countdownTMP = countdownGO.GetComponentInChildren<TMP_Text>(true);
        }

        if (countdownText == null && countdownTMP == null)
            Debug.LogError("DeathPanel: CRITICAL ERROR! Could NOT find UI.Text or TMP_Text component inside 'countdownGO'. The countdown will not update! Please check your GameObject assignments.");

        // 2. Hide the main panel root at start
        if (panelRoot != null) panelRoot.SetActive(false);
    }


    /// <summary>
    /// Call this when the player dies (called by DeathScreenActivator).
    /// </summary>
    public void ShowDeath(float overrideDuration = -1f)
    {
        float duration = (overrideDuration > 0f) ? overrideDuration : respawnDuration;

        // Activate all relevant UI elements
        if (panelRoot != null) panelRoot.SetActive(true);
        if (youDiedGO != null) youDiedGO.SetActive(true);
        if (respawningGO != null) respawningGO.SetActive(true);
        if (countdownGO != null) countdownGO.SetActive(true);

        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(CountdownRoutine(duration));
    }

    IEnumerator CountdownRoutine(float duration)
    {
        // Use realtime so this works even when Time.timeScale = 0
        float endTime = Time.realtimeSinceStartup + Mathf.Max(0f, duration);

        // Force immediate display for the starting number
        UpdateCountdown(endTime - Time.realtimeSinceStartup);

        // While there's still time left, update once per frame so the number can change
        while (Time.realtimeSinceStartup < endTime)
        {
            // Calculate remaining seconds (ceiled)
            float remaining = endTime - Time.realtimeSinceStartup;
            UpdateCountdown(remaining);

            // Wait a short time to avoid tight-loop CPU usage but be responsive.
            // WaitForSecondsRealtime(0.1f) is fine; it won't be affected by timescale.
            yield return new WaitForSecondsRealtime(0.1f);
        }

        // Ensure final display is 0
        UpdateCountdown(0f);

        // Done
        countdownRoutine = null;

        // hide UI
        if (panelRoot != null) panelRoot.SetActive(false);

        // respawn callback
        onRespawn?.Invoke();
    }


    private void UpdateCountdown(float remaining)
    {
        int displayNum = Mathf.CeilToInt(Mathf.Max(remaining, 0f));
        if (countdownText != null)
        {
            countdownText.text = displayNum.ToString();
            Canvas.ForceUpdateCanvases();
        }
        else if (countdownTMP != null)
        {
            countdownTMP.text = displayNum.ToString();
            // TMP updates don't require ForceUpdateCanvases
        }
    }
}
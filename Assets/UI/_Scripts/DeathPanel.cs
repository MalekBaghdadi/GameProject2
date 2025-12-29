using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class DeathPanel : MonoBehaviour
{
    [Header("UI references (GameObjects)")]
    public GameObject panelRoot;        
    public GameObject youDiedGO;        
    public GameObject respawningGO;     
    public GameObject countdownGO;      

    [Header("Countdown Settings")]
    public float respawnDuration = 5f;  

    [Header("Optional Callbacks")]
    public UnityEvent onRespawn;

    private Text countdownText;
    private TMP_Text countdownTMP;
    private Coroutine countdownRoutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        // Try to get components in the countdown GO
        if (countdownGO != null)
        {
            countdownText = countdownGO.GetComponentInChildren<Text>(true);
            countdownTMP = countdownGO.GetComponentInChildren<TMP_Text>(true);
        }

        // Setup CanvasGroup for blocking/unblocking clicks
        if (panelRoot != null)
        {
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            
            // Start in a clean, hidden state
            ForceHide();
        }
    }

    public void ShowDeath(float overrideDuration = -1f)
    {
        float duration = (overrideDuration > 0f) ? overrideDuration : respawnDuration;

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        panelRoot.SetActive(true);

        // Enable blocking
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        youDiedGO?.SetActive(true);
        respawningGO?.SetActive(true);
        countdownGO?.SetActive(true);

        countdownRoutine = StartCoroutine(CountdownRoutine(duration));
    }

    IEnumerator CountdownRoutine(float duration)
    {
        float remaining = duration;

        while (remaining > 0)
        {
            UpdateCountdown(remaining);
            // Use yield return null or a small wait to stay responsive
            yield return new WaitForSecondsRealtime(0.1f);
            remaining -= 0.1f;
        }

        UpdateCountdown(0f);
        
        // --- CRITICAL CLEANUP ---
        ForceHide();
        
        countdownRoutine = null;
        onRespawn?.Invoke();
    }

    // Created a dedicated method to ensure the UI is 100% "Gone"
    public void ForceHide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void UpdateCountdown(float remaining)
    {
        int displayNum = Mathf.CeilToInt(Mathf.Max(remaining, 0f));

        if (countdownText != null)
            countdownText.text = displayNum.ToString();
        
        if (countdownTMP != null)
            countdownTMP.text = displayNum.ToString();
    }

    // If the object is disabled externally, make sure raycasts are off
    void OnDisable()
    {
        ForceHide();
    }
}
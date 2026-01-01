using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CabinController : MonoBehaviour
{
    [Header("Construction Settings")]
    [SerializeField] private GameObject[] constructionStages;

    [Header("Audio")]
    [SerializeField] private AudioClip cabinUpgradeSound;

    [Header("UI")]
    [SerializeField] private GameOverMenu levelCompletedMenu;
    [SerializeField] private GameObject hudRoot;

    [Header("Player Control (Disable on Win)")]
    [SerializeField] private MonoBehaviour playerMovement;   // e.g. PlayerController, FirstPersonController
    [SerializeField] private MonoBehaviour playerLook;       // e.g. MouseLook, CameraLook, LookController

    private int currentStageIndex = 0;
    private bool isPlayerInRange = false;
    private bool levelCompleted = false;

    private const string PLAYER_TAG = "Player";
    private InventoryManager inventoryManager;

    private void Awake()
    {
        if (levelCompletedMenu == null)
            Debug.LogError("CabinController: LevelCompletedMenu NOT assigned");

        if (hudRoot == null)
            Debug.LogWarning("CabinController: HUD Root not assigned (HUD will not be hidden)");

        if (playerMovement == null)
            Debug.LogWarning("CabinController: Player Movement script not assigned");

        if (playerLook == null)
            Debug.LogWarning("CabinController: Player Look script not assigned");
    }

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
            Debug.LogError("CabinController: InventoryManager NOT FOUND");

        foreach (GameObject stage in constructionStages)
        {
            if (stage != null)
                stage.SetActive(false);
        }
    }

    private void Update()
    {
        if (levelCompleted)
            return;

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (levelCompleted)
            return;

        if (other.CompareTag(PLAYER_TAG))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            isPlayerInRange = false;
        }
    }

    public void Interact()
    {
        if (levelCompleted || inventoryManager == null)
            return;

        if (currentStageIndex >= constructionStages.Length)
            return;

        if (inventoryManager.currentItem == null)
        {
            inventoryManager.DepositItem();
            return;
        }

        if (inventoryManager.DepositItem())
        {
            ActivateNextStage();
        }
    }

    private void ActivateNextStage()
    {
        if (currentStageIndex >= constructionStages.Length)
            return;

        GameObject stage = constructionStages[currentStageIndex];
        if (stage != null)
            stage.SetActive(true);

        if (cabinUpgradeSound != null)
            EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, cabinUpgradeSound);

        currentStageIndex++;

        if (currentStageIndex >= constructionStages.Length)
        {
            FORCE_COMPLETE_LEVEL();
        }
    }

    // 🚨 ABSOLUTE HARD LOCK WIN STATE
    private void FORCE_COMPLETE_LEVEL()
    {
        if (levelCompleted)
            return;

        levelCompleted = true;

        Debug.Log("LEVEL COMPLETED — FULL INPUT & HUD LOCK");

        // Stop interaction
        isPlayerInRange = false;
        enabled = false;

        // Disable player control
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerLook != null)
            playerLook.enabled = false;

        // Hide HUD
        if (hudRoot != null)
            hudRoot.SetActive(false);

        // Pause gameplay systems
        Time.timeScale = 0f;

        // Cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show menu
        if (levelCompletedMenu != null)
        {
            levelCompletedMenu.gameObject.SetActive(true);
            levelCompletedMenu.ShowGameOver();
        }
        else
        {
            Debug.LogError("CabinController: LevelCompletedMenu missing");
        }
    }
}

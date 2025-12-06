using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class PersistenceManager : MonoBehaviour
{
    public static PersistenceManager Instance { get; private set; }

    [Header("File Settings")]
    [SerializeField] private string fileName = "data.game";
    
    [Header("Item Database")]
    [Tooltip("Drag ALL possible ItemDataSOs here so we can find them by ID on load.")]
    public List<ItemDataSO> allItemsDatabase;

    private GameData gameData;
    private List<ISaveable> saveableObjects;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Setup the list of objects that need saving
        saveableObjects = FindAllSaveableObjects();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
        // Reset Logic here if needed
    }

    public void SaveGame()
    {
        // 1. If no data exists, create new
        if (this.gameData == null) this.gameData = new GameData();

        // 2. Refresh list of objects (in case things were destroyed/spawned)
        saveableObjects = FindAllSaveableObjects();

        // 3. Ask every object to write its data
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.SaveData(ref gameData);
        }

        // 4. Write to file
        string dataPath = Path.Combine(Application.persistentDataPath, fileName);
        string jsonData = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(dataPath, jsonData);

        Debug.Log("Game Saved to " + dataPath);
    }

    public void LoadGame()
    {
        // 1. Read file
        string dataPath = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(dataPath))
        {
            Debug.Log("No save file found. Starting New Game.");
            NewGame();
            return;
        }

        // 2. Deserialize
        string jsonData = File.ReadAllText(dataPath);
        this.gameData = JsonUtility.FromJson<GameData>(jsonData);

        // 3. Refresh objects
        saveableObjects = FindAllSaveableObjects();

        // 4. Push data to objects
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.LoadData(gameData);
        }
        
        Debug.Log("Game Loaded.");
    }

    private List<ISaveable> FindAllSaveableObjects()
    {
        // Finds all MonoBehaviours that implement ISaveable (including disabled ones if needed)
        IEnumerable<ISaveable> saveables = FindObjectsOfType<MonoBehaviour>()
            .OfType<ISaveable>();
        return new List<ISaveable>(saveables);
    }

    // Helper to find an Item SO by string ID
    public ItemDataSO GetItemByID(string id)
    {
        return allItemsDatabase.FirstOrDefault(i => i.itemID == id);
    }
    
    public void InitializeAndLoad()
    {
        // The existing Start() logic will happen here
        if (saveableObjects == null) // Check if Start() has run yet
        {
            saveableObjects = FindAllSaveableObjects();
        }
    
        // Now perform the load immediately
        LoadGame();
    }
}